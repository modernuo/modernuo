using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
  public class KazeKemono : BaseCreature
  {
    private static Dictionary<Mobile, ExpireTimer> m_FlurryOfTwigsTable = new Dictionary<Mobile, ExpireTimer>();
    private static Dictionary<Mobile, ExpireTimer> m_ChlorophylBlastTable = new Dictionary<Mobile, ExpireTimer>();

    [Constructible]
    public KazeKemono()
      : base(AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4)
    {
      Body = 196;
      BaseSoundID = 655;

      SetStr(201, 275);
      SetDex(101, 155);
      SetInt(101, 105);

      SetHits(251, 330);

      SetDamage(15, 20);

      SetDamageType(ResistanceType.Physical, 70);
      SetDamageType(ResistanceType.Fire, 10);
      SetDamageType(ResistanceType.Cold, 10);
      SetDamageType(ResistanceType.Poison, 10);

      SetResistance(ResistanceType.Physical, 50, 70);
      SetResistance(ResistanceType.Fire, 30, 60);
      SetResistance(ResistanceType.Cold, 30, 60);
      SetResistance(ResistanceType.Poison, 50, 70);
      SetResistance(ResistanceType.Energy, 60, 80);

      SetSkill(SkillName.MagicResist, 110.1, 125.0);
      SetSkill(SkillName.Tactics, 55.1, 65.0);
      SetSkill(SkillName.Wrestling, 85.1, 95.0);
      SetSkill(SkillName.Anatomy, 25.1, 35.0);
      SetSkill(SkillName.Magery, 95.1, 105.0);

      Fame = 8000;
      Karma = -8000;
    }

    public KazeKemono(Serial serial)
      : base(serial)
    {
    }

    public override string CorpseName => "a kaze kemono corpse";
    public override string DefaultName => "a kaze kemono";

    public override bool BleedImmune => true;


    public override void GenerateLoot()
    {
      AddLoot(LootPack.Rich, 3);
    }

    public override void OnGaveMeleeAttack(Mobile defender)
    {
      base.OnGaveMeleeAttack(defender);

      if (0.1 > Utility.RandomDouble())
      {
        /* Flurry of Twigs
         * Start cliloc: 1070850
         * Effect: Physical resistance -15% for 5 seconds
         * End cliloc: 1070852
         * Effect: Type: "3" From: "0x57D4F5B" To: "0x0" ItemId: "0x37B9" ItemIdName: "glow" FromLocation: "(1048 779, 6)" ToLocation: "(1048 779, 6)" Speed: "10" Duration: "5" FixedDirection: "True" Explode: "False"
         */

        ExpireTimer timer = m_FlurryOfTwigsTable[defender];

        if (timer != null)
        {
          timer.DoExpire();
          defender.SendLocalizedMessage(1070851); // The creature lands another blow in your weakened state.
        }
        else
        {
          defender.SendLocalizedMessage(
            1070850); // The creature's flurry of twigs has made you more susceptible to physical attacks!
        }

        int effect = -(defender.PhysicalResistance * 15 / 100);

        ResistanceMod mod = new ResistanceMod(ResistanceType.Physical, effect);

        defender.FixedEffect(0x37B9, 10, 5);
        defender.AddResistanceMod(mod);

        timer = new ExpireTimer(defender, mod, m_FlurryOfTwigsTable, TimeSpan.FromSeconds(5.0));
        timer.Start();
        m_FlurryOfTwigsTable[defender] = timer;
      }
      else if (0.05 > Utility.RandomDouble())
      {
        /* Chlorophyl Blast
         * Start cliloc: 1070827
         * Effect: Energy resistance -50% for 10 seconds
         * End cliloc: 1070829
         * Effect: Type: "3" From: "0x57D4F5B" To: "0x0" ItemId: "0x37B9" ItemIdName: "glow" FromLocation: "(1048 779, 6)" ToLocation: "(1048 779, 6)" Speed: "10" Duration: "5" FixedDirection: "True" Explode: "False"
         */

        ExpireTimer timer = m_ChlorophylBlastTable[defender];

        if (timer != null)
        {
          timer.DoExpire();
          defender.SendLocalizedMessage(1070828); // The creature continues to hinder your energy resistance!
        }
        else
        {
          defender.SendLocalizedMessage(
            1070827); // The creature's attack has made you more susceptible to energy attacks!
        }

        int effect = -(defender.EnergyResistance / 2);

        ResistanceMod mod = new ResistanceMod(ResistanceType.Energy, effect);

        defender.FixedEffect(0x37B9, 10, 5);
        defender.AddResistanceMod(mod);

        timer = new ExpireTimer(defender, mod, m_ChlorophylBlastTable, TimeSpan.FromSeconds(10.0));
        timer.Start();
        m_ChlorophylBlastTable[defender] = timer;
      }
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);
      writer.Write(0);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);
      int version = reader.ReadInt();
    }

    private class ExpireTimer : Timer
    {
      private Mobile m_Mobile;
      private ResistanceMod m_Mod;
      private Dictionary<Mobile, ExpireTimer> m_Table;

      public ExpireTimer(Mobile m, ResistanceMod mod, Dictionary<Mobile, ExpireTimer> table, TimeSpan delay)
        : base(delay)
      {
        m_Mobile = m;
        m_Mod = mod;
        m_Table = table;
        Priority = TimerPriority.TwoFiftyMS;
      }

      public void DoExpire()
      {
        m_Mobile.RemoveResistanceMod(m_Mod);
        Stop();
        m_Table.Remove(m_Mobile);
      }

      protected override void OnTick()
      {
        if (m_Mod.Type == ResistanceType.Physical)
          m_Mobile.SendLocalizedMessage(1070852); // Your resistance to physical attacks has returned.
        else
          m_Mobile.SendLocalizedMessage(1070829); // Your resistance to energy attacks has returned.

        DoExpire();
      }
    }
  }
}
