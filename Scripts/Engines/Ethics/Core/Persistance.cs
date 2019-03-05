namespace Server.Ethics
{
  public class EthicsPersistance : Item
  {
    [Constructible]
    public EthicsPersistance()
      : base(1)
    {
      Movable = false;

      if (Instance?.Deleted != false)
        Instance = this;
      else
        base.Delete();
    }

    public EthicsPersistance(Serial serial)
      : base(serial)
    {
      Instance = this;
    }

    public static EthicsPersistance Instance{ get; private set; }

    public override string DefaultName => "Ethics Persistance - Internal";

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version

      for (int i = 0; i < Ethic.Ethics.Length; ++i)
        Ethic.Ethics[i].Serialize(writer);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 0:
        {
          for (int i = 0; i < Ethic.Ethics.Length; ++i)
            Ethic.Ethics[i].Deserialize(reader);

          break;
        }
      }
    }

    public override void Delete()
    {
    }
  }
}
