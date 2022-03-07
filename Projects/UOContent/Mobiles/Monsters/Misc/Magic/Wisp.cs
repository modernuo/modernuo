using System;
using Server.Engines.Plants;
using Server.Ethics;
using Server.Factions;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles
{
    public class Wisp : BaseCreature
    {
        [Constructible]
        public Wisp() : base(AIType.AI_Mage, FightMode.Aggressor, 10, 1)
        {
            Body = 58;
            BaseSoundID = 466;

            SetStr(196, 225);
            SetDex(196, 225);
            SetInt(196, 225);

            SetHits(118, 135);

            SetDamage(17, 18);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Energy, 50);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 20, 40);
            SetResistance(ResistanceType.Cold, 10, 30);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 50, 70);

            SetSkill(SkillName.EvalInt, 80.0);
            SetSkill(SkillName.Magery, 80.0);
            SetSkill(SkillName.MagicResist, 80.0);
            SetSkill(SkillName.Tactics, 80.0);
            SetSkill(SkillName.Wrestling, 80.0);

            Fame = 4000;
            Karma = 0;

            VirtualArmor = 40;

            if (Core.ML && Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomPeculiarSeed(3));
            }

            AddItem(new LightSource());
        }

        public Wisp(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a wisp corpse";
        public override InhumanSpeech SpeechType => InhumanSpeech.Wisp;

        public override Faction FactionAllegiance => CouncilOfMages.Instance;
        public override Ethic EthicAllegiance => Ethic.Hero;

        public override TimeSpan ReacquireDelay => TimeSpan.FromSeconds(1.0);

        public override string DefaultName => "a wisp";

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Average);
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
