using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LightOfWayGlasses : ElvenGlasses
    {
        [Constructible]
        public LightOfWayGlasses()
        {
            Attributes.BonusStr = 7;
            Attributes.BonusInt = 5;
            Attributes.WeaponDamage = 30;

            Hue = 0x256;
        }

        public override int LabelNumber => 1073378; // Light Of Way Reading Glasses

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 10;
        public override int BaseColdResistance => 10;
        public override int BasePoisonResistance => 10;
        public override int BaseEnergyResistance => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
