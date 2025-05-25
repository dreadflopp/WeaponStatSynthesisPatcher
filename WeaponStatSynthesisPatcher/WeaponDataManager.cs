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
    public struct MaterialOffsets
    {
        public int DamageOffset;
        public float ReachOffset;
        public float SpeedOffset;
        public float StaggerOffset;
        public float CriticalDamageOffset;
        public float CriticalDamageChanceMultiplierOffset;
        public float CriticalDamageMultiplierOffset;

        public static MaterialOffsets Zero => new()
        {
            DamageOffset = 0,
            ReachOffset = 0f,
            SpeedOffset = 0f,
            StaggerOffset = 0f,
            CriticalDamageOffset = 0f,
            CriticalDamageChanceMultiplierOffset = 0f,
            CriticalDamageMultiplierOffset = 0f
        };
    }

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

        [JsonPropertyName("reach_offset")]
        public float ReachOffset { get; set; }

        [JsonPropertyName("speed_offset")]
        public float SpeedOffset { get; set; }

        [JsonPropertyName("stagger_offset")]
        public float StaggerOffset { get; set; }

        [JsonPropertyName("critical_damage_offset")]
        public float CriticalDamageOffset { get; set; }

        [JsonPropertyName("critical_damage_chance_multiplier_offset")]
        public float CriticalDamageChanceMultiplierOffset { get; set; }

        [JsonPropertyName("critical_damage_multiplier_offset")]
        public float CriticalDamageMultiplierOffset { get; set; }
    }

    public class SpecialWeaponData
    {
        [JsonPropertyName("editor_id")]
        public string EditorID { get; set; } = string.Empty;

        [JsonPropertyName("form_key")]
        public string FormKey { get; set; } = string.Empty;

        [JsonPropertyName("damage_offset")]
        public int? DamageOffset { get; set; }

        [JsonPropertyName("speed_offset")]
        public float? SpeedOffset { get; set; }

        [JsonPropertyName("reach_offset")]
        public float? ReachOffset { get; set; }

        [JsonPropertyName("stagger_offset")]
        public float? StaggerOffset { get; set; }

        [JsonPropertyName("critical_damage_offset")]
        public float? CriticalDamageOffset { get; set; }

        [JsonPropertyName("critical_damage_chance_multiplier_offset")]
        public float? CriticalDamageChanceMultiplierOffset { get; set; }
    }

    public class DefaultWeaponStatsData
    {
        [JsonPropertyName("Keyword")]
        public string Keyword { get; set; } = string.Empty;

        [JsonPropertyName("damage")]
        public int Damage { get; set; }

        [JsonPropertyName("damage_waccf")]
        public int DamageWaccf { get; set; }

        [JsonPropertyName("speed")]
        public decimal Speed { get; set; }

        [JsonPropertyName("reach")]
        public decimal Reach { get; set; }

        [JsonPropertyName("stagger")]
        public decimal Stagger { get; set; }

        [JsonPropertyName("critical_damage")]
        public int CriticalDamage { get; set; }

        [JsonPropertyName("critical_chance_mult")]
        public decimal CriticalChanceMult { get; set; } = 1.0m;
    }

    public class WeaponDataManager
    {
        private readonly Settings _settings;
        private static Action<string>? _logger;
        private static List<MaterialData> _materialDataKeyword = new();
        private static List<MaterialData> _materialDataName = new();
        private static List<SpecialWeaponData> _specialWeapons = new();
        private static List<DefaultWeaponStatsData> _defaultWeaponStatsData = new();
        private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
        private List<(string Key, WeaponSettings Settings)> _sortedSettings = new();

        // Constants for value bounds
        private const int MAX_VALUE = 60000;

        /// <summary>
        /// Ensures an unsigned short value stays within safe bounds (0 to MAX_VALUE)
        /// </summary>
        public static ushort ClampUshort(int value)
        {
            return (ushort)Math.Max(0, Math.Min(MAX_VALUE, value));
        }

        /// <summary>
        /// Ensures a float value stays within safe bounds (0 to MAX_VALUE)
        /// </summary>
        public static float ClampFloat(float value)
        {
            return Math.Max(0f, Math.Min(MAX_VALUE, value));
        }

        public void SetSortedSettings(List<(string Key, WeaponSettings Settings)> settings)
        {
            _sortedSettings = settings;
        }

        public WeaponDataManager(Settings settings, Action<string> logger, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            LoadWeaponAndMaterialData();
        }

        private void LoadWeaponAndMaterialData()
        {
            try
            {
                var materialNamePath = _state.RetrieveInternalFile("material_data_name.json");
                var materialKeywordPath = _state.RetrieveInternalFile("material_data_keyword.json");
                var specialWeaponsPath = _state.RetrieveInternalFile("special_weapons.json");
                var defaultWeaponStatsPath = _state.RetrieveInternalFile("default_weapon_stats.json");

                _materialDataName = LoadJsonData<List<MaterialData>>(materialNamePath) ?? new List<MaterialData>();
                _materialDataKeyword = LoadJsonData<List<MaterialData>>(materialKeywordPath) ?? new List<MaterialData>();
                _specialWeapons = LoadJsonData<List<SpecialWeaponData>>(specialWeaponsPath) ?? new List<SpecialWeaponData>();
                _defaultWeaponStatsData = LoadJsonData<List<DefaultWeaponStatsData>>(defaultWeaponStatsPath) ?? new List<DefaultWeaponStatsData>();

                // Sort only name-based material data by keyword length in descending order
                if (_materialDataName != null)
                {
                    _materialDataName = _materialDataName.OrderByDescending(m => m.Keyword.Length).ToList();
                }

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
            if (_settings.DebugMode)
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
                    LoadWeaponAndMaterialData();
                }
                return dataField;
            }
            catch (Exception ex)
            {
                DebugLog($"Error loading data: {ex.Message}");
                return dataField; // Return the existing data even if it's empty
            }
        }
        public List<SpecialWeaponData> GetLoadedSpecialWeapons() { return GetLoadedData(ref _specialWeapons); }

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

            // Use stored sorted settings
            DebugLog($"      Sorted settings: {string.Join(", ", _sortedSettings.Select(kvp => $"{kvp.Key}: {kvp.Settings.MatchLogicSettings.Skill}"))}");

            foreach (var (settingKey, settings) in _sortedSettings)
            {
                DebugLog($"   Checking setting key: {settingKey}");
                DebugLog($"      Skill: {settings.MatchLogicSettings.Skill}");
                DebugLog($"      NamedIDs: {settings.MatchLogicSettings.NamedIDs}");
                DebugLog($"      KeywordIDs: {settings.MatchLogicSettings.KeywordIDs}");
                DebugLog($"      SearchLogic: {settings.MatchLogicSettings.SearchLogic}");

                // Check if both fields are empty
                if (string.IsNullOrWhiteSpace(settings.MatchLogicSettings.NamedIDs) && string.IsNullOrWhiteSpace(settings.MatchLogicSettings.KeywordIDs))
                {
                    DebugLog($"   Both NamedIDs and KeywordIDs are empty, skipping");
                    continue;
                }

                // Check skill type match
                if (settings.MatchLogicSettings.Skill != skillType)
                {
                    DebugLog($"   Skill type mismatch: {settings.MatchLogicSettings.Skill} != {skillType}");
                    continue;
                }

                // Process name patterns
                bool nameMatch = true;
                if (!string.IsNullOrWhiteSpace(settings.MatchLogicSettings.NamedIDs))
                {
                    DebugLog($"   Matching name patterns");
                    nameMatch = IsMatch(weaponName, settings.MatchLogicSettings.NamedIDs);
                    DebugLog($"   Name match: {nameMatch}");
                }

                // Process keyword patterns
                bool keywordMatch = true;
                if (!string.IsNullOrWhiteSpace(settings.MatchLogicSettings.KeywordIDs))
                {
                    DebugLog($"   Matching keyword patterns");
                    keywordMatch = IsMatch(string.Join(", ", keywords), settings.MatchLogicSettings.KeywordIDs);
                    DebugLog($"   Keyword match: {keywordMatch}");
                }

                // Determine final match based on SearchLogic
                bool finalMatch;
                if (string.IsNullOrWhiteSpace(settings.MatchLogicSettings.NamedIDs))
                {
                    // If only NamedIDs is empty, use keyword match
                    finalMatch = keywordMatch;
                }
                else if (string.IsNullOrWhiteSpace(settings.MatchLogicSettings.KeywordIDs))
                {
                    // If only KeywordIDs is empty, use name match
                    finalMatch = nameMatch;
                }
                else
                {
                    // Both fields have patterns, use SearchLogic
                    finalMatch = settings.MatchLogicSettings.SearchLogic == LogicOperator.AND
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

        private MaterialOffsets? CalculateMaterialOffsets(
            string input,
            List<MaterialData> materialData,
            WeaponSkill weaponSkill,
            bool includeWACCF)
        {
            int highestDamageOffset = int.MinValue;
            MaterialOffsets? bestMatch = null;
            bool foundMatch = false;

            foreach (var material in materialData)
            {
                if (string.IsNullOrEmpty(material.Keyword))
                {
                    DebugLog($"   Warning: material {System.Text.Json.JsonSerializer.Serialize(material)} keyword is null");
                    continue;
                }

                // Create a regex pattern that matches the word as a whole word
                string pattern = $"\\b{Regex.Escape(material.Keyword)}\\b";
                DebugLog($"   Checking material pattern: {pattern}");

                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    foundMatch = true;
                    int damageOffset = weaponSkill == WeaponSkill.OneHanded
                        ? (includeWACCF ? material.DamageOffset1hWaccf : material.DamageOffset1h)
                        : (includeWACCF ? material.DamageOffset2hWaccf : material.DamageOffset2h);

                    DebugLog($"   {(weaponSkill == WeaponSkill.OneHanded ? "One-handed" : "Two-handed")} weapon, damage offset: {damageOffset}");
                    DebugLog($"   Additional offsets - Reach: {material.ReachOffset}, Speed: {material.SpeedOffset}, Stagger: {material.StaggerOffset}");
                    DebugLog($"   Critical offsets - Damage: {material.CriticalDamageOffset}, Chance Mult: {material.CriticalDamageChanceMultiplierOffset}, Damage Mult: {material.CriticalDamageMultiplierOffset}");

                    if (damageOffset > highestDamageOffset)
                    {
                        highestDamageOffset = damageOffset;
                        bestMatch = new MaterialOffsets
                        {
                            DamageOffset = damageOffset,
                            ReachOffset = material.ReachOffset,
                            SpeedOffset = material.SpeedOffset,
                            StaggerOffset = material.StaggerOffset,
                            CriticalDamageOffset = material.CriticalDamageOffset,
                            CriticalDamageChanceMultiplierOffset = material.CriticalDamageChanceMultiplierOffset,
                            CriticalDamageMultiplierOffset = material.CriticalDamageMultiplierOffset
                        };
                        DebugLog($"   New best match with damage offset: {highestDamageOffset}");
                    }
                }
            }

            return foundMatch ? bestMatch : null;
        }

        private MaterialOffsets? GetNameBasedMaterialOffsets(string weaponName, WeaponSkill weaponSkill, bool includeWACCF)
        {
            var materialDataName = GetLoadedData(ref _materialDataName);
            return CalculateMaterialOffsets(weaponName, materialDataName, weaponSkill, includeWACCF);
        }

        private MaterialOffsets? GetKeywordBasedMaterialOffsets(List<string> weaponKeywords, WeaponSkill weaponSkill, bool includeWACCF)
        {
            var materialData = GetLoadedData(ref _materialDataKeyword);
            int highestDamageOffset = int.MinValue;
            MaterialOffsets? bestMatch = null;
            bool foundMatch = false;

            foreach (var keyword in weaponKeywords)
            {
                var offsets = CalculateMaterialOffsets(keyword, materialData, weaponSkill, includeWACCF);
                if (offsets.HasValue)
                {
                    foundMatch = true;
                    if (offsets.Value.DamageOffset > highestDamageOffset)
                    {
                        highestDamageOffset = offsets.Value.DamageOffset;
                        bestMatch = offsets.Value;
                    }
                }
            }

            return foundMatch ? bestMatch : null;
        }

        public MaterialOffsets? GetMaterialOffsets(IWeaponGetter weapon, ILinkCache linkCache, bool includeWACCF)
        {
            if (weapon == null || linkCache == null)
            {
                DebugLog("   ERROR: weapon or linkCache is null");
                return null;
            }

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

            if (weaponKeywords.Count == 0)
            {
                DebugLog("   Warning: weapon keywords are empty");
            }
            else
            {
                DebugLog($"   Keywords: {string.Join(", ", weaponKeywords)}");
            }

            WeaponSkill? weaponSkill = GetWeaponSkillType(weapon);
            if (weaponSkill == null)
            {
                DebugLog("   Warning: could not determine weapon skill");
                return null;
            }

            // Try name-based offset first
            if (!string.IsNullOrEmpty(weaponName))
            {
                var nameBasedOffsets = GetNameBasedMaterialOffsets(weaponName, weaponSkill.Value, includeWACCF);
                if (nameBasedOffsets.HasValue)
                {
                    DebugLog($"   Found name-based offsets - Damage: {nameBasedOffsets.Value.DamageOffset}, Reach: {nameBasedOffsets.Value.ReachOffset}, Speed: {nameBasedOffsets.Value.SpeedOffset}, Stagger: {nameBasedOffsets.Value.StaggerOffset}");
                    DebugLog($"   Critical offsets - Damage: {nameBasedOffsets.Value.CriticalDamageOffset}, Chance Mult: {nameBasedOffsets.Value.CriticalDamageChanceMultiplierOffset}, Damage Mult: {nameBasedOffsets.Value.CriticalDamageMultiplierOffset}");
                    return nameBasedOffsets.Value;
                }
            }

            // Try keyword-based offset if name-based failed
            if (weaponKeywords.Count > 0)
            {
                var keywordBasedOffsets = GetKeywordBasedMaterialOffsets(weaponKeywords, weaponSkill.Value, includeWACCF);
                if (keywordBasedOffsets.HasValue)
                {
                    DebugLog($"   Found keyword-based offsets - Damage: {keywordBasedOffsets.Value.DamageOffset}, Reach: {keywordBasedOffsets.Value.ReachOffset}, Speed: {keywordBasedOffsets.Value.SpeedOffset}, Stagger: {keywordBasedOffsets.Value.StaggerOffset}");
                    DebugLog($"   Critical offsets - Damage: {keywordBasedOffsets.Value.CriticalDamageOffset}, Chance Mult: {keywordBasedOffsets.Value.CriticalDamageChanceMultiplierOffset}, Damage Mult: {keywordBasedOffsets.Value.CriticalDamageMultiplierOffset}");
                    return keywordBasedOffsets.Value;
                }
            }

            DebugLog($"   No material offsets found for {weaponName}");
            return null;
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
        /// Checks if the input value (weapon name or keywords) matches any of the given patterns.
        /// Used for both name matching and keyword matching.
        /// </summary>
        /// <param name="input">The input string to check (either weapon name or keywords)</param>
        /// <param name="patterns">Semicolon-separated list of patterns to match against</param>
        /// <returns>True if input matches any pattern, false otherwise</returns>
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

            // evaluate the patterns, we need to find at least one match
            foreach (var pattern in patternsList)
            {
                // Create a regex pattern that matches the word as a whole word
                string regexPattern = $@"\b{Regex.Escape(pattern)}\b";

                if (Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase))
                {
                    DebugLog($"         Pattern '{pattern}' matched input '{input}'. Returning true");
                    return true;
                }
            }

            DebugLog("         No matches found. Returning false");
            return false;
        }

        public List<DefaultWeaponStatsData> GetLoadedDefaultWeaponStatsData()
        {
            return _defaultWeaponStatsData;
        }

        public DefaultWeaponStatsData? GetDefaultWeaponStats(List<string> weaponKeywords)
        {
            foreach (var keyword in weaponKeywords)
            {
                var stats = _defaultWeaponStatsData.FirstOrDefault(d =>
                    string.Equals(d.Keyword, keyword, StringComparison.OrdinalIgnoreCase));
                if (stats != null)
                {
                    return stats;
                }
            }
            return null;
        }

        public (int additionalDamage, float additionalReach, float additionalSpeed, float additionalStagger,
                float additionalCriticalDamageOffset, float additionalCriticalDamageChanceMultiplier,
                float additionalCriticalDamageMultiplier) GetVariantStats(IWeaponGetter weapon, ILinkCache linkCache)
        {
            int additionalDamage = 0;
            decimal additionalReach = 0m;
            decimal additionalSpeed = 0m;
            decimal additionalStagger = 0m;
            decimal additionalCriticalDamageOffset = 0m;
            decimal additionalCriticalDamageChanceMultiplier = 0m;
            decimal additionalCriticalDamageMultiplier = 0m;

            // Get weapon skill type
            var weaponSkill = GetWeaponSkillType(weapon);
            if (weaponSkill == null)
            {
                DebugLog($"Could not determine weapon skill type for {weapon.EditorID}");
                return (additionalDamage, 0f, 0f, 0f, 0f, 0f, 0f);
            }

            // Get weapon keywords
            var weaponKeywords = GetWeaponKeywords(weapon, linkCache);

            // Check all variants for matches
            foreach (var variant in _settings.Variants.Variants)
            {
                // Skip if variant has no NameIDs
                if (string.IsNullOrEmpty(variant.Value.NameIDs))
                    continue;

                // Split NameIDs into individual words to check
                var nameIDs = variant.Value.NameIDs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                // Check if any NameID matches either the weapon name or any keyword
                bool nameOrKeywordMatch = nameIDs.Any(id =>
                {
                    // Create a regex pattern that matches the word as a whole word
                    string pattern = $@"\b{Regex.Escape(id)}\b";

                    // Check weapon name
                    bool nameMatch = Regex.IsMatch(weapon.Name?.String ?? "", pattern, RegexOptions.IgnoreCase);
                    if (nameMatch) return true;

                    // Check keywords
                    bool keywordMatch = weaponKeywords.Any(kw =>
                        Regex.IsMatch(kw, pattern, RegexOptions.IgnoreCase));
                    if (keywordMatch) return true;

                    return false;
                });

                // Check if skill matches
                bool skillMatch = variant.Value.Skill == weaponSkill;

                // Check if weapon has any excluded keywords or name matches
                bool exclusionMatch = false;
                if (!string.IsNullOrEmpty(variant.Value.ExcludeIDs))
                {
                    var excludeIDs = variant.Value.ExcludeIDs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    // Check keywords
                    bool hasExcludedKeyword = weaponKeywords.Any(kw => excludeIDs.Contains(kw, StringComparer.OrdinalIgnoreCase));
                    // Check weapon name for whole word matches
                    bool hasExcludedName = excludeIDs.Any(id =>
                    {
                        string pattern = $@"\b{Regex.Escape(id)}\b";
                        return Regex.IsMatch(weapon.Name?.String ?? "", pattern, RegexOptions.IgnoreCase);
                    });
                    exclusionMatch = hasExcludedKeyword || hasExcludedName;
                }

                // If either name or keyword matches, and skill matches, and no excluded keywords are present, add the variant's stats
                if (nameOrKeywordMatch && skillMatch && !exclusionMatch)
                {
                    additionalDamage += variant.Value.AdditionalDamage;
                    additionalReach += (decimal)variant.Value.AdditionalReach;
                    additionalSpeed += (decimal)variant.Value.AdditionalSpeed;
                    additionalStagger += (decimal)variant.Value.AdditionalStagger;
                    additionalCriticalDamageOffset += (decimal)variant.Value.AdditionalCriticalDamageOffset;
                    additionalCriticalDamageChanceMultiplier += (decimal)variant.Value.AdditionalCriticalDamageChanceMultiplier;
                    additionalCriticalDamageMultiplier += (decimal)variant.Value.AdditionalCriticalDamageMultiplier;
                }
            }

            return (additionalDamage,
                    (float)additionalReach,
                    (float)additionalSpeed,
                    (float)additionalStagger,
                    (float)additionalCriticalDamageOffset,
                    (float)additionalCriticalDamageChanceMultiplier,
                    (float)additionalCriticalDamageMultiplier);
        }

        public WeaponSettings? GetWeaponSettings(string weaponSettingKey)
        {
            if (string.IsNullOrEmpty(weaponSettingKey))
            {
                DebugLog("Error: Invalid weapon setting key");
                return null;
            }

            foreach (var category in _settings.GetAllWeaponCategories())
            {
                if (category.Weapons.TryGetValue(weaponSettingKey, out var weaponSettings))
                {
                    return weaponSettings;
                }
            }

            DebugLog($"Warning: No weapon settings found for key {weaponSettingKey}");
            return null;
        }

        public int? GetBoundWeaponDamageOffset(IWeapon weapon, WeaponSettings settings)
        {
            if (weapon == null || settings == null || weapon.BasicStats == null)
            {
                DebugLog("Error: Invalid parameters in ApplyBoundWeaponStats");
                return null;
            }

            DebugLog($"Applying bound weapon stats for {weapon.EditorID} using setting: {_settings.BoundWeaponParsing}");
            switch (_settings.BoundWeaponParsing)
            {
                case BoundWeaponParsing.IgnoreWeapon:
                    DebugLog($"Skipping bound weapon: {weapon.EditorID}");
                    return null;

                case BoundWeaponParsing.FromSettings:
                    bool isMysticBound = weapon.EditorID?.Contains("mystic", StringComparison.OrdinalIgnoreCase) ?? false;
                    int damageOffset = isMysticBound
                        ? settings.BoundMysticWeaponAdditionalDamage
                        : settings.BoundWeaponAdditionalDamage;
                    DebugLog($"Damage offset for bound weapon read from settings: {weapon.EditorID}: {damageOffset}");
                    return damageOffset;

                case BoundWeaponParsing.CalculateFromMods:
                    DebugLog($"Calculating damage offset for bound weapon {weapon.EditorID} from mods");
                    // Get current damage and keywords
                    var currentDamage = weapon.BasicStats.Damage;
                    var keywords = GetWeaponKeywords(weapon, _state.LinkCache);
                    DebugLog($"Current damage for bound weapon {weapon.EditorID}: {currentDamage}");
                    DebugLog($"Keywords for bound weapon {weapon.EditorID}: {string.Join(", ", keywords)}");
                    // Find matching default weapon stats
                    var defaultStats = GetLoadedDefaultWeaponStatsData()
                        .FirstOrDefault(d => keywords.Contains(d.Keyword));

                    if (defaultStats == null)
                    {
                        DebugLog($"No matching default weapon stats found for bound weapon {weapon.EditorID}");
                        return null;
                    }

                    DebugLog($"Default stats for bound weapon {weapon.EditorID}: {defaultStats.Keyword}, {defaultStats.Damage}");

                    // Calculate damage offset
                    int calculatedOffset = currentDamage - defaultStats.Damage;
                    weapon.BasicStats.Damage = ClampUshort(settings.Damage + calculatedOffset);
                    DebugLog($"Calculated damage offset for bound weapon {weapon.EditorID}: {calculatedOffset}");
                    return calculatedOffset;
            }
            DebugLog($"Unhandled case for bound weapon {weapon.EditorID}");
            return null; // Default return for any unhandled cases
        }
    }
}