using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins.Order;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using Mutagen.Bethesda.Plugins.Binary.Headers;
#pragma warning disable CA1416

namespace WeaponStatSynthesisPatcher
{
    public class WeaponDebugInfo
    {
        public string EditorID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int InDamage { get; set; }
        public float InSpeed { get; set; }
        public float InReach { get; set; }
        public float InStagger { get; set; }
        public int InCriticalDamage { get; set; }
        public float InCriticalChanceMult { get; set; }
        public bool UsesTemplate { get; set; }
        public bool IsPlayable { get; set; }
        public string SettingsKey { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public int? MaterialDamageOffset { get; set; }
        public bool IsSpecial { get; set; }
        public bool IsIgnored { get; set; }
        public string Variant { get; set; } = string.Empty;
        public int VariantDamageOffset { get; set; }
        public float VariantReachOffset { get; set; }
        public float VariantSpeedOffset { get; set; }
        public float VariantStaggerOffset { get; set; }
        public float VariantCriticalDamageOffset { get; set; }
        public float VariantCriticalChanceMultOffset { get; set; }
        public float VariantCriticalDamageMultOffset { get; set; }
        public int SettingsDamageOffset { get; set; }
        public float SettingsSpeedOffset { get; set; }
        public float SettingsReachOffset { get; set; }
        public float SettingsStaggerOffset { get; set; }
        public float SettingsCriticalDamageOffset { get; set; }
        public float SettingsCriticalChanceMultOffset { get; set; }
        public float SettingsCriticalDamageMultOffset { get; set; }
        public int FinalDamage { get; set; }
        public float FinalSpeed { get; set; }
        public float FinalReach { get; set; }
        public float FinalStagger { get; set; }
        public int FinalCriticalDamage { get; set; }
        public float FinalCriticalChanceMult { get; set; }
        public string ProcessingResult { get; set; } = string.Empty;
        public string InKeywords { get; set; } = string.Empty;
    }

    public class Program
    {
        private static Settings CurrentSettings = new Settings();
        private static WeaponDataManager? _weaponDataManager;
        private static List<WeaponDebugInfo> _processedWeapons = new();

        [STAThread]
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetOpenForSettings(OpenForSettings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "Weapon_Mod_Synergy.esp")
                .Run(args);
        }

        public static int OpenForSettings(IOpenForSettingsState state)
        {
            try
            {
                Exception? openException = null;
                int openResult = 0;

                var uiThread = new Thread(() =>
                {
                    try
                    {
                        var app = Application.Current;
                        if (app == null)
                        {
                            app = new Application
                            {
                                ShutdownMode = ShutdownMode.OnExplicitShutdown
                            };
                        }

                        var settings = SettingsFileStore.Load(state.ExtraSettingsDataPath);
                        var window = new SettingsWindow(settings, state.ExtraSettingsDataPath)
                        {
                            ShowInTaskbar = true
                        };

                        _ = window.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        openException = ex;
                        openResult = 1;
                    }
                });

                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.Start();
                uiThread.Join();

                if (openException != null)
                {
                    Console.WriteLine($"ERROR: Failed to open custom settings UI: {openException}");
                    try
                    {
                        var logPath = Path.Combine(AppContext.BaseDirectory, "open-settings-error.log");
                        File.WriteAllText(logPath, openException.ToString());
                        MessageBox.Show(
                            $"Failed to open settings UI.\n\nDetails were written to:\n{logPath}",
                            "Weapon Stats Synthesis Patcher",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    catch
                    {
                        // Best effort diagnostics only.
                    }
                }

                return openResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to open custom settings UI: {ex}");
                return 1;
            }
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            CurrentSettings = SettingsFileStore.Load(state.ExtraSettingsDataPath);

            // Create WeaponDataManager instance with the new Settings structure
            _weaponDataManager = new WeaponDataManager(CurrentSettings, Console.WriteLine, state);

            // Print settings
            _weaponDataManager?.DebugLog($"Settings:");
            _weaponDataManager?.DebugLog($"  - PluginFilter: {CurrentSettings.PluginFilter}");
            _weaponDataManager?.DebugLog($"  - WACCFMaterialTiers: {CurrentSettings.WACCFMaterialTiers}");
            _weaponDataManager?.DebugLog($"  - EnableRestlessDeadNerfs: {CurrentSettings.EnableRestlessDeadNerfs}");
            _weaponDataManager?.DebugLog($"  - StalhrimStaggerBonus: {CurrentSettings.StalhrimStaggerBonus}");
            _weaponDataManager?.DebugLog($"  - StalhrimDamageBonus: {CurrentSettings.StalhrimDamageBonus}");
            _weaponDataManager?.DebugLog($"  - BoundWeaponParsing: {CurrentSettings.BoundWeaponParsing}");

            // Resolve weapon setting overlaps
            Action<string> logger = _weaponDataManager != null
                ? (message) => _weaponDataManager.DebugLog(message)
                : Console.WriteLine;
            var overlapResolver = new WeaponSettingOverlapResolver(CurrentSettings, logger);
            Console.WriteLine("Checking settings integrity...");
            overlapResolver.ResolveOverlaps();
            Console.WriteLine("Settings integrity check complete.");

            // Set the resolved settings in WeaponDataManager
            _weaponDataManager?.SetSortedSettings(overlapResolver.GetSortedSettings());

            // Get the list of plugins to include
            var pluginIncludeList = CurrentSettings.PluginIncludeList
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Get the list of plugins to exclude
            var pluginExcludeList = CurrentSettings.PluginExcludeList
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Get the list of ignored weapon form keys
            var ignoredWeaponFormKeys = CurrentSettings.IgnoredWeaponFormKeys
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _weaponDataManager?.DebugLog($"Raw ignored weapons string: '{CurrentSettings.IgnoredWeaponFormKeys}'");

            Console.WriteLine("Plugin Processing Settings:");
            Console.WriteLine($"Filter Mode: {CurrentSettings.PluginFilter}");
            _weaponDataManager?.DebugLog("Plugin Include List:");
            if (CurrentSettings.DebugMode)
            {
                foreach (var plugin in pluginIncludeList)
                {
                    _weaponDataManager?.DebugLog($"     {plugin}");
                }
                _weaponDataManager?.DebugLog("Plugin Exclude List:");
                foreach (var plugin in pluginExcludeList)
                {
                    _weaponDataManager?.DebugLog($"     {plugin}");
                }
            }

            // Get all available plugins in load order
            var availablePlugins = state.LoadOrder.PriorityOrder
                .Select(mod => mod.ModKey.FileName.String)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Check for missing plugins if we're in include mode
            if (CurrentSettings.PluginFilter == PluginFilter.IncludePlugins)
            {
                var missingPlugins = pluginIncludeList.Where(p => !availablePlugins.Contains(p)).ToList();
                if (missingPlugins.Any())
                {
                    Console.WriteLine("WARNING: The following plugins were listed for inclusion but could not be found:");
                    foreach (var plugin in missingPlugins)
                    {
                        Console.WriteLine($"     {plugin}");
                    }
                    Console.WriteLine();
                }
            }

            Console.WriteLine("Processing the following plugins:");
            foreach (var modGetter in state.LoadOrder.PriorityOrder)
            {
                var shouldProcess = CurrentSettings.PluginFilter switch
                {
                    PluginFilter.AllPlugins => true,
                    PluginFilter.ExcludePlugins => !pluginExcludeList.Contains(modGetter.ModKey.FileName.String),
                    PluginFilter.IncludePlugins => pluginIncludeList.Contains(modGetter.ModKey.FileName.String),
                    _ => true
                };

                if (shouldProcess)
                {
                    Console.WriteLine($"     {modGetter.ModKey.FileName}");
                }
            }
            Console.WriteLine();

            // Verify form keys in special_weapons.json only in debug mode
            if (CurrentSettings.DebugMode)
            {
                VerifyFormKeys(state);
            }

            // Process weapons in a single pass across winning overrides.
            var loggedProcessedMods = new HashSet<ModKey>();
            foreach (var weapon in state.LoadOrder.PriorityOrder.Weapon().WinningOverrides())
            {
                var sourceMod = weapon.FormKey.ModKey;
                var sourcePluginName = sourceMod.FileName.String;
                var shouldProcess = CurrentSettings.PluginFilter switch
                {
                    PluginFilter.AllPlugins => true,
                    PluginFilter.ExcludePlugins => !pluginExcludeList.Contains(sourcePluginName),
                    PluginFilter.IncludePlugins => pluginIncludeList.Contains(sourcePluginName),
                    _ => true
                };

                if (!shouldProcess)
                {
                    continue;
                }

                if (loggedProcessedMods.Add(sourceMod))
                {
                    _weaponDataManager?.DebugLog("========================================");
                    Console.WriteLine($"Processing mod: {sourceMod.FileName}");
                    _weaponDataManager?.DebugLog("========================================");
                }

                _weaponDataManager?.DebugLog("------------------------------------------");
                _weaponDataManager?.DebugLog($"Processing weapon: {weapon.EditorID}");
                _weaponDataManager?.DebugLog("------------------------------------------");
                bool isSpecialWeapon = _weaponDataManager?.IsSpecialWeapon(weapon) ?? false;
                _weaponDataManager?.DebugLog($"Is EDID {weapon.EditorID} a Special Weapon: {isSpecialWeapon}");

                // Check if weapon is bound
                bool isBound = (weapon.EditorID?.Contains("bound", StringComparison.OrdinalIgnoreCase) ?? false) ||
                              (weapon.Name?.String?.Contains("bound", StringComparison.OrdinalIgnoreCase) ?? false);

                // Skip if weapon is not playable and is not bound
                if (weapon.Data?.Flags.HasFlag(WeaponData.Flag.NonPlayable) == true && !isSpecialWeapon && !isBound)
                {
                    _weaponDataManager?.DebugLog($"Skipping non-playable weapon: {weapon.EditorID}");
                    continue;
                }

                // Skip if weapon uses a template
                if (weapon.Template?.FormKey != null &&
                    !weapon.Template.FormKey.IsNull &&
                    weapon.Template.FormKey.ToString() != "Null" &&
                    !isSpecialWeapon)
                {
                    _weaponDataManager?.DebugLog($"Skipping template weapon: {weapon.EditorID}");
                    continue;
                }

                // Check if weapon should be ignored
                var formKeyString = weapon.FormKey.ToString();
                if (formKeyString != null && ignoredWeaponFormKeys.Contains(formKeyString) && !isSpecialWeapon)
                {
                    _weaponDataManager?.DebugLog($"Weapon {weapon.EditorID} is in ignoredWeaponFormKeys list");
                    continue;
                }

                // Resolve keywords once and reuse for downstream checks.
                var weaponKeywords = _weaponDataManager?.GetWeaponKeywords(weapon, state.LinkCache) ?? new List<string>();

                // Skip dummy weapons
                bool isDummyWeapon = weaponKeywords.Any(k => k.Contains("Dummy", StringComparison.OrdinalIgnoreCase));
                if (isDummyWeapon)
                {
                    _weaponDataManager?.DebugLog($"Skipping dummy weapon: {weapon.EditorID}");
                    continue;
                }

                // Get weapon setting key
                var weaponSettingKey = _weaponDataManager?.GetWeaponSettingKey(weapon, state.LinkCache, weaponKeywords);
                if (weaponSettingKey == null)
                {
                    _weaponDataManager?.DebugLog($"Warning: Could not determine weapon setting key for {weapon.EditorID}");
                    continue;
                }

                // Process weapon based on if it is a special weapon or not
                if (isSpecialWeapon)
                {
                    _weaponDataManager?.DebugLog($"Processing special weapon: {weapon.EditorID}");
                    ProcessSpecialWeapon(weapon, state, weaponSettingKey, weaponKeywords);
                }
                else
                {
                    _weaponDataManager?.DebugLog($"Processing weapon: {weapon.EditorID}");
                    ProcessWeapon(weapon, state, weaponSettingKey, weaponKeywords);
                }
            }

        }
        private static void ProcessSpecialWeapon(IWeaponGetter weapon, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string weaponSettingKey, List<string>? preResolvedKeywords = null)
        {
            if (weapon == null || state == null || string.IsNullOrEmpty(weaponSettingKey))
            {
                _weaponDataManager?.DebugLog("Invalid parameters in ProcessSpecialWeapon");
                return;
            }

            // Create debug info object
            var debugInfo = new WeaponDebugInfo
            {
                EditorID = weapon.EditorID ?? string.Empty,
                Name = weapon.Name?.String ?? string.Empty,
                InDamage = weapon.BasicStats?.Damage ?? 0,
                InSpeed = weapon.Data?.Speed ?? 0,
                InReach = weapon.Data?.Reach ?? 0,
                InStagger = weapon.Data?.Stagger ?? 0,
                InCriticalDamage = weapon.Critical?.Damage ?? 0,
                InCriticalChanceMult = weapon.Critical?.PercentMult ?? 0,
                UsesTemplate = weapon.Template?.FormKey != null && !weapon.Template.FormKey.IsNull && weapon.Template.FormKey.ToString() != "Null",
                IsPlayable = !(weapon.Data?.Flags.HasFlag(WeaponData.Flag.NonPlayable) == true),
                SettingsKey = weaponSettingKey,
                IsSpecial = true,
                IsIgnored = false
            };

            // Get weapon settings
            var settings = _weaponDataManager?.GetWeaponSettings(weaponSettingKey);
            if (settings == null)
            {
                _weaponDataManager?.DebugLog($"{weaponSettingKey} is not enabled in settings");
                debugInfo.ProcessingResult = "No settings found";
                if (CurrentSettings.DebugMode) _processedWeapons.Add(debugInfo);
                return;
            }

            // Get weapon keywords
            var weaponKeywords = preResolvedKeywords ?? _weaponDataManager?.GetWeaponKeywords(weapon, state.LinkCache) ?? new List<string>();
            debugInfo.InKeywords = string.Join(";", weaponKeywords);

            // Get default weapon stats
            var defaultStats = _weaponDataManager?.GetDefaultWeaponStats(weaponKeywords);
            if (defaultStats == null)
            {
                _weaponDataManager?.DebugLog($"No matching default weapon stats found for {weapon.EditorID}");
                debugInfo.ProcessingResult = "No default stats found";
                if (CurrentSettings.DebugMode) _processedWeapons.Add(debugInfo);
                return;
            }

            // Create a copy of the weapon for modification
            var weaponOverride = weapon.DeepCopy();
            if (weaponOverride?.Data == null || weaponOverride.BasicStats == null || weaponOverride.Critical == null)
            {
                _weaponDataManager?.DebugLog($"Warning: Could not create override for {weapon.EditorID}");
                debugInfo.ProcessingResult = "Could not create override";
                if (CurrentSettings.DebugMode) _processedWeapons.Add(debugInfo);
                return;
            }

            // Store original values for later comparison
            var originalStats = new
            {
                Damage = weaponOverride.BasicStats.Damage,
                Speed = weaponOverride.Data.Speed,
                Reach = weaponOverride.Data.Reach,
                Stagger = weaponOverride.Data.Stagger,
                CriticalDamage = weaponOverride.Critical.Damage,
                CriticalChanceMult = weaponOverride.Critical.PercentMult
            };

            _weaponDataManager?.DebugLog($"Default stats: " +
                $"   Damage: {defaultStats.Damage}, " +
                $"   DamageWaccf: {defaultStats.DamageWaccf}, " +
                $"   Speed: {defaultStats.Speed}, " +
                $"   Reach: {defaultStats.Reach}, " +
                $"   Stagger: {defaultStats.Stagger}, " +
                $"   CriticalDamage: {defaultStats.CriticalDamage}, " +
                $"   CriticalChanceMult: {defaultStats.CriticalChanceMult}");

            _weaponDataManager?.DebugLog($"Weapon stats: " +
                $"   Damage: {weaponOverride.BasicStats.Damage}, " +
                $"   Speed: {weaponOverride.Data.Speed}, " +
                $"   Reach: {weaponOverride.Data.Reach}, " +
                $"   Stagger: {weaponOverride.Data.Stagger}, " +
                $"   CriticalDamage: {weaponOverride.Critical.Damage}, " +
                $"   CriticalChanceMult: {weaponOverride.Critical.PercentMult}");

            // Calculate offsets between current weapon stats and default stats            
            int damageOffset = weaponOverride.BasicStats.Damage - defaultStats.Damage;
            decimal speedOffset = (decimal)weaponOverride.Data.Speed - defaultStats.Speed;
            decimal reachOffset = (decimal)weaponOverride.Data.Reach - defaultStats.Reach;
            decimal staggerOffset = (decimal)weaponOverride.Data.Stagger - defaultStats.Stagger;
            int criticalDamageOffset = weaponOverride.Critical.Damage - weaponOverride.BasicStats.Damage / 2;
            decimal criticalDamageChanceMultiplierOffset = (decimal)weaponOverride.Critical.PercentMult - defaultStats.CriticalChanceMult;

            _weaponDataManager?.DebugLog($"Offsets: " +
                $"   Damage: {damageOffset}, " +
                $"   Speed: {speedOffset}, " +
                $"   Reach: {reachOffset}, " +
                $"   Stagger: {staggerOffset}, " +
                $"   CriticalDamage: {criticalDamageOffset}, " +
                $"   CriticalChanceMult: {criticalDamageChanceMultiplierOffset}");

            // Update debug info with offsets
            debugInfo.SettingsDamageOffset = damageOffset;
            debugInfo.SettingsSpeedOffset = (float)speedOffset;
            debugInfo.SettingsReachOffset = (float)reachOffset;
            debugInfo.SettingsStaggerOffset = (float)staggerOffset;
            debugInfo.SettingsCriticalDamageOffset = criticalDamageOffset;
            debugInfo.SettingsCriticalChanceMultOffset = (float)criticalDamageChanceMultiplierOffset;

            // Apply stats with offsets (checking enablers)
            var enablers = CurrentSettings.WeaponAttributeEnablers;

            if (enablers.EnableDamage)
            {
                weaponOverride.BasicStats.Damage = WeaponDataManager.ClampUshort(settings.Damage + damageOffset);
            }

            if (enablers.EnableSpeed)
            {
                weaponOverride.Data.Speed = WeaponDataManager.ClampFloat((float)((decimal)settings.Speed + (decimal)speedOffset));
            }

            if (enablers.EnableReach)
            {
                weaponOverride.Data.Reach = WeaponDataManager.ClampFloat((float)((decimal)settings.Reach + (decimal)reachOffset));
            }

            if (enablers.EnableStagger)
            {
                weaponOverride.Data.Stagger = WeaponDataManager.ClampFloat((float)((decimal)settings.Stagger + staggerOffset));
            }

            if (enablers.EnableCriticalDamageChanceMultiplier)
            {
                weaponOverride.Critical.PercentMult = WeaponDataManager.ClampFloat((float)((decimal)settings.CriticalDamageChanceMultiplier + (decimal)criticalDamageChanceMultiplierOffset));
            }

            if (enablers.EnableCriticalDamage)
            {
                weaponOverride.Critical.Damage = WeaponDataManager.ClampUshort((int)Math.Floor((decimal)settings.CriticalDamageMultiplier * ((decimal)criticalDamageOffset + (decimal)settings.CriticalDamageOffset + weaponOverride.BasicStats.Damage / 2m)));
            }

            // Update debug info with final stats (these reflect actual weapon values, whether changed or not)
            debugInfo.FinalDamage = weaponOverride.BasicStats.Damage;
            debugInfo.FinalSpeed = weaponOverride.Data.Speed;
            debugInfo.FinalReach = weaponOverride.Data.Reach;
            debugInfo.FinalStagger = weaponOverride.Data.Stagger;
            debugInfo.FinalCriticalDamage = weaponOverride.Critical.Damage;
            debugInfo.FinalCriticalChanceMult = weaponOverride.Critical.PercentMult;
            debugInfo.ProcessingResult = "Successfully processed";

            // Check if any values actually changed
            bool madeModification = weaponOverride.BasicStats.Damage != originalStats.Damage ||
                                   weaponOverride.Data.Speed != originalStats.Speed ||
                                   weaponOverride.Data.Reach != originalStats.Reach ||
                                   weaponOverride.Data.Stagger != originalStats.Stagger ||
                                   weaponOverride.Critical.Damage != originalStats.CriticalDamage ||
                                   weaponOverride.Critical.PercentMult != originalStats.CriticalChanceMult;

            // Only add to mod if changes were made
            if (madeModification)
            {
                state.PatchMod.Weapons.Set(weaponOverride);
                _weaponDataManager?.DebugLog($"Successfully processed special weapon: {weapon.EditorID}");
                _weaponDataManager?.DebugLog($"Final stats:");
                _weaponDataManager?.DebugLog($"- Damage: {weaponOverride.BasicStats.Damage}");
                _weaponDataManager?.DebugLog($"- Speed: {weaponOverride.Data.Speed}");
                _weaponDataManager?.DebugLog($"- Reach: {weaponOverride.Data.Reach}");
                _weaponDataManager?.DebugLog($"- Stagger: {weaponOverride.Data.Stagger}");
                _weaponDataManager?.DebugLog($"- Critical Damage: {weaponOverride.Critical.Damage}");
                _weaponDataManager?.DebugLog($"- Critical Damage Chance Multiplier: {weaponOverride.Critical.PercentMult}");
            }
            else
            {
                _weaponDataManager?.DebugLog($"No changes needed for special weapon: {weapon.EditorID}");
                debugInfo.ProcessingResult = "No changes needed";
            }

            if (CurrentSettings.DebugMode) _processedWeapons.Add(debugInfo);
        }

        private static void ProcessWeapon(IWeaponGetter weapon, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string weaponSettingKey, List<string>? preResolvedKeywords = null)
        {
            if (weapon == null || state == null || string.IsNullOrEmpty(weaponSettingKey))
            {
                _weaponDataManager?.DebugLog("Invalid parameters in ProcessWeapon");
                return;
            }

            // Create debug info object
            var debugInfo = new WeaponDebugInfo
            {
                EditorID = weapon.EditorID ?? string.Empty,
                Name = weapon.Name?.String ?? string.Empty,
                InDamage = weapon.BasicStats?.Damage ?? 0,
                InSpeed = weapon.Data?.Speed ?? 0,
                InReach = weapon.Data?.Reach ?? 0,
                InStagger = weapon.Data?.Stagger ?? 0,
                InCriticalDamage = weapon.Critical?.Damage ?? 0,
                InCriticalChanceMult = weapon.Critical?.PercentMult ?? 0,
                UsesTemplate = weapon.Template?.FormKey != null && !weapon.Template.FormKey.IsNull && weapon.Template.FormKey.ToString() != "Null",
                IsPlayable = !(weapon.Data?.Flags.HasFlag(WeaponData.Flag.NonPlayable) == true),
                SettingsKey = weaponSettingKey,
                IsSpecial = false,
                IsIgnored = false
            };

            // Get weapon settings
            var settings = _weaponDataManager?.GetWeaponSettings(weaponSettingKey);
            if (settings == null)
            {
                _weaponDataManager?.DebugLog($"{weaponSettingKey} is not enabled in settings");
                debugInfo.ProcessingResult = "No settings found";
                if (CurrentSettings.DebugMode) _processedWeapons.Add(debugInfo);
                return;
            }

            // Create a copy of the weapon for modification
            var weaponOverride = weapon.DeepCopy();
            if (weaponOverride?.Data == null || weaponOverride.BasicStats == null || weaponOverride.Critical == null)
            {
                _weaponDataManager?.DebugLog($"Warning: Could not create override for {weapon.EditorID}");
                debugInfo.ProcessingResult = "Could not create override";
                if (CurrentSettings.DebugMode) _processedWeapons.Add(debugInfo);
                return;
            }

            // Store original values for later comparison
            var originalStats = new
            {
                Damage = weaponOverride.BasicStats.Damage,
                Speed = weaponOverride.Data.Speed,
                Reach = weaponOverride.Data.Reach,
                Stagger = weaponOverride.Data.Stagger,
                CriticalDamage = weaponOverride.Critical.Damage,
                CriticalChanceMult = weaponOverride.Critical.PercentMult
            };

            _weaponDataManager?.DebugLog($"Original stats: " +
                $"   Damage: {originalStats.Damage}, " +
                $"   Speed: {originalStats.Speed}, " +
                $"   Reach: {originalStats.Reach}, " +
                $"   Stagger: {originalStats.Stagger}, " +
                $"   CriticalDamage: {originalStats.CriticalDamage}, " +
                $"   CriticalChanceMult: {originalStats.CriticalChanceMult}");

            int damageOffset = 0;
            decimal staggerOffset = 0;
            decimal criticalDamageOffset = 0;

            // Initialize material offsets with default values
            int additionalMaterialDamage = 0;
            float additionalMaterialReach = 0.0f;
            float additionalMaterialSpeed = 0.0f;
            float additionalMaterialStagger = 0.0f;
            float additionalMaterialCriticalDamageOffset = 0.0f;
            float additionalMaterialCriticalDamageChanceMultiplier = 0.0f;
            float additionalMaterialCriticalDamageMultiplier = 0.0f;

            // Initialize variant stats with default values
            int additionalVariantDamage = 0;
            float additionalVariantReach = 0.0f;
            float additionalVariantSpeed = 0.0f;
            float additionalVariantStagger = 0.0f;
            float additionalVariantCriticalDamageOffset = 0.0f;
            float additionalVariantCriticalDamageChanceMultiplier = 0.0f;
            float additionalVariantCriticalDamageMultiplier = 0.0f;

            // Check if weapon is bound
            bool isBound = (weapon.EditorID?.Contains("bound", StringComparison.OrdinalIgnoreCase) ?? false) ||
                          (weapon.Name?.String?.Contains("bound", StringComparison.OrdinalIgnoreCase) ?? false);

            // Handle bound weapons
            if (isBound)
            {
                var boundDamageOffset = _weaponDataManager?.GetBoundWeaponDamageOffset(weaponOverride, settings);
                if (boundDamageOffset.HasValue)
                {
                    damageOffset += boundDamageOffset.Value;
                    debugInfo.MaterialDamageOffset = boundDamageOffset.Value;
                }
            }
            else
            {
                // Get material offsets for non-bound weapons
                var materialOffsets = _weaponDataManager?.GetMaterialOffsets(weapon, state.LinkCache, CurrentSettings.WACCFMaterialTiers, preResolvedKeywords);
                if (materialOffsets == null)
                {
                    _weaponDataManager?.DebugLog($"Warning: Could not determine material offsets for {weapon.EditorID}. Skipping.");
                    debugInfo.ProcessingResult = "Could not determine material offsets";
                    if (CurrentSettings.DebugMode) _processedWeapons.Add(debugInfo);
                    return;
                }
                additionalMaterialDamage = materialOffsets.Value.DamageOffset;
                additionalMaterialReach = materialOffsets.Value.ReachOffset;
                additionalMaterialSpeed = materialOffsets.Value.SpeedOffset;
                additionalMaterialStagger = materialOffsets.Value.StaggerOffset;
                additionalMaterialCriticalDamageOffset = materialOffsets.Value.CriticalDamageOffset;
                additionalMaterialCriticalDamageChanceMultiplier = materialOffsets.Value.CriticalDamageChanceMultiplierOffset;
                additionalMaterialCriticalDamageMultiplier = materialOffsets.Value.CriticalDamageMultiplierOffset;
                debugInfo.MaterialDamageOffset = materialOffsets.Value.DamageOffset;

                // Get variant settings for non-bound weapons
                var variantStats = _weaponDataManager?.GetVariantStats(weapon, state.LinkCache, preResolvedKeywords);
                if (variantStats != null)
                {
                    (additionalVariantDamage, additionalVariantReach, additionalVariantSpeed, additionalVariantStagger,
                     additionalVariantCriticalDamageOffset, additionalVariantCriticalDamageChanceMultiplier,
                     additionalVariantCriticalDamageMultiplier) = variantStats.Value;

                    debugInfo.VariantDamageOffset = additionalVariantDamage;
                    debugInfo.VariantReachOffset = additionalVariantReach;
                    debugInfo.VariantSpeedOffset = additionalVariantSpeed;
                    debugInfo.VariantStaggerOffset = additionalVariantStagger;
                    debugInfo.VariantCriticalDamageOffset = additionalVariantCriticalDamageOffset;
                    debugInfo.VariantCriticalChanceMultOffset = additionalVariantCriticalDamageChanceMultiplier;
                    debugInfo.VariantCriticalDamageMultOffset = additionalVariantCriticalDamageMultiplier;

                    _weaponDataManager?.DebugLog($"Weapon variant stats: " +
                        $"   AdditionalDamage: {additionalVariantDamage}, " +
                        $"   AdditionalReach: {additionalVariantReach}, " +
                        $"   AdditionalSpeed: {additionalVariantSpeed}, " +
                        $"   AdditionalStagger: {additionalVariantStagger}, " +
                        $"   AdditionalCriticalDamageOffset: {additionalVariantCriticalDamageOffset}, " +
                        $"   AdditionalCriticalDamageChanceMultiplier: {additionalVariantCriticalDamageChanceMultiplier}, " +
                        $"   AdditionalCriticalDamageMultiplier: {additionalVariantCriticalDamageMultiplier}");
                }

                // Get weapon keywords for material checks
                List<string> weaponKeywords = preResolvedKeywords ?? _weaponDataManager?.GetWeaponKeywords(weapon, state.LinkCache) ?? new List<string>();
                debugInfo.InKeywords = string.Join(";", weaponKeywords);

                // Apply Stalhrim-specific bonuses if applicable
                bool isStalhrim = weaponKeywords.Contains("DLC2WeaponMaterialStalhrim");
                if (isStalhrim)
                {
                    _weaponDataManager?.DebugLog($"Has DLC2WeaponMaterialStalhrim: {isStalhrim}");

                    // Apply Stalhrim stagger bonus
                    if (weaponOverride.Data.Stagger > 0)
                    {
                        _weaponDataManager?.DebugLog($"Stagger: {weaponOverride.Data.Stagger}. Adding Stalhrim stagger bonus: {CurrentSettings.StalhrimStaggerBonus}");
                        staggerOffset += (decimal)CurrentSettings.StalhrimStaggerBonus;
                    }

                    // Apply Stalhrim damage bonus for war axes and maces
                    bool isWarAxe = weaponKeywords.Contains("WeapTypeWarAxe");
                    bool isMace = weaponKeywords.Contains("WeapTypeMace");
                    if (isWarAxe || isMace)
                    {
                        _weaponDataManager?.DebugLog($"Is War Axe: {isWarAxe}. Is Mace: {isMace}. Adding Stalhrim damage bonus: {CurrentSettings.StalhrimDamageBonus}");
                        damageOffset += CurrentSettings.StalhrimDamageBonus;
                        _weaponDataManager?.DebugLog($"Applied Stalhrim damage bonus. Damage: {weaponOverride.BasicStats.Damage}");
                    }
                }

                // if material is glass and waccf is enabled, apply glass critical damage bonus of 0.5
                if (CurrentSettings.WACCFMaterialTiers)
                {
                    bool isGlass = weaponKeywords.Contains("WeapMaterialGlass");
                    bool isDaedric = weaponKeywords.Contains("WeapMaterialDaedric");
                    bool isFalmerHoned = weaponKeywords.Contains("WeapMaterialFalmerHoned");
                    bool isWarAxe = weaponKeywords.Contains("WeapTypeWarAxe");
                    bool isWarHammer = weaponKeywords.Contains("WeapTypeWarhammer");
                    _weaponDataManager?.DebugLog($"WACCFMaterialTiers: {CurrentSettings.WACCFMaterialTiers}, checking if glass or daedric warhammer or falmer honed waraxe");
                    _weaponDataManager?.DebugLog($"isGlass: {isGlass}, isDaedric: {isDaedric}, isFalmerHoned: {isFalmerHoned}, isWarAxe: {isWarAxe}, isWarHammer: {isWarHammer}");
                    if (isGlass || (isDaedric && isWarHammer) || (isFalmerHoned && isWarAxe))
                    {
                        _weaponDataManager?.DebugLog($"Adding critical damage bonus of 0.5");
                        criticalDamageOffset += 0.5m;
                    }
                    else
                    {
                        _weaponDataManager?.DebugLog($"No glass or daedric warhammer or falmer honed waraxe, not adding critical damage bonus");
                    }
                }
            }

            // Apply stats (checking enablers)
            var enablers = CurrentSettings.WeaponAttributeEnablers;

            if (enablers.EnableDamage)
            {
                weaponOverride.BasicStats.Damage = WeaponDataManager.ClampUshort(settings.Damage + damageOffset + additionalMaterialDamage + additionalVariantDamage);
            }

            if (enablers.EnableSpeed)
            {
                weaponOverride.Data.Speed = WeaponDataManager.ClampFloat((float)((decimal)settings.Speed + (decimal)additionalMaterialSpeed + (decimal)additionalVariantSpeed));
            }

            if (enablers.EnableReach)
            {
                weaponOverride.Data.Reach = WeaponDataManager.ClampFloat((float)((decimal)settings.Reach + (decimal)additionalMaterialReach + (decimal)additionalVariantReach));
            }

            if (enablers.EnableStagger)
            {
                weaponOverride.Data.Stagger = WeaponDataManager.ClampFloat((float)((decimal)settings.Stagger + staggerOffset + (decimal)additionalMaterialStagger + (decimal)additionalVariantStagger));
            }

            if (enablers.EnableCriticalDamageChanceMultiplier)
            {
                weaponOverride.Critical.PercentMult = WeaponDataManager.ClampFloat((float)((decimal)settings.CriticalDamageChanceMultiplier + (decimal)additionalMaterialCriticalDamageChanceMultiplier + (decimal)additionalVariantCriticalDamageChanceMultiplier));
            }

            // Apply critical damage stats
            if (enablers.EnableCriticalDamage)
            {
                decimal halfDamage = (decimal)weaponOverride.BasicStats.Damage / 2m;
                decimal finalCriticalDamageOffset = (decimal)settings.CriticalDamageOffset + (decimal)additionalMaterialCriticalDamageOffset + (decimal)additionalVariantCriticalDamageOffset + criticalDamageOffset;
                decimal criticalDamageMultiplier = (decimal)settings.CriticalDamageMultiplier + (decimal)additionalMaterialCriticalDamageMultiplier + (decimal)additionalVariantCriticalDamageMultiplier;
                decimal criticalDamage = Math.Floor(criticalDamageMultiplier * (halfDamage + finalCriticalDamageOffset));
                weaponOverride.Critical.Damage = WeaponDataManager.ClampUshort((int)criticalDamage);
                _weaponDataManager?.DebugLog($"Critical damage: {criticalDamage}");
            }

            // Update debug info with final stats (these reflect actual weapon values, whether changed or not)
            debugInfo.FinalDamage = weaponOverride.BasicStats.Damage;
            debugInfo.FinalSpeed = weaponOverride.Data.Speed;
            debugInfo.FinalReach = weaponOverride.Data.Reach;
            debugInfo.FinalStagger = weaponOverride.Data.Stagger;
            debugInfo.FinalCriticalDamage = weaponOverride.Critical.Damage;
            debugInfo.FinalCriticalChanceMult = weaponOverride.Critical.PercentMult;
            debugInfo.ProcessingResult = "Successfully processed";

            // Check if any values actually changed
            bool madeModification = weaponOverride.BasicStats.Damage != originalStats.Damage ||
                                   weaponOverride.Data.Speed != originalStats.Speed ||
                                   weaponOverride.Data.Reach != originalStats.Reach ||
                                   weaponOverride.Data.Stagger != originalStats.Stagger ||
                                   weaponOverride.Critical.Damage != originalStats.CriticalDamage ||
                                   weaponOverride.Critical.PercentMult != originalStats.CriticalChanceMult;

            // Only add to mod if changes were made
            if (madeModification)
            {
                state.PatchMod.Weapons.Set(weaponOverride);
                _weaponDataManager?.DebugLog($"Successfully processed weapon: {weapon.EditorID}");
                _weaponDataManager?.DebugLog($"Final stats:");
                _weaponDataManager?.DebugLog($"- Damage: {weaponOverride.BasicStats.Damage}");
                _weaponDataManager?.DebugLog($"- Speed: {weaponOverride.Data.Speed}");
                _weaponDataManager?.DebugLog($"- Reach: {weaponOverride.Data.Reach}");
                _weaponDataManager?.DebugLog($"- Stagger: {weaponOverride.Data.Stagger}");
                _weaponDataManager?.DebugLog($"- Critical Damage: {weaponOverride.Critical.Damage}");
                _weaponDataManager?.DebugLog($"- Critical Damage Chance Multiplier: {weaponOverride.Critical.PercentMult}");
            }
            else
            {
                _weaponDataManager?.DebugLog($"No changes needed for weapon: {weapon.EditorID}");
                debugInfo.ProcessingResult = "No changes needed";
            }

            if (CurrentSettings.DebugMode) _processedWeapons.Add(debugInfo);
        }

        /// <summary>
        /// Verifies that all form keys in the special_weapons.json file are correct by looking up each editor ID.
        /// </summary>
        private static void VerifyFormKeys(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Console.WriteLine("Verifying form keys in special_weapons.json...");

            try
            {
                var specialWeaponsPath = state.RetrieveInternalFile("special_weapons.json");
                string jsonContent = File.ReadAllText(specialWeaponsPath);
                var specialWeapons = System.Text.Json.JsonSerializer.Deserialize<List<SpecialWeaponData>>(jsonContent) ?? new List<SpecialWeaponData>();

                _weaponDataManager?.DebugLog($"Loaded {specialWeapons.Count} special weapons from JSON file.");

                // Lists to store valid, invalid, and skipped entries
                var validEntries = new List<(string EditorID, string FormKey)>();
                var invalidEntries = new List<(string EditorID, string FormKey, string Error)>();
                var skippedEntries = new List<(string EditorID, string FormKey, string Reason)>();
                var suggestedFormKeys = new List<(string EditorID, string CurrentFormKey, string SuggestedFormKey)>();

                // Check each special weapon
                foreach (var specialWeapon in specialWeapons)
                {
                    if (string.IsNullOrEmpty(specialWeapon.EditorID))
                    {
                        invalidEntries.Add((specialWeapon.EditorID ?? "null", specialWeapon.FormKey ?? "null", "EditorID is null or empty"));
                        continue;
                    }

                    if (string.IsNullOrEmpty(specialWeapon.FormKey))
                    {
                        invalidEntries.Add((specialWeapon.EditorID, specialWeapon.FormKey ?? "null", "FormKey is null or empty"));
                        continue;
                    }

                    // Try to parse the form key
                    if (!FormKey.TryFactory(specialWeapon.FormKey, out var formKey))
                    {
                        invalidEntries.Add((specialWeapon.EditorID, specialWeapon.FormKey, "Invalid form key format"));
                        continue;
                    }

                    // Check if the mod is in the load order
                    if (!state.LoadOrder.PriorityOrder.Any(mod => mod.ModKey == formKey.ModKey))
                    {
                        skippedEntries.Add((specialWeapon.EditorID, specialWeapon.FormKey, $"Mod '{formKey.ModKey.FileName}' not in load order"));
                        continue;
                    }

                    try
                    {
                        // Look up the weapon by form key
                        var weapon = state.LinkCache.Resolve<IWeaponGetter>(formKey);

                        if (weapon == null)
                        {
                            // Try to look up the weapon by editor ID in original definitions
                            var originalWeapon = state.LoadOrder.ListedOrder.Weapon()
                                .WinningOverrides()
                                .FirstOrDefault(w => w.EditorID == specialWeapon.EditorID);

                            if (originalWeapon != null)
                            {
                                var suggestedFormKey = $"{originalWeapon.FormKey.ID:X6}:{originalWeapon.FormKey.ModKey.FileName}";
                                suggestedFormKeys.Add((specialWeapon.EditorID, specialWeapon.FormKey, suggestedFormKey));
                                invalidEntries.Add((specialWeapon.EditorID, specialWeapon.FormKey, $"Record not found in load order. Suggested form key: {suggestedFormKey}"));
                            }
                            else
                            {
                                invalidEntries.Add((specialWeapon.EditorID, specialWeapon.FormKey, "Record not found in load order"));
                            }
                            continue;
                        }

                        // Check if the editor ID matches
                        if (weapon.EditorID != specialWeapon.EditorID)
                        {
                            // Get the original definition of this weapon
                            var originalWeapon = state.LoadOrder.ListedOrder.Weapon()
                                .WinningOverrides()
                                .FirstOrDefault(w => w.FormKey == weapon.FormKey);

                            if (originalWeapon != null && originalWeapon.EditorID == specialWeapon.EditorID)
                            {
                                // The original version matches our editor ID, so this is valid
                                validEntries.Add((specialWeapon.EditorID, specialWeapon.FormKey));
                            }
                            else
                            {
                                invalidEntries.Add((specialWeapon.EditorID, specialWeapon.FormKey,
                                    $"EditorID mismatch: expected '{specialWeapon.EditorID}', found '{originalWeapon?.EditorID ?? "unknown"}'."));
                            }
                            continue;
                        }

                        // If we get here, the form key is valid
                        validEntries.Add((specialWeapon.EditorID, specialWeapon.FormKey));
                    }
                    catch (Mutagen.Bethesda.Plugins.Exceptions.MissingRecordException)
                    {
                        // Try to look up the weapon by editor ID in original definitions
                        var originalWeapon = state.LoadOrder.ListedOrder.Weapon()
                            .WinningOverrides()
                            .FirstOrDefault(w => w.EditorID == specialWeapon.EditorID);

                        if (originalWeapon != null)
                        {
                            var suggestedFormKey = $"{originalWeapon.FormKey.ID:X6}:{originalWeapon.FormKey.ModKey.FileName}";
                            suggestedFormKeys.Add((specialWeapon.EditorID, specialWeapon.FormKey, suggestedFormKey));
                            invalidEntries.Add((specialWeapon.EditorID, specialWeapon.FormKey, $"Record not found in load order. Suggested form key: {suggestedFormKey}"));
                        }
                        else
                        {
                            invalidEntries.Add((specialWeapon.EditorID, specialWeapon.FormKey, "Record not found in load order"));
                        }
                    }
                }

                // Print results
                _weaponDataManager?.DebugLog("=== VALID FORM KEYS ===");
                if (validEntries.Count == 0)
                {
                    _weaponDataManager?.DebugLog("No valid form keys found.");
                }
                else
                {
                    foreach (var entry in validEntries)
                    {
                        _weaponDataManager?.DebugLog($"Valid: {entry.EditorID}: {entry.FormKey}");
                    }
                }

                Console.WriteLine("=== INVALID FORM KEYS ===");
                if (invalidEntries.Count == 0)
                {
                    Console.WriteLine("No invalid form keys found.");
                }
                else
                {
                    foreach (var entry in invalidEntries)
                    {
                        Console.WriteLine($"Invalid: {entry.EditorID}: {entry.FormKey} - {entry.Error}");
                    }
                }

                _weaponDataManager?.DebugLog("\n=== SKIPPED FORM KEYS ===");
                if (skippedEntries.Count == 0)
                {
                    _weaponDataManager?.DebugLog("No skipped form keys found.");
                }
                else
                {
                    foreach (var entry in skippedEntries)
                    {
                        _weaponDataManager?.DebugLog($"Skipped: {entry.EditorID}: {entry.FormKey} - {entry.Reason}");
                    }
                }

                Console.WriteLine($"\nSummary: {validEntries.Count} valid, {invalidEntries.Count} invalid, {skippedEntries.Count} skipped form keys.\n");

                // Print suggested form keys
                if (suggestedFormKeys.Count > 0)
                {
                    Console.WriteLine("\n=== SUGGESTED FORM KEY CORRECTIONS ===");
                    Console.WriteLine("The following entries have incorrect form keys. Here are the suggested corrections:");
                    foreach (var suggestion in suggestedFormKeys)
                    {
                        Console.WriteLine($"  {suggestion.EditorID}: {suggestion.CurrentFormKey} -> {suggestion.SuggestedFormKey}");
                    }
                    Console.WriteLine("==================\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying form keys: {ex.Message}");
            }
        }
    }
}
