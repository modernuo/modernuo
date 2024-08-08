using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Feather : Item, ICommodity
{
    [Constructible]
    public Feather(int amount = 1) : base(0x1BD1)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
    TextDefinition ICommodity.Description => LabelNumber;
    bool ICommodity.IsDeedable => true;
}
