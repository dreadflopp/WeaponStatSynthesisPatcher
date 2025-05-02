using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Weapon_Mod_Synergy
{
    public class WeaponSettingOverlapResolver
    {
        private readonly Settings _settings;
        private readonly Action<string> _logger;
        private List<(string Key, WeaponSettings Settings)> _sortedSettings;

        public WeaponSettingOverlapResolver(Settings settings, Action<string> logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sortedSettings = new List<(string Key, WeaponSettings Settings)>();
        }

        public void ResolveOverlaps()
        {
            _logger("Starting ResolveOverlaps...");

            // Get all settings with their keys
            var allSettings = new List<(string Key, WeaponSettings Settings)>();
            foreach (var category in _settings.GetAllWeaponCategories())
            {
                _logger($"Processing category: {GetCategoryName(category.Weapons.First().Value)}");
                foreach (var weapon in category.Weapons)
                {
                    allSettings.Add((weapon.Key, weapon.Value));
                    _logger($"  Added setting: {weapon.Key} (Enabled: {weapon.Value.Enabled})");
                }
            }

            _logger($"Total settings before filtering: {allSettings.Count}");

            // Step 1: Remove disabled settings
            allSettings = allSettings.Where(s => s.Settings.Enabled).ToList();
            _logger($"Settings after removing disabled: {allSettings.Count}");

            // Step 2: Remove settings with both fields empty and log warning
            var settingsToRemove = allSettings.Where(s =>
                string.IsNullOrWhiteSpace(s.Settings.MatchLogicSettings.NamedIDs) &&
                string.IsNullOrWhiteSpace(s.Settings.MatchLogicSettings.KeywordIDs)).ToList();

            foreach (var setting in settingsToRemove)
            {
                Console.WriteLine($"WARNING: Setting '{GetSettingDescription(setting.Settings)}' has both name and keyword fields empty. Removing it.");
                allSettings.Remove(setting);
            }

            // Step 3: Change logical operator to AND if one field is empty and log warning
            foreach (var setting in allSettings)
            {
                bool hasEmptyName = string.IsNullOrWhiteSpace(setting.Settings.MatchLogicSettings.NamedIDs);
                bool hasEmptyKeyword = string.IsNullOrWhiteSpace(setting.Settings.MatchLogicSettings.KeywordIDs);

                if (setting.Settings.MatchLogicSettings.SearchLogic == LogicOperator.OR && (hasEmptyName || hasEmptyKeyword))
                {
                    Console.WriteLine($"WARNING: Setting '{GetSettingDescription(setting.Settings)}' has OR logic with empty {(hasEmptyName ? "names" : "keywords")} field.");
                    Console.WriteLine($"         Automatically changing to AND logic to make the setting meaningful.");
                    setting.Settings.MatchLogicSettings.SearchLogic = LogicOperator.AND;
                }
            }

            // Step 4: Sort settings into groups
            var orSettings = allSettings.Where(s => s.Settings.MatchLogicSettings.SearchLogic == LogicOperator.OR).ToList();
            var andSettings = allSettings.Where(s => s.Settings.MatchLogicSettings.SearchLogic == LogicOperator.AND).ToList();

            _logger($"OR settings count: {orSettings.Count}");
            _logger($"AND settings count: {andSettings.Count}");

            // Further divide AND settings
            var andCompleteSettings = andSettings.Where(s =>
                !string.IsNullOrWhiteSpace(s.Settings.MatchLogicSettings.NamedIDs) &&
                !string.IsNullOrWhiteSpace(s.Settings.MatchLogicSettings.KeywordIDs)).ToList();

            var andNameOnlySettings = andSettings.Where(s =>
                !string.IsNullOrWhiteSpace(s.Settings.MatchLogicSettings.NamedIDs) &&
                string.IsNullOrWhiteSpace(s.Settings.MatchLogicSettings.KeywordIDs)).ToList();

            var andKeywordOnlySettings = andSettings.Where(s =>
                string.IsNullOrWhiteSpace(s.Settings.MatchLogicSettings.NamedIDs) &&
                !string.IsNullOrWhiteSpace(s.Settings.MatchLogicSettings.KeywordIDs)).ToList();

            _logger($"AND complete settings: {andCompleteSettings.Count}");
            _logger($"AND name only settings: {andNameOnlySettings.Count}");
            _logger($"AND keyword only settings: {andKeywordOnlySettings.Count}");

            // Step 5: Process OR settings
            ProcessOrSettings(orSettings.Select(s => s.Settings).ToList());

            // Step 6: Process AND settings
            ProcessAndCompleteSettings(andCompleteSettings.Select(s => s.Settings).ToList());
            ProcessAndNameOnlySettings(andNameOnlySettings.Select(s => s.Settings).ToList());
            ProcessAndKeywordOnlySettings(andKeywordOnlySettings.Select(s => s.Settings).ToList());

            // Step 7: Sort all enabled settings by priority
            _sortedSettings.Clear();

            // Helper function to sort settings by NamedIDs
            List<(string Key, WeaponSettings Settings)> SortSettingsByNamedIDs(List<(string Key, WeaponSettings Settings)> settings)
            {
                var result = new List<(string Key, WeaponSettings Settings)>();
                var remaining = new List<(string Key, WeaponSettings Settings)>(settings);

                while (remaining.Count != 0)
                {
                    var current = remaining[0];
                    remaining.RemoveAt(0);

                    // Get all NamedIDs and KeywordIDs for current setting
                    var currentNames = current.Settings.MatchLogicSettings.NamedIDs?
                        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
                    var currentKeywords = current.Settings.MatchLogicSettings.KeywordIDs?
                        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

                    // Check for conflicts with remaining settings
                    bool hasConflict = false;
                    foreach (var other in remaining)
                    {
                        // Only check for conflicts within the same skill
                        if (current.Settings.MatchLogicSettings.Skill != other.Settings.MatchLogicSettings.Skill)
                        {
                            continue;
                        }

                        var otherNames = other.Settings.MatchLogicSettings.NamedIDs?
                            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
                        var otherKeywords = other.Settings.MatchLogicSettings.KeywordIDs?
                            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

                        // For AND settings, we need to check both names and keywords
                        if (current.Settings.MatchLogicSettings.SearchLogic == LogicOperator.AND &&
                            other.Settings.MatchLogicSettings.SearchLogic == LogicOperator.AND)
                        {
                            // Check if both settings have keywords
                            if (currentKeywords.Length > 0 && otherKeywords.Length > 0)
                            {
                                // If they have different keyword patterns, they can't conflict
                                bool hasCommonKeyword = currentKeywords.Any(k1 => otherKeywords.Any(k2 => k1.Equals(k2, StringComparison.OrdinalIgnoreCase)));
                                if (!hasCommonKeyword)
                                {
                                    continue; // Different keywords, no conflict possible
                                }
                            }
                        }

                        // Check for mutual substring relationships in names
                        bool hasMutualSubstring = false;
                        foreach (var name1 in currentNames)
                        {
                            foreach (var name2 in otherNames)
                            {
                                // Create regex patterns that match whole words
                                string pattern1 = $@"\b{Regex.Escape(name1)}\b";
                                string pattern2 = $@"\b{Regex.Escape(name2)}\b";

                                // Check if one pattern matches the other (ignoring case)
                                bool name1ContainsName2 = Regex.IsMatch(name1, pattern2, RegexOptions.IgnoreCase);
                                bool name2ContainsName1 = Regex.IsMatch(name2, pattern1, RegexOptions.IgnoreCase);

                                if (name1ContainsName2 || name2ContainsName1)
                                {
                                    // If we already found a substring relationship in the opposite direction,
                                    // this is an ambiguous case
                                    if (hasMutualSubstring)
                                    {
                                        Console.WriteLine($"WARNING: Found ambiguous NamedIDs in settings. Disabling both:");
                                        Console.WriteLine($"- {GetSettingDescription(current.Settings)}");
                                        Console.WriteLine($"- {GetSettingDescription(other.Settings)}");
                                        current.Settings.Enabled = false;
                                        other.Settings.Enabled = false;
                                        remaining.Remove(other);
                                        hasConflict = true;
                                        break;
                                    }

                                    // If one is a substring of the other, we can sort them
                                    if (name1.Length > name2.Length)
                                    {
                                        // Current setting has longer name, so it should come first
                                        result.Add(current);
                                        result.Add(other);
                                        remaining.Remove(other);
                                    }
                                    else if (name1.Length < name2.Length)
                                    {
                                        // Other setting has longer name, so it should come first
                                        result.Add(other);
                                        result.Add(current);
                                        remaining.Remove(other);
                                    }
                                    else
                                    {
                                        // Names are the same length but one is a substring of the other
                                        // This shouldn't happen with proper input, but just in case
                                        Console.WriteLine($"WARNING: Found ambiguous NamedIDs in settings. Disabling both:");
                                        Console.WriteLine($"- {GetSettingDescription(current.Settings)}");
                                        Console.WriteLine($"- {GetSettingDescription(other.Settings)}");
                                        current.Settings.Enabled = false;
                                        other.Settings.Enabled = false;
                                        remaining.Remove(other);
                                    }
                                    hasConflict = true;
                                    hasMutualSubstring = true;
                                    break;
                                }
                            }
                            if (hasConflict) break;
                        }
                        if (hasConflict) break;
                    }

                    if (!hasConflict)
                    {
                        result.Add(current);
                    }
                }

                return result;
            }

            // First add all enabled OR settings
            var enabledOrSettings = orSettings.Where(s => s.Settings.Enabled).ToList();
            _sortedSettings.AddRange(SortSettingsByNamedIDs(enabledOrSettings));
            _logger($"Enabled OR settings: {_sortedSettings.Count}");

            // Then add all enabled AND settings with both fields
            var enabledAndCompleteSettings = andCompleteSettings.Where(s => s.Settings.Enabled).ToList();
            _sortedSettings.AddRange(SortSettingsByNamedIDs(enabledAndCompleteSettings));
            _logger($"Enabled AND complete settings: {_sortedSettings.Count}");

            // Finally add all enabled AND settings with only one field
            var enabledAndNameOnlySettings = andNameOnlySettings.Where(s => s.Settings.Enabled).ToList();
            var enabledAndKeywordOnlySettings = andKeywordOnlySettings.Where(s => s.Settings.Enabled).ToList();
            _sortedSettings.AddRange(SortSettingsByNamedIDs(enabledAndNameOnlySettings));
            _sortedSettings.AddRange(enabledAndKeywordOnlySettings); // No need to sort keyword-only settings
            _logger($"Final sorted settings count: {_sortedSettings.Count}");

            // Log final state
            _logger("\n=== Final Weapon Settings State ===");
            foreach (var setting in _sortedSettings)
            {
                _logger(GetSettingDescription(setting.Settings));
            }
        }

        private void ProcessOrSettings(List<WeaponSettings> orSettings)
        {
            // Group OR settings by skill
            var settingsBySkill = orSettings.GroupBy(s => s.MatchLogicSettings.Skill);

            foreach (var skillGroup in settingsBySkill)
            {
                var skillSettings = skillGroup.ToList();
                var disabledOrSettings = new HashSet<WeaponSettings>();

                foreach (var orSetting in skillSettings)
                {
                    if (disabledOrSettings.Contains(orSetting))
                    {
                        continue;
                    }

                    // Check names
                    if (!string.IsNullOrWhiteSpace(orSetting.MatchLogicSettings.NamedIDs))
                    {
                        var names = orSetting.MatchLogicSettings.NamedIDs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var name in names)
                        {
                            // Check against all other settings in the same skill group
                            var overlappingSettings = skillSettings.Where(s =>
                                s != orSetting &&
                                !disabledOrSettings.Contains(s) &&
                                !string.IsNullOrWhiteSpace(s.MatchLogicSettings.NamedIDs) &&
                                s.MatchLogicSettings.NamedIDs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                 .Contains(name)).ToList();

                            if (overlappingSettings.Any())
                            {
                                Console.WriteLine($"WARNING: Name '{name}' is used in multiple settings within skill {skillGroup.Key}. Disabling ALL conflicting OR settings:");
                                Console.WriteLine($"- {GetSettingDescription(orSetting)}");
                                orSetting.Enabled = false;
                                disabledOrSettings.Add(orSetting);

                                foreach (var overlappingSetting in overlappingSettings)
                                {
                                    if (overlappingSetting.MatchLogicSettings.SearchLogic == LogicOperator.OR)
                                    {
                                        Console.WriteLine($"- {GetSettingDescription(overlappingSetting)}");
                                        overlappingSetting.Enabled = false;
                                        disabledOrSettings.Add(overlappingSetting);
                                    }
                                }
                            }
                        }
                    }

                    // Check keywords
                    if (!string.IsNullOrWhiteSpace(orSetting.MatchLogicSettings.KeywordIDs))
                    {
                        var keywords = orSetting.MatchLogicSettings.KeywordIDs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var keyword in keywords)
                        {
                            // Check against all other settings in the same skill group
                            var overlappingSettings = skillSettings.Where(s =>
                                s != orSetting &&
                                !disabledOrSettings.Contains(s) &&
                                !string.IsNullOrWhiteSpace(s.MatchLogicSettings.KeywordIDs) &&
                                s.MatchLogicSettings.KeywordIDs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                 .Contains(keyword)).ToList();

                            if (overlappingSettings.Any())
                            {
                                Console.WriteLine($"WARNING: Keyword '{keyword}' is used in multiple settings within skill {skillGroup.Key}. Disabling ALL conflicting OR settings:");
                                Console.WriteLine($"- {GetSettingDescription(orSetting)}");
                                orSetting.Enabled = false;
                                disabledOrSettings.Add(orSetting);

                                foreach (var overlappingSetting in overlappingSettings)
                                {
                                    if (overlappingSetting.MatchLogicSettings.SearchLogic == LogicOperator.OR)
                                    {
                                        Console.WriteLine($"- {GetSettingDescription(overlappingSetting)}");
                                        overlappingSetting.Enabled = false;
                                        disabledOrSettings.Add(overlappingSetting);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ProcessAndCompleteSettings(List<WeaponSettings> andCompleteSettings)
        {
            // Group settings by skill
            var settingsBySkill = andCompleteSettings.GroupBy(s => s.MatchLogicSettings.Skill);

            foreach (var skillGroup in settingsBySkill)
            {
                var skillSettings = skillGroup.ToList();

                for (int i = 0; i < skillSettings.Count; i++)
                {
                    if (!skillSettings[i].Enabled) continue;

                    var names1 = skillSettings[i].MatchLogicSettings.NamedIDs?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
                    var keywords1 = skillSettings[i].MatchLogicSettings.KeywordIDs?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

                    // Check if both fields are empty
                    if (names1.Length == 0 && keywords1.Length == 0)
                    {
                        Console.WriteLine($"WARNING: AND setting has both name and keyword fields empty. Disabling it:");
                        Console.WriteLine($"- {GetSettingDescription(skillSettings[i])}");
                        skillSettings[i].Enabled = false;
                        continue;
                    }

                    for (int j = i + 1; j < skillSettings.Count; j++)
                    {
                        if (!skillSettings[j].Enabled) continue;

                        var names2 = skillSettings[j].MatchLogicSettings.NamedIDs?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
                        var keywords2 = skillSettings[j].MatchLogicSettings.KeywordIDs?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

                        // Check if both fields are empty
                        if (names2.Length == 0 && keywords2.Length == 0)
                        {
                            Console.WriteLine($"WARNING: AND setting has both name and keyword fields empty. Disabling it:");
                            Console.WriteLine($"- {GetSettingDescription(skillSettings[j])}");
                            skillSettings[j].Enabled = false;
                            continue;
                        }

                        // Check for conflicts
                        bool hasNameConflict = false;
                        bool hasKeywordConflict = false;

                        // If either setting has empty names, consider it a wildcard
                        if (names1.Length == 0 || names2.Length == 0)
                        {
                            hasNameConflict = true;
                        }
                        else
                        {
                            // Check if they share any names
                            hasNameConflict = names1.Any(n1 => names2.Any(n2 => n1.Equals(n2, StringComparison.OrdinalIgnoreCase)));
                        }

                        // If either setting has empty keywords, consider it a wildcard
                        if (keywords1.Length == 0 || keywords2.Length == 0)
                        {
                            hasKeywordConflict = true;
                        }
                        else
                        {
                            // Check if they share any keywords
                            hasKeywordConflict = keywords1.Any(k1 => keywords2.Any(k2 => k1.Equals(k2, StringComparison.OrdinalIgnoreCase)));
                        }

                        // If they share both names and keywords (or have wildcards), they conflict
                        if (hasNameConflict && hasKeywordConflict)
                        {
                            Console.WriteLine($"WARNING: Found ambiguous AND settings within skill {skillGroup.Key}. Disabling both:");
                            Console.WriteLine($"- {GetSettingDescription(skillSettings[i])}");
                            Console.WriteLine($"- {GetSettingDescription(skillSettings[j])}");
                            skillSettings[i].Enabled = false;
                            skillSettings[j].Enabled = false;
                        }
                    }
                }
            }
        }

        private void ProcessAndSingleFieldSettings(
            List<WeaponSettings> settings,
            Func<WeaponSettings, string> getField,  // Function to get either NamedIDs or KeywordIDs
            string fieldName)  // "name" or "keyword" for logging
        {
            // Group settings by skill
            var settingsBySkill = settings.GroupBy(s => s.MatchLogicSettings.Skill);

            foreach (var skillGroup in settingsBySkill)
            {
                var skillSettings = skillGroup.ToList();
                var allValues = new HashSet<string>();

                foreach (var setting in skillSettings)
                {
                    if (!setting.Enabled) continue;

                    var values = getField(setting).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var value in values)
                    {
                        if (allValues.Contains(value))
                        {
                            Console.WriteLine($"WARNING: Found single field AND settings with same {fieldName} '{value}' for skill {skillGroup.Key}. Disabling all affected settings:");
                            var overlappingSettings = skillSettings.Where(s =>
                                s.Enabled &&
                                getField(s).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                 .Contains(value)).ToList();

                            foreach (var overlappingSetting in overlappingSettings)
                            {
                                Console.WriteLine($"- {GetSettingDescription(overlappingSetting)}");
                                overlappingSetting.Enabled = false;
                            }
                        }
                        else
                        {
                            allValues.Add(value);
                        }
                    }
                }
            }
        }

        private void ProcessAndNameOnlySettings(List<WeaponSettings> andNameOnlySettings)
        {
            ProcessAndSingleFieldSettings(
                andNameOnlySettings,
                s => s.MatchLogicSettings.NamedIDs,
                "name");
        }

        private void ProcessAndKeywordOnlySettings(List<WeaponSettings> andKeywordOnlySettings)
        {
            ProcessAndSingleFieldSettings(
                andKeywordOnlySettings,
                s => s.MatchLogicSettings.KeywordIDs,
                "keyword");
        }

        private string GetSettingDescription(WeaponSettings setting)
        {
            var category = GetCategoryName(setting);
            var weaponName = GetWeaponName(setting);
            var skill = setting.MatchLogicSettings.Skill;
            var namePatterns = setting.MatchLogicSettings.NamedIDs;
            var keywordPatterns = setting.MatchLogicSettings.KeywordIDs;
            var logic = setting.MatchLogicSettings.SearchLogic;

            return $"{category}.{weaponName} (Skill: {skill}, Logic: {logic}) - Names: {namePatterns}, Keywords: {keywordPatterns}";
        }

        private string GetWeaponName(WeaponSettings setting)
        {
            foreach (var category in _settings.GetAllWeaponCategories())
            {
                var weapon = category.Weapons.FirstOrDefault(w => w.Value == setting);
                if (weapon.Key != null)
                {
                    return weapon.Key;
                }
            }

            return "Unknown Weapon";
        }

        private string GetCategoryName(WeaponSettings setting)
        {
            foreach (var category in _settings.GetAllWeaponCategories())
            {
                if (category.Weapons.Values.Contains(setting))
                {
                    return category.GetType().Name;
                }
            }

            return "Unknown Category";
        }

        public List<(string Key, WeaponSettings Settings)> GetSortedSettings()
        {
            return _sortedSettings;
        }
    }
}