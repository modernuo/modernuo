namespace Server.Items
{
  public class MagicArrowScroll : SpellScroll
  {
    [Constructible]
    public MagicArrowScroll(int amount = 1) : base(4, 0x1F32, amount)
    {
    }

    public MagicArrowScroll(Serial serial) : base(serial)
    {
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
