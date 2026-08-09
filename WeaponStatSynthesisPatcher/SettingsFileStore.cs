using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System;
using System.Linq;

namespace WeaponStatSynthesisPatcher
{
    public static class SettingsFileStore
    {
        public const string SettingsFileName = "settings.json";

        public static Settings Load(string? settingsFolder, string? defaultSettingsFolder = null)
        {
            var userPath = GetSettingsPath(settingsFolder);
            var defaultPath = string.IsNullOrWhiteSpace(defaultSettingsFolder)
                ? null
                : Path.Combine(defaultSettingsFolder, SettingsFileName);

            try
            {
                if (!File.Exists(userPath))
                {
                    var defaults = LoadCurrentDefaults(defaultPath);
                    Save(settingsFolder, defaults);
                    return defaults;
                }

                var userJson = JObject.Parse(File.ReadAllText(userPath));
                int sourceVersion = userJson.Value<int?>(nameof(Settings.SettingsVersion)) ?? 0;
                bool requiresMigration = sourceVersion < Settings.CurrentSettingsVersion;

                JObject effectiveJson = userJson;
                if (requiresMigration)
                {
                    var defaults = LoadCurrentDefaults(defaultPath);
                    effectiveJson = JObject.FromObject(defaults);
                    effectiveJson.Merge(
                        userJson,
                        new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Replace,
                            MergeNullValueHandling = MergeNullValueHandling.Merge
                        });
                    effectiveJson[nameof(Settings.SettingsVersion)] = Settings.CurrentSettingsVersion;
                }

                var loaded = PopulateSettings(effectiveJson.ToString(Formatting.None));
                loaded.UniqueWeapons ??= new List<SpecialWeaponData>();
                NormalizeWeaponMaterials(loaded);

                if (requiresMigration)
                {
                    Save(settingsFolder, loaded);
                }

                return loaded;
            }
            catch
            {
                return LoadCurrentDefaults(defaultPath);
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

            settings.SettingsVersion = Settings.CurrentSettingsVersion;
            NormalizeWeaponMatchLogic(settings);
            NormalizeWeaponMaterials(settings);

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        private static Settings LoadCurrentDefaults(string? defaultPath)
        {
            Settings defaults;
            if (!string.IsNullOrWhiteSpace(defaultPath) && File.Exists(defaultPath))
            {
                defaults = PopulateSettings(File.ReadAllText(defaultPath));
            }
            else
            {
                defaults = new Settings();
            }

            defaults.SettingsVersion = Settings.CurrentSettingsVersion;
            defaults.UniqueWeapons ??= new List<SpecialWeaponData>();
            defaults.EnsureDefaultWeaponMaterialsPresent();
            NormalizeWeaponMaterials(defaults);
            return defaults;
        }

        private static Settings PopulateSettings(string json)
        {
            var settings = new Settings(loadBundledDefaults: false);
            JsonConvert.PopulateObject(
                json,
                settings,
                new JsonSerializerSettings
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                });
            return settings;
        }

        public static string GetUserSettingsPath(string? settingsFolder) => GetSettingsPath(settingsFolder);

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

        private static void NormalizeWeaponMaterials(Settings settings)
        {
            settings.WeaponMaterials ??= new List<WeaponMaterialSetting>();

            var dedupedByTitle = new Dictionary<string, WeaponMaterialSetting>(StringComparer.OrdinalIgnoreCase);
            var untitled = new List<WeaponMaterialSetting>();

            foreach (var material in settings.WeaponMaterials)
            {
                material.Title = material.Title?.Trim() ?? string.Empty;
                material.Identifiers = NormalizeDelimitedList(material.Identifiers);

                if (string.IsNullOrWhiteSpace(material.Title))
                {
                    untitled.Add(material);
                    continue;
                }

                // Keep the latest entry so addon applications overwrite older duplicates.
                dedupedByTitle[material.Title] = material;
            }

            settings.WeaponMaterials = dedupedByTitle.Values.Concat(untitled).ToList();
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

        private static List<string> NormalizeDelimitedList(IEnumerable<string>? entries)
        {
            if (entries == null)
            {
                return new List<string>();
            }

            return entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .Select(entry => entry.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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
