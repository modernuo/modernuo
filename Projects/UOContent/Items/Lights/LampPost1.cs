using System;

namespace Server.Items;

public class LampPost1 : BaseLight
{
    [Constructible]
    public LampPost1() : base(0xB21)
    {
        Movable = false;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle300;
        Weight = 40.0;
    }

    public LampPost1(Serial serial) : base(serial)
    {
    }

    public override int LitItemID => 0xB20;
    public override int UnlitItemID => 0xB21;

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);
        writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);
        var version = reader.ReadInt();
    }
}