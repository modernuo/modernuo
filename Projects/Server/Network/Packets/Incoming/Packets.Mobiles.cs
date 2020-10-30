/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Packets.Mobiles.cs                                              *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network
{
    public static partial class Packets
    {
        public static void RenameRequest(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;
            var targ = World.FindMobile(reader.ReadUInt32());

            if (targ != null)
            {
                EventSink.InvokeRenameRequest(from, targ, reader.ReadAsciiSafe());
            }
        }

        public static void MobileNameRequest(this NetState state, CircularBufferReader reader)
        {
            var m = World.FindMobile(reader.ReadUInt32());

            if (m != null && Utility.InUpdateRange(state.Mobile, m) && state.Mobile.CanSee(m))
            {
                state.Send(new MobileName(m));
            }
        }

        public static void ProfileReq(this NetState state, CircularBufferReader reader)
        {
            int type = reader.ReadByte();
            Serial serial = reader.ReadUInt32();

            var beholder = state.Mobile;
            var beheld = World.FindMobile(serial);

            if (beheld == null)
            {
                return;
            }

            switch (type)
            {
                case 0x00: // display request
                    {
                        EventSink.InvokeProfileRequest(beholder, beheld);

                        break;
                    }
                case 0x01: // edit request
                    {
                        reader.ReadInt16(); // Skip
                        int length = reader.ReadUInt16();

                        if (length > 511)
                        {
                            return;
                        }

                        var text = reader.ReadBigUni(length);

                        EventSink.InvokeChangeProfileRequest(beholder, beheld, text);

                        break;
                    }
            }
        }
    }
}
