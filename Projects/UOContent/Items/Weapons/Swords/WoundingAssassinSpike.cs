namespace Server.Items;

[Serializable(0)]
public partial class WoundingAssassinSpike : AssassinSpike
{
    [Constructible]
    public WoundingAssassinSpike() => WeaponAttributes.HitHarm = 15;

    public override int LabelNumber => 1073520; // wounding assassin spike
}