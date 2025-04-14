using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System.ComponentModel;
using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json;

namespace Weapon_Mod_Synergy
{
    public enum PluginFilter
    {
        [SettingName("All Plugins")]
        [Tooltip("Process weapons from all plugins")]
        AllPlugins,

        [SettingName("Exclude Listed Plugins")]
        [Tooltip("Process weapons from all plugins except the specified ones")]
        ExcludePlugins,

        [SettingName("Include Listed Plugins Only")]
        [Tooltip("Only process weapons from the specified plugins")]
        IncludePlugins
    }

    public class Settings
    {
        public Settings()
        {
            // Set default plugin filter
            PluginFilter = PluginFilter.IncludePlugins;
            
            // Set default plugin list
            PluginList = "PrvtI_HeavyArmory.esp;KatanaCrafting.esp;NewArmoury.esp;SkyrimSpearMechanic.esp";
            
            // Set default WACCF material tiers setting
            WACCFMaterialTiers = false;
            
            // Set default weapon processing flags
            ProcessDaggers = true;
            ProcessTantos = true;
            ProcessClaws = true;
            ProcessJavelins = true;
            ProcessRapiers = true;
            ProcessWakizashis = true;
            ProcessShortswords = true;
            ProcessSpears1h = true;
            ProcessSwords = true;
            ProcessKatanas = true;
            ProcessHatchets = true;
            ProcessClubs = true;
            ProcessShortspears = true;
            ProcessWhips = true;
            ProcessWarAxes = true;
            ProcessMaces = true;
            ProcessHeavyArmoryQuarterstaffs = true;
            ProcessAnimatedArmouryQuarterstaffs = true;
            ProcessMauls = true;
            ProcessSpears2h = true;
            ProcessPikes = true;
            ProcessGlaives = true;
            ProcessTridents = true;
            ProcessGreatswords = true;
            ProcessDaiKatanas = true;
            ProcessHeavyArmoryHalberds = true;
            ProcessAnimatedArmouryHalberds = true;
            ProcessLongMaces = true;
            ProcessBattleaxes = true;
            ProcessWarhammers = true;
            
            // Set default weapon settings
            Dagger = new WeaponSettings { Damage = 4, Reach = 0.7f, Speed = 1.3f, Stagger = 0.0f, CriticalDamage = 2, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Tanto = new WeaponSettings { Damage = 4, Reach = 0.7f, Speed = 1.3f, Stagger = 0.0f, CriticalDamage = 2, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Claw = new WeaponSettings { Damage = 5, Reach = 0.7f, Speed = 1.2f, Stagger = 0.0f, CriticalDamage = 1, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Javelin = new WeaponSettings { Damage = 5, Reach = 1.0f, Speed = 1.2f, Stagger = 0.5f, CriticalDamage = 2, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Rapier = new WeaponSettings { Damage = 5, Reach = 1.1f, Speed = 1.15f, Stagger = 0.6f, CriticalDamage = 3, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Wakizashi = new WeaponSettings { Damage = 5, Reach = 0.9f, Speed = 1.215f, Stagger = 0.6f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Shortsword = new WeaponSettings { Damage = 6, Reach = 0.9f, Speed = 1.15f, Stagger = 0.6f, CriticalDamage = 3, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Spear1h = new WeaponSettings { Damage = 6, Reach = 1.3f, Speed = 1.1f, Stagger = 0.75f, CriticalDamage = 3, CriticalDamageMultiplier = 1.0f, UseFloorDivision = false };
            Sword = new WeaponSettings { Damage = 7, Reach = 1.0f, Speed = 1.0f, Stagger = 0.75f, CriticalDamage = 3, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Katana = new WeaponSettings { Damage = 6, Reach = 1.0f, Speed = 1.125f, Stagger = 0.75f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Hatchet = new WeaponSettings { Damage = 7, Reach = 0.85f, Speed = 1.1f, Stagger = 0.6f, CriticalDamage = 3, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Club = new WeaponSettings { Damage = 7, Reach = 1.0f, Speed = 1.0f, Stagger = 0.9f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Shortspear = new WeaponSettings { Damage = 7, Reach = 1.2f, Speed = 0.9f, Stagger = 0.75f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = false };
            Whip = new WeaponSettings { Damage = 6, Reach = 2.0f, Speed = 0.9f, Stagger = 0.4f, CriticalDamage = 1, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            WarAxe = new WeaponSettings { Damage = 8, Reach = 1.0f, Speed = 0.9f, Stagger = 0.85f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Mace = new WeaponSettings { Damage = 9, Reach = 1.0f, Speed = 0.8f, Stagger = 1.0f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            HeavyArmoryQuarterstaff = new WeaponSettings { Damage = 10, Reach = 1.2f, Speed = 1.1f, Stagger = 1.0f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            AnimatedArmouryQuarterstaff = new WeaponSettings { Damage = 10, Reach = 1.2f, Speed = 1.1f, Stagger = 1.0f, CriticalDamage = 4, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Maul = new WeaponSettings { Damage = 12, Reach = 1.0f, Speed = 0.75f, Stagger = 1.1f, CriticalDamage = 5, CriticalDamageMultiplier = 1.0f, UseFloorDivision = false };
            Spear2h = new WeaponSettings { Damage = 12, Reach = 1.6f, Speed = 0.8f, Stagger = 1.0f, CriticalDamage = 7, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Pike = new WeaponSettings { Damage = 12, Reach = 1.7f, Speed = 0.7f, Stagger = 1.0f, CriticalDamage = 7, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Glaive = new WeaponSettings { Damage = 13, Reach = 1.6f, Speed = 0.8f, Stagger = 1.1f, CriticalDamage = 7, CriticalDamageMultiplier = 1.0f, UseFloorDivision = false };
            Trident = new WeaponSettings { Damage = 14, Reach = 1.5f, Speed = 0.7f, Stagger = 1.15f, CriticalDamage = 9, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Greatsword = new WeaponSettings { Damage = 15, Reach = 1.3f, Speed = 0.75f, Stagger = 1.1f, CriticalDamage = 7, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            DaiKatana = new WeaponSettings { Damage = 14, Reach = 1.3f, Speed = 0.85f, Stagger = 1.1f, CriticalDamage = 8, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            HeavyArmoryHalberd = new WeaponSettings { Damage = 15, Reach = 1.45f, Speed = 0.75f, Stagger = 1.1f, CriticalDamage = 8, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            AnimatedArmouryHalberd = new WeaponSettings { Damage = 15, Reach = 1.55f, Speed = 0.65f, Stagger = 1.1f, CriticalDamage = 8, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            LongMace = new WeaponSettings { Damage = 17, Reach = 1.3f, Speed = 0.65f, Stagger = 1.2f, CriticalDamage = 8, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Battleaxe = new WeaponSettings { Damage = 16, Reach = 1.3f, Speed = 0.7f, Stagger = 1.15f, CriticalDamage = 8, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
            Warhammer = new WeaponSettings { Damage = 18, Reach = 1.3f, Speed = 0.6f, Stagger = 1.25f, CriticalDamage = 9, CriticalDamageMultiplier = 1.0f, UseFloorDivision = true };
        }

        [SettingName("Plugin Processing")]
        [Tooltip("Choose which plugins to process")]
        public PluginFilter PluginFilter { get; set; } = PluginFilter.IncludePlugins;

        [SettingName("Plugin List")]
        [Tooltip("Semi-colon separated list of plugins (e.g., 'Plugin1.esp;Plugin2.esp')")]
        public string PluginList { get; set; } = "PrvtI_HeavyArmory.esp;KatanaCrafting.esp;NewArmoury.esp";

        [SettingName("WACCF material tiers")]
        [Tooltip("Enable support for WACCF material tiers")]
        public bool WACCFMaterialTiers { get; set; } = false;

        // Process flags for vanilla weapons
        [SettingName("Process Daggers")]
        [Tooltip("Enable processing of Dagger weapons")]
        public bool ProcessDaggers { get; set; } = true;

        [SettingName("Process Tantos")]
        [Tooltip("Enable processing of Tanto weapons")]
        public bool ProcessTantos { get; set; } = true;

        [SettingName("Process Claws")]
        [Tooltip("Enable processing of Claw weapons")]
        public bool ProcessClaws { get; set; } = true;

        [SettingName("Process Javelins")]
        [Tooltip("Enable processing of Javelin weapons")]
        public bool ProcessJavelins { get; set; } = true;

        [SettingName("Process Rapiers")]
        [Tooltip("Enable processing of Rapier weapons")]
        public bool ProcessRapiers { get; set; } = true;

        [SettingName("Process Wakizashis")]
        [Tooltip("Enable processing of Wakizashi weapons")]
        public bool ProcessWakizashis { get; set; } = true;

        [SettingName("Process Shortswords")]
        [Tooltip("Enable processing of Shortsword weapons")]
        public bool ProcessShortswords { get; set; } = true;

        [SettingName("Process One-Handed Spears")]
        [Tooltip("Enable processing of one-handed Spear weapons")]
        public bool ProcessSpears1h { get; set; } = true;

        [SettingName("Process Swords")]
        [Tooltip("Enable processing of Sword weapons")]
        public bool ProcessSwords { get; set; } = true;

        [SettingName("Process Katanas")]
        [Tooltip("Enable processing of Katana weapons")]
        public bool ProcessKatanas { get; set; } = true;

        [SettingName("Process Hatchets")]
        [Tooltip("Enable processing of Hatchet weapons")]
        public bool ProcessHatchets { get; set; } = true;

        [SettingName("Process Clubs")]
        [Tooltip("Enable processing of Club weapons")]
        public bool ProcessClubs { get; set; } = true;

        [SettingName("Process Shortspears")]
        [Tooltip("Enable processing of Shortspear weapons")]
        public bool ProcessShortspears { get; set; } = true;

        [SettingName("Process Whips")]
        [Tooltip("Enable processing of Whip weapons")]
        public bool ProcessWhips { get; set; } = true;

        [SettingName("Process War Axes")]
        [Tooltip("Enable processing of War Axe weapons")]
        public bool ProcessWarAxes { get; set; } = true;

        [SettingName("Process Maces")]
        [Tooltip("Enable processing of Mace weapons")]
        public bool ProcessMaces { get; set; } = true;

        [SettingName("Process Heavy Armory Quarterstaffs")]
        [Tooltip("Enable processing of Quarterstaff weapons from Heavy Armory")]
        public bool ProcessHeavyArmoryQuarterstaffs { get; set; } = true;

        [SettingName("Process Animated Armoury Quarterstaffs")]
        [Tooltip("Enable processing of Quarterstaff weapons from Animated Armoury")]
        public bool ProcessAnimatedArmouryQuarterstaffs { get; set; } = true;

        [SettingName("Process Mauls")]
        [Tooltip("Enable processing of Maul weapons")]
        public bool ProcessMauls { get; set; } = true;

        [SettingName("Process Two-Handed Spears")]
        [Tooltip("Enable processing of two-handed Spear weapons")]
        public bool ProcessSpears2h { get; set; } = true;

        [SettingName("Process Pikes")]
        [Tooltip("Enable processing of Pike weapons")]
        public bool ProcessPikes { get; set; } = true;

        [SettingName("Process Glaives")]
        [Tooltip("Enable processing of Glaive weapons")]
        public bool ProcessGlaives { get; set; } = true;

        [SettingName("Process Tridents")]
        [Tooltip("Enable processing of Trident weapons")]
        public bool ProcessTridents { get; set; } = true;

        [SettingName("Process Greatswords")]
        [Tooltip("Enable processing of Greatsword weapons")]
        public bool ProcessGreatswords { get; set; } = true;

        [SettingName("Process Dai-Katanas")]
        [Tooltip("Enable processing of Dai-Katana weapons")]
        public bool ProcessDaiKatanas { get; set; } = true;

        [SettingName("Process Heavy Armory Halberds")]
        [Tooltip("Enable processing of Halberd weapons from Heavy Armory")]
        public bool ProcessHeavyArmoryHalberds { get; set; } = true;

        [SettingName("Process Animated Armoury Halberds")]
        [Tooltip("Enable processing of Halberd weapons from Animated Armoury")]
        public bool ProcessAnimatedArmouryHalberds { get; set; } = true;

        [SettingName("Process Long Maces")]
        [Tooltip("Enable processing of Long Mace weapons")]
        public bool ProcessLongMaces { get; set; } = true;

        [SettingName("Process Battleaxes")]
        [Tooltip("Enable processing of Battleaxe weapons")]
        public bool ProcessBattleaxes { get; set; } = true;

        [SettingName("Process Warhammers")]
        [Tooltip("Enable processing of Warhammer weapons")]
        public bool ProcessWarhammers { get; set; } = true;

        // Weapon settings
        [SettingName("Dagger (iron) [WeapTypeDagger]")]
        [Tooltip("Settings for the Dagger weapon")]
        public WeaponSettings Dagger { get; set; }

        [SettingName("Tanto (iron) [WeapTypeTanto]")]
        [Tooltip("Settings for the Tanto weapon")]
        public WeaponSettings Tanto { get; set; }

        [SettingName("Claw (iron) [WeapTypeClaw]")]
        [Tooltip("Settings for the Claw weapon")]
        public WeaponSettings Claw { get; set; }

        [SettingName("Javelin (iron) [WeapTypeJavelin]")]
        [Tooltip("Settings for the Javelin weapon")]
        public WeaponSettings Javelin { get; set; }

        [SettingName("Rapier (iron) [WeapTypeRapier]")]
        [Tooltip("Settings for the Rapier weapon")]
        public WeaponSettings Rapier { get; set; }

        [SettingName("Wakizashi (iron) [WeapTypeWakizashi]")]
        [Tooltip("Settings for the Wakizashi weapon")]
        public WeaponSettings Wakizashi { get; set; }

        [SettingName("Shortsword (iron) [ShWeapTypeShortsword]")]
        [Tooltip("Settings for the Shortsword weapon")]
        public WeaponSettings Shortsword { get; set; }

        [SettingName("Spear one handed (iron) [WeapTypeSpear]")]
        [Tooltip("Settings for the Spear weapon from Skyrim Spear Mechanic")]
        public WeaponSettings Spear1h { get; set; }

        [SettingName("Sword (iron) [WeapTypeSword]")]
        [Tooltip("Settings for the Sword weapon")]
        public WeaponSettings Sword { get; set; }

        [SettingName("Katana (iron) [WeapTypeKatana]")]
        [Tooltip("Settings for the Katana weapon")]
        public WeaponSettings Katana { get; set; }

        [SettingName("Hatchet (iron) [ShWeapTypeHatchet]")]
        [Tooltip("Settings for the Hatchet weapon")]
        public WeaponSettings Hatchet { get; set; }

        [SettingName("Club (iron) [ShWeapTypeClub]")]
        [Tooltip("Settings for the Club weapon")]
        public WeaponSettings Club { get; set; }

        [SettingName("Shortspear (iron) [ShWeapTypeShortspear]")]
        [Tooltip("Settings for the Shortspear weapon")]
        public WeaponSettings Shortspear { get; set; }

        [SettingName("Whip (iron) [WeapTypeWhip]")]
        [Tooltip("Settings for the Whip weapon")]
        public WeaponSettings Whip { get; set; }

        [SettingName("War Axe (iron) [WeapTypeWarAxe]")]
        [Tooltip("Settings for the War Axe weapon")]
        public WeaponSettings WarAxe { get; set; }

        [SettingName("Mace (iron) [WeapTypeMace]")]
        [Tooltip("Settings for the Mace weapon")]
        public WeaponSettings Mace { get; set; }

        [SettingName("Heavy Armory Quarterstaff (iron) [ShWeapTypeQuarterStaff]")]
        [Tooltip("Settings for the Quarterstaff weapon from Heavy Armory")]
        public WeaponSettings HeavyArmoryQuarterstaff { get; set; }

        [SettingName("Animated Armoury Quarterstaff (iron) [WeapTypeQtrStaff]")]
        [Tooltip("Settings for the Quarterstaff weapon from Animated Armoury")]
        public WeaponSettings AnimatedArmouryQuarterstaff { get; set; }

        [SettingName("Maul (iron) [ShWeapTypeMaul]")]
        [Tooltip("Settings for the Maul weapon")]
        public WeaponSettings Maul { get; set; }

        [SettingName("Spear two handed (iron) [ShWeapTypeSpear]")]
        [Tooltip("Settings for the two-handed Spear weapon")]
        public WeaponSettings Spear2h { get; set; }

        [SettingName("Pike (iron) [WeapTypePike]")]
        [Tooltip("Settings for the Pike weapon")]
        public WeaponSettings Pike { get; set; }

        [SettingName("Glaive (iron) [ShWeapTypeGlaive]")]
        [Tooltip("Settings for the Glaive weapon")]
        public WeaponSettings Glaive { get; set; }

        [SettingName("Trident (iron) [ShWeapTypeTrident]")]
        [Tooltip("Settings for the Trident weapon")]
        public WeaponSettings Trident { get; set; }

        [SettingName("Greatsword (iron) [WeapTypeGreatsword]")]
        [Tooltip("Settings for the Greatsword weapon")]
        public WeaponSettings Greatsword { get; set; }

        [SettingName("Dai-Katana (iron) [WeapTypeDaiKatana]")]
        [Tooltip("Settings for the Dai-Katana weapon")]
        public WeaponSettings DaiKatana { get; set; }

        [SettingName("Heavy Armory Halberd (iron) [ShWeapTypeHalberd]")]
        [Tooltip("Settings for the Halberd weapon from Heavy Armory")]
        public WeaponSettings HeavyArmoryHalberd { get; set; }

        [SettingName("Animated Armoury Halberd (iron) [WeapTypeHalberd]")]
        [Tooltip("Settings for the Halberd weapon from Animated Armoury")]
        public WeaponSettings AnimatedArmouryHalberd { get; set; }

        [SettingName("Long Mace (iron)")]
        [Tooltip("Settings for iron Long Mace weapons")]
        public WeaponSettings LongMace { get; set; }

        [SettingName("Battleaxe (iron) [WeapTypeBattleaxe]")]
        [Tooltip("Settings for the Battleaxe weapon")]
        public WeaponSettings Battleaxe { get; set; }

        [SettingName("Warhammer (iron) [WeapTypeWarhammer]")]
        [Tooltip("Settings for the Warhammer weapon")]
        public WeaponSettings Warhammer { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class WeaponSettings
    {
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

        [SettingName("Use Floor Division")]
        [Tooltip("If enabled, damage values will be rounded down to the nearest 10")]
        [JsonProperty]
        public bool UseFloorDivision { get; set; }

        public WeaponSettings()
        {
            CriticalDamageMultiplier = 1.0f;
        }
    }
} 