using Server.Items;

namespace Server.Mobiles
{
    public class AgapiteElemental : BaseCreature
    {
        [Constructible]
        public AgapiteElemental(int oreAmount = 2) : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
        {
            Body = 107;
            BaseSoundID = 268;

            SetStr(226, 255);
            SetDex(126, 145);
            SetInt(71, 92);

            SetHits(136, 153);

            SetDamage(28);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.MagicResist, 50.1, 95.0);
            SetSkill(SkillName.Tactics, 60.1, 100.0);
            SetSkill(SkillName.Wrestling, 60.1, 100.0);

            Fame = 3500;
            Karma = -3500;

            VirtualArmor = 32;

            Item ore = new AgapiteOre(oreAmount);
            ore.ItemID = 0x19B9;
            PackItem(ore);
        }

        public AgapiteElemental(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an ore elemental corpse";
        public override string DefaultName => "an agapite elemental";

        public override bool BleedImmune => true;
        public override bool AutoDispel => true;
        public override int TreasureMapLevel => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Gems, 2);
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
