using System.Buffers;
using Server.Collections;

namespace Server.Gumps;

public abstract class GumpEntry
{
    private Gump m_Parent;

    public Gump Parent
    {
        get => m_Parent;
        set
        {
            if (m_Parent != value)
            {
                m_Parent?.Remove(this);

                m_Parent = value;

                m_Parent?.Add(this);
            }
        }
    }

    public abstract string Compile(OrderedHashSet<string> strings);

    // TODO: Replace OrderedHashSet with InsertOnlyHashSet, a copy of HashSet that is ReadOnly compatible, but includes
    // a public AddIfNotPresent function that returns the index of the element
    public abstract void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches);
}