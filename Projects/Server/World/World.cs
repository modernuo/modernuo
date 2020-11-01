/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: World.cs                                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Server.Guilds;
using Server.Network;

namespace Server
{
    public enum WorldState
    {
        Initial,
        Loading,
        Running,
        Saving,
        WritingSave
    }

    public static class World
    {
        private static readonly ManualResetEvent m_DiskWriteHandle = new ManualResetEvent(true);

        private static Dictionary<Serial, IEntity> _pendingAdd;
        private static Dictionary<Serial, IEntity> _pendingDelete;
        private static ConcurrentQueue<Item> _decayQueue = new ConcurrentQueue<Item>();

        public const uint ItemOffset = 0x40000000;
        public const uint MaxItemSerial = 0x7FFFFFFF;
        private const uint _maxItems = int.MaxValue - ItemOffset;
        private const uint _maxMobiles = ItemOffset;

        private static Serial _lastMobile = Serial.Zero;
        private static Serial _lastItem = ItemOffset;
        private static Serial _lastGuild = Serial.Zero;

        public static Serial NewMobile
        {
            get
            {
                uint last = _lastMobile;

                for (int i = 0; i < _maxMobiles; i++)
                {
                    last++;

                    if (last >= _lastMobile)
                    {
                        last = 0;
                    }

                    if (FindMobile(last) == null)
                    {
                        _lastMobile = last;
                        return last;
                    }
                }

                throw new Exception("No serials left to allocate for mobiles");
            }
        }

        public static Serial NewItem
        {
            get
            {
                uint last = _lastItem;

                for (int i = 0; i < _maxItems; i++)
                {
                    last++;

                    if (last - ItemOffset >= _maxItems)
                    {
                        last = ItemOffset;
                    }

                    if (FindItem(last) == null)
                    {
                        _lastItem = last;
                        return last;
                    }
                }

                throw new Exception("No serials left to allocate for items");
            }
        }

        public static Serial NewGuild
        {
            get
            {
                while (FindGuild(_lastGuild += 1) != null)
                {
                }

                return _lastGuild;
            }
        }


        internal static int _Saves;
        internal static List<Type> ItemTypes { get; } = new List<Type>();
        internal static List<Type> MobileTypes { get; } = new List<Type>();
        internal static List<Type> GuildTypes { get; } = new List<Type>();

        public static WorldState WorldState { get; private set; }
        public static bool Saving => WorldState == WorldState.Saving;
        public static bool Running => WorldState == WorldState.Running;
        public static bool Loading => WorldState == WorldState.Loading;

        public static Dictionary<Serial, Mobile> Mobiles { get; private set; }
        public static Dictionary<Serial, Item> Items { get; private set; }
        public static Dictionary<Serial, BaseGuild> Guilds { get; private set; }

        public static void WaitForWriteCompletion()
        {
            m_DiskWriteHandle.WaitOne();
        }

        private static void EnqueueForDecay(Item item)
        {
            if (WorldState != WorldState.Saving)
            {
                Console.WriteLine("Attempting to queue {0} for decay but the world is not saving", item);
                return;
            }

            _decayQueue.Enqueue(item);
        }

        public static void Broadcast(int hue, bool ascii, string text)
        {
            Packet p;

            if (ascii)
            {
                p = new AsciiMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, "System", text);
            }
            else
            {
                p = new UnicodeMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, "ENU", "System", text);
            }

            var list = TcpServer.Instances;

            p.Acquire();

            for (var i = 0; i < list.Count; ++i)
            {
                if (list[i].Mobile != null)
                {
                    list[i].Send(p);
                }
            }

            p.Release();
        }

        public static void Broadcast(int hue, bool ascii, string format, params object[] args)
        {
            Broadcast(hue, ascii, string.Format(format, args));
        }

        private static List<Tuple<ConstructorInfo, string>> ReadTypes<I>(BinaryReader tdbReader)
        {
            var constructorTypes = new[] { typeof(I) };

            var count = tdbReader.ReadInt32();

            var types = new List<Tuple<ConstructorInfo, string>>(count);

            for (var i = 0; i < count; ++i)
            {
                var typeName = tdbReader.ReadString();

                var t = AssemblyHandler.FindFirstTypeForName(typeName);

                if (t?.IsAbstract != false)
                {
                    Console.WriteLine("failed");

                    Console.WriteLine(
                        "Error: Type '{0}' was {1}. Delete all of those types? (y/n)",
                        typeName,
                        t?.IsAbstract == true ? "marked abstract" : "not found"
                    );

                    if (Console.ReadKey(true).Key == ConsoleKey.Y)
                    {
                        types.Add(null);
                        Console.Write("World: Loading...");
                        continue;
                    }

                    Console.WriteLine("Types will not be deleted. An exception will be thrown.");

                    throw new Exception($"Bad type '{typeName}'");
                }

                var ctor = t.GetConstructor(constructorTypes);

                if (ctor != null)
                {
                    types.Add(new Tuple<ConstructorInfo, string>(ctor, typeName));
                }
                else
                {
                    throw new Exception($"Type '{t}' does not have a serialization constructor");
                }
            }

            return types;
        }

        private static Dictionary<I, T> LoadIndex<I, T>(IIndexInfo<I> indexInfo, out List<EntityIndex<T>> entities) where T : class, ISerializable
        {
            var map = new Dictionary<I, T>();
            object[] ctorArgs = new object[1];

            var indexType = indexInfo.TypeName;

            string indexPath = Path.Combine("Saves", indexType, $"{indexType}.idx");
            string typesPath = Path.Combine("Saves", indexType, $"{indexType}.tdb");

            entities = new List<EntityIndex<T>>();

            if (!File.Exists(indexPath) || !File.Exists(typesPath))
            {
                return map;
            }

            using FileStream idx = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryReader idxReader = new BinaryReader(idx);

            using (FileStream tdb = new FileStream(typesPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BinaryReader tdbReader = new BinaryReader(tdb);

                List<Tuple<ConstructorInfo, string>> types = ReadTypes<I>(tdbReader);

                var count = idxReader.ReadInt32();

                for (int i = 0; i < count; ++i)
                {
                    var typeID = idxReader.ReadInt32();
                    var number = idxReader.ReadUInt32();
                    var pos = idxReader.ReadInt64();
                    var length = idxReader.ReadInt32();

                    Tuple<ConstructorInfo, string> objs = types[typeID];

                    if (objs == null)
                    {
                        continue;
                    }

                    T t;
                    ConstructorInfo ctor = objs.Item1;
                    I indexer = indexInfo.CreateIndex(number);

                    ctorArgs[0] = indexer;
                    t = ctor.Invoke(ctorArgs) as T;

                    if (t != null)
                    {
                        entities.Add(new EntityIndex<T>(t, typeID, pos, length));
                        map[indexer] = t;
                    }
                }

                tdbReader.Close();
            }

            idxReader.Close();

            return map;
        }

        private static void LoadData<I, T>(IIndexInfo<I> indexInfo, List<EntityIndex<T>> entities) where T : ISerializable
        {
            var indexType = indexInfo.TypeName;

            string dataPath = Path.Combine("Saves", indexType, $"{indexType}.bin");

            if (!File.Exists(dataPath))
            {
                return;
            }

            using FileStream bin = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryFileReader reader = new BinaryFileReader(new BinaryReader(bin));

            foreach (var entry in entities)
            {
                T t = entry.Entity;

                if (t == null)
                {
                    continue;
                }

                reader.Seek(entry.Position, SeekOrigin.Begin);

                t.Deserialize(reader);

                if (reader.Position != entry.Position + entry.Length)
                {
                    Console.WriteLine($"***** Bad deserialize on {t.GetType()} *****");
                    Console.WriteLine(
                        $"Serialized object was {entry.Length} bytes, but {reader.Position - entry.Position} bytes deserialized"
                    );

                    Console.WriteLine("Delete the object and continue? (y/n)");

                    if (Console.ReadKey(true).Key != ConsoleKey.Y)
                    {
                        throw new Exception("Deserialization failed.");
                    }
                    t.Delete();
                }

                reader.Seek(entry.Position, SeekOrigin.Begin);

                t.SaveBuffer = new BufferWriter(new byte[entry.Length], true);
                reader.Read(t.SaveBuffer.Data);
            }

            reader.Close();
        }

        public static void Load()
        {
            if (WorldState != WorldState.Initial)
            {
                return;
            }

            WorldState = WorldState.Loading;

            Console.Write("World: Loading...");

            var watch = Stopwatch.StartNew();

            _pendingAdd = new Dictionary<Serial, IEntity>();
            _pendingDelete = new Dictionary<Serial, IEntity>();

            List<EntityIndex<Item>> items;
            List<EntityIndex<Mobile>> mobiles;
            List<EntityIndex<BaseGuild>> guilds;

            IIndexInfo<Serial> itemIndexInfo = new EntityTypeIndex("Items");
            IIndexInfo<Serial> mobileIndexInfo = new EntityTypeIndex("Mobiles");
            IIndexInfo<Serial> guildIndexInfo = new EntityTypeIndex("Guilds");

            Items = LoadIndex(itemIndexInfo, out items);
            Mobiles = LoadIndex(mobileIndexInfo, out mobiles);
            Guilds = LoadIndex(guildIndexInfo, out guilds);

            LoadData(itemIndexInfo, items);
            LoadData(mobileIndexInfo, mobiles);
            LoadData(guildIndexInfo, guilds);

            EventSink.InvokeWorldLoad();

            WorldState = WorldState.Running;

            ProcessSafetyQueues();

            foreach (var item in Items.Values)
            {
                if (item.Parent == null)
                {
                    item.UpdateTotals();
                }

                item.ClearProperties();
            }

            foreach (var m in Mobiles.Values)
            {
                m.UpdateRegion(); // Is this really needed?
                m.UpdateTotals();

                m.ClearProperties();
            }

            watch.Stop();

            Console.WriteLine(
                "done ({1} items, {2} mobiles) ({0:F2} seconds)",
                watch.Elapsed.TotalSeconds,
                Items.Count,
                Mobiles.Count
            );
        }

        private static void ProcessSafetyQueues()
        {
            foreach (var entity in _pendingAdd.Values)
            {
                AddEntity(entity);
            }

            foreach (var entity in _pendingDelete.Values)
            {
                if (_pendingAdd.ContainsKey(entity.Serial))
                {
                    Console.Error.WriteLine("Entity {0} was both pending both deletion and addition after save", entity);
                }

                RemoveEntity(entity);
            }
        }

        private static void AppendSafetyLog(string action, ISerializable entity)
        {
            var message =
                $"Warning: Attempted to {action} {entity} during world save.{Environment.NewLine}This action could cause inconsistent state.{Environment.NewLine}It is strongly advised that the offending scripts be corrected.";

            Console.WriteLine(message);

            try
            {
                using var op = new StreamWriter("world-save-errors.log", true);
                op.WriteLine("{0}\t{1}", DateTime.UtcNow, message);
                op.WriteLine(new StackTrace(2).ToString());
                op.WriteLine();
            }
            catch
            {
                // ignored
            }
        }

        private static void FinishWorldSave()
        {
            WorldState = WorldState.Running;

            ProcessDecay();
            ProcessSafetyQueues();
        }

        public static void WriteFiles(object state)
        {
            IIndexInfo<Serial> itemIndexInfo = new EntityTypeIndex("Items");
            IIndexInfo<Serial> mobileIndexInfo = new EntityTypeIndex("Mobiles");
            IIndexInfo<Serial> guildIndexInfo = new EntityTypeIndex("Guilds");

            WriteEntities(itemIndexInfo, Items, ItemTypes);
            WriteEntities(mobileIndexInfo, Mobiles, MobileTypes);
            WriteEntities(guildIndexInfo, Guilds, GuildTypes);

            if (m_DiskWriteHandle.Set())
            {
                Console.WriteLine("Closing Save Files.");
            }

            Timer.DelayCall(FinishWorldSave);
        }

        private static void WriteEntities<I, T>(IIndexInfo<I> indexInfo, Dictionary<I, T> entities, List<Type> types) where T : class, ISerializable
        {
            var typeName = indexInfo.TypeName;

            var path = Path.Combine("Saves", typeName);

            AssemblyHandler.EnsureDirectory(path);

            string idxPath = Path.Combine(path, $"{typeName}.idx");
            string tdbPath = Path.Combine(path, $"{typeName}.tdb");
            string binPath = Path.Combine(path, $"{typeName}.bin");

            var idx = new BinaryFileWriter(idxPath, false);
            var tdb = new BinaryFileWriter(tdbPath, false);
            var bin = new BinaryFileWriter(binPath, true);

            idx.Write(entities.Count);
            foreach (var mob in entities.Values)
            {
                long start = bin.Position;

                idx.Write(mob.TypeRef);
                idx.Write(mob.Serial);
                idx.Write(start);

                mob.SerializeTo(bin);

                idx.Write((int)(bin.Position - start));
            }

            tdb.Write(types.Count);
            for (int i = 0; i < types.Count; ++i)
            {
                tdb.Write(types[i].FullName);
            }

            idx.Close();
            tdb.Close();
            bin.Close();
        }

        private static void SaveEntities<T>(IEnumerable<T> list, DateTime serializeStart) where T : class, ISerializable
        {
            Parallel.ForEach(list, t => {
                if (t is Item item && item.CanDecay() && item.LastMoved + item.DecayTime <= serializeStart)
                {
                    EnqueueForDecay(item);
                }

                t.Serialize();
            });
        }

        private static void ProcessDecay()
        {
            Item item;

            while (_decayQueue.TryDequeue(out item))
            {
                if (item.OnDecay())
                {
                    // TODO: Add Logging
                    item.Delete();
                }
            }
        }

        public static void Save()
        {
            if (WorldState != WorldState.Running)
            {
                return;
            }

            ++_Saves;

            NetState.FlushAll();
            NetState.Pause();

            WaitForWriteCompletion(); // Blocks Save until current disk flush is done.

            WorldState = WorldState.Saving;

            m_DiskWriteHandle.Reset();

            Broadcast(0x35, true, "The world is saving, please wait.");

            var now = DateTime.UtcNow;

            Console.Write("[{0}] World: Saving...", now.ToLongTimeString());

            var watch = Stopwatch.StartNew();

            SaveEntities(Items.Values, now);
            SaveEntities(Mobiles.Values, now);
            SaveEntities(Guilds.Values, now);

            try
            {
                EventSink.InvokeWorldSave();
            }
            catch (Exception e)
            {
                throw new Exception("World Save event threw an exception. Save failed!", e);
            }

            WorldState = WorldState.WritingSave;

            ThreadPool.QueueUserWorkItem(WriteFiles);

            watch.Stop();

            var duration = watch.Elapsed.TotalSeconds;

            Console.WriteLine("Save done in {0:F2} seconds.", duration);

            Broadcast(
                0x35,
                true,
                "World save complete. The entire process took {0:F2} seconds.",
                duration
            );

            NetState.Resume();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEntity FindEntity(Serial serial, bool returnDeleted = false) => FindEntity<IEntity>(serial);

        public static T FindEntity<T>(Serial serial, bool returnDeleted = false) where T : class, IEntity
        {
            switch (WorldState)
            {
                default: return default;
                case WorldState.Loading:
                case WorldState.Saving:
                case WorldState.WritingSave:
                    {
                        if (_pendingDelete.TryGetValue(serial, out var entity))
                        {
                            return !returnDeleted ? null : entity as T;
                        }

                        if (_pendingAdd.TryGetValue(serial, out entity))
                        {
                            return entity as T;
                        }

                        goto case WorldState.Running;
                    }
                case WorldState.Running:
                    {
                        if (serial.IsItem)
                        {
                            Items.TryGetValue(serial, out var item);
                            return item as T;
                        }

                        if (serial.IsMobile)
                        {
                            Mobiles.TryGetValue(serial, out var mob);
                            return mob as T;
                        }

                        return default;
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Item FindItem(Serial serial, bool returnDeleted = false) => FindEntity<Item>(serial, returnDeleted);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mobile FindMobile(Serial serial, bool returnDeleted = false) =>
            FindEntity<Mobile>(serial, returnDeleted);

        public static BaseGuild FindGuild(Serial serial) => Guilds.TryGetValue(serial, out var guild) ? guild : null;


        public static void AddEntity<T>(T entity) where T : class, IEntity
        {
            switch (WorldState)
            {
                default: // Not Running
                    {
                        throw new Exception($"Added {typeof(T).Name} before world load.\n");
                    }
                case WorldState.Saving:
                    {
                        AppendSafetyLog("add", entity);
                        goto case WorldState.WritingSave;
                    }
                case WorldState.Loading:
                case WorldState.WritingSave:
                    {
                        if (_pendingDelete.Remove(entity.Serial))
                        {
                            Utility.PushColor(ConsoleColor.Red);
                            Console.WriteLine($"Deleted then added {typeof(T).Name} during {WorldState.ToString().ToLower()} state.");
                            Utility.PopColor();
                        }
                        _pendingAdd[entity.Serial] = entity;
                        break;
                    }
                case WorldState.Running:
                    {
                        if (entity.Serial.IsItem)
                        {
                            Items[entity.Serial] = entity as Item;
                        }

                        if (entity.Serial.IsMobile)
                        {
                            Mobiles[entity.Serial] = entity as Mobile;
                        }
                        break;
                    }
            }
        }

        public static void AddGuild(BaseGuild guild) => Guilds[guild.Serial] = guild;

        public static void RemoveEntity<T>(T entity) where T : class, IEntity
        {
            switch (WorldState)
            {
                default: // Not Running
                    {
                        throw new Exception($"Removed {typeof(T).Name} before world load.\n");
                    }
                case WorldState.Saving:
                    {
                        AppendSafetyLog("delete", entity);
                        goto case WorldState.WritingSave;
                    }
                case WorldState.Loading:
                case WorldState.WritingSave:
                    {
                        _pendingAdd.Remove(entity.Serial);
                        _pendingDelete[entity.Serial] = entity;
                        break;
                    }
                case WorldState.Running:
                    {
                        if (entity.Serial.IsItem)
                        {
                            Items.Remove(entity.Serial);
                        }

                        if (entity.Serial.IsMobile)
                        {
                            Mobiles.Remove(entity.Serial);
                        }
                        break;
                    }
            }
        }

        public static void RemoveGuild(BaseGuild guild) => Guilds.Remove(guild.Serial);

        public static void SerializeTo(this ISerializable entity, IGenericWriter writer)
        {
            var saveBuffer = entity.SaveBuffer;
            writer.Write(saveBuffer.Data, (int)saveBuffer.Position);

            // Resize to exact buffer size
            entity.SaveBuffer.Resize((int)entity.SaveBuffer.Position);
        }
    }
}
