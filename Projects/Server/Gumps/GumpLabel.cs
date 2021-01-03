using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpLabel : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("text");

        public GumpLabel(int x, int y, int hue, string text)
        {
            X = x;
            Y = y;
            Hue = hue;
            Text = text;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Hue { get; set; }

        public string Text { get; set; }

        public override string Compile(NetState ns) => $"{{ text {X} {Y} {Hue} {Parent.Intern(Text)} }}";
        public override string Compile(OrderedHashSet<string> strings) => $"{{ text {X} {Y} {Hue} {strings.GetOrAdd(Text)} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(Hue);
            disp.AppendLayout(Parent.Intern(Text));
        }

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(m_LayoutName);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(X.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Y.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Hue.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(strings.GetOrAdd(Text).ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
