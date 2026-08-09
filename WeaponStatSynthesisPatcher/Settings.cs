using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System.ComponentModel;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
namespace WeaponStatSynthesisPatcher
{
    public enum WeaponSkill
    {
        [SettingName("Onehanded")]
        OneHanded,

        [SettingName("Twohanded")]
        TwoHanded
    }


    public enum PluginFilter
    {
        [SettingName("All Plugins")]
        AllPlugins,

        [SettingName("Exclude Plugins")]
        ExcludePlugins,

        [SettingName("Include Plugins")]
        IncludePlugins
    }


    public enum LogicOperator
    {
        [SettingName("AND")]
        AND,

        [SettingName("OR")]
        OR
    }


    public enum BoundWeaponParsing
    {
        [SettingName("From Settings")]
        FromSettings,

        [SettingName("Calculate From Mods")]
        CalculateFromMods,

        [SettingName("Ignore Weapon")]
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
        [JsonProperty]
        public bool EnableDamage { get; set; }

        [SettingName("Enable Reach Edits")]
        [JsonProperty]
        public bool EnableReach { get; set; }

        [SettingName("Enable Speed Edits")]
        [JsonProperty]
        public bool EnableSpeed { get; set; }

        [SettingName("Enable Stagger Edits")]
        [JsonProperty]
        public bool EnableStagger { get; set; }

        [SettingName("Enable Critical Damage Edits")]
        [JsonProperty]
        public bool EnableCriticalDamage { get; set; }

        [SettingName("Enable Critical Damage Chance Multiplier Edits")]
        [JsonProperty]
        public bool EnableCriticalDamageChanceMultiplier { get; set; }
    }

        [JsonObject(MemberSerialization.OptIn)]
    public class WeaponMaterialSetting
    {
        [SettingName("Title")]
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [SettingName("Enabled")]
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [SettingName("Identifiers")]
        [JsonProperty("identifiers")]
        public List<string> Identifiers { get; set; } = new();

        [SettingName("1H Damage Offset")]
        [JsonProperty("damage_offset_1h")]
        public int? DamageOffset1h { get; set; }

        [SettingName("2H Damage Offset")]
        [JsonProperty("damage_offset_2h")]
        public int? DamageOffset2h { get; set; }
    }

    public class VariantCategory
    {
        [JsonProperty]
        [SettingName("Variants (add more by adding panes)")]
        public Dictionary<string, VariantSettings> Variants { get; set; } = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Settings
    {
        public const int CurrentSettingsVersion = 1;

        public Settings() : this(loadBundledDefaults: true)
        {
        }

        internal Settings(bool loadBundledDefaults)
        {
            if (loadBundledDefaults)
            {
                TryLoadBundledDefaults();
                EnsureWeaponMaterialsInitialized();
            }
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

        [Browsable(false)]
        [JsonProperty(Order = -100)]
        public int SettingsVersion { get; set; } = CurrentSettingsVersion;

        [SettingName("Debug Mode")]
        [JsonProperty]
        public bool DebugMode { get; set; }

        [SettingName("Plugin Processing")]
        [JsonProperty(ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public PluginFilter PluginFilter { get; set; }

        [SettingName("Plugin Include List")]
        [JsonProperty]
        public string PluginIncludeList { get; set; } = string.Empty;

        [SettingName("Plugin Exclude List")]
        [JsonProperty]
        public string PluginExcludeList { get; set; } = string.Empty;

        [SettingName("Ignored Weapons Form Keys (semicolon separated)")]
        [JsonProperty]
        public string IgnoredWeaponFormKeys { get; set; } = string.Empty;

        [SettingName("Weapon Materials")]
        [JsonProperty("WeaponMaterials")]
        public List<WeaponMaterialSetting> WeaponMaterials { get; set; } = new();

        [SettingName("Stalhrim stagger bonus (default vanilla value is 0.1, WACCF value is 0)")]
        [JsonProperty]
        public float StalhrimStaggerBonus { get; set; }

        [SettingName("Stalhrim War Axe and Mace damage bonus (default vanilla value is 1, WACCF value is 0)")]
        [JsonProperty]
        public int StalhrimDamageBonus { get; set; }

        [SettingName("Read bound weapon damage from settings, calculate mod offsets or ignore")]
        [JsonProperty]
        public BoundWeaponParsing BoundWeaponParsing { get; set; }

        [SettingName("Weapon Attribute Enablers")]
        [JsonProperty]
        public WeaponAttributeEnablers WeaponAttributeEnablers { get; set; } = new WeaponAttributeEnablers();

        [JsonProperty]
        public VariantCategory Variants { get; set; } = new VariantCategory();

        [JsonProperty("Unique Weapons")]
        public List<SpecialWeaponData> UniqueWeapons { get; set; } = new();

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

        public void EnsureDefaultWeaponMaterialsPresent()
        {
            EnsureWeaponMaterialsInitialized();

            var defaultMaterials = LoadBundledWeaponMaterials();
            if (defaultMaterials.Count == 0)
            {
                return;
            }

            var existingTitles = WeaponMaterials
                .Where(m => !string.IsNullOrWhiteSpace(m.Title))
                .Select(m => m.Title)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var material in defaultMaterials)
            {
                if (existingTitles.Add(material.Title))
                {
                    WeaponMaterials.Add(material);
                }
            }
        }

        private void EnsureWeaponMaterialsInitialized()
        {
            if (WeaponMaterials.Count > 0)
            {
                return;
            }

            WeaponMaterials = LoadBundledWeaponMaterials();
        }

        public static List<WeaponMaterialSetting> LoadBundledWeaponMaterials()
        {
            try
            {
                var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrWhiteSpace(assemblyDirectory))
                {
                    return new List<WeaponMaterialSetting>();
                }

                var defaultPath = Path.Combine(assemblyDirectory, "InternalData", "material_data.json");
                if (!File.Exists(defaultPath))
                {
                    return new List<WeaponMaterialSetting>();
                }

                var json = File.ReadAllText(defaultPath);
                var materials = JsonConvert.DeserializeObject<List<WeaponMaterialSetting>>(json);
                return materials?.Select(CloneMaterial).ToList() ?? new List<WeaponMaterialSetting>();
            }
            catch
            {
                return new List<WeaponMaterialSetting>();
            }
        }

        private static WeaponMaterialSetting CloneMaterial(WeaponMaterialSetting source)
        {
            return new WeaponMaterialSetting
            {
                Title = source.Title,
                Enabled = source.Enabled,
                Identifiers = source.Identifiers?.ToList() ?? new List<string>(),
                DamageOffset1h = source.DamageOffset1h,
                DamageOffset2h = source.DamageOffset2h
            };
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
        [JsonProperty]
        public bool Enabled { get; set; }

        [SettingName("Damage")]
        [JsonProperty]
        public ushort Damage { get; set; }

        [SettingName("Bound weapon additional damage")]
        [JsonProperty]
        public int BoundWeaponAdditionalDamage { get; set; }

        [SettingName("Bound Mystic Weapon additional damage")]
        [JsonProperty]
        public int BoundMysticWeaponAdditionalDamage { get; set; }

        [SettingName("Reach")]
        [JsonProperty]
        public float Reach { get; set; }

        [SettingName("Speed")]
        [JsonProperty]
        public float Speed { get; set; }

        [SettingName("Stagger")]
        [JsonProperty]
        public float Stagger { get; set; }

        [SettingName("Critical Damage Offset")]
        [JsonProperty]
        public float CriticalDamageOffset { get; set; }

        [SettingName("Critical Damage Chance Multiplier")]
        [JsonProperty]
        public float CriticalDamageChanceMultiplier { get; set; }

        [SettingName("Critical Damage Multiplier")]
        [JsonProperty]
        public float CriticalDamageMultiplier { get; set; }

        [SettingName("Match Logic")]
        [JsonProperty]
        public MatchLogicSettings MatchLogicSettings { get; set; }
    }

    public class VariantSettings
    {
        [SettingName("Has in keywords or in name")]
        [JsonProperty]
        public string NameIDs { get; set; } = string.Empty;

        [SettingName("Exclude if has this name or keyword")]
        [JsonProperty]
        public string ExcludeIDs { get; set; } = string.Empty;

        [SettingName("Skill")]
        [JsonProperty]
        public WeaponSkill Skill { get; set; }

        [SettingName("Additional Damage Offset")]
        [JsonProperty]
        public int AdditionalDamage { get; set; }

        [SettingName("Damage Multiplier")]
        [JsonProperty]
        public decimal DamageMultiplier { get; set; } = 1.0m;

        [SettingName("Additional Reach Offset")]
        [JsonProperty]
        public float AdditionalReach { get; set; }

        [SettingName("Reach Multiplier")]
        [JsonProperty]
        public decimal ReachMultiplier { get; set; } = 1.0m;

        [SettingName("Additional Speed Offset")]
        [JsonProperty]
        public float AdditionalSpeed { get; set; }

        [SettingName("Speed Multiplier")]
        [JsonProperty]
        public decimal SpeedMultiplier { get; set; } = 1.0m;

        [SettingName("Additional Stagger Offset")]
        [JsonProperty]
        public float AdditionalStagger { get; set; }

        [SettingName("Stagger Multiplier")]
        [JsonProperty]
        public decimal StaggerMultiplier { get; set; } = 1.0m;

        [SettingName("Additional Critical Damage Offset")]
        [JsonProperty]
        public float AdditionalCriticalDamageOffset { get; set; }

        [SettingName("Critical Damage Offset Multiplier")]
        [JsonProperty]
        public decimal CriticalDamageOffsetMultiplier { get; set; } = 1.0m;

        [SettingName("Additional Critical Damage Chance Multiplier")]
        [JsonProperty]
        public float AdditionalCriticalDamageChanceMultiplier { get; set; }

        [SettingName("Critical Chance Multiplier Multiplier")]
        [JsonProperty]
        public decimal CriticalDamageChanceMultiplierMultiplier { get; set; } = 1.0m;

        [SettingName("Additional Critical Damage Multiplier")]
        [JsonProperty]
        public float AdditionalCriticalDamageMultiplier { get; set; }

        [SettingName("Critical Damage Multiplier Multiplier")]
        [JsonProperty]
        public decimal CriticalDamageMultiplierMultiplier { get; set; } = 1.0m;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MatchLogicSettings
    {
        [SettingName("Skill")]
        [JsonProperty]
        public WeaponSkill Skill { get; set; }

        [SettingName("Has in name")]
        [JsonProperty]
        public string NamedIDs { get; set; } = string.Empty;

        [SettingName("AND/OR")]
        [JsonProperty]
        public LogicOperator SearchLogic { get; set; }

        [SettingName("Has keywords")]
        [JsonProperty]
        public string KeywordIDs { get; set; } = string.Empty;
    }
}
