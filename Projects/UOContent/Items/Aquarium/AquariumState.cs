using System;

namespace Server.Items;

public enum WaterState
{
    Dead,
    Dying,
    Unhealthy,
    Healthy,
    Strong
}

public enum FoodState
{
    Dead,
    Starving,
    Hungry,
    Full,
    Overfed
}

[PropertyObject]
[EmbeddedSerializable(0, false)]
public partial class AquariumState
{
    [SerializableParent]
    private Aquarium _aquarium;

    private int _state;

    public AquariumState(Aquarium parent) => _aquarium = parent;

    [SerializableField(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = Math.Clamp(value, 0, 4);
                _aquarium.MarkDirty();
            }
        }
    }

    [SerializableField(1)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private int _maintain;

    [SerializableField(2)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private int _improve;

    [SerializableField(3)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private int _added;

    public override string ToString() => "...";
}