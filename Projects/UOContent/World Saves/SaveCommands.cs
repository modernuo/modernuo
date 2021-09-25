using Server.Saves;

namespace Server.Commands
{
    public static class SaveCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("Save", AccessLevel.Administrator, Save_OnCommand);
            CommandSystem.Register("SetSaves", AccessLevel.Administrator, SetAutoSaves_OnCommand);
            CommandSystem.Register("AutoSave", AccessLevel.Administrator, SetAutoSaves_OnCommand);
            CommandSystem.Register("SaveFrequency", AccessLevel.Administrator, SetSaveFrequency_OnCommand);
            CommandSystem.Register("PruneArchives", AccessLevel.Administrator, PruneArchives_OnCommand);
            CommandSystem.Register("Archive", AccessLevel.Administrator, ArchiveLocally_OnCommand);
        }

        [Usage("Archive"), Description("Starts an async hourly archive.")]
        private static void ArchiveLocally_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Started hourly archive.");
            AutoArchive.ArchiveLocally();
        }

        [Usage("PruneArchives"), Description("Prunes archives folder.")]
        private static void PruneArchives_OnCommand(CommandEventArgs e)
        {
            AutoArchive.PruneLocalArchives();
        }

        [Usage("Save"), Description("Saves the world.")]
        private static void Save_OnCommand(CommandEventArgs e)
        {
            AutoSave.Save();
        }

        [Usage("AutoSave <on | off>"), Description("Enables or disables automatic shard saving.")]
        public static void SetAutoSaves_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1)
            {

                var enabled = AutoSave.SavesEnabled = e.GetBoolean(0);
                e.Mobile.SendMessage("Saves have been {0}.", enabled ? "enabled" : "disabled");
            }
            else
            {
                e.Mobile.SendMessage("Format: AutoSave <on | off>");
            }
        }

        [Usage("SaveFrequency <duration> [warning duration]"), Description("Sets the save frequency starting at midnight local to the server.")]
        public static void SetSaveFrequency_OnCommand(CommandEventArgs e)
        {
            if (e.Length < 1)
            {
                e.Mobile.SendMessage("Format: SaveFrequency <duration> [warning duration]");
                return;
            }

            var saveDelay = e.GetTimeSpan(0);
            var warningDelay = e.Length >= 2 ? e.GetTimeSpan(1) : AutoSave.Warning;

            AutoSave.ResetAutoSave(saveDelay, warningDelay);

            e.Mobile.SendMessage("Save frequency has been updated.");
        }
    }
}
