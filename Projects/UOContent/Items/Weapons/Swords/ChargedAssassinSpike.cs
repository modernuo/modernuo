namespace Server.Items
{
    [Serializable(0)]
    public partial class ChargedAssassinSpike : AssassinSpike
    {
        [Constructible]
        public ChargedAssassinSpike() => WeaponAttributes.HitLightning = 10;

        public override int LabelNumber => 1073518; // charged assassin spike
    }
}
