using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System.ComponentModel;
using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeaponStatSynthesisPatcher
{
    public enum WeaponSkill
    {
        [SettingName("Onehanded")]
        [Tooltip("Weapons wielded with one hand")]
        OneHanded,

        [SettingName("Twohanded")]
        [Tooltip("Weapons wielded with two hands")]
        TwoHanded
    }


    public enum PluginFilter
    {
        [SettingName("All Plugins")]
        [Tooltip("Process all plugins in the load order")]
        AllPlugins,

        [SettingName("Exclude Plugins")]
        [Tooltip("Process all plugins except those in the exclude list")]
        ExcludePlugins,

        [SettingName("Include Plugins")]
        [Tooltip("Only process plugins in the include list")]
        IncludePlugins
    }


    public enum LogicOperator
    {
        [SettingName("AND")]
        [Tooltip("All criteria must match")]
        AND,

        [SettingName("OR")]
        [Tooltip("At least one criteria must match")]
        OR
    }


    public enum BoundWeaponParsing
    {
        [SettingName("From Settings")]
        [Tooltip("Use the damage values specified in settings")]
        FromSettings,

        [SettingName("Calculate From Mods")]
        [Tooltip("Calculate damage based on mod edits")]
        CalculateFromMods,

        [SettingName("Ignore Weapon")]
        [Tooltip("Skip bound weapons entirely")]
        IgnoreWeapon
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class WeaponCategory
    {
        [JsonProperty]
        [SettingName("Weapons (add more by adding panes)")]
        public Dictionary<string, WeaponSettings> Weapons { get; set; } = new();
    }

    public class WeaponAttributeEnablers
    {
        [SettingName("Enable Damage Edits")]
        [Tooltip("Enable or disable damage attribute edits")]
        [JsonProperty]
        public bool EnableDamage { get; set; }

        [SettingName("Enable Reach Edits")]
        [Tooltip("Enable or disable reach attribute edits")]
        [JsonProperty]
        public bool EnableReach { get; set; }

        [SettingName("Enable Speed Edits")]
        [Tooltip("Enable or disable speed attribute edits")]
        [JsonProperty]
        public bool EnableSpeed { get; set; }

        [SettingName("Enable Stagger Edits")]
        [Tooltip("Enable or disable stagger attribute edits")]
        [JsonProperty]
        public bool EnableStagger { get; set; }

        [SettingName("Enable Critical Damage Edits")]
        [Tooltip("Enable or disable critical damage edits (damage and multiplier)")]
        [JsonProperty]
        public bool EnableCriticalDamage { get; set; }

        [SettingName("Enable Critical Damage Chance Multiplier Edits")]
        [Tooltip("Enable or disable critical damage chance multiplier edits")]
        [JsonProperty]
        public bool EnableCriticalDamageChanceMultiplier { get; set; }
    }

    public class VariantCategory
    {
        [JsonProperty]
        [SettingName("Variants (add more by adding panes)")]
        [Tooltip("Variants of weapons identified by specific words in their names. Stats are applied on top of the weapon stats.")]
        public Dictionary<string, VariantSettings> Variants { get; set; } = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Settings
    {
        public Settings()
        {
            TryLoadBundledDefaults();
        }

        private void TryLoadBundledDefaults()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrWhiteSpace(assemblyDirectory))
            {
                return;
            }

            var defaultPath = Path.Combine(assemblyDirectory, "Data", "settings.json");
            if (!File.Exists(defaultPath))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(defaultPath);
                JsonConvert.PopulateObject(
                    json,
                    this,
                    new JsonSerializerSettings
                    {
                        ObjectCreationHandling = ObjectCreationHandling.Replace
                    });
            }
            catch
            {
                // Keep CLR defaults if bundled settings cannot be parsed.
            }
        }

        [SettingName("Debug Mode")]
        [Tooltip("Enable debug output")]
        [JsonProperty]
        public bool DebugMode { get; set; }

        [SettingName("Plugin Processing")]
        [Tooltip("Choose which plugins to process")]
        [JsonProperty(ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public PluginFilter PluginFilter { get; set; }

        [SettingName("Plugin Include List")]
        [Tooltip("List of plugins to include (semicolon separated)")]
        [JsonProperty]
        public string PluginIncludeList { get; set; } = string.Empty;

        [SettingName("Plugin Exclude List")]
        [Tooltip("List of plugins to exclude (semicolon separated)")]
        [JsonProperty]
        public string PluginExcludeList { get; set; } = string.Empty;

        [SettingName("Ignored Weapons Form Keys (semicolon separated)")]
        [Tooltip("List of weapon form keys to ignore (semicolon separated)")]
        [JsonProperty]
        public string IgnoredWeaponFormKeys { get; set; } = string.Empty;

        [SettingName("WACCF material tiers and stat changes")]
        [Tooltip("Enable support for WACCF material tiers and stat changes")]
        [JsonProperty]
        public bool WACCFMaterialTiers { get; set; }

        [SettingName("Enable support for 'The Restless Dead' nerfs")]
        [Tooltip("Reduce damage for Ancient Nord weapons. Enable this if you use the mod 'The Restless Dead'.")]
        [JsonProperty]
        public bool EnableRestlessDeadNerfs { get; set; }

        [SettingName("Stalhrim stagger bonus (default vanilla value is 0.1, WACCF value is 0)")]
        [Tooltip("Enable support for Stalhrim stagger bonus, if current stagger is greater than 0")]
        [JsonProperty]
        public float StalhrimStaggerBonus { get; set; }

        [SettingName("Stalhrim War Axe and Mace damage bonus (default vanilla value is 1, WACCF value is 0)")]
        [Tooltip("Enable support for Stalhrim War Axe and Mace damage bonus")]
        [JsonProperty]
        public int StalhrimDamageBonus { get; set; }

        [SettingName("Read bound weapon damage from settings, calculate mod offsets or ignore")]
        [Tooltip("Choose how to parse bound weapons")]
        [JsonProperty]
        public BoundWeaponParsing BoundWeaponParsing { get; set; }

        [SettingName("Weapon Attribute Enablers")]
        [Tooltip("Global enablers/disablers for weapon attribute edits")]
        [JsonProperty]
        public WeaponAttributeEnablers WeaponAttributeEnablers { get; set; } = new WeaponAttributeEnablers();

        [JsonProperty]
        public VariantCategory Variants { get; set; } = new VariantCategory();

        // Weapon categories
        [JsonProperty]
        public WeaponCategory Daggers { get; set; } = new WeaponCategory();

        [JsonProperty]
        public WeaponCategory OnehandedSwords { get; set; } = new WeaponCategory();

        [JsonProperty]
        public WeaponCategory OnehandedSpears { get; set; } = new WeaponCategory();

        [JsonProperty]
        public WeaponCategory OnehandedBluntWeapons { get; set; } = new WeaponCategory();

        [JsonProperty]
        public WeaponCategory OnehandedAxes { get; set; } = new WeaponCategory();

        [JsonProperty]
        public WeaponCategory TwohandedSwords { get; set; } = new WeaponCategory();

        [JsonProperty]
        public WeaponCategory TwohandedSpears { get; set; } = new WeaponCategory();

        [JsonProperty]
        public WeaponCategory TwohandedBluntWeapons { get; set; } = new WeaponCategory();

        [JsonProperty]
        public WeaponCategory TwohandedAxes { get; set; } = new WeaponCategory();

        [JsonProperty]
        public WeaponCategory Others { get; set; } = new WeaponCategory();

        public IEnumerable<WeaponCategory> GetAllWeaponCategories()
        {
            yield return Daggers;
            yield return OnehandedSwords;
            yield return OnehandedSpears;
            yield return OnehandedBluntWeapons;
            yield return OnehandedAxes;
            yield return TwohandedSwords;
            yield return TwohandedSpears;
            yield return TwohandedBluntWeapons;
            yield return TwohandedAxes;
            yield return Others;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class WeaponSettings
    {
        public WeaponSettings()
        {
            MatchLogicSettings = new MatchLogicSettings();
        }

        [SettingName("Enabled")]
        [Tooltip("Whether this weapon type is enabled")]
        [JsonProperty]
        public bool Enabled { get; set; }

        [SettingName("Damage")]
        [Tooltip("Base damage of the weapon")]
        [JsonProperty]
        public ushort Damage { get; set; }

        [SettingName("Bound weapon additional damage")]
        [Tooltip("Additional damage for bound weapons, added to the base damage")]
        [JsonProperty]
        public int BoundWeaponAdditionalDamage { get; set; }

        [SettingName("Bound Mystic Weapon additional damage")]
        [Tooltip("Additional damage for bound mystic weapons, added to the base damage")]
        [JsonProperty]
        public int BoundMysticWeaponAdditionalDamage { get; set; }

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

        [SettingName("Critical Damage Offset")]
        [Tooltip("Critical damage offset of the weapon. Default vanilla value is 0")]
        [JsonProperty]
        public float CriticalDamageOffset { get; set; }

        [SettingName("Critical Damage Chance Multiplier")]
        [Tooltip("Critical damage chance multiplier of the weapon. Default vanilla value is 1")]
        [JsonProperty]
        public float CriticalDamageChanceMultiplier { get; set; }

        [SettingName("Critical Damage Multiplier")]
        [Tooltip("Critical damage multiplier of the weapon. Default vanilla value is 1")]
        [JsonProperty]
        public float CriticalDamageMultiplier { get; set; }

        [SettingName("Match Logic")]
        [Tooltip("Weapon match logic")]
        [JsonProperty]
        public MatchLogicSettings MatchLogicSettings { get; set; }
    }

    public class VariantSettings
    {
        [SettingName("Has in keywords or in name")]
        [Tooltip("If not empty, the variant will be used. Semicolon separated list of words to search for in the weapon's keywords or name.")]
        [JsonProperty]
        public string NameIDs { get; set; } = string.Empty;

        [SettingName("Exclude if has this name or keyword")]
        [Tooltip("If not empty, the variant will be excluded if the weapon has any of these in its name or keywords. Semicolon separated list.")]
        [JsonProperty]
        public string ExcludeIDs { get; set; } = string.Empty;

        [SettingName("Skill")]
        [Tooltip("Skill required to use the weapon")]
        [JsonProperty]
        public WeaponSkill Skill { get; set; }

        [SettingName("Additional Damage Offset")]
        [Tooltip("Damage added to the base damage")]
        [JsonProperty]
        public int AdditionalDamage { get; set; }

        [SettingName("Additional Reach Offset")]
        [Tooltip("Reach offset added to the base reach")]
        [JsonProperty]
        public float AdditionalReach { get; set; }

        [SettingName("Additional Speed Offset")]
        [Tooltip("Speed offset added to the base speed")]
        [JsonProperty]
        public float AdditionalSpeed { get; set; }

        [SettingName("Additional Stagger Offset")]
        [Tooltip("Stagger offset added to the base stagger")]
        [JsonProperty]
        public float AdditionalStagger { get; set; }

        [SettingName("Additional Critical Damage Offset")]
        [Tooltip("Critical damage offset added to the base critical damage")]
        [JsonProperty]
        public float AdditionalCriticalDamageOffset { get; set; }

        [SettingName("Additional Critical Damage Chance Multiplier")]
        [Tooltip("Critical damage chance multiplier added to the base critical damage chance")]
        [JsonProperty]
        public float AdditionalCriticalDamageChanceMultiplier { get; set; }

        [SettingName("Additional Critical Damage Multiplier")]
        [Tooltip("Critical damage multiplier added to the base critical damage multiplier")]
        [JsonProperty]
        public float AdditionalCriticalDamageMultiplier { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MatchLogicSettings
    {
        [SettingName("Skill")]
        [Tooltip("Skill required to use the weapon")]
        [JsonProperty]
        public WeaponSkill Skill { get; set; }

        [SettingName("Has in name")]
        [Tooltip(
        "Identify which weapons to patch by name. Semicolon separated list."
        )]
        [JsonProperty]
        public string NamedIDs { get; set; } = string.Empty;

        [SettingName("AND/OR")]
        [Tooltip("Use name and/or keywords, blank input fields are ignored")]
        [JsonProperty]
        public LogicOperator SearchLogic { get; set; }

        [SettingName("Has keywords")]
        [Tooltip("Identify which weapons to patch by keywords. Semicolon separated list.")]
        [JsonProperty]
        public string KeywordIDs { get; set; } = string.Empty;
    }
}