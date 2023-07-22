using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Server.Collections;
using Server.Mobiles;

namespace Server.Engines.CannedEvil;

public static class ChampionTitleSystem
{
    // All of the players with murders
    private static readonly Dictionary<PlayerMobile, ChampionTitleContext> _championTitleContexts = new();

    private static readonly Timer _championTitleTimer = new ChampionTitleTimer();

    public static void Configure()
    {
        GenericPersistence.Register("ChampionTitles", Serialize, Deserialize);
    }

    public static void Initialize()
    {
        EventSink.PlayerDeleted += OnPlayerDeleted;

        _championTitleTimer.Start();
    }

    private static void OnPlayerDeleted(Mobile m)
    {
        if (m is PlayerMobile pm)
        {
            _championTitleContexts.Remove(pm);
        }
    }

    private static void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; ++i)
        {
            var context = new ChampionTitleContext(reader.ReadEntity<PlayerMobile>());
            context.Deserialize(reader);

            _championTitleContexts.Add(context.Player, context);
        }
    }

    private static void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(_championTitleContexts.Count);
        foreach (var (m, context) in _championTitleContexts)
        {
            writer.Write(m);
            context.Serialize(writer);
        }
    }

    public static bool GetChampionTitleContext(this PlayerMobile player, out ChampionTitleContext context) =>
        _championTitleContexts.TryGetValue(player, out context);

    public static ChampionTitleContext GetOrCreateChampionTitleContext(this PlayerMobile player)
    {
        ref var context = ref CollectionsMarshal.GetValueRefOrAddDefault(_championTitleContexts, player, out var exists);
        if (!exists)
        {
            context = new ChampionTitleContext(player);
        }

        return context;
    }

    // Called when killing a harrower. Will give a minimum of 1 point.
    public static void AwardHarrowerTitle(PlayerMobile pm)
    {
        var context = pm.GetOrCreateChampionTitleContext();

        var count = 1;
        for (var i = 0; i < ChampionSpawnInfo.Table.Length; i++)
        {
            var title = context.GetTitle(ChampionSpawnInfo.Table[i].Type);
            if (title?.Value > 900)
            {
                count++;
            }
        }

        context.Harrower = Math.Max(count, context.Harrower); // Harrower titles never decay.
    }

    private class ChampionTitleTimer : Timer
    {
        public ChampionTitleTimer() : base(TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(5.0))
        {
        }

        protected override void OnTick()
        {
            if (_championTitleContexts.Count == 0)
            {
                return;
            }

            using var queue = PooledRefQueue<Mobile>.Create();

            foreach (var context in _championTitleContexts.Values)
            {
                if (!context.CheckAtrophy())
                {
                    queue.Enqueue(context.Player);
                }
            }

            while (queue.Count > 0)
            {
                _championTitleContexts.Remove((PlayerMobile)queue.Dequeue());
            }
        }
    }
}
