using Server.Items;

namespace Server.Mobiles
{
    public class RedSolenWorker : BaseCreature
    {
        [Constructible]
        public RedSolenWorker() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
        {
            Body = 781;
            BaseSoundID = 959;

            SetStr(96, 120);
            SetDex(81, 105);
            SetInt(36, 60);

            SetHits(58, 72);

            SetDamage(5, 7);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 30);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.MagicResist, 60.0);
            SetSkill(SkillName.Tactics, 65.0);
            SetSkill(SkillName.Wrestling, 60.0);

            Fame = 1500;
            Karma = -1500;

            VirtualArmor = 28;

            PackGold(Utility.Random(100, 180));

            SolenHelper.PackPicnicBasket(this);

            PackItem(new ZoogiFungus());
        }

        public RedSolenWorker(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a solen worker corpse";
        public override string DefaultName => "a red solen worker";

        public override int GetAngerSound() => 0x269;

        public override int GetIdleSound() => 0x269;

        public override int GetAttackSound() => 0x186;

        public override int GetHurtSound() => 0x1BE;

        public override int GetDeathSound() => 0x8E;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Gems, Utility.RandomMinMax(1, 2));
        }

        public override bool IsEnemy(Mobile m)
        {
            if (SolenHelper.CheckRedFriendship(m))
            {
                return false;
            }

            return base.IsEnemy(m);
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            SolenHelper.OnRedDamage(from);

            base.OnDamage(amount, from, willKill);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
