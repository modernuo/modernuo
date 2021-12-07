namespace Server.Items;

[Serializable(0)]
public partial class RangersShortbow : MagicalShortbow
{
    [Constructible]
    public RangersShortbow() => Attributes.WeaponSpeed = 5;

    public override int LabelNumber => 1073509; // ranger's shortbow
}