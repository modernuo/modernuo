using System;
using System.Collections.Generic;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class CrystalRechargeInfo
    {
        public static readonly CrystalRechargeInfo[] Table =
        {
            new(typeof(Citrine), 500),
            new(typeof(Amber), 500),
            new(typeof(Tourmaline), 750),
            new(typeof(Emerald), 1000),
            new(typeof(Sapphire), 1000),
            new(typeof(Amethyst), 1000),
            new(typeof(StarSapphire), 1250),
            new(typeof(Diamond), 2000)
        };

        private CrystalRechargeInfo(Type type, int amount)
        {
            Type = type;
            Amount = amount;
        }

        public Type Type { get; }

        public int Amount { get; }

        public static CrystalRechargeInfo Get(Type type)
        {
            foreach (var info in Table)
            {
                if (info.Type == type)
                {
                    return info;
                }
            }

            return null;
        }
    }

    public class BroadcastCrystal : Item
    {
        public static readonly int MaxCharges = 2000;

        private int m_Charges;

        [Constructible]
        public BroadcastCrystal(int charges = 2000) : base(0x1ED0)
        {
            Light = LightType.Circle150;

            m_Charges = charges;

            Receivers = new List<ReceiverCrystal>();
        }

        public BroadcastCrystal(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1060740; // communication crystal

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => ItemID == 0x1ECD;
            set
            {
                ItemID = value ? 0x1ECD : 0x1ED0;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get => m_Charges;
            set
            {
                m_Charges = value;
                InvalidateProperties();
            }
        }

        public List<ReceiverCrystal> Receivers { get; private set; }

        public override bool HandlesOnSpeech => true;

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(Active ? 1060742 : 1060743);  // active / inactive
            list.Add(1060745);                     // broadcast
            list.Add(1060741, Charges.ToString()); // charges: ~1_val~

            if (Receivers.Count > 0)
            {
                list.Add(1060746, Receivers.Count.ToString()); // links: ~1_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, Active ? 1060742 : 1060743);  // active / inactive
            LabelTo(from, 1060745);                     // broadcast
            LabelTo(from, 1060741, Charges.ToString()); // charges: ~1_val~

            if (Receivers.Count > 0)
            {
                LabelTo(from, 1060746, Receivers.Count.ToString()); // links: ~1_val~
            }
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!Active || Receivers.Count == 0 || RootParent != null && !(RootParent is Mobile))
            {
                return;
            }

            if (e.Type == MessageType.Emote)
            {
                return;
            }

            var from = e.Mobile;
            var speech = e.Speech;

            foreach (var receiver in new List<ReceiverCrystal>(Receivers))
            {
                if (receiver.Deleted)
                {
                    Receivers.Remove(receiver);
                }
                else if (Charges > 0)
                {
                    receiver.TransmitMessage(from, speech);
                    Charges--;
                }
                else
                {
                    Active = false;
                    break;
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            from.Target = new InternalTarget(this);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(m_Charges);
            writer.WriteItemList(Receivers);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Charges = reader.ReadEncodedInt();
            Receivers = reader.ReadStrongItemList<ReceiverCrystal>();
        }

        private class InternalTarget : Target
        {
            private readonly BroadcastCrystal m_Crystal;

            public InternalTarget(BroadcastCrystal crystal) : base(2, false, TargetFlags.None) => m_Crystal = crystal;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!m_Crystal.IsAccessibleTo(from))
                {
                    return;
                }

                if (from.Map != m_Crystal.Map || !from.InRange(m_Crystal.GetWorldLocation(), 2))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                    return;
                }

                if (targeted == m_Crystal)
                {
                    if (m_Crystal.Active)
                    {
                        m_Crystal.Active = false;
                        from.SendLocalizedMessage(500672); // You turn the crystal off.
                    }
                    else
                    {
                        if (m_Crystal.Charges > 0)
                        {
                            m_Crystal.Active = true;
                            from.SendLocalizedMessage(500673); // You turn the crystal on.
                        }
                        else
                        {
                            from.SendLocalizedMessage(500676); // This crystal is out of charges.
                        }
                    }
                }
                else if (targeted is ReceiverCrystal receiver)
                {
                    if (m_Crystal.Receivers.Count >= 10)
                    {
                        from.SendLocalizedMessage(1010042); // This broadcast crystal is already linked to 10 receivers.
                    }
                    else if (receiver.Sender == m_Crystal)
                    {
                        from.SendLocalizedMessage(500674); // This crystal is already linked with that crystal.
                    }
                    else if (receiver.Sender != null)
                    {
                        from.SendLocalizedMessage(
                            1010043
                        ); // That receiver crystal is already linked to another broadcast crystal.
                    }
                    else
                    {
                        receiver.Sender = m_Crystal;
                        from.SendLocalizedMessage(500675); // That crystal has been linked to this crystal.
                    }
                }
                else if (targeted == from)
                {
                    foreach (var rc in new List<ReceiverCrystal>(m_Crystal.Receivers))
                    {
                        rc.Sender = null;
                    }

                    from.SendLocalizedMessage(1010046); // You unlink the broadcast crystal from all of its receivers.
                }
                else
                {
                    if (targeted is Item targItem && targItem.VerifyMove(from))
                    {
                        var info = CrystalRechargeInfo.Get(targItem.GetType());

                        if (info != null)
                        {
                            if (m_Crystal.Charges >= MaxCharges)
                            {
                                from.SendLocalizedMessage(500678); // This crystal is already fully charged.
                            }
                            else
                            {
                                targItem.Consume();

                                if (m_Crystal.Charges + info.Amount >= MaxCharges)
                                {
                                    m_Crystal.Charges = MaxCharges;
                                    from.SendLocalizedMessage(500679); // You completely recharge the crystal.
                                }
                                else
                                {
                                    m_Crystal.Charges += info.Amount;
                                    from.SendLocalizedMessage(500680); // You recharge the crystal.
                                }
                            }

                            return;
                        }
                    }

                    from.SendLocalizedMessage(500681); // You cannot use this crystal on that.
                }
            }
        }
    }

    public class ReceiverCrystal : Item
    {
        private BroadcastCrystal m_Sender;

        [Constructible]
        public ReceiverCrystal() : base(0x1ED0) => Light = LightType.Circle150;

        public ReceiverCrystal(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1060740; // communication crystal

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => ItemID == 0x1ED1;
            set
            {
                ItemID = value ? 0x1ED1 : 0x1ED0;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BroadcastCrystal Sender
        {
            get => m_Sender;
            set
            {
                if (m_Sender != null)
                {
                    m_Sender.Receivers.Remove(this);
                    m_Sender.InvalidateProperties();
                }

                m_Sender = value;

                if (value != null)
                {
                    value.Receivers.Add(this);
                    value.InvalidateProperties();
                }
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(Active ? 1060742 : 1060743); // active / inactive
            list.Add(1060744);                    // receiver
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, Active ? 1060742 : 1060743); // active / inactive
            LabelTo(from, 1060744);                    // receiver
        }

        public void TransmitMessage(Mobile from, string message)
        {
            if (!Active)
            {
                return;
            }

            var text = $"{from.Name} says {message}";

            if (RootParent is Mobile mobile)
            {
                mobile.SendMessage(0x2B2, $"Crystal: {text}");
            }
            else if (RootParent is Item item)
            {
                item.PublicOverheadMessage(MessageType.Regular, 0x2B2, false, $"Crystal: {text}");
            }
            else
            {
                PublicOverheadMessage(MessageType.Regular, 0x2B2, false, text);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            from.Target = new InternalTarget(this);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.WriteItem(m_Sender);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Sender = reader.ReadItem<BroadcastCrystal>();
        }

        private class InternalTarget : Target
        {
            private readonly ReceiverCrystal m_Crystal;

            public InternalTarget(ReceiverCrystal crystal) : base(-1, false, TargetFlags.None) => m_Crystal = crystal;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!m_Crystal.IsAccessibleTo(from))
                {
                    return;
                }

                if (from.Map != m_Crystal.Map || !from.InRange(m_Crystal.GetWorldLocation(), 2))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                    return;
                }

                if (targeted == m_Crystal)
                {
                    if (m_Crystal.Active)
                    {
                        m_Crystal.Active = false;
                        from.SendLocalizedMessage(500672); // You turn the crystal off.
                    }
                    else
                    {
                        m_Crystal.Active = true;
                        from.SendLocalizedMessage(500673); // You turn the crystal on.
                    }
                }
                else if (targeted == from)
                {
                    if (m_Crystal.Sender != null)
                    {
                        m_Crystal.Sender = null;
                        from.SendLocalizedMessage(1010044); // You unlink the receiver crystal.
                    }
                    else
                    {
                        from.SendLocalizedMessage(1010045); // That receiver crystal is not linked.
                    }
                }
                else
                {
                    if (targeted is Item targItem && targItem.VerifyMove(from))
                    {
                        var info = CrystalRechargeInfo.Get(targItem.GetType());

                        if (info != null)
                        {
                            from.SendLocalizedMessage(500677); // This crystal cannot be recharged.
                            return;
                        }
                    }

                    from.SendLocalizedMessage(1010045); // That receiver crystal is not linked.
                }
            }
        }
    }
}
