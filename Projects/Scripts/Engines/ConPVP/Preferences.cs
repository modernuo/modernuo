using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP
{
  public class PreferencesController : Item
  {
    [Constructible]
    public PreferencesController() : base(0x1B7A)
    {
      Visible = false;
      Movable = false;

      Preferences = new Preferences();

      if (Preferences.Instance == null)
        Preferences.Instance = Preferences;
      else
        Delete();
    }

    public PreferencesController(Serial serial) : base(serial)
    {
    }

    [CommandProperty( AccessLevel.Administrator )]
    public Preferences Preferences{ get; private set; }

    public override string DefaultName => "preferences controller";

    public override void Delete()
    {
      if (Preferences.Instance != Preferences)
        base.Delete();
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);

      Preferences.Serialize(writer);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 0:
        {
          Preferences = new Preferences(reader);
          Preferences.Instance = Preferences;
          break;
        }
      }
    }
  }

  public class Preferences
  {
    private Dictionary<Mobile, PreferencesEntry> m_Table;

    public Preferences()
    {
      m_Table = new Dictionary<Mobile, PreferencesEntry>();
      Entries = new List<PreferencesEntry>();
    }

    public Preferences(GenericReader reader)
    {
      int version = reader.ReadEncodedInt();

      switch (version)
      {
        case 0:
        {
          int count = reader.ReadEncodedInt();

          m_Table = new Dictionary<Mobile, PreferencesEntry>(count);
          Entries = new List<PreferencesEntry>(count);

          for (int i = 0; i < count; ++i)
          {
            PreferencesEntry entry = new PreferencesEntry(reader, version);

            if (entry.Mobile != null)
            {
              m_Table[entry.Mobile] = entry;
              Entries.Add(entry);
            }
          }

          break;
        }
      }
    }

    public List<PreferencesEntry> Entries{ get; }

    public static Preferences Instance{ get; set; }

    public PreferencesEntry Find(Mobile mob)
    {
      if (m_Table.TryGetValue(mob, out PreferencesEntry entry))
      {
        m_Table[mob] = entry = new PreferencesEntry(mob);
        Entries.Add(entry);
      }

      return entry;
    }

    public void Serialize(GenericWriter writer)
    {
      writer.WriteEncodedInt(0); // version;

      writer.WriteEncodedInt(Entries.Count);

      for (int i = 0; i < Entries.Count; ++i)
        Entries[i].Serialize(writer);
    }
  }

  public class PreferencesEntry
  {
    public PreferencesEntry(Mobile mob)
    {
      Mobile = mob;
      Disliked = new List<string>();
    }

    public PreferencesEntry(GenericReader reader, int version)
    {
      switch (version)
      {
        case 0:
        {
          Mobile = reader.ReadMobile();

          int count = reader.ReadEncodedInt();

          Disliked = new List<string>(count);

          for (int i = 0; i < count; ++i)
            Disliked.Add(reader.ReadString());

          break;
        }
      }
    }

    public Mobile Mobile{ get; }

    public List<string> Disliked{ get; }

    public void Serialize(GenericWriter writer)
    {
      writer.Write(Mobile);

      writer.WriteEncodedInt(Disliked.Count);

      for (int i = 0; i < Disliked.Count; ++i)
        writer.Write(Disliked[i]);
    }
  }

  public class PreferencesGump : Gump
  {
    private int m_ColumnX = 12;
    private PreferencesEntry m_Entry;

    public PreferencesGump(Mobile from, Preferences prefs) : base(50, 50)
    {
      m_Entry = prefs.Find(from);

      if (m_Entry == null)
        return;

      List<Arena> arenas = Arena.Arenas;

      AddPage(0);

      int height = 12 + 20 + arenas.Count * 31 + 24 + 12;

      AddBackground(0, 0, 499 + 40 - 365, height, 0x2436);

      for (int i = 1; i < arenas.Count; i += 2)
        AddImageTiled(12, 32 + i * 31, 475 + 40 - 365, 30, 0x2430);

      AddAlphaRegion(10, 10, 479 + 40 - 365, height - 20);

      AddColumnHeader(35, null);
      AddColumnHeader(115, "Arena");

      AddButton(499 + 40 - 365 - 12 - 63 - 4 - 63, height - 12 - 24, 247, 248, 1);
      AddButton(499 + 40 - 365 - 12 - 63, height - 12 - 24, 241, 242, 2);

      for (int i = 0; i < arenas.Count; ++i)
      {
        Arena ar = arenas[i];

        string name = ar.Name ?? "(no name)";

        int x = 12;
        int y = 32 + i * 31;

        int color = 0xCCFFCC;

        AddCheck(x + 3, y + 1, 9730, 9727, m_Entry.Disliked.Contains(name), i);
        x += 35;

        AddBorderedText(x + 5, y + 5, 115 - 5, name, color, 0);
      }
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
      if (m_Entry == null)
        return;

      if (info.ButtonID != 1)
        return;

      m_Entry.Disliked.Clear();

      List<Arena> arenas = Arena.Arenas;

      for (int i = 0; i < info.Switches.Length; ++i)
      {
        int idx = info.Switches[i];

        if (idx >= 0 && idx < arenas.Count)
          m_Entry.Disliked.Add(arenas[idx].Name);
      }
    }

    public string Center(string text) => $"<CENTER>{text}</CENTER>";

    public string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

    private void AddBorderedText(int x, int y, int width, string text, int color, int borderColor)
    {
      AddColoredText(x, y, width, text, color);
    }

    private void AddColoredText(int x, int y, int width, string text, int color)
    {
      if (color == 0)
        AddHtml(x, y, width, 20, text);
      else
        AddHtml(x, y, width, 20, Color(text, color));
    }

    private void AddColumnHeader(int width, string name)
    {
      AddBackground(m_ColumnX, 12, width, 20, 0x242C);
      AddImageTiled(m_ColumnX + 2, 14, width - 4, 16, 0x2430);

      if (name != null)
        AddBorderedText(m_ColumnX, 13, width, Center(name), 0xFFFFFF, 0);

      m_ColumnX += width;
    }
  }
}
