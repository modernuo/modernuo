using System;
using System.Linq;

namespace Server.Mobiles
{
    public class BladeSpirits : BaseCreature
    {
        [Constructible]
        public BladeSpirits()
            : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Body = 574;

            SetStr(150);
            SetDex(150);
            SetInt(100);

            SetHits(Core.SE ? 160 : 80);
            SetStam(250);
            SetMana(0);

            SetDamage(10, 14);

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Poison, 20);
            SetDamageType(ResistanceType.Energy, 20);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 100);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.MagicResist, 70.0);
            SetSkill(SkillName.Tactics, 90.0);
            SetSkill(SkillName.Wrestling, 90.0);

            Fame = 0;
            Karma = 0;

            VirtualArmor = 40;
            ControlSlots = Core.SE ? 2 : 1;
        }

        public BladeSpirits(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a blade spirit corpse";
        public override bool DeleteCorpseOnDeath => Core.AOS;
        public override bool IsHouseSummonable => true;

        public override double DispelDifficulty => 0.0;
        public override double DispelFocus => 20.0;

        public override string DefaultName => "a blade spirit";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;

        public override double GetFightModeRanking(Mobile m, FightMode acqType, bool bPlayerOnly) =>
            (m.Str + m.Skills.Tactics.Value) / Math.Max(GetDistanceToSqrt(m), 1.0);

        public override int GetAngerSound() => 0x23A;

        public override int GetAttackSound() => 0x3B8;

        public override int GetHurtSound() => 0x23A;

        public override void OnThink()
        {
            if (Core.SE && Summoned)
            {
                var eable = GetMobilesInRange(5);
                var spiritsOrVortexes = eable
                    .Where(m => m is EnergyVortex or BladeSpirits && ((BaseCreature)m).Summoned)
                    .ToList();

                eable.Free();

                while (spiritsOrVortexes.Count > 6)
                {
                    var random = spiritsOrVortexes.RandomElement();
                    Dispel(random);
                    spiritsOrVortexes.Remove(random);
                }
            }

            base.OnThink();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
