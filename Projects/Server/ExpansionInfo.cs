using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Server.Json;

namespace Server
{
    public enum Expansion
    {
        None,
        T2A,
        UOR,
        UOTD,
        LBR,
        AOS,
        SE,
        ML,
        SA,
        HS,
        TOL,
        EJ
    }

    [Flags]
    public enum ClientFlags
    {
        None = 0x00000000,
        Felucca = 0x00000001,
        Trammel = 0x00000002,
        Ilshenar = 0x00000004,
        Malas = 0x00000008,
        Tokuno = 0x00000010,
        TerMur = 0x00000020,
        Unk1 = 0x00000040,
        Unk2 = 0x00000080,
        UOTD = 0x00000100
    }

    [Flags]
    public enum FeatureFlags
    {
        None = 0x00000000,
        T2A = 0x00000001,
        UOR = 0x00000002,
        UOTD = 0x00000004,
        LBR = 0x00000008,
        AOS = 0x00000010,
        SixthCharacterSlot = 0x00000020,
        SE = 0x00000040,
        ML = 0x00000080,
        EigthAge = 0x00000100,
        NinthAge = 0x00000200, /* Crystal/Shadow Custom House Tiles */
        TenthAge = 0x00000400,
        IncreasedStorage = 0x00000800, /* Increased Housing/Bank Storage */
        SeventhCharacterSlot = 0x00001000,
        RoleplayFaces = 0x00002000,
        TrialAccount = 0x00004000,
        LiveAccount = 0x00008000,
        SA = 0x00010000,
        HS = 0x00020000,
        Gothic = 0x00040000,
        Rustic = 0x00080000,
        Jungle = 0x00100000,
        Shadowguard = 0x00200000,
        TOL = 0x00400000,
        EJ = 0x00800000,

        ExpansionNone = None,
        ExpansionT2A = T2A,
        ExpansionUOR = ExpansionT2A | UOR,
        ExpansionUOTD = ExpansionUOR | UOTD,
        ExpansionLBR = ExpansionUOTD | LBR,
        ExpansionAOS = ExpansionLBR | AOS | LiveAccount,
        ExpansionSE = ExpansionAOS | SE,
        ExpansionML = ExpansionSE | ML | NinthAge,
        ExpansionSA = ExpansionML | SA | Gothic | Rustic,
        ExpansionHS = ExpansionSA | HS,
        ExpansionTOL = ExpansionHS | TOL | Jungle | Shadowguard,
        ExpansionEJ = ExpansionTOL | EJ
    }

    [Flags]
    public enum CharacterListFlags
    {
        None = 0x00000000,
        Unk1 = 0x00000001,
        OverwriteConfigButton = 0x00000002,
        OneCharacterSlot = 0x00000004,
        ContextMenus = 0x00000008,
        SlotLimit = 0x00000010,
        AOS = 0x00000020,
        SixthCharacterSlot = 0x00000040,
        SE = 0x00000080,
        ML = 0x00000100,
        Unk2 = 0x00000200,
        UO3DClientType = 0x00000400,
        Unk3 = 0x00000800,
        SeventhCharacterSlot = 0x00001000,
        Unk4 = 0x00002000,
        NewMovementSystem = 0x00004000, // Doesn't seem to be used on OSI
        NewFeluccaAreas = 0x00008000,

        ExpansionNone = ContextMenus,
        ExpansionT2A = ContextMenus,
        ExpansionUOR = ContextMenus,
        ExpansionUOTD = ContextMenus,
        ExpansionLBR = ContextMenus,
        ExpansionAOS = ContextMenus | AOS,
        ExpansionSE = ExpansionAOS | SE,
        ExpansionML = ExpansionSE | ML,
        ExpansionSA = ExpansionML,
        ExpansionHS = ExpansionSA,
        ExpansionTOL = ExpansionHS,
        ExpansionEJ = ExpansionTOL
    }

    [Flags]
    public enum HousingFlags
    {
        None = 0x0,
        AOS = 0x10,
        SE = 0x40,
        ML = 0x80,
        Crystal = 0x200,
        SA = 0x10000,
        HS = 0x20000,
        Gothic = 0x40000,
        Rustic = 0x80000,
        Jungle = 0x100000,
        Shadowguard = 0x200000,
        TOL = 0x400000,
        EJ = 0x800000,

        HousingAOS = AOS,
        HousingSE = HousingAOS | SE,
        HousingML = HousingSE | ML | Crystal,
        HousingSA = HousingML | SA | Gothic | Rustic,
        HousingHS = HousingSA | HS,
        HousingTOL = HousingHS | TOL | Jungle | Shadowguard,
        HousingEJ = HousingTOL | EJ
    }

    public class ExpansionInfo
    {
        static ExpansionInfo()
        {
            var path = Path.Combine(Core.BaseDirectory, "Data/expansion.json");
            var expansions = JsonConfig.Deserialize<List<ExpansionConfig>>(path);

            Table = new ExpansionInfo[expansions.Count];

            for (var i = 0; i < expansions.Count; i++)
            {
                var expansion = expansions[i];
                if (expansion.ClientVersion != null)
                    Table[i] = new ExpansionInfo(
                        i,
                        expansion.Name,
                        expansion.ClientVersion,
                        expansion.FeatureFlags,
                        expansion.CharacterListFlags,
                        expansion.HousingFlags
                    );
                else
                    Table[i] = new ExpansionInfo(
                        i,
                        expansion.Name,
                        expansion.ClientFlags ?? ClientFlags.None,
                        expansion.FeatureFlags,
                        expansion.CharacterListFlags,
                        expansion.HousingFlags
                    );
            }
        }

        public ExpansionInfo(
            int id,
            string name,
            ClientFlags clientFlags,
            FeatureFlags supportedFeatures,
            CharacterListFlags charListFlags,
            HousingFlags customHousingFlag
        ) : this(id, name, supportedFeatures, charListFlags, customHousingFlag) => ClientFlags = clientFlags;

        public ExpansionInfo(
            int id,
            string name,
            ClientVersion requiredClient,
            FeatureFlags supportedFeatures,
            CharacterListFlags charListFlags,
            HousingFlags customHousingFlag
        ) : this(id, name, supportedFeatures, charListFlags, customHousingFlag) => RequiredClient = requiredClient;

        private ExpansionInfo(
            int id,
            string name,
            FeatureFlags supportedFeatures,
            CharacterListFlags charListFlags,
            HousingFlags customHousingFlag
        )
        {
            ID = id;
            Name = name;

            SupportedFeatures = supportedFeatures;
            CharacterListFlags = charListFlags;
            CustomHousingFlag = customHousingFlag;
        }

        public static ExpansionInfo CoreExpansion => GetInfo(Core.Expansion);

        public static ExpansionInfo[] Table { get; }

        public int ID { get; }
        public string Name { get; set; }

        public ClientFlags ClientFlags { get; set; }
        public FeatureFlags SupportedFeatures { get; set; }
        public CharacterListFlags CharacterListFlags { get; set; }
        public ClientVersion RequiredClient { get; set; }
        public HousingFlags CustomHousingFlag { get; set; }

        public static FeatureFlags GetFeatures(Expansion ex)
        {
            var info = GetInfo(ex);

            if (info != null)
            {
                return info.SupportedFeatures;
            }

            return ex switch
            {
                Expansion.T2A  => FeatureFlags.ExpansionT2A,
                Expansion.UOR  => FeatureFlags.ExpansionUOR,
                Expansion.UOTD => FeatureFlags.ExpansionUOTD,
                Expansion.LBR  => FeatureFlags.ExpansionLBR,
                Expansion.AOS  => FeatureFlags.ExpansionAOS,
                Expansion.SE   => FeatureFlags.ExpansionSE,
                Expansion.ML   => FeatureFlags.ExpansionML,
                Expansion.SA   => FeatureFlags.ExpansionSA,
                Expansion.HS   => FeatureFlags.ExpansionHS,
                Expansion.TOL  => FeatureFlags.ExpansionTOL,
                Expansion.EJ   => FeatureFlags.EJ,
                _              => FeatureFlags.ExpansionNone
            };
        }

        public static ExpansionInfo GetInfo(Expansion ex) => GetInfo((int)ex);

        public static ExpansionInfo GetInfo(int ex)
        {
            var v = ex;

            if (v < 0 || v >= Table.Length)
            {
                v = 0;
            }

            return Table[v];
        }

        public override string ToString() => Name;
    }

    public record ExpansionConfig
    {
        public string Name { get; init; }

        [JsonConverter(typeof(ClientVersionConverter))]
        public ClientVersion? ClientVersion { get; init; }

        public ClientFlags? ClientFlags { get; init; }

        [JsonConverter(typeof(FlagsConverter<FeatureFlags>))]
        public FeatureFlags FeatureFlags { get; init; }

        [JsonConverter(typeof(FlagsConverter<CharacterListFlags>))]
        public CharacterListFlags CharacterListFlags { get; init; }

        [JsonConverter(typeof(FlagsConverter<HousingFlags>))]
        public HousingFlags HousingFlags { get; init; }
    }
}
