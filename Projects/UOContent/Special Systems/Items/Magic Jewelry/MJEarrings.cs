#region copyright
//Copyright (C) 2021  3HMonkey

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//any later version.
//
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.
//
//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion


using Server.Spells;
using Server.Spells.Third;
using System;
using System.Collections;

namespace Server.Items
{
    ///////////////////////////////////////////////////earrings of agility///////////////////////////////////////////////////////////
    public class MJEofAgility : BaseMJE
    {
        [Constructible]
        public MJEofAgility() : base(MJEEffect.Agility, 9, 21)
        {
        }

        public MJEofAgility(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        public override void OnJBUse(Mobile from)
        {
            SpellHelper.Turn(from, from);

            SpellHelper.AddStatBonus(from, from, StatType.Dex);

            from.FixedParticles(0x375A, 10, 15, 5010, EffectLayer.Waist);
            from.PlaySound(0x28E);

            int percentage = (int)(SpellHelper.GetOffsetScalar(from, from, false) * 100);
            TimeSpan length = SpellHelper.GetDuration(from, from);

            BuffInfo.AddBuff(from, new BuffInfo(BuffIcon.Agility, 1075841, length, from, percentage.ToString()));

            OnFinish(from);
        }
    }
    //////////////////////////////////////////////////////earrings of cunning////////////////////////////////////////////////////////
    public class MJEofCunning : BaseMJE
    {
        [Constructible]
        public MJEofCunning() : base(MJEEffect.Cunning, 9, 21)
        {
        }

        public MJEofCunning(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        public override void OnJBUse(Mobile from)
        {
            SpellHelper.Turn(from, from);

            SpellHelper.AddStatBonus(from, from, StatType.Int);

            from.FixedParticles(0x375A, 10, 15, 5011, EffectLayer.Head);
            from.PlaySound(0x1EB);

            int percentage = (int)(SpellHelper.GetOffsetScalar(from, from, false) * 100);
            TimeSpan length = SpellHelper.GetDuration(from, from);

            BuffInfo.AddBuff(from, new BuffInfo(BuffIcon.Cunning, 1075843, length, from, percentage.ToString()));

            OnFinish(from);
        }
    }
    //////////////////////////////////////////////////////earrings of strength///////////////////////////////////////////////////////
    public class MJEofStrength : BaseMJE
    {
        [Constructible]
        public MJEofStrength() : base(MJEEffect.Strength, 9, 21)
        {
        }

        public MJEofStrength(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        public override void OnJBUse(Mobile from)
        {
            SpellHelper.Turn(from, from);

            SpellHelper.AddStatBonus(from, from, StatType.Str);

            from.FixedParticles(0x375A, 10, 15, 5017, EffectLayer.Waist);
            from.PlaySound(0x1EE);

            int percentage = (int)(SpellHelper.GetOffsetScalar(from, from, false) * 100);
            TimeSpan length = SpellHelper.GetDuration(from, from);

            BuffInfo.AddBuff(from, new BuffInfo(BuffIcon.Strength, 1075845, length, from, percentage.ToString()));

            OnFinish(from);
        }
    }
    ////////////////////////////////////////////////////earrings of teleport/////////////////////////////////////////////////////////
    public class MJEofTeleport : BaseMJE
    {
        [Constructible]
        public MJEofTeleport() : base(MJEEffect.Teleport, 9, 21)
        {
        }

        public MJEofTeleport(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        public override void OnJBUse(Mobile from)
        {
            Cast(new TeleportSpell(from, this));
            OnFinish(from);
        }
    }
    ///////////////////////////////////////////////////////earrings of blessing//////////////////////////////////////////////////////
    public class MJEofBless : BaseMJE
    {
        [Constructible]
        public MJEofBless() : base(MJEEffect.Bless, 9, 21)
        {
        }

        public MJEofBless(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        public override void OnJBUse(Mobile from)
        {
            SpellHelper.Turn(from, from);

            SpellHelper.AddStatBonus(from, from, StatType.Str); SpellHelper.DisableSkillCheck = true;
            SpellHelper.AddStatBonus(from, from, StatType.Dex);
            SpellHelper.AddStatBonus(from, from, StatType.Int); SpellHelper.DisableSkillCheck = false;

            from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
            from.PlaySound(0x1EA);

            int percentage = (int)(SpellHelper.GetOffsetScalar(from, from, true) * 100);
            TimeSpan length = SpellHelper.GetDuration(from, from);

            string args = string.Format("{0}\t{1}\t{2}", percentage, percentage, percentage);

            BuffInfo.AddBuff(from, new BuffInfo(BuffIcon.Bless, 1075847, 1075848, length, from, args.ToString()));

            OnFinish(from);
        }
    }
    ////////////////////////////////////////////////earrings of invisibility/////////////////////////////////////////////////////////
    public class MJEofInvisibility : BaseMJE
    {
        [Constructible]
        public MJEofInvisibility() : base(MJEEffect.Invisibility, 9, 21)
        {
        }

        public MJEofInvisibility(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }


        public override void OnJBUse(Mobile from)
        {
            if (from.Hidden != true)
            {
                SpellHelper.Turn(from, from);

                from.Hidden = true;

                BuffInfo.RemoveBuff(from, BuffIcon.HidingAndOrStealth);
                BuffInfo.AddBuff(from, new BuffInfo(BuffIcon.Invisibility, 1075825));

                RemoveTimer(from);

                TimeSpan duration = TimeSpan.FromSeconds(120);

                Timer t = new InternalTimer(from, duration);

                m_Table[from] = t;

                t.Start();

                OnFinish(from);
            }
            else
                from.SendMessage("You are already hidden.");
            return;

        }
        private static Hashtable m_Table = new Hashtable();

        public static bool HasTimer(Mobile from)
        {
            return m_Table[from] != null;
        }

        public static void RemoveTimer(Mobile from)
        {
            Timer t = (Timer)m_Table[from];

            if (t != null)
            {
                t.Stop();
                m_Table.Remove(from);
            }
        }

        private class InternalTimer : Timer
        {
            private Mobile m_Mobile;

            public InternalTimer(Mobile from, TimeSpan duration)
                : base(duration)
            {
                Priority = TimerPriority.OneSecond;
                m_Mobile = from;
            }

            protected override void OnTick()
            {
                m_Mobile.RevealingAction();
                RemoveTimer(m_Mobile);
            }
        }
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
