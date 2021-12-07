namespace Server.Items;

[Serializable(0)]
public partial class MagesRuneBlade : RuneBlade
{
    [Constructible]
    public MagesRuneBlade() => Attributes.CastSpeed = 1;

    public override int LabelNumber => 1073538; // mage's rune blade
}