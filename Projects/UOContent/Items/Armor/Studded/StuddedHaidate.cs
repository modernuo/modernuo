namespace Server.Items
{
    [Serializable(0, false)]
    public partial class StuddedHaidate : BaseArmor
    {
        [Constructible]
        public StuddedHaidate() : base(0x278B) => Weight = 5.0;

        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 35;
        public override int InitMaxHits => 45;

        public override int AosStrReq => 30;
        public override int OldStrReq => 30;

        public override int ArmorBase => 3;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Studded;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;
    }
}
