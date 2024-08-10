using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class PricedHealer : BaseHealer
{
    [Constructible]
    public PricedHealer(int price = 5000) : base(price)
    {
        if (!Core.AOS)
        {
            NameHue = 0x35;
        }
    }

    public override bool IsInvulnerable => true;

    public override bool HealsYoungPlayers => false;
}
