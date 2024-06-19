/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Persistence.cs                                                  *
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
using System.IO;

namespace Server;

public abstract class Persistence
{
    private static readonly SortedSet<Persistence> _registry = new(new PersistenceComparer());

    public int Priority { get; }

    public Persistence(int priority = 100)
    {
        Priority = priority;
        _registry.Add(this);
    }

    public bool Register() => _registry.Add(this);

    public void Unregister() => _registry.Remove(this);

    public static void Load(string path)
    {
        var typesDb = LoadTypes(path);

        foreach (var entry in _registry)
        {
            (entry as IGenericEntityPersistence)?.DeserializeIndexes(path, typesDb);
        }

        // This should probably not be parallel since Mobiles must be loaded before Items
        foreach (var entry in _registry)
        {
            entry.Deserialize(path, typesDb);
        }
    }

    private static Dictionary<ulong, string> LoadTypes(string path)
    {
        var db = new Dictionary<ulong, string>();

        string tdbPath = Path.Combine(path, "SerializedTypes.db");
        if (!File.Exists(tdbPath))
        {
            return db;
        }

        using FileStream tdb = new FileStream(tdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        BinaryReader tdbReader = new BinaryReader(tdb);

        var version = tdbReader.ReadInt32();
        var count = tdbReader.ReadInt32();

        for (var i = 0; i < count; ++i)
        {
            var hash = tdbReader.ReadUInt64();
            var typeName = tdbReader.ReadString();
            db[hash] = typeName;
        }

        return db;
    }

    // Note: This is strictly on a background thread
    internal static void PreSerializeAll(string path, ConcurrentQueue<Type> types)
    {
        foreach (var p in _registry)
        {
            p.Preserialize(path, types);
        }
    }

    private static readonly HashSet<Type> _typesSet = [];

    // Note: This is strictly on a background thread
    internal static void WriteSnapshotAll(string path, ConcurrentQueue<Type> types)
    {
        foreach (var p in _registry)
        {
            p.WriteSnapshot();
        }

        // Dedupe the queue.
        foreach (var type in types)
        {
            _typesSet.Add(type);
        }

        WriteSerializedTypesSnapshot(path, _typesSet);
        _typesSet.Clear();
    }

    internal static void SerializeAll()
    {
        foreach (var p in _registry)
        {
            p.Serialize();
        }
    }

    internal static void PostWorldSaveAll()
    {
        foreach (var p in _registry)
        {
            p.PostWorldSave();
        }
    }

    internal static void PostDeserializeAll()
    {
        foreach (var p in _registry)
        {
            p.PostDeserialize();
        }
    }

    public static void WriteSerializedTypesSnapshot(string path, HashSet<Type> types)
    {
        string tdbPath = Path.Combine(path, "SerializedTypes.db");
        using var fs = new FileStream(tdbPath, FileMode.Create);
        using var writer = new MemoryMapFileWriter(fs, 1024 * 1024 * 4);

        writer.Write(0); // version
        writer.Write(types.Count);

        foreach (var type in types)
        {
            var fullName = type.FullName;
            writer.Write(HashUtility.ComputeHash64(fullName));
            writer.Write(fullName);
        }
    }

    // Open file streams, MMFs, prepare data structures
    // Note: This should only be run on a background thread
    public abstract void Preserialize(string savePath, ConcurrentQueue<Type> types);

    // Note: This should only be run on a background thread
    public abstract void Serialize(IGenericSerializable e, int threadIndex);

    // Note: This should only be run on a background thread
    public abstract void WriteSnapshot();

    public abstract void Serialize();

    public abstract void Deserialize(string savePath, Dictionary<ulong, string> typesDb);

    public virtual void PostWorldSave()
    {
    }

    public virtual void PostDeserialize()
    {
    }

    internal class PersistenceComparer : IComparer<Persistence>
    {
        public int Compare(Persistence x, Persistence y)
        {
            if (x == y)
            {
                return 0;
            }

            if (x == null)
            {
                return 1;
            }

            if (y == null)
            {
                return -1;
            }

            // First sort by priority
            var cmp = x.Priority.CompareTo(y.Priority);

            // Then alphabetically. We won't allow the same entry (by name) twice in the SortedSet
            return cmp != 0 ? cmp : x.GetHashCode().CompareTo(y.GetHashCode());
        }
    }

    public static void TraceException(Exception ex)
    {
        try
        {
            using var op = new StreamWriter("save-errors.log", true);
            op.WriteLine("# {0}", Core.Now);

            op.WriteLine(ex);

            op.WriteLine();
            op.WriteLine();
        }
        catch
        {
            // ignored
        }

        Console.WriteLine(ex);
    }
}
