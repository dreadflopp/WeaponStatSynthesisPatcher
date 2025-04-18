using Mutagen.Bethesda.Skyrim;
using System;
using System.Linq;
using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins.Order;
using System.IO;
using System.Text;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Text.Json;
using Mutagen.Bethesda.Skyrim.Internals;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Weapon_Mod_Synergy
{
    // Classes for JSON deserialization
    public class MaterialData
    {
        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = "";

        [JsonPropertyName("damage_offset_1h")]
        public int DamageOffset1h { get; set; }

        [JsonPropertyName("damage_offset_2h")]
        public int DamageOffset2h { get; set; }

        [JsonPropertyName("damage_offset_1h_waccf")]
        public int DamageOffset1hWaccf { get; set; }

        [JsonPropertyName("damage_offset_2h_waccf")]
        public int DamageOffset2hWaccf { get; set; }
    }

    public class SpecialWeaponData
    {
        [JsonPropertyName("editor_id")]  // Just for JSON readability
        public string EditorID { get; set; } = "";

        [JsonPropertyName("form_key")]   // This is what we actually use
        public string FormKey { get; set; } = "";

        [JsonPropertyName("damage_offset")]
        public int? DamageOffset { get; set; }

        [JsonPropertyName("damage_offset_waccf")]
        public int? DamageOffsetWaccf { get; set; }

        [JsonPropertyName("damage_offset_artificer")]
        public int? DamageOffsetArtificer { get; set; }

        [JsonPropertyName("damage_offset_mysticism")]
        public int? DamageOffsetMysticism { get; set; }

        [JsonPropertyName("speed_offset")]
        public float? SpeedOffset { get; set; }

        [JsonPropertyName("speed_offset_waccf")]
        public float? SpeedOffsetWaccf { get; set; }

        [JsonPropertyName("speed_offset_artificer")]
        public float? SpeedOffsetArtificer { get; set; }

        [JsonPropertyName("reach_offset")]
        public float? ReachOffset { get; set; }

        [JsonPropertyName("reach_offset_waccf")]
        public float? ReachOffsetWaccf { get; set; }

        [JsonPropertyName("reach_offset_artificer")]
        public float? ReachOffsetArtificer { get; set; }

        [JsonPropertyName("stagger_offset")]
        public float? StaggerOffset { get; set; }

        [JsonPropertyName("stagger_offset_waccf")]
        public float? StaggerOffsetWaccf { get; set; }

        [JsonPropertyName("stagger_offset_artificer")]
        public float? StaggerOffsetArtificer { get; set; }

        [JsonPropertyName("critical_damage_offset")]
        public int? CriticalDamageOffset { get; set; }

        [JsonPropertyName("critical_damage_offset_waccf")]
        public int? CriticalDamageOffsetWaccf { get; set; }

        [JsonPropertyName("critical_damage_offset_artificer")]
        public int? CriticalDamageOffsetArtificer { get; set; }

        [JsonPropertyName("critical_damage_multiplier_offset")]
        public float? CriticalDamageMultiplierOffset { get; set; }

        [JsonPropertyName("critical_damage_multiplier_offset_waccf")]
        public float? CriticalDamageMultiplierOffsetWaccf { get; set; }

        [JsonPropertyName("critical_damage_multiplier_offset_artificer")]
        public float? CriticalDamageMultiplierOffsetArtificer { get; set; }        
    }

    public class WeaponHelper
    {
        // Debug flag to enable/disable debug output
        private const bool DEBUG_MODE = true;

        private readonly Dictionary<string, WeaponSettings> _weaponSettings;
        private static Action<string>? _logger;

        // Static lists to hold the JSON data
        private static List<MaterialData> _materialData = new();
        private static List<MaterialData> _materialDataName = new();
        private static List<SpecialWeaponData> _specialWeapons = new();

        // Dictionary to store file paths for each data type
        private static readonly Dictionary<string, string> _jsonFilePaths = new Dictionary<string, string>
        {
            { "materialData", "material_data_keyword.json" },
            { "materialDataName", "material_data_name.json" },
            { "specialWeapons", "special_weapons.json" }
        };

        public WeaponHelper(Dictionary<string, WeaponSettings> weaponSettings, Action<string> logger)
        {
            _weaponSettings = weaponSettings ?? throw new ArgumentNullException(nameof(weaponSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LoadMaterialData();
        }

        private void LoadMaterialData()
        {
            try
            {
                string patcherDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";

                // Load name-based material data first
                var nameJson = File.ReadAllText(Path.Combine(patcherDirectory, _jsonFilePaths["materialDataName"]));
                DebugLog($"Loading name-based material data from: {Path.Combine(patcherDirectory, _jsonFilePaths["materialDataName"])}");
                DebugLog($"Name-based material data JSON content: {nameJson}");
                _materialDataName = System.Text.Json.JsonSerializer.Deserialize<List<MaterialData>>(nameJson) ?? new List<MaterialData>();
                DebugLog($"Loaded {_materialDataName.Count} name-based material data entries");
                foreach (var material in _materialDataName)
                {
                    DebugLog($"Name-based material: Keyword='{material.Keyword}', 1h={material.DamageOffset1h}, 2h={material.DamageOffset2h}, 1hWaccf={material.DamageOffset1hWaccf}, 2hWaccf={material.DamageOffset2hWaccf}");
                }

                // Load vanilla material data (includes WACCF offsets)
                var vanillaJson = File.ReadAllText(Path.Combine(patcherDirectory, _jsonFilePaths["materialData"]));
                DebugLog($"Loading vanilla material data from: {Path.Combine(patcherDirectory, _jsonFilePaths["materialData"])}");
                DebugLog($"Vanilla material data JSON content: {vanillaJson}");
                _materialData = System.Text.Json.JsonSerializer.Deserialize<List<MaterialData>>(vanillaJson) ?? new List<MaterialData>();
                DebugLog($"Loaded {_materialData.Count} vanilla material data entries");
                foreach (var material in _materialData)
                {
                    DebugLog($"Vanilla material: Keyword='{material.Keyword}', 1h={material.DamageOffset1h}, 2h={material.DamageOffset2h}, 1hWaccf={material.DamageOffset1hWaccf}, 2hWaccf={material.DamageOffset2hWaccf}");
                }

                // Load special weapons list
                string specialWeaponsPath = Path.Combine(patcherDirectory, _jsonFilePaths["specialWeapons"]);
                if (File.Exists(specialWeaponsPath))
                {
                    var specialWeaponsJson = File.ReadAllText(specialWeaponsPath);
                    DebugLog($"Loading special weapons from: {specialWeaponsPath}");
                    DebugLog($"Special weapons JSON content: {specialWeaponsJson}");

                    _specialWeapons = System.Text.Json.JsonSerializer.Deserialize<List<SpecialWeaponData>>(specialWeaponsJson) ?? new List<SpecialWeaponData>();
                    DebugLog($"Loaded {_specialWeapons.Count} special weapons");

                    foreach (var weapon in _specialWeapons)
                    {
                        var sb = new StringBuilder();
                        sb.Append($"Special weapon: EditorID='{weapon.EditorID}', FormKey='{weapon.FormKey}'\n");
                        sb.Append($"  Base offsets: Damage={weapon.DamageOffset}, Speed={weapon.SpeedOffset}, Reach={weapon.ReachOffset}, Stagger={weapon.StaggerOffset}\n");
                        sb.Append($"  WACCF offsets: Damage={weapon.DamageOffsetWaccf}, Speed={weapon.SpeedOffsetWaccf}, Reach={weapon.ReachOffsetWaccf}, Stagger={weapon.StaggerOffsetWaccf}\n");
                        sb.Append($"  Artificer offsets: Damage={weapon.DamageOffsetArtificer}, Speed={weapon.SpeedOffsetArtificer}, Reach={weapon.ReachOffsetArtificer}, Stagger={weapon.StaggerOffsetArtificer}\n");
                        sb.Append($"  Critical offsets: Base={weapon.CriticalDamageOffset}, WACCF={weapon.CriticalDamageOffsetWaccf}, Artificer={weapon.CriticalDamageOffsetArtificer}\n");
                        sb.Append($"  Critical Multiplier offsets: Base={weapon.CriticalDamageMultiplierOffset}, WACCF={weapon.CriticalDamageMultiplierOffsetWaccf}, Artificer={weapon.CriticalDamageMultiplierOffsetArtificer}");
                        DebugLog(sb.ToString());
                    }
                }
                else
                {
                    DebugLog($"Special weapons file not found at: {specialWeaponsPath}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"Error loading material data: {ex.Message}");
                throw;
            }
        }

        // Debug logging function
        public void DebugLog(string message)
        {
            if (DEBUG_MODE)
            {
                _logger?.Invoke($"[DEBUG] {message}");
            }
        }

        // Generic method to get loaded data
        private T GetLoadedData<T>(ref T dataField)
        {
            if (_materialData.Count == 0 || _materialDataName.Count == 0 || _specialWeapons.Count == 0)
            {
                LoadMaterialData();
            }
            return dataField;
        }

        /// <summary>
        /// Gets the weapon skill type (1h or 2h) based on the weapon's Skill property
        /// If weapon Skill is Skill.OneHanded, return WeaponSkill.OneHanded
        /// If weapon Skill is Skill.TwoHanded, return WeaponSkill.TwoHanded
        /// Otherwise, return null
        /// </summary>
        public WeaponSkill? GetWeaponSkillType(IWeaponGetter weapon)
        {
            DebugLog($"------------------- GetWeaponSkillType start -------------------");
            DebugLog($"     GetWeaponSkillType called for weapon: {weapon?.EditorID ?? "null"}");

            if (weapon == null || weapon.Data == null)
            {
                DebugLog("     Weapon or weapon.Data is null, returning null");
                DebugLog($"------------------- GetWeaponSkillType end -------------------");
                return null;
            }

            // Get the skill from the weapon's Data property
            var skill = weapon.Data.Skill;
            DebugLog($"     Weapon skill value: {skill}");

            // Determine the weapon skill type based on the Skill property
            if (skill.HasValue)
            {
                // Convert the skill to a string for comparison
                string skillString = skill.Value.ToString();
                DebugLog($"     Skill string: {skillString}");

                if (skillString.Contains("OneHanded"))
                {
                    DebugLog("     Detected OneHanded skill, returning WeaponSkill.OneHanded");
                    DebugLog($"------------------- GetWeaponSkillType end -------------------");
                    return WeaponSkill.OneHanded;
                }
                else if (skillString.Contains("TwoHanded"))
                {
                    DebugLog("     Detected TwoHanded skill, returning WeaponSkill.TwoHanded");
                    DebugLog($"------------------- GetWeaponSkillType end -------------------");
                    return WeaponSkill.TwoHanded;
                }
            }

            DebugLog("     No matching skill type found, returning null");
            DebugLog($"------------------- GetWeaponSkillType end -------------------");
            return null;
        }

        /// <summary>
        /// Gets all keywords from a weapon as a list of strings
        /// </summary>
        public List<string> GetWeaponKeywords(IWeaponGetter weapon, ILinkCache linkCache)
        {
            DebugLog($"------------------- GetWeaponKeywords start -------------------");
            DebugLog($"     GetWeaponKeywords called for weapon: {weapon?.EditorID ?? "null"}");

            var keywords = new List<string>();

            if (weapon == null) return keywords;

            if (weapon.Keywords != null)
            {
                DebugLog($"     Weapon has {weapon.Keywords.Count} keywords");

                foreach (var keyword in weapon.Keywords)
                {
                    if (keyword is IFormLinkGetter<IKeywordGetter> formLink &&
                        formLink.TryResolve(linkCache, out var keywordRecord))
                    {
                        if (keywordRecord.EditorID != null)
                        {
                            keywords.Add(keywordRecord.EditorID);
                            DebugLog($"     Added keyword: {keywordRecord.EditorID}");
                        }
                    }
                }
            }
            else
            {
                DebugLog("     Weapon has no keywords");
                DebugLog($"------------------- GetWeaponKeywords end -------------------");
            }

            DebugLog($"     Returning {keywords.Count} keywords");
            DebugLog($"------------------- GetWeaponKeywords end -------------------");
            return keywords;
        }

        /// <summary>
        /// Gets the weapon setting key based on the weapon's name, keywords, and skill type
        /// </summary>
        public string GetWeaponSettingKey(IWeaponGetter weapon, ILinkCache linkCache)
        {
            DebugLog($"------------------- GetWeaponSettingKey start -------------------");
            if (weapon == null || linkCache == null)
            {
                DebugLog("     Weapon or link cache is null");
                DebugLog($"------------------- GetWeaponSettingKey end -------------------");
                return string.Empty;
            }

            var weaponName = weapon.Name?.String ?? string.Empty;
            if (string.IsNullOrEmpty(weaponName))
            {
                DebugLog("     Weapon name is empty");
                DebugLog($"------------------- GetWeaponSettingKey end -------------------");
                return string.Empty;
            }

            DebugLog($"     -----------------------------------------");
            DebugLog($"     Looking up weapon setting for: {weaponName}");
            DebugLog($"     -----------------------------------------");

            var skillType = GetWeaponSkillType(weapon);
            DebugLog($"     Skill Type: {skillType}");

            var keywords = GetWeaponKeywords(weapon, linkCache);
            DebugLog($"     Keywords: {string.Join(", ", keywords)}");

            foreach (var setting in _weaponSettings)
            {
                var settingKey = setting.Key;
                var settings = setting.Value;

                DebugLog($"     -----------------------------------------");
                DebugLog($"     Checking setting key: {settingKey}");
                DebugLog($"     Skill: {settings.Skill}");
                DebugLog($"     NamedIDs: {settings.NamedIDs}");
                DebugLog($"     SearchLogic: {settings.SearchLogic}");
                DebugLog($"     KeywordIDs: {settings.KeywordIDs}");
                DebugLog($"     -----------------------------------------\n");

                // Check skill type match
                if (settings.Skill != skillType && settings.Skill != WeaponSkill.Either)
                {
                    DebugLog($"     Skill type mismatch: {settings.Skill} != {skillType}");
                    continue;
                }

                // Check if both fields are empty
                if (string.IsNullOrWhiteSpace(settings.NamedIDs) && string.IsNullOrWhiteSpace(settings.KeywordIDs))
                {
                    DebugLog($"     Both NamedIDs and KeywordIDs are empty");
                    continue;
                }

                var namePatterns = settings.NamedIDs.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var keywordPatternList = settings.KeywordIDs.Split(';', StringSplitOptions.RemoveEmptyEntries);

                // Process name patterns
                bool nameMatch = true;
                if (!string.IsNullOrWhiteSpace(settings.NamedIDs))
                {
                    DebugLog($"     -----------------------------------------");
                    DebugLog($"     NameIDs");
                    DebugLog($"     -----------------------------------------");

                    bool hasRequiredPatterns = namePatterns.Any(p => p.StartsWith("+"));
                    bool hasForbiddenPatterns = namePatterns.Any(p => p.StartsWith("-"));
                    bool hasOptionalPatterns = namePatterns.Any(p => !p.StartsWith("+") && !p.StartsWith("-"));

                    // Check required patterns
                    if (hasRequiredPatterns)
                    {
                        foreach (var pattern in namePatterns.Where(p => p.StartsWith("+")))
                        {
                            string processedPattern = pattern[1..];
                            string regexPattern = processedPattern.StartsWith("re:")
                                ? processedPattern[3..]
                                : processedPattern.Contains("*")
                                    ? processedPattern.Replace("*", ".*")
                                    : $"\\b{Regex.Escape(processedPattern)}\\b";

                            bool matchResult = Regex.IsMatch(weaponName, regexPattern, RegexOptions.IgnoreCase);
                            if (!matchResult)
                            {
                                nameMatch = false;
                                break;
                            }
                        }
                    }

                    // Check forbidden patterns
                    if (nameMatch && hasForbiddenPatterns)
                    {
                        foreach (var pattern in namePatterns.Where(p => p.StartsWith("-")))
                        {
                            string processedPattern = pattern[1..];
                            string regexPattern = processedPattern.StartsWith("re:")
                                ? processedPattern[3..]
                                : processedPattern.Contains("*")
                                    ? processedPattern.Replace("*", ".*")
                                    : $"\\b{Regex.Escape(processedPattern)}\\b";

                            bool matchResult = Regex.IsMatch(weaponName, regexPattern, RegexOptions.IgnoreCase);
                            if (matchResult)
                            {
                                nameMatch = false;
                                break;
                            }
                        }
                    }

                    // Check optional patterns (OR logic)
                    if (nameMatch && hasOptionalPatterns)
                    {
                        bool anyOptionalMatch = false;
                        foreach (var pattern in namePatterns.Where(p => !p.StartsWith("+") && !p.StartsWith("-")))
                        {
                            string regexPattern = pattern.StartsWith("re:")
                                ? pattern[3..]
                                : pattern.Contains("*")
                                    ? pattern.Replace("*", ".*")
                                    : $"\\b{Regex.Escape(pattern)}\\b";

                            if (Regex.IsMatch(weaponName, regexPattern, RegexOptions.IgnoreCase))
                            {
                                anyOptionalMatch = true;
                                break;
                            }
                        }
                        nameMatch = anyOptionalMatch;
                    }
                }

                // Process keyword patterns
                bool keywordMatch = true;
                if (!string.IsNullOrWhiteSpace(settings.KeywordIDs))
                {
                    var hasRequiredPatterns = keywordPatternList.Any(p => p.StartsWith("+"));
                    var hasForbiddenPatterns = keywordPatternList.Any(p => p.StartsWith("-"));
                    var hasOptionalPatterns = keywordPatternList.Any(p => !p.StartsWith("+") && !p.StartsWith("-"));

                    // Check required patterns (AND logic)
                    if (hasRequiredPatterns)
                    {
                        foreach (var pattern in keywordPatternList.Where(p => p.StartsWith("+")))
                        {
                            string processedPattern = pattern[1..];
                            string regexPattern = processedPattern.StartsWith("re:")
                                ? processedPattern[3..]
                                : processedPattern.Contains("*")
                                    ? processedPattern.Replace("*", ".*")
                                    : $"\\b{Regex.Escape(processedPattern)}\\b";

                            if (!keywords.Any(k => Regex.IsMatch(k, regexPattern, RegexOptions.IgnoreCase)))
                            {
                                keywordMatch = false;
                                break;
                            }
                        }
                    }

                    // Check forbidden patterns (AND logic)
                    if (keywordMatch && hasForbiddenPatterns)
                    {
                        foreach (var pattern in keywordPatternList.Where(p => p.StartsWith("-")))
                        {
                            string processedPattern = pattern[1..];
                            string regexPattern = processedPattern.StartsWith("re:")
                                ? processedPattern[3..]
                                : processedPattern.Contains("*")
                                    ? processedPattern.Replace("*", ".*")
                                    : $"\\b{Regex.Escape(processedPattern)}\\b";

                            if (keywords.Any(k => Regex.IsMatch(k, regexPattern, RegexOptions.IgnoreCase)))
                            {
                                keywordMatch = false;
                                break;
                            }
                        }
                    }

                    // Check optional patterns (OR logic)
                    if (keywordMatch && hasOptionalPatterns)
                    {
                        bool anyOptionalMatch = false;
                        foreach (var pattern in keywordPatternList.Where(p => !p.StartsWith("+") && !p.StartsWith("-")))
                        {
                            string regexPattern = pattern.StartsWith("re:")
                                ? pattern[3..]
                                : pattern.Contains("*")
                                    ? pattern.Replace("*", ".*")
                                    : $"\\b{Regex.Escape(pattern)}\\b";

                            if (keywords.Any(k => Regex.IsMatch(k, regexPattern, RegexOptions.IgnoreCase)))
                            {
                                anyOptionalMatch = true;
                                break;
                            }
                        }
                        keywordMatch = anyOptionalMatch;
                    }
                }
                else
                {
                    // If KeywordIDs is empty, we should ignore keyword matching
                    keywordMatch = true;
                }

                // Determine final match based on SearchLogic
                bool finalMatch;
                if (string.IsNullOrWhiteSpace(settings.NamedIDs) && string.IsNullOrWhiteSpace(settings.KeywordIDs))
                {
                    // If both fields are empty, no match
                    finalMatch = false;
                }
                else if (string.IsNullOrWhiteSpace(settings.NamedIDs))
                {
                    // If only NamedIDs is empty, use keyword match
                    finalMatch = keywordMatch;
                }
                else if (string.IsNullOrWhiteSpace(settings.KeywordIDs))
                {
                    // If only KeywordIDs is empty, use name match
                    finalMatch = nameMatch;
                }
                else
                {
                    // Both fields have patterns, use SearchLogic
                    finalMatch = settings.SearchLogic == LogicOperator.AND
                        ? nameMatch && keywordMatch
                        : nameMatch || keywordMatch;
                }

                DebugLog($"     ----------------------------------------");
                DebugLog($"     Match result for setting key {settingKey}: {finalMatch}");
                DebugLog($"     ----------------------------------------\n");

                if (finalMatch)
                {
                    DebugLog($"------------------- GetWeaponSettingKey end -------------------");
                    return settingKey;
                }
            }

            DebugLog($"     No matching weapon setting found");
            DebugLog($"------------------- GetWeaponSettingKey end -------------------");
            return string.Empty;
        }

        /// <summary>
        /// Gets the damage offset for a weapon based on its material and settings
        /// </summary>
        public int? GetDamageOffset(IWeaponGetter weapon, ILinkCache linkCache, bool includeWACCF)
        {
            DebugLog($"------------------- GetDamageOffset start -------------------");
            DebugLog($"     Getting Damage Offset:");
            DebugLog($"     Weapon: {weapon?.EditorID ?? "null"}");
            DebugLog($"     Include WACCF: {includeWACCF}");

            if (weapon == null || linkCache == null)
            {
                DebugLog("     Result: null (weapon or linkCache is null)");
                DebugLog($"------------------- GetDamageOffset end -------------------");
                return null;
            }

            // Get weapon name, not editor id
            string weaponName = weapon.Name?.String ?? "";
            DebugLog($"     Name: {weaponName}");

            if (string.IsNullOrEmpty(weaponName))
            {
                DebugLog("     Result: null (weapon name is empty)");
                DebugLog($"------------------- GetDamageOffset end -------------------");
                return null;
            }

            // Get weapon keywords
            List<string> weaponKeywords = GetWeaponKeywords(weapon, linkCache);
            DebugLog($"     Keywords: {string.Join(", ", weaponKeywords)}");

            // Get weapon skill type
            WeaponSkill? weaponSkill = GetWeaponSkillType(weapon);
            DebugLog($"     Skill: {weaponSkill}");

            if (weaponSkill == null)
            {
                DebugLog("     Result: null (could not determine weapon skill)");
                DebugLog($"------------------- GetDamageOffset end -------------------");
                return null;
            }

            // Search in material_data_name.json
            DebugLog($"     Checking name-based material data...");
            int? nameBasedOffset = GetNameBasedDamageOffset(weaponName, weaponSkill.Value, includeWACCF);
            if (nameBasedOffset.HasValue)
            {
                DebugLog($"     Found name-based offset: {nameBasedOffset.Value}");
                DebugLog($"------------------- GetDamageOffset end -------------------");
                return nameBasedOffset.Value;
            }

            // Search in material_data_keyword.json
            DebugLog($"     Checking keyword-based material data...");
            int? keywordBasedOffset = GetKeywordBasedDamageOffset(weaponKeywords, weaponSkill.Value, includeWACCF);
            if (keywordBasedOffset.HasValue)
            {
                DebugLog($"     Found keyword-based offset: {keywordBasedOffset.Value}");
                DebugLog($"------------------- GetDamageOffset end -------------------");
                return keywordBasedOffset.Value;
            }

            // No match found
            DebugLog($"     No damage offset found for {weaponName}");
            DebugLog($"------------------- GetDamageOffset end -------------------");
            return null;
        }

        /// <summary>
        /// Gets the damage offset based on the weapon name
        /// </summary>
        private int? GetNameBasedDamageOffset(string weaponName, WeaponSkill weaponSkill, bool includeWACCF)
        {
            DebugLog($"------------------- GetNameBasedDamageOffset start -------------------");
            DebugLog($"     Getting Name-Based Damage Offset:");
            DebugLog($"     Name: '{weaponName}'");
            DebugLog($"     Skill: {weaponSkill}");
            DebugLog($"     Include WACCF: {includeWACCF}");

            var materialDataName = GetLoadedData(ref _materialDataName);
            DebugLog($"     Loaded {materialDataName.Count} name-based material data entries");

            int highestOffset = int.MinValue;
            bool foundMatch = false;

            foreach (var material in materialDataName)
            {
                // Convert the keyword to a regex pattern, properly handling wildcards
                string pattern = material.Keyword;
                if (!string.IsNullOrEmpty(pattern))
                {
                    // Add word boundaries around the pattern
                    pattern = $"\\b{pattern.Replace("*", ".*")}\\b";
                }
                else
                {
                    pattern = "^$"; // Only match empty strings if keyword is empty
                }
                DebugLog($"     Checking material pattern: {pattern}");

                if (Regex.IsMatch(weaponName, pattern, RegexOptions.IgnoreCase))
                {
                    foundMatch = true;
                    int offset = 0;

                    // Determine the appropriate offset based on weapon skill and WACCF setting
                    if (weaponSkill == WeaponSkill.OneHanded)
                    {
                        offset = includeWACCF ? material.DamageOffset1hWaccf : material.DamageOffset1h;
                        DebugLog($"     One-handed weapon, offset: {offset}");
                    }
                    else if (weaponSkill == WeaponSkill.TwoHanded)
                    {
                        offset = includeWACCF ? material.DamageOffset2hWaccf : material.DamageOffset2h;
                        DebugLog($"     Two-handed weapon, offset: {offset}");
                    }

                    // Keep track of the highest offset
                    if (offset > highestOffset)
                    {
                        highestOffset = offset;
                        DebugLog($"     New highest offset: {highestOffset}");
                    }
                }
            }

            DebugLog($"     Result: {(foundMatch ? highestOffset : (int?)null)}");
            DebugLog($"------------------- GetNameBasedDamageOffset end -------------------");
            return foundMatch ? highestOffset : (int?)null;
        }

        /// <summary>
        /// Gets the damage offset based on the weapon keywords
        /// </summary>
        private int? GetKeywordBasedDamageOffset(List<string> weaponKeywords, WeaponSkill weaponSkill, bool includeWACCF)
        {
            DebugLog($"------------------- GetKeywordBasedDamageOffset start -------------------");
            DebugLog($"     Getting Keyword-Based Damage Offset:");
            DebugLog($"     Keywords: {string.Join(", ", weaponKeywords)}");
            DebugLog($"     Skill: {weaponSkill}");
            DebugLog($"     Include WACCF: {includeWACCF}");

            var materialData = GetLoadedData(ref _materialData);
            DebugLog($"     Loaded {materialData.Count} keyword-based material data entries");

            int highestOffset = int.MinValue;
            bool foundMatch = false;

            foreach (var material in materialData)
            {
                // Convert the keyword to a regex pattern, properly handling wildcards
                string pattern = material.Keyword;
                if (!string.IsNullOrEmpty(pattern))
                {
                    pattern = pattern.Replace("*", ".*");
                }
                else
                {
                    pattern = "^$"; // Only match empty strings if keyword is empty
                }
                DebugLog($"     Checking material pattern: {pattern}");

                foreach (var keyword in weaponKeywords)
                {
                    if (Regex.IsMatch(keyword, pattern, RegexOptions.IgnoreCase))
                    {
                        foundMatch = true;
                        int offset = 0;

                        // Determine the appropriate offset based on weapon skill and WACCF setting
                        if (weaponSkill == WeaponSkill.OneHanded)
                        {
                            offset = includeWACCF ? material.DamageOffset1hWaccf : material.DamageOffset1h;
                            DebugLog($"     One-handed weapon, offset: {offset}");
                        }
                        else if (weaponSkill == WeaponSkill.TwoHanded)
                        {
                            offset = includeWACCF ? material.DamageOffset2hWaccf : material.DamageOffset2h;
                            DebugLog($"     Two-handed weapon, offset: {offset}");
                        }

                        // Keep track of the highest offset
                        if (offset > highestOffset)
                        {
                            highestOffset = offset;
                            DebugLog($"     New highest offset: {highestOffset}");
                        }
                    }
                }
            }

            DebugLog($"     Result: {(foundMatch ? highestOffset : (int?)null)}");
            DebugLog($"------------------- GetKeywordBasedDamageOffset end -------------------");
            return foundMatch ? highestOffset : (int?)null;
        }

        /// <summary>
        /// Checks if a weapon is a special weapon by form key or editor ID
        /// </summary>
        public bool IsSpecialWeapon(IWeaponGetter weapon)
        {
            DebugLog($"------------------- IsSpecialWeapon start -------------------");
            if (weapon == null)
            {
                DebugLog("     IsSpecialWeapon: Weapon is null");
                DebugLog($"------------------- IsSpecialWeapon end -------------------");
                return false;
            }

            var specialWeapons = GetLoadedData(ref _specialWeapons);
            if (specialWeapons == null || specialWeapons.Count == 0)
            {
                DebugLog("     IsSpecialWeapon: No special weapons list loaded");
                DebugLog($"------------------- IsSpecialWeapon end -------------------");
                return false;
            }

            var isSpecial = specialWeapons.Any(sw =>
                !string.IsNullOrEmpty(sw.FormKey) &&
                FormKey.TryFactory(sw.FormKey, out var formKey) &&
                weapon.FormKey == formKey);

            DebugLog($"     IsSpecialWeapon: Weapon {weapon.EditorID} (FormKey: {weapon.FormKey}) is{(isSpecial ? "" : " not")} special");
            DebugLog($"------------------- IsSpecialWeapon end -------------------");
            return isSpecial;
        }

        /// <summary>
        /// Gets the list of special weapons from the loaded data
        /// </summary>
        public List<SpecialWeaponData> GetLoadedSpecialWeapons()
        {
            DebugLog($"------------------- GetLoadedSpecialWeapons start -------------------");
            DebugLog("     GetLoadedSpecialWeapons called");
            var result = GetLoadedData(ref _specialWeapons);
            DebugLog($"     Returning {result.Count} special weapons");
            DebugLog($"------------------- GetLoadedSpecialWeapons end -------------------");
            return result;
        }
    }
}