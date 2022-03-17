using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Gumps;

namespace Server.Regions
{
    public class RegionController : Item
    {
        private Region _region;
        public RegionController(Serial serial) : base(serial) { }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Name { get; set; } = "Region Controller";
        [CommandProperty(AccessLevel.GameMaster)]
        public int Priority { get; set; } = 50;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Guarded { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool MountsAllowed { get; set; } = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ResurrectionAllowed { get; set; } = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool LogoutAllowed { get; set; } = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Housellowed { get; set; } = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public string EnterMessage { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string OutMessage { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D Bounds { get; set; }

        private bool m_Active;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => m_Active;
            set
            {
                if (m_Active == value)
                {
                    return;
                }

                m_Active = value;

                if (m_Active)
                {
                    Init();
                }
                else
                {
                    _region?.Unregister();
                }
            }
        }
        private bool _showRegion;
        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowRegion
        {
            get => _showRegion;
            set
            {
                _showRegion = value;

                if (_showRegion && Bounds.X > 0 && Bounds.Y > 0)
                {
                     var xWall = 0x398C;
                    var yWall = 0x3996;
                    //x
                    for (int i = Bounds.X; i < Bounds.X+Bounds.Width; i++)
                    {
                        new ShowRegionField(xWall, new Point3D(i, Bounds.Y, Z), Map, TimeSpan.FromMinutes(2));
                        new ShowRegionField(xWall, new Point3D(i, Bounds.Y + Bounds.Height, Z), Map, TimeSpan.FromMinutes(2));
                    }
                    //y
                    for (int i = Bounds.Y; i < Bounds.Y + Bounds.Height; i++)
                    {
                        new ShowRegionField(yWall, new Point3D(Bounds.X, i, Z), Map, TimeSpan.FromMinutes(2));
                        new ShowRegionField(yWall, new Point3D(Bounds.X + Bounds.Width, i, Z), Map, TimeSpan.FromMinutes(2));
                    }

                    _showRegion = false;
                }
            }
        }
        

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanMark { get; set; } = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TravelTo { get; set; } = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TravelFrom { get; set; } = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AttackAllowed { get; set; } = true;
        [CommandProperty(AccessLevel.GameMaster)]
        public bool CastAllowed { get; set; } = true;

        private string[] _excludeSpell = new string[0];

        [CommandProperty(AccessLevel.GameMaster)]
        public string ExcludeSpell
        {
            get
            {
                return string.Join(", ", _excludeSpell);
            }
            set
            {
                if (value.Length > 0)
                {
                    var buff = value.Split(',');
                    for (int i = 0; i < buff.Length; i++)
                    {
                        buff[i] = buff[i].TrimStart();
                    }
                    _excludeSpell = buff;
                }
            }
        }

        [Constructible]
        public RegionController() : base(5609)
        {
            Movable = false;
            Visible = false;
        }
        public void Init()
        {
            _region?.Unregister();
            _region = new CustomRegion(Name,
                Map,
                Priority,
                Guarded,
                MountsAllowed,
                ResurrectionAllowed,
                LogoutAllowed,
                Housellowed,
                CanMark,
                TravelTo,
                TravelFrom,
                AttackAllowed,
                CastAllowed,
                _excludeSpell,
                EnterMessage,
                OutMessage,

                Bounds);

            _region?.Register();
        }

        public override void OnDelete()
        {
            _region?.Unregister();
            base.OnDelete();
        }
        public override void OnDoubleClick(Mobile from) => from.SendGump(new PropertiesGump(from, this));

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(3); // version
            //version 3
            writer.Write(ExcludeSpell);
            //version 2
            writer.Write(TravelTo);
            writer.Write(TravelFrom);
            writer.Write(AttackAllowed);
            writer.Write(CastAllowed);

            //version 1
            writer.Write(Name);
            writer.Write(Priority);
            writer.Write(Guarded);
            writer.Write(MountsAllowed);
            writer.Write(ResurrectionAllowed);
            writer.Write(LogoutAllowed);
            writer.Write(Housellowed);
            writer.Write(EnterMessage);
            writer.Write(OutMessage);
            writer.Write(Bounds);
            writer.Write(Active);
            writer.Write(CanMark);

           
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        ExcludeSpell = reader.ReadString();
                        goto case 2;
                    }
                case 2:
                    {
                        TravelTo = reader.ReadBool();
                        TravelFrom = reader.ReadBool();
                        AttackAllowed = reader.ReadBool();
                        CastAllowed = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        Name = reader.ReadString();
                        Priority = reader.ReadInt();
                        Guarded = reader.ReadBool();
                        MountsAllowed = reader.ReadBool();
                        ResurrectionAllowed = reader.ReadBool();
                        LogoutAllowed = reader.ReadBool();
                        Housellowed = reader.ReadBool();
                        EnterMessage = reader.ReadString();
                        OutMessage = reader.ReadString();
                        Bounds = reader.ReadRect2D();
                        Active = reader.ReadBool();
                        CanMark = reader.ReadBool();
                        break;
                    }
                default:
                    break;
            }

            Init();
        }

    }
}
