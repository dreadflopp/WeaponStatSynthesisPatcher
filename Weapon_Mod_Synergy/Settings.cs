using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System.ComponentModel;
using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json;

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

    public class Settings
    {
        public Settings()
        {
            // Set default plugin filter
            PluginFilter = PluginFilter.IncludePlugins;

            // Set default WACCF material tiers setting
            WACCFMaterialTiers = false;

            // Set default weapon settings
            Dagger = new WeaponSettings { Damage = 4, Reach = 0.7f, Speed = 1.3f, Stagger = 0.0f, CriticalDamage = 2, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "-tanto;-claw", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeDagger" };
            Tanto = new WeaponSettings { Damage = 4, Reach = 0.7f, Speed = 1.3f, Stagger = 0.0f, CriticalDamage = 2, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "tanto", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeDagger;-WeapTypeClaw" };
            Claw = new WeaponSettings { Damage = 5, Reach = 0.7f, Speed = 1.2f, Stagger = 0.0f, CriticalDamage = 1, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "claw", KeywordIDs = "WeapTypeClaw" };
            Javelin = new WeaponSettings { Damage = 5, Reach = 1.0f, Speed = 1.2f, Stagger = 0.5f, CriticalDamage = 2, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "javelin", KeywordIDs = "WeapTypeJavelin" };
            Rapier = new WeaponSettings { Damage = 5, Reach = 1.1f, Speed = 1.15f, Stagger = 0.6f, CriticalDamage = 3, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "rapier", KeywordIDs = "WeapTypeRapier" };
            Wakizashi = new WeaponSettings { Damage = 5, Reach = 0.9f, Speed = 1.215f, Stagger = 0.6f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "wakizashi;ninjato", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeSword" };
            Shortsword = new WeaponSettings { Damage = 6, Reach = 0.9f, Speed = 1.15f, Stagger = 0.6f, CriticalDamage = 3, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "shortsword", KeywordIDs = "ShWeapTypeShortsword" };
            Spear1h = new WeaponSettings { Damage = 6, Reach = 1.3f, Speed = 1.1f, Stagger = 0.75f, CriticalDamage = 3, CriticalDamageMultiplier = 1.0f, UseFloorDivision = false, Skill = WeaponSkill.OneHanded, NamedIDs = "spear", KeywordIDs = "WeapTypeSpear" };
            Sword = new WeaponSettings { Damage = 7, Reach = 1.0f, Speed = 1.0f, Stagger = 0.75f, CriticalDamage = 3, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "-katana;-spear;-shortspear;-javelin;-scimitar", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeSword;-ShWeapTypeShortspear;-WeapTypeWhip;-WeapTypeKatana;-WeapTypeScimitar" };
            Scimitar = new WeaponSettings { Damage = 6, Reach = 0.95f, Speed = 1.15f, Stagger = 0.7f, CriticalDamage = 3, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "scimitar", KeywordIDs = "WeapTypeScimitar" };
            Katana = new WeaponSettings { Damage = 6, Reach = 1.0f, Speed = 1.125f, Stagger = 0.75f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "katana", KeywordIDs = "WeapTypeKatana" };
            Hatchet = new WeaponSettings { Damage = 7, Reach = 0.85f, Speed = 1.1f, Stagger = 0.6f, CriticalDamage = 3, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "hatchet", KeywordIDs = "ShWeapTypeHatchet" };
            Club = new WeaponSettings { Damage = 7, Reach = 1.0f, Speed = 1.0f, Stagger = 0.9f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "club", KeywordIDs = "ShWeapTypeClub" };
            Shortspear = new WeaponSettings { Damage = 7, Reach = 1.2f, Speed = 0.9f, Stagger = 0.75f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = false, Skill = WeaponSkill.OneHanded, NamedIDs = "shortspear", KeywordIDs = "ShWeapTypeShortspear" };
            Whip = new WeaponSettings { Damage = 6, Reach = 2.0f, Speed = 0.9f, Stagger = 0.4f, CriticalDamage = 1, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "whip", KeywordIDs = "WeapTypeWhip" };
            WarAxe = new WeaponSettings { Damage = 8, Reach = 1.0f, Speed = 0.9f, Stagger = 0.85f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "-hatchet", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeWarAxe;-ShWeapTypeHatchet" };
            Mace = new WeaponSettings { Damage = 9, Reach = 1.0f, Speed = 0.8f, Stagger = 1.0f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.OneHanded, NamedIDs = "-club;-mallet;-hammer;-war pick", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeMace;-ShWeapTypeMaul" };
            Shortstaff = new WeaponSettings { Damage = 10, Reach = 1.2f, Speed = 1.1f, Stagger = 1.0f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "short*staff", KeywordIDs = "ShWeapTypeQuarterStaff" };
            Quarterstaff = new WeaponSettings { Damage = 10, Reach = 1.2f, Speed = 1.1f, Stagger = 1.0f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "battle*staff;quarterstaff;-short*staff", KeywordIDs = "WeapTypeQtrStaff" };
            Maul = new WeaponSettings { Damage = 12, Reach = 1.0f, Speed = 0.75f, Stagger = 1.1f, CriticalDamage = 5, CriticalDamageMultiplier = 1.0f, UseFloorDivision = false, Skill = WeaponSkill.TwoHanded, NamedIDs = "maul", KeywordIDs = "ShWeapTypeMaul" };
            Spear2h = new WeaponSettings { Damage = 12, Reach = 1.6f, Speed = 0.8f, Stagger = 1.0f, CriticalDamage = 7, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "spear;half*pike", KeywordIDs = "ShWeapTypeSpear" };
            Pike = new WeaponSettings { Damage = 12, Reach = 1.7f, Speed = 0.7f, Stagger = 1.0f, CriticalDamage = 7, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "pike", KeywordIDs = "WeapTypePike" };
            Glaive = new WeaponSettings { Damage = 13, Reach = 1.6f, Speed = 0.8f, Stagger = 1.1f, CriticalDamage = 7, CriticalDamageMultiplier = 1.0f, UseFloorDivision = false, Skill = WeaponSkill.TwoHanded, NamedIDs = "glaive", KeywordIDs = "ShWeapTypeGlaive" };
            Trident = new WeaponSettings { Damage = 14, Reach = 1.5f, Speed = 0.7f, Stagger = 1.15f, CriticalDamage = 9, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "trident", KeywordIDs = "ShWeapTypeTrident" };
            Greatsword = new WeaponSettings { Damage = 15, Reach = 1.3f, Speed = 0.75f, Stagger = 1.1f, CriticalDamage = 7, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "-dai*katana;-nodachi;-glaive;-pike;-trident;-*spear", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeGreatsword;-WeapTypePike;-ShWeapTypeGlaive;-ShWeapTypeTrident;-ShWeapTypeSpear" };
            DaiKatana = new WeaponSettings { Damage = 14, Reach = 1.3f, Speed = 0.85f, Stagger = 1.1f, CriticalDamage = 8, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "dai*katana;nodachi", KeywordIDs = "" };
            ShortHalberd = new WeaponSettings { Damage = 15, Reach = 1.45f, Speed = 0.75f, Stagger = 1.1f, CriticalDamage = 8, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "", KeywordIDs = "ShWeapTypeHalberd;" };
            LongHalberd = new WeaponSettings { Damage = 15, Reach = 1.55f, Speed = 0.65f, Stagger = 1.1f, CriticalDamage = 8, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "halberd", KeywordIDs = "WeapTypeHalberd;-ShWeapTypeHalberd" };
            LongMace = new WeaponSettings { Damage = 17, Reach = 1.3f, Speed = 0.65f, Stagger = 1.2f, CriticalDamage = 8, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "long*mace", KeywordIDs = "ShWeapTypeLongMace" };
            Battleaxe = new WeaponSettings { Damage = 16, Reach = 1.3f, Speed = 0.7f, Stagger = 1.15f, CriticalDamage = 8, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "-halberd", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeBattleaxe" };
            Warhammer = new WeaponSettings { Damage = 18, Reach = 1.3f, Speed = 0.6f, Stagger = 1.25f, CriticalDamage = 9, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true, Skill = WeaponSkill.TwoHanded, NamedIDs = "-maul;-*mace;-*staff", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeWarhammer" };
        }

        [SettingName("Plugin Processing")]
        [Tooltip("Choose which plugins to process")]
        public PluginFilter PluginFilter { get; set; } = PluginFilter.IncludePlugins;

        [SettingName("Plugin List (semicolon separated)")]
        [Tooltip("List of plugins to include or exclude (semicolon separated)")]
        public string PluginList { get; set; } = "Skyrim.esm;Dawnguard.esm;Dragonborn.esm;PrvtI_HeavyArmory.esp;KatanaCrafting.esp;NewArmoury.esp;SkyrimSpearMechanic.esp";

        [SettingName("Ignored Weapons Form Keys (semicolon separated)")]
        [Tooltip("List of weapon form keys to ignore (semicolon separated)")]
        public string IgnoredWeaponFormKeys { get; set; } = "02F2F4:Skyrim.esm;0E3C16:Skyrim.esm;0E7A31:Skyrim.esm;004D91:Hearthfires.esm";

        [SettingName("WACCF material tiers")]
        [Tooltip("Enable support for WACCF material tiers")]
        public bool WACCFMaterialTiers { get; set; } = false;

        [SettingName("Artificer")]
        [Tooltip("Honor Artificer changes")]
        public bool Artificer { get; set; } = true;

        [SettingName("Mysticism")]
        [Tooltip("Honor Mysticism changes to bound weapons")]
        public bool Mysticism { get; set; } = true;

        // Weapon settings
        [SettingName("Dagger (iron)")]
        public WeaponSettings Dagger { get; set; }

        [SettingName("Tanto (iron)")]
        public WeaponSettings Tanto { get; set; }

        [SettingName("Claw (iron)")]
        public WeaponSettings Claw { get; set; }

        [SettingName("Javelin (iron)")]
        public WeaponSettings Javelin { get; set; }

        [SettingName("Rapier (iron)")]
        public WeaponSettings Rapier { get; set; }

        [SettingName("Wakizashi (iron)")]
        public WeaponSettings Wakizashi { get; set; }

        [SettingName("Shortsword (iron)")]
        public WeaponSettings Shortsword { get; set; }

        [SettingName("Spear one handed (iron)")]
        public WeaponSettings Spear1h { get; set; }

        [SettingName("Sword (iron)")]
        public WeaponSettings Sword { get; set; }

        [SettingName("Scimitar (iron)")]
        public WeaponSettings Scimitar { get; set; }

        [SettingName("Katana (iron)")]
        public WeaponSettings Katana { get; set; }

        [SettingName("Hatchet (iron)")]
        public WeaponSettings Hatchet { get; set; }

        [SettingName("Club (iron)")]
        public WeaponSettings Club { get; set; }

        [SettingName("Shortspear (iron)")]
        public WeaponSettings Shortspear { get; set; }

        [SettingName("Whip (iron)")]
        public WeaponSettings Whip { get; set; }

        [SettingName("War Axe (iron)")]
        public WeaponSettings WarAxe { get; set; }

        [SettingName("Mace (iron)")]
        public WeaponSettings Mace { get; set; }

        [SettingName("Quarterstaff, shorter (iron)")]
        public WeaponSettings Shortstaff { get; set; }

        [SettingName("Quarterstaff (iron)")]
        public WeaponSettings Quarterstaff { get; set; }

        [SettingName("Maul (iron)")]
        public WeaponSettings Maul { get; set; }

        [SettingName("Spear two handed (iron)")]
        public WeaponSettings Spear2h { get; set; }

        [SettingName("Pike (iron)")]
        public WeaponSettings Pike { get; set; }

        [SettingName("Glaive (iron)")]
        public WeaponSettings Glaive { get; set; }

        [SettingName("Trident (iron)")]
        public WeaponSettings Trident { get; set; }

        [SettingName("Greatsword (iron)")]
        public WeaponSettings Greatsword { get; set; }

        [SettingName("Dai-Katana (iron)")]
        public WeaponSettings DaiKatana { get; set; }

        [SettingName("Halberd, shorter (iron)")]
        public WeaponSettings ShortHalberd { get; set; }

        [SettingName("Halberd (iron)")]
        public WeaponSettings LongHalberd { get; set; }

        [SettingName("Long Mace (iron)")]
        public WeaponSettings LongMace { get; set; }

        [SettingName("Battleaxe (iron)")]
        public WeaponSettings Battleaxe { get; set; }

        [SettingName("Warhammer (iron)")]
        public WeaponSettings Warhammer { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class WeaponSettings
    {
        /// <summary>
        /// Identify which weapons to patch by name.  
        /// • Enter semicolon‑separated tokens.  
        /// • Prefix "+" → **must** contain  
        /// • Prefix "-" → **must not** contain  
        /// • No prefix → "OR" match  
        /// • Use "*" for simple wildcard (e.g. "dagg*")  
        /// • Use regex for more complex patterns (e.g. "^dagger$"). Regex start with re:
        /// • No other logic is allowed when using regex
        /// </summary>
        /// 
        [SettingName("Damage")]
        [Tooltip("Base damage of the weapon")]
        [JsonProperty]
        public ushort Damage { get; set; }

        [SettingName("Reach")]
        [Tooltip("Reach of the weapon")]
        [JsonProperty]
        public float Reach { get; set; }

        [SettingName("Speed")]
        [Tooltip("Speed of the weapon")]
        [JsonProperty]
        public float Speed { get; set; }

        [SettingName("Stagger")]
        [Tooltip("Stagger of the weapon")]
        [JsonProperty]
        public float Stagger { get; set; }

        [SettingName("Critical Damage")]
        [Tooltip("Critical damage of the weapon")]
        [JsonProperty]
        public ushort CriticalDamage { get; set; }

        [SettingName("Critical Damage Multiplier")]
        [Tooltip("Multiplier applied to critical damage")]
        [JsonProperty]
        public float CriticalDamageMultiplier { get; set; }

        [SettingName("Use Floor Division for critical damage calculation")]
        [Tooltip("If enabled, damage values will be rounded down to the nearest 10")]
        [JsonProperty]
        public bool UseFloorDivision { get; set; }

        [SettingName("Skill")]
        [Tooltip("Skill required to use the weapon")]
        [JsonProperty]
        public WeaponSkill Skill { get; set; } = WeaponSkill.OneHanded;

        [SettingName("Has in name")]
        [Tooltip(
        "Identify which weapons to patch by name. " +
        "Enter semicolon‑separated tokens: " +
        "prefix '+' for must contain; " +
        "prefix '–' for must not contain; " +
        "no prefix for OR match; " +
        "use '*' for wildcard (e.g. 'dagg*'); " +
        "use regex (prefix with 're:') for complex patterns (e.g. 're:^dagger$') — " +
        "no other logic allowed when using regex."
        )]
        [JsonProperty]
        public string NamedIDs { get; set; } = "";

        [SettingName("AND/OR")]
        [Tooltip("Use name and/or keywords, blank fields are ignored")]
        [JsonProperty]
        public LogicOperator SearchLogic { get; set; } = LogicOperator.OR;

        [SettingName("Has keywords (semicolon separated)")]
        [Tooltip("Identify which weapons to patch by keywords. " +
        "Enter semicolon‑separated tokens: " +
        "prefix '+' for must contain; " +
        "prefix '–' for must not contain; " +
        "no prefix for OR match; " +
        "use '*' for wildcard (e.g. 'dagg*'); " +
        "use regex (prefix with 're:') for complex patterns (e.g. 're:^weapTypeSword$') — " +
        "no other logic allowed when using regex.")]
        [JsonProperty]
        public string KeywordIDs { get; set; } = "";

        public WeaponSettings()
        {
            CriticalDamageMultiplier = 1.0f;
        }
    }
}