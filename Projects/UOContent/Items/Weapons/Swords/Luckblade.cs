namespace Server.Items;

[Serializable(0)]
public partial class Luckblade : Leafblade
{
    [Constructible]
    public Luckblade() => Attributes.Luck = 20;

    public override int LabelNumber => 1073522; // luckblade
}