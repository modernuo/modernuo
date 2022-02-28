using System;

namespace Server.Items;

[Serializable(1, false)]
public partial class BaseTreasureChest : LockableContainer
{
    public enum TreasureLevel
    {
        Level1,
        Level2,
        Level3,
        Level4,
        Level5,
        Level6
    }

    private TimerExecutionToken _resetTimer;

    public BaseTreasureChest(int itemID, TreasureLevel level = TreasureLevel.Level2) : base(itemID)
    {
        _level = level;
        _minSpawnTime = TimeSpan.FromMinutes(10);
        _maxSpawnTime = TimeSpan.FromMinutes(60);

        Locked = true;
        Movable = false;

        SetLockLevel();
        GenerateTreasure();
    }

    [SerializableField(0)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private TreasureLevel _level;

    [SerializableField(1)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private TimeSpan _minSpawnTime;

    [SerializableField(2)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private TimeSpan _maxSpawnTime;

    [CommandProperty(AccessLevel.GameMaster)]
    public override bool Locked
    {
        get => base.Locked;
        set
        {
            if (base.Locked != value)
            {
                base.Locked = value;

                if (!value)
                {
                    StartResetTimer();
                }
            }
        }
    }

    public override bool IsDecoContainer => false;

    public override string DefaultName => Locked ? "a locked treasure chest" : "a treasure chest";

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (!Locked)
        {
            StartResetTimer();
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _level = (TreasureLevel)reader.ReadByte();
        _minSpawnTime = TimeSpan.FromMinutes(reader.ReadShort());
        _maxSpawnTime = TimeSpan.FromMinutes(reader.ReadShort());
    }

    protected virtual void SetLockLevel()
    {
        RequiredSkill = _level switch
        {
            TreasureLevel.Level1 => LockLevel = 5,
            TreasureLevel.Level2 => LockLevel = 20,
            TreasureLevel.Level3 => LockLevel = 50,
            TreasureLevel.Level4 => LockLevel = 70,
            TreasureLevel.Level5 => LockLevel = 90,
            TreasureLevel.Level6 => LockLevel = 100,
            _                    => RequiredSkill
        };
    }

    private void StartResetTimer()
    {
        _resetTimer.Cancel();
        var randomDuration = Utility.RandomMinMax(_minSpawnTime.Ticks, _maxSpawnTime.Ticks);
        Timer.StartTimer(TimeSpan.FromTicks(randomDuration), Reset, out _resetTimer);
    }

    protected virtual void GenerateTreasure()
    {
        var minGold = 1;
        var maxGold = 2;

        switch (_level)
        {
            case TreasureLevel.Level1:
                minGold = 100;
                maxGold = 300;
                break;

            case TreasureLevel.Level2:
                minGold = 300;
                maxGold = 600;
                break;

            case TreasureLevel.Level3:
                minGold = 600;
                maxGold = 900;
                break;

            case TreasureLevel.Level4:
                minGold = 900;
                maxGold = 1200;
                break;

            case TreasureLevel.Level5:
                minGold = 1200;
                maxGold = 5000;
                break;

            case TreasureLevel.Level6:
                minGold = 5000;
                maxGold = 9000;
                break;
        }

        DropItem(new Gold(minGold, maxGold));
    }

    public void ClearContents()
    {
        for (var i = Items.Count - 1; i >= 0; --i)
        {
            if (i < Items.Count)
            {
                Items[i].Delete();
            }
        }
    }

    public void Reset()
    {
        _resetTimer.Cancel();
        Locked = true;
        ClearContents();
        GenerateTreasure();
    }
}
