namespace Server.Items
{
  public class CocoaLiquor : Item
  {
    [Constructible]
    public CocoaLiquor()
      : base(0x103F) =>
      Hue = 0x46A;

    public CocoaLiquor(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1080007; // Cocoa liquor
    public override double DefaultWeight => 1.0;

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

  public class SackOfSugar : Item
  {
    [Constructible]
    public SackOfSugar(int amount = 1)
      : base(0x1039)
    {
      Hue = 0x461;
      Stackable = true;
      Amount = amount;
    }

    public SackOfSugar(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1080003; // Sack of sugar
    public override double DefaultWeight => 1.0;

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

  public class CocoaButter : Item
  {
    [Constructible]
    public CocoaButter()
      : base(0x1044) =>
      Hue = 0x457;

    public CocoaButter(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1080005; // Cocoa butter
    public override double DefaultWeight => 1.0;

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

  public class Vanilla : Item
  {
    [Constructible]
    public Vanilla(int amount = 1)
      : base(0xE2A)
    {
      Hue = 0x462;
      Stackable = true;
      Amount = amount;
    }

    public Vanilla(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1080009; // Vanilla
    public override double DefaultWeight => 1.0;

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

  public class CocoaPulp : Item
  {
    [Constructible]
    public CocoaPulp(int amount = 1)
      : base(0xF7C)
    {
      Hue = 0x219;
      Stackable = true;
      Amount = amount;
    }

    public CocoaPulp(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1080530; // cocoa pulp
    public override double DefaultWeight => 1.0;

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

  public class DarkChocolate : CandyCane
  {
    [Constructible]
    public DarkChocolate()
      : base(0xF10)
    {
      Hue = 0x465;
      LootType = LootType.Regular;
    }

    public DarkChocolate(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1079994; // Dark chocolate
    public override double DefaultWeight => 1.0;

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class MilkChocolate : CandyCane
  {
    [Constructible]
    public MilkChocolate()
      : base(0xF18)
    {
      Hue = 0x461;
      LootType = LootType.Regular;
    }

    public MilkChocolate(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1079995; // Milk chocolate
    public override double DefaultWeight => 1.0;

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class WhiteChocolate : CandyCane
  {
    [Constructible]
    public WhiteChocolate()
      : base(0xF11)
    {
      Hue = 0x47E;
      LootType = LootType.Regular;
    }

    public WhiteChocolate(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1079996; // White chocolate
    public override double DefaultWeight => 1.0;

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
