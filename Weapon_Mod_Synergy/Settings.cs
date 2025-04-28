using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System.ComponentModel;
using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weapon_Mod_Synergy
{
    public enum WeaponSkill
    {
        OneHanded,
        TwoHanded
    }

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

    public enum SpecialWeaponsMod
    {
        None,
        Artificer
    }

    public enum BoundWeaponParsing
    {
        FromSettings,
        CalculateFromMods,
        IgnoreWeapon
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class WeaponCategory
    {
        [JsonProperty]
        [SettingName("Weapons (add more by adding panes)")]
        public Dictionary<string, WeaponSettings> Weapons { get; set; } = new();
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

            // set default bound weapon parsing
            BoundWeaponParsing = BoundWeaponParsing.FromSettings;

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

            // initialize variant categories
            Variants = new VariantCategory();
            InitializeVariants();

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

        private void InitializeVariants()
        {
            Variants.Variants["Eastern Onehanded"] = new VariantSettings { NameIDs = "eastern", ExcludeIDs = "WeapTypeDagger;katana;wakizashi", Skill = WeaponSkill.OneHanded, AdditionalDamage = -1, AdditionalReach = 0.0f, AdditionalSpeed = 0.125f, AdditionalStagger = 0.0f, AdditionalCriticalDamageOffset = 0.0f, AdditionalCriticalDamageChanceMultiplier = 0.0f, AdditionalCriticalDamageMultiplier = 0.0f };
            Variants.Variants["Eastern Twohanded"] = new VariantSettings { NameIDs = "eastern", ExcludeIDs = "dai-katana;daikatana;dai katana;nodachi", Skill = WeaponSkill.TwoHanded, AdditionalDamage = -1, AdditionalReach = 0.0f, AdditionalSpeed = 0.125f, AdditionalStagger = 0.0f, AdditionalCriticalDamageOffset = 0.0f, AdditionalCriticalDamageChanceMultiplier = 0.0f, AdditionalCriticalDamageMultiplier = 0.0f };
            Variants.Variants["Cyrodilic Onehanded"] = new VariantSettings { NameIDs = "cyrodiilic;cyrodilic;cyrodiil;cyrodil", ExcludeIDs = "WeapTypeDagger", Skill = WeaponSkill.OneHanded, AdditionalDamage = 1, AdditionalReach = 0.0f, AdditionalSpeed = -0.1f, AdditionalStagger = 0.0f, AdditionalCriticalDamageOffset = 0.0f, AdditionalCriticalDamageChanceMultiplier = 0.0f, AdditionalCriticalDamageMultiplier = 0.0f };
            Variants.Variants["Cyrodilic Twohanded"] = new VariantSettings { NameIDs = "cyrodiilic;cyrodilic;cyrodiil;cyrodil", ExcludeIDs = "", Skill = WeaponSkill.TwoHanded, AdditionalDamage = 1, AdditionalReach = 0.0f, AdditionalSpeed = -0.1f, AdditionalStagger = 0.0f, AdditionalCriticalDamageOffset = 0.0f, AdditionalCriticalDamageChanceMultiplier = 0.0f, AdditionalCriticalDamageMultiplier = 0.0f };

        }

        private void InitializeDaggers()
        {
            Daggers.Weapons["Dagger"] = new WeaponSettings { Damage = 4, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 6, Reach = 0.7f, Speed = 1.3f, Stagger = 0.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "", SearchLogic = LogicOperator.OR, KeywordIDs = "WeapTypeDagger" } };
            Daggers.Weapons["Tanto"] = new WeaponSettings { Damage = 4, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 6, Reach = 0.7f, Speed = 1.3f, Stagger = 0.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "tanto", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeDagger" } };
            Daggers.Weapons["Claw"] = new WeaponSettings { Damage = 5, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 6, Reach = 0.7f, Speed = 1.2f, Stagger = 0.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "claw", KeywordIDs = "WeapTypeClaw" } };
        }

        private void InitializeOnehandedSwords()
        {
            OnehandedSwords.Weapons["Sword"] = new WeaponSettings { Damage = 7, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 1.0f, Speed = 1.0f, Stagger = 0.75f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeSword" } };
            OnehandedSwords.Weapons["Rapier"] = new WeaponSettings { Damage = 5, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 1.1f, Speed = 1.15f, Stagger = 0.6f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "rapier", KeywordIDs = "WeapTypeRapier" } };
            OnehandedSwords.Weapons["Wakizashi"] = new WeaponSettings { Damage = 5, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 0.9f, Speed = 1.25f, Stagger = 0.6f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "wakizashi;ninjato", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeSword" } };
            OnehandedSwords.Weapons["Shortsword"] = new WeaponSettings { Damage = 6, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 0.9f, Speed = 1.15f, Stagger = 0.6f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "shortsword", KeywordIDs = "ShWeapTypeShortsword" } };
            OnehandedSwords.Weapons["Katana"] = new WeaponSettings { Damage = 6, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 1.0f, Speed = 1.2f, Stagger = 0.7f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "katana;akaviri sword;blades sword;bolar's oathblade;shadow's edge;dragonbane", KeywordIDs = "WeapTypeKatana" } };
            OnehandedSwords.Weapons["Scimitar"] = new WeaponSettings { Damage = 7, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 0.95f, Speed = 1.1f, Stagger = 0.65f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "scimitar;heartsplitter;blade of yokuda;bloodscythe;soulrender;viper's fang", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeSword" } };
        }

        private void InitializeOnehandedSpears()
        {
            OnehandedSpears.Weapons["Javelin"] = new WeaponSettings { Damage = 5, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 1.0f, Speed = 1.2f, Stagger = 0.5f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "javelin", KeywordIDs = "WeapTypeJavelin" } };
            OnehandedSpears.Weapons["Onehanded Spear"] = new WeaponSettings { Damage = 6, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 1.3f, Speed = 1.1f, Stagger = 0.75f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "spear", KeywordIDs = "WeapTypeSpear" } };
            OnehandedSpears.Weapons["Shortspear"] = new WeaponSettings { Damage = 7, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 1.2f, Speed = 0.9f, Stagger = 0.75f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "shortspear", KeywordIDs = "ShWeapTypeShortspear" } };
        }

        private void InitializeOnehandedBluntWeapons()
        {
            OnehandedBluntWeapons.Weapons["Mace"] = new WeaponSettings { Damage = 9, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 1.0f, Speed = 0.8f, Stagger = 1.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeMace" } };
            OnehandedBluntWeapons.Weapons["Club"] = new WeaponSettings { Damage = 7, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 1.0f, Speed = 1.0f, Stagger = 0.9f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "club", KeywordIDs = "ShWeapTypeClub" } };
            OnehandedBluntWeapons.Weapons["Maul"] = new WeaponSettings { Damage = 12, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 1.0f, Speed = 0.75f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "maul;hammer;mallet", KeywordIDs = "ShWeapTypeMaul" } };
        }

        private void InitializeOnehandedAxes()
        {
            OnehandedAxes.Weapons["War Axe"] = new WeaponSettings { Damage = 8, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 1.0f, Speed = 0.9f, Stagger = 0.85f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeWarAxe" } };
            OnehandedAxes.Weapons["Hatchet"] = new WeaponSettings { Damage = 7, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 0.85f, Speed = 1.1f, Stagger = 0.6f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "hatchet", KeywordIDs = "ShWeapTypeHatchet" } };
        }

        private void InitializeTwohandedSwords()
        {
            TwohandedSwords.Weapons["Greatsword"] = new WeaponSettings { Damage = 15, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.3f, Speed = 0.75f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeGreatSword" } };
            TwohandedSwords.Weapons["Glaive"] = new WeaponSettings { Damage = 13, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.6f, Speed = 0.8f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "glaive", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeGreatsword " } };
            TwohandedSwords.Weapons["DaiKatana"] = new WeaponSettings { Damage = 14, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.3f, Speed = 0.85f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "dai-katana;daikatana;dai katana;nodachi", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeSword" } };
        }

        private void InitializeTwohandedSpears()
        {
            TwohandedSpears.Weapons["Pike"] = new WeaponSettings { Damage = 12, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.7f, Speed = 0.7f, Stagger = 1.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "pike", KeywordIDs = "WeapTypePike" } };
            TwohandedSpears.Weapons["Twohanded Spear"] = new WeaponSettings { Damage = 12, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.6f, Speed = 0.8f, Stagger = 1.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "spear;halfpike;half pike", KeywordIDs = "ShWeapTypeSpear" } };
            TwohandedSpears.Weapons["Trident"] = new WeaponSettings { Damage = 14, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.5f, Speed = 0.7f, Stagger = 1.15f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "trident", KeywordIDs = "ShWeapTypeTrident" } };
        }

        private void InitializeTwohandedBluntWeapons()
        {
            TwohandedBluntWeapons.Weapons["Warhammer"] = new WeaponSettings { Damage = 18, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.3f, Speed = 0.6f, Stagger = 1.25f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeWarhammer" } };
            TwohandedBluntWeapons.Weapons["Quarterstaff, shorter"] = new WeaponSettings { Damage = 10, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.2f, Speed = 1.1f, Stagger = 1.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "shortstaff", KeywordIDs = "ShWeapTypeQuarterStaff" } };
            TwohandedBluntWeapons.Weapons["Quarterstaff"] = new WeaponSettings { Damage = 10, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.2f, Speed = 1.1f, Stagger = 1.0f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "battlestaff;quarterstaff", KeywordIDs = "WeapTypeQtrStaff" } };
            TwohandedBluntWeapons.Weapons["LongMace"] = new WeaponSettings { Damage = 17, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.3f, Speed = 0.65f, Stagger = 1.2f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "longmace;long mace", KeywordIDs = "ShWeapTypeLongMace" } };
            TwohandedAxes.Weapons["Greatclub, hammer version"] = new WeaponSettings { Damage = 16, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.34f, Speed = 0.8f, Stagger = 1.25f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "greatclub", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeWarhammer" } };

        }

        private void InitializeTwohandedAxes()
        {
            TwohandedAxes.Weapons["Battleaxe"] = new WeaponSettings { Damage = 16, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.3f, Speed = 0.7f, Stagger = 1.15f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeBattleaxe" } };
            TwohandedAxes.Weapons["Glaive"] = new WeaponSettings { Damage = 13, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.6f, Speed = 0.8f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "glaive", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeBattleaxe " } };
            TwohandedAxes.Weapons["Halberd, shorter"] = new WeaponSettings { Damage = 15, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.45f, Speed = 0.75f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "", SearchLogic = LogicOperator.AND, KeywordIDs = "ShWeapTypeHalberd;" } };
            TwohandedAxes.Weapons["Halberd"] = new WeaponSettings { Damage = 15, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.55f, Speed = 0.65f, Stagger = 1.1f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "halberd", KeywordIDs = "WeapTypeHalberd" } };
            TwohandedAxes.Weapons["Greatclub, axe version"] = new WeaponSettings { Damage = 16, BoundWeaponAdditionalDamage = 1, BoundMysticWeaponAdditionalDamage = 6, Reach = 1.34f, Speed = 0.8f, Stagger = 1.25f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.TwoHanded, NamedIDs = "greatclub", SearchLogic = LogicOperator.AND, KeywordIDs = "WeapTypeBattleaxe" } };
        }

        private void InitializeOthers()
        {
            Others.Weapons["Whip"] = new WeaponSettings { Damage = 6, BoundWeaponAdditionalDamage = 2, BoundMysticWeaponAdditionalDamage = 7, Reach = 2.0f, Speed = 0.9f, Stagger = 0.4f, MatchLogicSettings = new MatchLogicSettings { Skill = WeaponSkill.OneHanded, NamedIDs = "whip", KeywordIDs = "WeapTypeWhip" } };
        }

        [SettingName("Plugin Processing")]
        [Tooltip("Choose which plugins to process")]
        [JsonProperty(ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public PluginFilter PluginFilter { get; set; }

        // **********************************
        // @TODO: Add support for: 
        // Vigilant
        // Unslaad
        // Jaysus Swords
        // **********************************

        [SettingName("Plugin Include List")]
        [Tooltip("List of plugins to include (semicolon separated)")]
        [JsonProperty]
        public string PluginIncludeList { get; set; } = "Skyrim.esm;Dawnguard.esm;Dragonborn.esm;ccasvsse001-almsivi.esm;ccbgssse001-fish.esm;ccbgssse003-zombies.esl;ccbgssse005-goldbrand.esl;" +
        "ccbgssse006-stendarshammer.esl;ccbgssse007-chrysamere.esl;ccmtysse001-knightsofthenine.esl;ccbgssse018-shadowrend.esl;ccbgssse008-wraithguard.esl;ccffbsse001-imperialdragon.esl;" +
        "ccbgssse016-umbra.esm;ccbgssse031-advcyrus.esm;ccbgssse059-ba_dragonplate.esl;ccbgssse067-daedinv.esm;ccbgssse068-bloodfall.esl;ccbgssse069-contest.esl;ccvsvsse003-necroarts.esl;" +
        "ccbgssse025-advdsgs.esm;ccedhsse003-redguard.esl;cckrtsse001_altar.esl;ccbgssse006-stendarshammer.esl;Skyrim Extended Cut - Saints and Seducers.esp;MoonAndStar_MAS.esp;Dwarfsphere.esp;" +
        "Midwood Isle.esp;Gray Fox Cowl.esm;Wyrmstooth.esp;KhajiitWillFollow.esp;Skyrim On Skooma.esp;Skyrim Weapons Expansion.esp;Inigo.esp;WZOblivionArtifacts.esp;Destroy the Dark Brotherhood - Quest Expansion.esp;" +
        "BeyondSkyrimMerchant.esp;aMidianborn_Skyforge_Weapons.esp;DawnguardArsenal.esp;RMB SPID - Realistic Armors.esp;Bound Armory Extravaganza.esp;Summermyst - Enchantments of Skyrim.esp;" +
        "Lore Weapon Expansion - Daedric Crescent.esp;Lore Weapon Expansion.esp;Lore Weapon Expansion - Goldbrand.esp;Lore Weapon Expansion - Relics of the Crusader.esp;Skyrim Weapons Expansion.esp;" +
        "Weapons Armor Clothing & Clutter Fixes.esp;BSMBonemoldSet.esp;PrvtI_DaedricKatanas.esp;Prvti_ElvenSteel.esp;PrvtI_ImperialSilver.esp;PrvtI_BSBonemold.esp;Prvti_DragonCultWeapons.esp;" +
        "PrvtI_BrumaArmory.esp;PrvtI_BSMerchantArmory.esp;PrvtI_LunarArmory.esp;PrvtI_LunarArmory.esp;" +
        "PrvtI_HeavyArmory.esp;KatanaCrafting.esp;NewArmoury.esp;SkyrimSpearMechanic.esp";

        [SettingName("Plugin Exclude List")]
        [Tooltip("List of plugins to exclude (semicolon separated)")]
        [JsonProperty]
        public string PluginExcludeList { get; set; } = "";

        [SettingName("Ignored Weapons Form Keys (semicolon separated)")]
        [Tooltip("List of weapon form keys to ignore (semicolon separated)")]
        [JsonProperty]
        public string IgnoredWeaponFormKeys { get; set; } = "02F2F4:Skyrim.esm;0E3C16:Skyrim.esm;0E7A31:Skyrim.esm;0AE086:Skyrim.esm;0398E6:Dragonborn.esm;0179C9:Dragonborn.esm;0206F2:Dragonborn.esm;00A801:ccbgssse019-staffofsheogorath.esl;009850;" +
        "ccbgssse020-graycowl.esl;02D854:ccvsvsse004-beafarmer.esl;59F322:Skyrim Extended Cut - Saints and Seducers.esp;59F323:Skyrim Extended Cut - Saints and Seducers.esp;" +
        "014FBA:MoonAndStar_MAS.esp;014FC0:MoonAndStar_MAS.esp;00341D:Gray Fox Cowl.esm;4A12C8:Wyrmstooth.esp;0AD90A;College Of Winterhold - Quest Expansion.esp;013209:BeyondSkyrimMerchant.esp;" +
        "10F6D4:Bound Armory Extravaganza.esp;10F6DA:Bound Armory Extravaganza.esp;";

        [SettingName("WACCF material tiers and stat changes")]
        [Tooltip("Enable support for WACCF material tiers and stat changes")]
        [JsonProperty]
        public bool WACCFMaterialTiers { get; set; }

        [SettingName("Stalhrim stagger bonus (default vanilla value is 0.1, WACCF value is 0)")]
        [Tooltip("Enable support for Stalhrim stagger bonus, if current stagger is greater than 0")]
        [JsonProperty]
        public float StalhrimStaggerBonus { get; set; }

        [SettingName("Stalhrim War Axe and Mace damage bonus (default vanilla value is 1, WACCF value is 0)")]
        [Tooltip("Enable support for Stalhrim War Axe and Mace damage bonus")]
        [JsonProperty]
        public int StalhrimDamageBonus { get; set; }

        [SettingName("Special Weapons Mod")]
        [Tooltip("Choose which special weapons mod to use")]
        [JsonProperty]
        public SpecialWeaponsMod SpecialWeaponsMod { get; set; }

        [SettingName("Bound Weapon Damage. Read from settings, honor mod offsets or ignore")]
        [Tooltip("Choose how to parse bound weapons")]
        [JsonProperty]
        public BoundWeaponParsing BoundWeaponParsing { get; set; }


        [JsonProperty]
        public VariantCategory Variants { get; set; }

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

        [SettingName("Bound weapon additional damage")]
        [Tooltip("Additional damage for bound weapons, added to the base damage")]
        [JsonProperty]
        public int BoundWeaponAdditionalDamage { get; set; } = 2;

        [SettingName("Bound Mystic Weapon additional damage")]
        [Tooltip("Additional damage for bound mystic weapons, added to the base damage")]
        [JsonProperty]
        public int BoundMysticWeaponAdditionalDamage { get; set; } = 7;

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

    public class VariantSettings
    {
        [SettingName("Has in name")]
        [Tooltip("If not empty, the variant will be used. Semicolon separated list of words to search for in the weapon name.")]
        [JsonProperty]
        public string NameIDs { get; set; } = "";

        [SettingName("Exclude if has this name or keyword")]
        [Tooltip("If not empty, the variant will be excluded if the weapon has any of these in its name or keywords. Semicolon separated list.")]
        [JsonProperty]
        public string ExcludeIDs { get; set; } = "WeapTypeDagger";

        [SettingName("Skill")]
        [Tooltip("Skill required to use the weapon")]
        [JsonProperty]
        public WeaponSkill Skill { get; set; } = WeaponSkill.OneHanded;

        [SettingName("Additional Damage Offset")]
        [Tooltip("Damage added to the base damage")]
        [JsonProperty]
        public int AdditionalDamage { get; set; } = 0;

        [SettingName("Additional Reach Offset")]
        [Tooltip("Reach offset added to the base reach")]
        [JsonProperty]
        public float AdditionalReach { get; set; } = 0.0f;

        [SettingName("Additional Speed Offset")]
        [Tooltip("Speed offset added to the base speed")]
        [JsonProperty]
        public float AdditionalSpeed { get; set; } = 0.0f;

        [SettingName("Additional Stagger Offset")]
        [Tooltip("Stagger offset added to the base stagger")]
        [JsonProperty]
        public float AdditionalStagger { get; set; } = 0.0f;

        [SettingName("Additional Critical Damage Offset")]
        [Tooltip("Critical damage offset added to the base critical damage")]
        [JsonProperty]
        public float AdditionalCriticalDamageOffset { get; set; } = 0.0f;

        [SettingName("Additional Critical Damage Chance Multiplier")]
        [Tooltip("Critical damage chance multiplier added to the base critical damage chance")]
        [JsonProperty]
        public float AdditionalCriticalDamageChanceMultiplier { get; set; } = 0.0f;

        [SettingName("Additional Critical Damage Multiplier")]
        [Tooltip("Critical damage multiplier added to the base critical damage multiplier")]
        [JsonProperty]
        public float AdditionalCriticalDamageMultiplier { get; set; } = 0.0f;
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
        "Identify which weapons to patch by name. Semicolon separated list."
        )]
        [JsonProperty]
        public string NamedIDs { get; set; } = "";

        [SettingName("AND/OR")]
        [Tooltip("Use name and/or keywords, blank input fields are ignored")]
        [JsonProperty]
        public LogicOperator SearchLogic { get; set; } = LogicOperator.OR;

        [SettingName("Has keywords")]
        [Tooltip("Identify which weapons to patch by keywords. Semicolon separated list.")]
        [JsonProperty]
        public string KeywordIDs { get; set; } = "";
    }
}