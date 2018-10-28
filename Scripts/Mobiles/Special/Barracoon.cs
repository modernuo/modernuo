using System;
using System.Linq;
using Server.Engines.CannedEvil;
using Server.Items;
using Server.Spells.Fifth;
using Server.Spells.Seventh;

namespace Server.Mobiles
{
  public class Barracoon : BaseChampion
  {
    [Constructible]
    public Barracoon() : base(AIType.AI_Melee)
    {
      Title = "the piper";
      Body = 0x190;
      Hue = 0x83EC;

      SetStr(305, 425);
      SetDex(72, 150);
      SetInt(505, 750);

      SetHits(4200);
      SetStam(102, 300);

      SetDamage(25, 35);

      SetDamageType(ResistanceType.Physical, 100);

      SetResistance(ResistanceType.Physical, 60, 70);
      SetResistance(ResistanceType.Fire, 50, 60);
      SetResistance(ResistanceType.Cold, 50, 60);
      SetResistance(ResistanceType.Poison, 40, 50);
      SetResistance(ResistanceType.Energy, 40, 50);

      SetSkill(SkillName.MagicResist, 100.0);
      SetSkill(SkillName.Tactics, 97.6, 100.0);
      SetSkill(SkillName.Wrestling, 97.6, 100.0);

      Fame = 22500;
      Karma = -22500;

      VirtualArmor = 70;

      AddItem(new FancyShirt(Utility.RandomGreenHue()));
      AddItem(new LongPants(Utility.RandomYellowHue()));
      AddItem(new JesterHat(Utility.RandomPinkHue()));
      AddItem(new Cloak(Utility.RandomPinkHue()));
      AddItem(new Sandals());

      HairItemID = 0x203B; // Short Hair
      HairHue = 0x94;
    }

    public Barracoon(Serial serial) : base(serial)
    {
    }

    public override ChampionSkullType SkullType => ChampionSkullType.Greed;

    public override Type[] UniqueList => new[] { typeof(FangOfRactus) };

    public override Type[] SharedList => new[]
    {
      typeof(EmbroideredOakLeafCloak),
      typeof(DjinnisRing),
      typeof(DetectiveBoots),
      typeof(GuantletsOfAnger)
    };

    public override Type[] DecorativeList => new[] { typeof(SwampTile), typeof(MonsterStatuette) };

    public override MonsterStatuetteType[] StatueTypes => new[] { MonsterStatuetteType.Slime };

    public override string DefaultName => "Barracoon";

    public override bool AlwaysMurderer => true;
    public override bool AutoDispel => true;
    public override double AutoDispelChance => 1.0;
    public override bool BardImmune => !Core.SE;
    public override bool Unprovokable => Core.SE;
    public override bool Uncalmable => Core.SE;
    public override Poison PoisonImmune => Poison.Deadly;

    public override bool ShowFameTitle => false;
    public override bool ClickTitle => false;

    public override void GenerateLoot()
    {
      AddLoot(LootPack.UltraRich, 3);
    }

    public void Polymorph(Mobile m)
    {
      if (!m.CanBeginAction<PolymorphSpell>() || !m.CanBeginAction<IncognitoSpell>() || m.IsBodyMod)
        return;

      IMount mount = m.Mount;

      if (mount != null)
        mount.Rider = null;

      if (m.Mounted)
        return;

      if (m.BeginAction<PolymorphSpell>())
      {
        Item disarm = m.FindItemOnLayer(Layer.OneHanded);

        if (disarm != null && disarm.Movable)
          m.AddToBackpack(disarm);

        disarm = m.FindItemOnLayer(Layer.TwoHanded);

        if (disarm != null && disarm.Movable)
          m.AddToBackpack(disarm);

        m.BodyMod = 42;
        m.HueMod = 0;

        new ExpirePolymorphTimer(m).Start();
      }
    }

    public void SpawnRatmen(Mobile target)
    {
      Map map = Map;

      if (map == null)
        return;

      IPooledEnumerable <BaseCreature> eable = GetMobilesInRange<BaseCreature>(10);
      int rats = eable.Aggregate(0, (c, m) => c + (m is Ratman || m is RatmanArcher || m is RatmanMage ? 1 : 0));
      eable.Free();

      if (rats >= 16)
        return;

      PlaySound(0x3D);

      rats = Utility.RandomMinMax(3, 6);

      for (int i = 0; i < rats; ++i)
      {
        BaseCreature rat;

        switch (Utility.Random(5))
        {
          default:
            rat = new Ratman();
            break;
          case 2:
          case 3:
            rat = new RatmanArcher();
            break;
          case 4:
            rat = new RatmanMage();
            break;
        }

        rat.Team = Team;
        rat.MoveToWorld(map.GetRandomNearbyLocation(Location), map);
        rat.Combatant = target;
      }
    }

    public void DoSpecialAbility(Mobile target)
    {
      if (target == null || target.Deleted) //sanity
        return;

      if (0.6 >= Utility.RandomDouble()) // 60% chance to polymorph attacker into a ratman
        Polymorph(target);

      if (0.2 >= Utility.RandomDouble()) // 20% chance to more ratmen
        SpawnRatmen(target);

      if (Hits < 500 && !IsBodyMod) // Baracoon is low on life, polymorph into a ratman
        Polymorph(this);
    }

    public override void OnGotMeleeAttack(Mobile attacker)
    {
      base.OnGotMeleeAttack(attacker);

      DoSpecialAbility(attacker);
    }

    public override void OnGaveMeleeAttack(Mobile defender)
    {
      base.OnGaveMeleeAttack(defender);

      DoSpecialAbility(defender);
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }

    private class ExpirePolymorphTimer : Timer
    {
      private Mobile m_Owner;

      public ExpirePolymorphTimer(Mobile owner) : base(TimeSpan.FromMinutes(3.0))
      {
        m_Owner = owner;

        Priority = TimerPriority.OneSecond;
      }

      protected override void OnTick()
      {
        if (!m_Owner.CanBeginAction<PolymorphSpell>())
        {
          m_Owner.BodyMod = 0;
          m_Owner.HueMod = -1;
          m_Owner.EndAction<PolymorphSpell>();
        }
      }
    }
  }
}
