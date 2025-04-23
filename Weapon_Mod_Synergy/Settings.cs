using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System.ComponentModel;
using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weapon_Mod_Synergy
{
    public enum PluginFilter
    {
        AllPlugins,

        ExcludePlugins,

        IncludePlugins
    }

    public enum LogicOperator
    {
        AND,
        OR
    }

    public enum SpecialWeaponsMod {
        None,
        Mysticism,
        Artificer
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class WeaponCategory
    {
        [JsonProperty]
        [SettingName("Variants (add more by adding panes)")]
        public Dictionary<string, WeaponSettings> Weapons { get; set; } = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Settings
    {
        public Settings()
        {
            // Set default plugin filter
            PluginFilter = PluginFilter.IncludePlugins;

            // Set default WACCF material tiers setting
            WACCFMaterialTiers = false;

            // set the default stalhrim stagger bonus setting
            StalhrimStaggerBonus = 0.1f;

            // Set the default stalhrim damage bonus setting
            StalhrimDamageBonus = 1;

            // set default special wewapons mod
            SpecialWeaponsMod = SpecialWeaponsMod.None;

            // Initialize weapon categories
            Daggers = new WeaponCategory();
            OnehandedSwords = new WeaponCategory();
            OnehandedSpears = new WeaponCategory();
            OnehandedBluntWeapons = new WeaponCategory();
            OnehandedAxes = new WeaponCategory();
            TwohandedSwords = new WeaponCategory();
            TwohandedSpears = new WeaponCategory();
            TwohandedBluntWeapons = new WeaponCategory();
            TwohandedAxes = new WeaponCategory();
            Others = new WeaponCategory();

            // Initialize weapons in each category
            InitializeDaggers();
            InitializeOnehandedSwords();
            InitializeOnehandedSpears();
            InitializeOnehandedBluntWeapons();
            InitializeOnehandedAxes();
            InitializeTwohandedSwords();
            InitializeTwohandedSpears();
            InitializeTwohandedBluntWeapons();
            InitializeTwohandedAxes();
            InitializeOthers();
        }

        private void InitializeDaggers()
        {
            Daggers.Weapons["Dagger"] = new WeaponSettings { Damage = 4, Reach = 0.7f, Speed = 1.3f, Stagger = 0.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "-tanto;-claw", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeDagger" } };
            Daggers.Weapons["Tanto"] = new WeaponSettings { Damage = 4, Reach = 0.7f, Speed = 1.3f, Stagger = 0.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "tanto", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeDagger;-WeapTypeClaw" } };
            Daggers.Weapons["Claw"] = new WeaponSettings { Damage = 5, Reach = 0.7f, Speed = 1.2f, Stagger = 0.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "claw", KeywordIDs = "WeapTypeClaw" } };
        }

        private void InitializeOnehandedSwords()
        {
            OnehandedSwords.Weapons["Sword"] = new WeaponSettings { Damage = 7, Reach = 1.0f, Speed = 1.0f, Stagger = 0.75f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "-katana;-spear;-shortspear;-javelin;-scimitar;-akaviri sword;-blades sword", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeSword;-ShWeapTypeShortspear;-WeapTypeWhip;-WeapTypeKatana;-WeapTypeScimitar" } };
            OnehandedSwords.Weapons["Rapier"] = new WeaponSettings { Damage = 5, Reach = 1.1f, Speed = 1.15f, Stagger = 0.6f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "rapier", KeywordIDs = "WeapTypeRapier" } };
            OnehandedSwords.Weapons["Wakizashi"] = new WeaponSettings { Damage = 5, Reach = 0.9f, Speed = 1.215f, Stagger = 0.6f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "wakizashi;ninjato", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeSword" } };
            OnehandedSwords.Weapons["Shortsword"] = new WeaponSettings { Damage = 6, Reach = 0.9f, Speed = 1.15f, Stagger = 0.6f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "shortsword", KeywordIDs = "ShWeapTypeShortsword" } };
            OnehandedSwords.Weapons["Katana"] = new WeaponSettings { Damage = 6, Reach = 1.0f, Speed = 1.125f, Stagger = 0.75f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "katana;akaviri sword;blades sword", KeywordIDs = "WeapTypeKatana" } };
            OnehandedSwords.Weapons["Scimitar"] = new WeaponSettings { Damage = 6, Reach = 0.95f, Speed = 1.15f, Stagger = 0.7f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "scimitar", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeSword" } };
        }

        private void InitializeOnehandedSpears()
        {
            OnehandedSpears.Weapons["Javelin"] = new WeaponSettings { Damage = 5, Reach = 1.0f, Speed = 1.2f, Stagger = 0.5f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "javelin", KeywordIDs = "WeapTypeJavelin" } };
            OnehandedSpears.Weapons["Spear, onehanded"] = new WeaponSettings { Damage = 6, Reach = 1.3f, Speed = 1.1f, Stagger = 0.75f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "spear", KeywordIDs = "WeapTypeSpear" } };
            OnehandedSpears.Weapons["Shortspear"] = new WeaponSettings { Damage = 7, Reach = 1.2f, Speed = 0.9f, Stagger = 0.75f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "shortspear", KeywordIDs = "ShWeapTypeShortspear" } };
        }

        private void InitializeOnehandedBluntWeapons()
        {
            OnehandedBluntWeapons.Weapons["Mace"] = new WeaponSettings { Damage = 9, Reach = 1.0f, Speed = 0.8f, Stagger = 1.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "-club;-mallet;-hammer;-war pick", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeMace;-ShWeapTypeMaul" } };
            OnehandedBluntWeapons.Weapons["Club"] = new WeaponSettings { Damage = 7, Reach = 1.0f, Speed = 1.0f, Stagger = 0.9f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "club", KeywordIDs = "ShWeapTypeClub" } };
            OnehandedBluntWeapons.Weapons["Maul"] = new WeaponSettings { Damage = 12, Reach = 1.0f, Speed = 0.75f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "maul", KeywordIDs = "ShWeapTypeMaul" } };
        }

        private void InitializeOnehandedAxes()
        {
            OnehandedAxes.Weapons["War Axe"] = new WeaponSettings { Damage = 8, Reach = 1.0f, Speed = 0.9f, Stagger = 0.85f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "-hatchet", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeWarAxe;-ShWeapTypeHatchet" } };
            OnehandedAxes.Weapons["Hatchet"] = new WeaponSettings { Damage = 7, Reach = 0.85f, Speed = 1.1f, Stagger = 0.6f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "hatchet", KeywordIDs = "ShWeapTypeHatchet" } };
        }

        private void InitializeTwohandedSwords()
        {
            TwohandedSwords.Weapons["Greatsword"] = new WeaponSettings { Damage = 15, Reach = 1.3f, Speed = 0.75f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "-dai*katana;-nodachi;-glaive;-pike;-trident;-*spear", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeGreatsword;-WeapTypePike;-ShWeapTypeGlaive;-ShWeapTypeTrident;-ShWeapTypeSpear" } };
            TwohandedSwords.Weapons["Glaive"] = new WeaponSettings { Damage = 13, Reach = 1.6f, Speed = 0.8f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "glaive", KeywordIDs = "ShWeapTypeGlaive" } };
            TwohandedSwords.Weapons["DaiKatana"] = new WeaponSettings { Damage = 14, Reach = 1.3f, Speed = 0.85f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "dai*katana;nodachi", KeywordIDs = "" } };
        }

        private void InitializeTwohandedSpears()
        {
            TwohandedSpears.Weapons["Pike"] = new WeaponSettings { Damage = 12, Reach = 1.7f, Speed = 0.7f, Stagger = 1.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "pike", KeywordIDs = "WeapTypePike" } };
            TwohandedSpears.Weapons["Spear, twohanded"] = new WeaponSettings { Damage = 12, Reach = 1.6f, Speed = 0.8f, Stagger = 1.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "spear;half*pike", KeywordIDs = "ShWeapTypeSpear" } };
            TwohandedSpears.Weapons["Trident"] = new WeaponSettings { Damage = 14, Reach = 1.5f, Speed = 0.7f, Stagger = 1.15f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "trident", KeywordIDs = "ShWeapTypeTrident" } };
        }

        private void InitializeTwohandedBluntWeapons()
        {
            TwohandedBluntWeapons.Weapons["Warhammer"] = new WeaponSettings { Damage = 18, Reach = 1.3f, Speed = 0.6f, Stagger = 1.25f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "-maul;-*mace;-*staff", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeWarhammer" } };
            TwohandedBluntWeapons.Weapons["Quarterstaff, shorter"] = new WeaponSettings { Damage = 10, Reach = 1.2f, Speed = 1.1f, Stagger = 1.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "short*staff", KeywordIDs = "ShWeapTypeQuarterStaff" } };
            TwohandedBluntWeapons.Weapons["Quarterstaff"] = new WeaponSettings { Damage = 10, Reach = 1.2f, Speed = 1.1f, Stagger = 1.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "battle*staff;quarterstaff;-short*staff", KeywordIDs = "WeapTypeQtrStaff" } };
            TwohandedBluntWeapons.Weapons["LongMace"] = new WeaponSettings { Damage = 17, Reach = 1.3f, Speed = 0.65f, Stagger = 1.2f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "long*mace", KeywordIDs = "ShWeapTypeLongMace" } };
        }

        private void InitializeTwohandedAxes()
        {
            TwohandedAxes.Weapons["Battleaxe"] = new WeaponSettings { Damage = 16, Reach = 1.3f, Speed = 0.7f, Stagger = 1.15f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "-halberd", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeBattleaxe" } };
            TwohandedAxes.Weapons["Halberd, shorter"] = new WeaponSettings { Damage = 15, Reach = 1.45f, Speed = 0.75f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "", KeywordIDs = "ShWeapTypeHalberd;" } };
            TwohandedAxes.Weapons["Halberd"] = new WeaponSettings { Damage = 15, Reach = 1.55f, Speed = 0.65f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "halberd", KeywordIDs = "WeapTypeHalberd;-ShWeapTypeHalberd" } };
        }

        private void InitializeOthers()
        {
            Others.Weapons["Whip"] = new WeaponSettings { Damage = 6, Reach = 2.0f, Speed = 0.9f, Stagger = 0.4f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "whip", KeywordIDs = "WeapTypeWhip" } };
        }

        [SettingName("Plugin Processing")]
        [Tooltip("Choose which plugins to process")]
        public PluginFilter PluginFilter { get; set; }

        [SettingName("Plugin List (semicolon separated)")]
        [Tooltip("List of plugins to include or exclude (semicolon separated)")]
        public string PluginList { get; set; } = "Skyrim.esm;Dawnguard.esm;Dragonborn.esm;ccasvsse001-almsivi.esm;ccbgssse001-fish.esm;ccbgssse003-zombies.esl;ccbgssse005-goldbrand.esl;" +
        "ccbgssse006-stendarshammer.esl;ccbgssse007-chrysamere.esl;ccmtysse001-knightsofthenine.esl;ccbgssse018-shadowrend.esl;ccbgssse008-wraithguard.esl;ccffbsse001-imperialdragon.esl;" +
        "ccbgssse016-umbra.esm;ccbgssse031-advcyrus.esm;ccbgssse059-ba_dragonplate.esl;ccbgssse067-daedinv.esm;ccbgssse068-bloodfall.esl;ccbgssse069-contest.esl;ccvsvsse003-necroarts.esl;" +
        "ccbgssse025-advdsgs.esm;ccedhsse003-redguard.esl;cckrtsse001_altar.esl;ccbgssse006-stendarshammer.esl;" +
        "PrvtI_HeavyArmory.esp;KatanaCrafting.esp;NewArmoury.esp;SkyrimSpearMechanic.esp";

        [SettingName("Ignored Weapons Form Keys (semicolon separated)")]
        [Tooltip("List of weapon form keys to ignore (semicolon separated)")]
        public string IgnoredWeaponFormKeys { get; set; } = "02F2F4:Skyrim.esm;0E3C16:Skyrim.esm;0E7A31:Skyrim.esm;004D91:Hearthfires.esm;008801:ccbgssse019-staffofsheogorath.esl;009850;" +
        "ccbgssse020-graycowl.esl;02D854:ccvsvsse004-beafarmer.esl";

        [SettingName("WACCF material tiers")]
        [Tooltip("Enable support for WACCF material tiers")]
        public bool WACCFMaterialTiers { get; set; }

        [SettingName("Stalhrim stagger bonus (default vanilla value is 0.1, WACCF value is 0)")]
        [Tooltip("Enable support for Stalhrim stagger bonus, if current stagger is greater than 0")]
        public float StalhrimStaggerBonus { get; set; }

        [SettingName("Stalhrim War Axe and Mace damage bonus (default vanilla value is 1)")]
        [Tooltip("Enable support for Stalhrim War Axe and Mace damage bonus")]
        public int StalhrimDamageBonus { get; set; }

        [SettingName("Special Weapons Mod")]
        [Tooltip("Choose which special weapons mod to use")]
        public SpecialWeaponsMod SpecialWeaponsMod { get; set; }
/*
        [SettingName("Artificer")]
        [Tooltip("Honor Artificer changes")]
        public bool Artificer { get; set; } = true;

        [SettingName("Mysticism")]
        [Tooltip("Honor Mysticism changes to bound weapons")]
        public bool Mysticism { get; set; } = true;
*/
        // Weapon categories
        [JsonProperty]
        public WeaponCategory Daggers { get; set; }

        [JsonProperty]
        public WeaponCategory OnehandedSwords { get; set; }

        [JsonProperty]
        public WeaponCategory OnehandedSpears { get; set; }

        [JsonProperty]
        public WeaponCategory OnehandedBluntWeapons { get; set; }

        [JsonProperty]
        public WeaponCategory OnehandedAxes { get; set; }

        [JsonProperty]
        public WeaponCategory TwohandedSwords { get; set; }

        [JsonProperty]
        public WeaponCategory TwohandedSpears { get; set; }

        [JsonProperty]
        public WeaponCategory TwohandedBluntWeapons { get; set; }

        [JsonProperty]
        public WeaponCategory TwohandedAxes { get; set; }

        [JsonProperty]
        public WeaponCategory Others { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class WeaponSettings
    {
        public WeaponSettings()
        {
            MatchLogicSettings = new MatchLogicSettings();
            Enabled = true; // Default to enabled
        }

        [SettingName("Enabled")]
        [Tooltip("Whether this weapon type is enabled")]
        [JsonProperty]
        public bool Enabled { get; set; }

        [SettingName("Damage")]
        [Tooltip("Base damage of the weapon")]
        [JsonProperty]
        public ushort Damage { get; set; } = 7;

        [SettingName("Reach")]
        [Tooltip("Reach of the weapon")]
        [JsonProperty]
        public float Reach { get; set; } = 1.0f;

        [SettingName("Speed")]
        [Tooltip("Speed of the weapon")]
        [JsonProperty]
        public float Speed { get; set; } = 1.0f;

        [SettingName("Stagger")]
        [Tooltip("Stagger of the weapon")]
        [JsonProperty]
        public float Stagger { get; set; } = 0.75f;

        [SettingName("Critical Damage Offset")]
        [Tooltip("Critical damage offset of the weapon. Default vanilla value is 0")]
        [JsonProperty]
        public float CriticalDamageOffset { get; set; } = 0.0f;

        [SettingName("Critical Damage Chance Multiplier")]
        [Tooltip("Critical damage chance multiplier of the weapon. Default vanilla value is 1")]
        [JsonProperty]
        public float CriticalDamageChanceMultiplier { get; set; } = 1.0f;

        [SettingName("Critical Damage Multiplier")]
        [Tooltip("Critical damage multiplier of the weapon. Default vanilla value is 1")]
        [JsonProperty]
        public float CriticalDamageMultiplier { get; set; } = 1.0f;

        [SettingName("Match Logic")]
        [Tooltip("Weapon match logic")]
        [JsonProperty]
        public MatchLogicSettings MatchLogicSettings { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MatchLogicSettings
    {
        [SettingName("Skill")]
        [Tooltip("Skill required to use the weapon")]
        [JsonProperty]
        public WeaponSkill Skill { get; set; } = WeaponSkill.OneHanded;

        [SettingName("Has in name")]
        [Tooltip(
        "Identify which weapons to patch by name. " +
        "Enter semicolon‑separated tokens: " +
        "prefix '–' for must not contain; " +
        "no prefix for OR match; " +
        "use '*' for wildcard (e.g. 'dagg*');"
        )]
        [JsonProperty]
        public string NamedIDs { get; set; } = "";

        [SettingName("AND/OR")]
        [Tooltip("Use name and/or keywords, blank input fields are ignored")]
        [JsonProperty]
        public LogicOperator SearchLogic { get; set; } = LogicOperator.OR;

        [SettingName("Has keywords (semicolon separated)")]
        [Tooltip("Identify which weapons to patch by keywords. " +
        "Enter semicolon‑separated tokens: " +
        "prefix '–' for must not contain; " +
        "no prefix for OR match; " +
        "use '*' for wildcard (e.g. 'dagg*');")]
        [JsonProperty]
        public string KeywordIDs { get; set; } = "";
    }
}