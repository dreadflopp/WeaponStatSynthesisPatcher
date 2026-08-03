using Newtonsoft.Json;
using System.IO;
using System;
using System.Linq;

namespace WeaponStatSynthesisPatcher
{
    public static class SettingsFileStore
    {
        public const string SettingsFileName = "settings.json";

        public static Settings Load(string? settingsFolder)
        {
            var path = GetSettingsPath(settingsFolder);
            if (!File.Exists(path))
            {
                return new Settings();
            }

            try
            {
                var json = File.ReadAllText(path);
                var settings = JsonConvert.DeserializeObject<Settings>(json);
                return settings ?? new Settings();
            }
            catch
            {
                // Fall back to defaults if the file cannot be parsed.
                return new Settings();
            }
        }

        public static void Save(string? settingsFolder, Settings settings)
        {
            var path = GetSettingsPath(settingsFolder);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            NormalizeWeaponMatchLogic(settings);

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        private static void NormalizeWeaponMatchLogic(Settings settings)
        {
            foreach (var category in settings.GetAllWeaponCategories())
            {
                foreach (var weaponEntry in category.Weapons)
                {
                    var matchLogic = weaponEntry.Value.MatchLogicSettings;
                    matchLogic.NamedIDs = NormalizeSemicolonDelimited(matchLogic.NamedIDs);
                    matchLogic.KeywordIDs = NormalizeSemicolonDelimited(matchLogic.KeywordIDs);

                    bool hasNames = !string.IsNullOrWhiteSpace(matchLogic.NamedIDs);
                    bool hasKeywords = !string.IsNullOrWhiteSpace(matchLogic.KeywordIDs);
                    if (!hasNames || !hasKeywords)
                    {
                        matchLogic.SearchLogic = LogicOperator.AND;
                    }
                }
            }
        }

        private static string NormalizeSemicolonDelimited(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return string.Join(";",
                value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Where(entry => !string.IsNullOrWhiteSpace(entry)));
        }

        private static string GetSettingsPath(string? settingsFolder)
        {
            if (string.IsNullOrWhiteSpace(settingsFolder))
            {
                return Path.Combine(Environment.CurrentDirectory, SettingsFileName);
            }

            return Path.Combine(settingsFolder, SettingsFileName);
        }
    }
}
