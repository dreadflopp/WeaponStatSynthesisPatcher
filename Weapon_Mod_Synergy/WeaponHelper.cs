using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Mutagen.Bethesda;
using Noggog;
#pragma warning disable CA1416 

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
        [JsonPropertyName("editor_id")]
        public string EditorID { get; set; } = string.Empty;

        [JsonPropertyName("form_key")]
        public string FormKey { get; set; } = string.Empty;

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

        [JsonPropertyName("critical_damage_chance_multiplier_offset")]
        public float? CriticalDamageChanceMultiplierOffset { get; set; }

        [JsonPropertyName("critical_damage_chance_multiplier_offset_waccf")]
        public float? CriticalDamageChanceMultiplierOffsetWaccf { get; set; }

        [JsonPropertyName("critical_damage_chance_multiplier_offset_artificer")]
        public float? CriticalDamageChanceMultiplierOffsetArtificer { get; set; }
    }

    public class WeaponHelper
    {
        // Debug flag to enable/disable debug output
        private static bool _debugMode = true;
        public static bool DebugMode
        {
            get => _debugMode;
            set => _debugMode = value;
        }

        private readonly Dictionary<string, WeaponSettings> _weaponSettings;
        private static Action<string>? _logger;
        private static List<MaterialData> _materialDataKeyword = new();
        private static List<MaterialData> _materialDataName = new();
        private static List<SpecialWeaponData> _specialWeapons = new();
        private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;

        public WeaponHelper(Dictionary<string, WeaponSettings> weaponSettings, Action<string> logger, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            _weaponSettings = weaponSettings ?? throw new ArgumentNullException(nameof(weaponSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            LoadMaterialData();
        }

        private void LoadMaterialData()
        {
            try
            {
                var materialNamePath = _state.RetrieveInternalFile("material_data_name.json");
                var materialKeywordPath = _state.RetrieveInternalFile("material_data_keyword.json");
                var specialWeaponsPath = _state.RetrieveInternalFile("special_weapons.json");

                _materialDataName = LoadJsonData<List<MaterialData>>(materialNamePath) ?? new List<MaterialData>();
                _materialDataKeyword = LoadJsonData<List<MaterialData>>(materialKeywordPath) ?? new List<MaterialData>();
                _specialWeapons = LoadJsonData<List<SpecialWeaponData>>(specialWeaponsPath) ?? new List<SpecialWeaponData>();

                DebugLog("Material data loaded successfully");
            }
            catch (Exception ex)
            {
                DebugLog($"Error loading material data: {ex.Message}");
                throw;
            }
        }

        private static T? LoadJsonData<T>(string filePath) where T : class
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Configuration file not found: {filePath}");
            }

            string jsonContent = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                throw new InvalidDataException($"Configuration file is empty: {filePath}");
            }

            return System.Text.Json.JsonSerializer.Deserialize<T>(jsonContent);
        }

        // Debug logging function
        public void DebugLog(string message)
        {
            if (_debugMode)
            {
                _logger?.Invoke($"[DEBUG] {message}");
            }
        }

        // Generic method to get loaded data
        private T GetLoadedData<T>(ref T dataField)
        {
            try
            {
                if (_materialDataKeyword.Count == 0 || _materialDataName.Count == 0 || _specialWeapons.Count == 0)
                {
                    LoadMaterialData();
                }
                return dataField;
            }
            catch (Exception ex)
            {
                DebugLog($"Error loading data: {ex.Message}");
                return dataField; // Return the existing data even if it's empty
            }
        }

        /// <summary>
        /// Gets the weapon skill type (1h or 2h) based on the weapon's Skill property
        /// If weapon Skill is Skill.OneHanded, return WeaponSkill.OneHanded
        /// If weapon Skill is Skill.TwoHanded, return WeaponSkill.TwoHanded
        /// Otherwise, return null
        /// </summary>
        public WeaponSkill? GetWeaponSkillType(IWeaponGetter weapon)
        {
            if (weapon == null || weapon.Data == null)
            {
                return null;
            }

            // Get the skill from the weapon's Data property
            var skill = weapon.Data.Skill;

            // Determine the weapon skill type based on the Skill property
            if (skill.HasValue)
            {
                // Convert the skill to a string for comparison
                string skillString = skill.Value.ToString();

                if (skillString.Contains("OneHanded"))
                {
                    return WeaponSkill.OneHanded;
                }
                else if (skillString.Contains("TwoHanded"))
                {
                    return WeaponSkill.TwoHanded;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all keywords from a weapon as a list of strings
        /// </summary>
        public List<string> GetWeaponKeywords(IWeaponGetter weapon, ILinkCache linkCache)
        {
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
            }
            return keywords;
        }

        /// <summary>
        /// Gets the weapon setting key based on the weapon's name, keywords, and skill type
        /// </summary>
        public string? GetWeaponSettingKey(IWeaponGetter weapon, ILinkCache linkCache)
        {
            if (weapon == null || linkCache == null)
            {
                DebugLog("   Weapon or link cache is null");
                return null;
            }

            string weaponName = weapon.Name?.String ?? string.Empty;
            WeaponSkill? skillType = GetWeaponSkillType(weapon);
            List<string> keywords = GetWeaponKeywords(weapon, linkCache);
            if (string.IsNullOrEmpty(weaponName) && keywords.Count == 0)
            {
                DebugLog("   Weapon name and keywords are empty");
                return null;
            }
            DebugLog($"      Skill Type: {skillType}");
            DebugLog($"      Keywords: {string.Join(", ", keywords)}");

            foreach (var setting in _weaponSettings)
            {
                var settingKey = setting.Key;
                var settings = setting.Value;

                DebugLog($"   Checking setting key: {settingKey}");
                DebugLog($"      Skill: {settings.Skill}");
                DebugLog($"      NamedIDs: {settings.NamedIDs}");
                DebugLog($"      KeywordIDs: {settings.KeywordIDs}");
                DebugLog($"      SearchLogic: {settings.SearchLogic}");

                // Check if both fields are empty
                if (string.IsNullOrWhiteSpace(settings.NamedIDs) && string.IsNullOrWhiteSpace(settings.KeywordIDs))
                {
                    DebugLog($"   Both NamedIDs and KeywordIDs are empty, skipping");
                    continue;
                }

                // Check skill type match
                if (settings.Skill != skillType && settings.Skill != WeaponSkill.Either)
                {
                    DebugLog($"   Skill type mismatch: {settings.Skill} != {skillType}");
                    continue;
                }

                // Process name patterns
                bool nameMatch = true;
                if (!string.IsNullOrWhiteSpace(settings.NamedIDs))
                {
                    DebugLog($"   Matching name patterns");
                    nameMatch = IsMatch(weaponName, settings.NamedIDs);
                    DebugLog($"   Name match: {nameMatch}");
                }

                // Process keyword patterns
                bool keywordMatch = true;
                if (!string.IsNullOrWhiteSpace(settings.KeywordIDs))
                {
                    DebugLog($"   Matching keyword patterns");
                    keywordMatch = IsMatch(string.Join(", ", keywords), settings.KeywordIDs);
                    DebugLog($"   Keyword match: {keywordMatch}");
                }

                // Determine final match based on SearchLogic
                bool finalMatch;
                if (string.IsNullOrWhiteSpace(settings.NamedIDs))
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
                DebugLog($"   Match result for setting key {settingKey}: {finalMatch}");

                if (finalMatch)
                {
                    DebugLog($"   Returning setting key: {settingKey}");
                    return settingKey;
                }
            }

            DebugLog($"   No matching weapon setting found");
            return null;
        }

        /// <summary>
        /// Gets the damage offset for a weapon based on its material and settings
        /// </summary>
        public int? GetDamageOffset(IWeaponGetter weapon, ILinkCache linkCache, bool includeWACCF)
        {

            if (weapon == null || linkCache == null)
            {
                DebugLog("   ERROR: weapon or linkCache is null");
                return null;
            }

            // Get weapon name, not editor id
            string weaponName = weapon.Name?.String ?? "";
            List<string> weaponKeywords = GetWeaponKeywords(weapon, linkCache);
            if (weaponKeywords.Count == 0 && string.IsNullOrEmpty(weaponName))
            {
                DebugLog("   Warning: weapon name and keywords are empty");
                return null;
            }

            if (string.IsNullOrEmpty(weaponName))
            {
                DebugLog("   Warning: weapon name is empty");
            }

            // Get weapon keywords

            if (weaponKeywords.Count == 0)
            {
                DebugLog("   Warning: weapon keywords are empty");
            }
            else
            {
                DebugLog($"   Keywords: {string.Join(", ", weaponKeywords)}");
            }

            // Get weapon skill type
            WeaponSkill? weaponSkill = GetWeaponSkillType(weapon);

            if (weaponSkill == null)
            {
                DebugLog("   Warning: could not determine weapon skill");
                return null;
            }

            // Search in material_data_name.json
            if (!string.IsNullOrEmpty(weaponName))
            {
                int? nameBasedOffset = GetNameBasedDamageOffset(weaponName, weaponSkill.Value, includeWACCF);
                if (nameBasedOffset.HasValue)
                {
                    DebugLog($"   Found name-based offset: {nameBasedOffset.Value}");
                    return nameBasedOffset.Value;
                }
            }

            // Search in material_data_keyword.json
            if (weaponKeywords.Count > 0)
            {
                int? keywordBasedOffset = GetKeywordBasedDamageOffset(weaponKeywords, weaponSkill.Value, includeWACCF);
                if (keywordBasedOffset.HasValue)
                {
                    DebugLog($"   Found keyword-based offset: {keywordBasedOffset.Value}");
                    return keywordBasedOffset.Value;
                }
            }

            // No match found
            DebugLog($"   No damage offset found for {weaponName}");
            return null;
        }

        /// <summary>
        /// Gets the damage offset based on the weapon name
        /// </summary>
        private int? GetNameBasedDamageOffset(string weaponName, WeaponSkill weaponSkill, bool includeWACCF)
        {

            var materialDataName = GetLoadedData(ref _materialDataName);

            int highestOffset = int.MinValue;
            bool foundMatch = false;

            foreach (var material in materialDataName)
            {
                if (string.IsNullOrEmpty(material.Keyword))
                {
                    DebugLog($"   Warning: material {System.Text.Json.JsonSerializer.Serialize(material)} keyword is null");
                    continue;
                }

                // Convert the json keyword to a regex pattern, properly handling wildcards
                string pattern = material.Keyword;

                // Add word boundaries around the pattern
                pattern = $"\\b{pattern.Replace("*", ".*")}\\b";

                DebugLog($"   Checking material pattern: {pattern}");

                if (Regex.IsMatch(weaponName, pattern, RegexOptions.IgnoreCase))
                {
                    foundMatch = true;
                    int offset = 0;

                    // Determine the appropriate offset based on weapon skill and WACCF setting
                    if (weaponSkill == WeaponSkill.OneHanded)
                    {
                        offset = includeWACCF ? material.DamageOffset1hWaccf : material.DamageOffset1h;
                        DebugLog($"   One-handed weapon, offset: {offset}");
                    }
                    else if (weaponSkill == WeaponSkill.TwoHanded)
                    {
                        offset = includeWACCF ? material.DamageOffset2hWaccf : material.DamageOffset2h;
                        DebugLog($"   Two-handed weapon, offset: {offset}");
                    }

                    // Keep track of the highest offset
                    if (offset > highestOffset)
                    {
                        highestOffset = offset;
                        DebugLog($"   New highest offset: {highestOffset}");
                    }
                }
            }
            return foundMatch ? highestOffset : (int?)null;
        }

        /// <summary>
        /// Gets the damage offset based on the weapon keywords
        /// </summary>
        private int? GetKeywordBasedDamageOffset(List<string> weaponKeywords, WeaponSkill weaponSkill, bool includeWACCF)
        {
            var materialData = GetLoadedData(ref _materialDataKeyword);

            int highestOffset = int.MinValue;
            bool foundMatch = false;

            foreach (var material in materialData)
            {
                if (string.IsNullOrEmpty(material.Keyword))
                {
                    DebugLog($"   Warning: material {System.Text.Json.JsonSerializer.Serialize(material)} keyword is null");
                    continue;
                }

                // Convert the keyword to a regex pattern, properly handling wildcards
                string pattern = material.Keyword;

                pattern = pattern.Replace("*", ".*");

                DebugLog($"   Checking material pattern: {pattern}");

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
                            DebugLog($"   One-handed weapon, offset: {offset}");
                        }
                        else if (weaponSkill == WeaponSkill.TwoHanded)
                        {
                            offset = includeWACCF ? material.DamageOffset2hWaccf : material.DamageOffset2h;
                            DebugLog($"   Two-handed weapon, offset: {offset}");
                        }

                        // Keep track of the highest offset
                        if (offset > highestOffset)
                        {
                            highestOffset = offset;
                            DebugLog($"   New highest offset: {highestOffset}");
                        }
                    }
                }
            }
            return foundMatch ? highestOffset : (int?)null;
        }

        /// <summary>
        /// Checks if a weapon is a special weapon by form key or editor ID
        /// </summary>
        public bool IsSpecialWeapon(IWeaponGetter weapon)
        {
            if (weapon == null)
            {
                return false;
            }

            var specialWeapons = GetLoadedData(ref _specialWeapons);
            if (specialWeapons == null || specialWeapons.Count == 0)
            {
                return false;
            }

            var isSpecial = specialWeapons.Any(sw =>
                !string.IsNullOrEmpty(sw.FormKey) &&
                FormKey.TryFactory(sw.FormKey, out var formKey) &&
                weapon.FormKey == formKey);
            return isSpecial;
        }

        /// <summary>
        /// Gets the list of special weapons from the loaded data
        /// </summary>
        public List<SpecialWeaponData> GetLoadedSpecialWeapons()
        {
            return GetLoadedData(ref _specialWeapons);
        }

        /// <summary>
        /// Checks if input matches any of the semicolons separated patterns
        /// </summary>
        private bool IsMatch(string input, string patterns)
        {
            DebugLog($"         Matching logic running...");
            DebugLog($"         Input: {input}");
            DebugLog($"         Patterns: {patterns}");
            // split patterns by semicolons
            List<string> patternsList = patterns.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            // if list is empty, return false
            if (patternsList.Count == 0)
            {
                DebugLog("         No patterns found. Returning false");
                return false;
            }

            // evaluate all patterns that begins with -. If the are found in input, return false
            foreach (var pattern in patternsList.Where(p => p.StartsWith('-')).ToList())
            {
                string regexPattern = WildcardToRegex(pattern[1..]);
                DebugLog($"         Checking forbidden regex pattern: {regexPattern}");
                if (Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase))
                {
                    DebugLog($"         Pattern found. Returning false");
                    return false;
                }

                patternsList.Remove(pattern);
            }

            // if no patterns remain, we are done
            if (patternsList.Count == 0)
            {
                DebugLog("         No forbidden patterns found. Returning true");
                return true;
            }

            // evaluate the remaining patterns, we need to find at least one match
            foreach (var pattern in patternsList)
            {
                string regexPattern = WildcardToRegex(pattern);
                DebugLog($"         Checking allowed regex pattern: {regexPattern}");
                if (Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase))
                {
                    DebugLog($"         Pattern found. Returning true");
                    return true;
                }
            }

            DebugLog("         No allowed patterns found. Returning false");
            return false;
        }

        // Converts wildcard pattern to regex with word boundaries
        private static string WildcardToRegex(string pattern)
        {
            string escaped = Regex.Escape(pattern);
            string withWildcards = escaped.Replace(@"\*", ".*");
            return $@"\b{withWildcards}\b";
        }
    }
}