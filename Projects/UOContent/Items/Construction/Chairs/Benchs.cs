namespace Server.Items
{
    [Furniture]
    [Serializable(0, false)]
    [Flippable(0xB2D, 0xB2C)]
    public class WoodenBench : Item
    {
        [Constructible]
        public WoodenBench() : base(0xB2D) => Weight = 6;
    }
}
