namespace Server.Items;

[Serializable(0)]
public partial class JackOLantern : BaseAddon
{
    [Constructible]
    public JackOLantern() : this(Utility.Random(2) < 1)
    {
    }

    [Constructible]
    public JackOLantern(bool south)
    {
        AddComponent(new AddonComponent(5703), 0, 0, +0);

        const int hue = 1161;

        if (!south)
        {
            AddComponent(GetComponent(3178, 0), 0, 0, -1);
            AddComponent(GetComponent(3883, hue), 0, 0, +1);
            AddComponent(GetComponent(3862, hue), 0, 0, +0);
        }
        else
        {
            AddComponent(GetComponent(3179, 0), 0, 0, +0);
            AddComponent(GetComponent(3885, hue), 0, 0, -1);
            AddComponent(GetComponent(3871, hue), 0, 0, +0);
        }
    }

    public override bool ShareHue => false;

    private static AddonComponent GetComponent(int itemID, int hue) =>
        new(itemID)
        {
            Hue = hue,
            Name = "jack-o-lantern"
        };

    private void Deserialize(IGenericReader reader, int version)
    {
        for (var i = 0; i < Components.Count; ++i)
        {
            var ac = Components[i];
            ac.Name = "jack-o-lantern";

            if (ac.Hue == 2118)
            {
                ac.Hue = 1161;
            }
        }
    }
}