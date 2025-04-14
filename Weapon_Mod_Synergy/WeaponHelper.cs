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
using System.Text.Json;

namespace Weapon_Mod_Synergy
{
    // Classes for JSON deserialization
    public class MaterialData
    {
        public string keyword { get; set; } = "";
        public int damage_offset_1h { get; set; }
        public int damage_offset_2h { get; set; }
        public int damage_offset_1h_waccf { get; set; }
        public int damage_offset_2h_waccf { get; set; }
    }

    public class WeaponTypeData
    {
        public string keyword { get; set; } = "";
        public string name { get; set; } = "";
        public string grip { get; set; } = "";
    }

    public static class WeaponHelper
    {
        // Static lists to hold the JSON data
        private static List<MaterialData> _materialData = new List<MaterialData>();
        private static List<MaterialData> _materialDataOverride = new List<MaterialData>();
        private static List<MaterialData> _materialDataEditorIdOverride = new List<MaterialData>();
        private static List<WeaponTypeData> _weaponTypeData = new List<WeaponTypeData>();
        private static List<WeaponTypeData> _weaponTypeDataOverride = new List<WeaponTypeData>();
        private static bool _dataLoaded = false;

        // Load JSON data from files
        private static void LoadJsonData()
        {
            if (_dataLoaded)
            {
                return;
            }

            try
            {
                string patcherDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
                
                string materialJsonPath = Path.Combine(patcherDirectory, "material_data.json");
                if (File.Exists(materialJsonPath))
                {                    
                    string materialJson = File.ReadAllText(materialJsonPath);
                    
                    _materialData = JsonSerializer.Deserialize<List<MaterialData>>(materialJson) ?? new List<MaterialData>();
                }
                else
                {
                    Console.WriteLine($"Error: material_data.json not found at {materialJsonPath}. The patcher requires this file to function.");
                    Environment.Exit(1);
                }

                string materialOverrideJsonPath = Path.Combine(patcherDirectory, "material_data_override_with_name.json");
                if (File.Exists(materialOverrideJsonPath))
                {
                    string materialOverrideJson = File.ReadAllText(materialOverrideJsonPath);
                    
                    _materialDataOverride = JsonSerializer.Deserialize<List<MaterialData>>(materialOverrideJson) ?? new List<MaterialData>();
                }
                else
                {
                    Console.WriteLine($"Error: material_data_override_with_name.json not found at {materialOverrideJsonPath}. The patcher requires this file to function.");
                    Environment.Exit(1);
                }

                string materialEditorIdOverrideJsonPath = Path.Combine(patcherDirectory, "material_data_override_with_edid.json");
                if (File.Exists(materialEditorIdOverrideJsonPath))
                {
                    string materialEditorIdOverrideJson = File.ReadAllText(materialEditorIdOverrideJsonPath);
                    
                    _materialDataEditorIdOverride = JsonSerializer.Deserialize<List<MaterialData>>(materialEditorIdOverrideJson) ?? new List<MaterialData>();
                }
                else
                {
                    Console.WriteLine($"Error: material_data_override_with_edid.json not found at {materialEditorIdOverrideJsonPath}. The patcher requires this file to function.");
                    Environment.Exit(1);
                }

                string weaponTypeJsonPath = Path.Combine(patcherDirectory, "weapon_type_data.json");
                if (File.Exists(weaponTypeJsonPath))
                {
                    string weaponTypeJson = File.ReadAllText(weaponTypeJsonPath);
                    
                    _weaponTypeData = JsonSerializer.Deserialize<List<WeaponTypeData>>(weaponTypeJson) ?? new List<WeaponTypeData>();
                }
                else
                {
                    Console.WriteLine($"Error: weapon_type_data.json not found at {weaponTypeJsonPath}. The patcher requires this file to function.");
                    Environment.Exit(1);
                }

                string weaponTypeOverrideJsonPath = Path.Combine(patcherDirectory, "weapon_type_data_override_with_name.json");
                if (File.Exists(weaponTypeOverrideJsonPath))
                {
                    string weaponTypeOverrideJson = File.ReadAllText(weaponTypeOverrideJsonPath);
                    
                    _weaponTypeDataOverride = JsonSerializer.Deserialize<List<WeaponTypeData>>(weaponTypeOverrideJson) ?? new List<WeaponTypeData>();
                }
                else
                {
                    Console.WriteLine($"Error: weapon_type_data_override_with_name.json not found at {weaponTypeOverrideJsonPath}. The patcher requires this file to function.");
                    Environment.Exit(1);
                }

                _dataLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading JSON data: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        // Helper method to load weapon type data
        private static List<WeaponTypeData> LoadWeaponTypeData()
        {
            LoadJsonData();
            return _weaponTypeData;
        }

        // Helper method to load weapon type override data
        private static List<WeaponTypeData> LoadWeaponTypeOverrideData()
        {
            LoadJsonData();
            return _weaponTypeDataOverride;
        }

        // Helper method to load material data
        private static List<MaterialData> LoadMaterialData()
        {
            LoadJsonData();
            return _materialData;
        }

        // Helper method to load material override data
        private static List<MaterialData> LoadMaterialOverrideData()
        {
            LoadJsonData();
            return _materialDataOverride;
        }

        // Helper method to load material editor ID override data
        private static List<MaterialData> LoadMaterialEditorIdOverrideData()
        {
            LoadJsonData();
            return _materialDataEditorIdOverride;
        }

        /// <summary>
        /// Gets the weapon type based on keywords and name
        /// </summary>
        public static string? GetWeaponType(IWeaponGetter weapon, ILinkCache linkCache, bool includeWACCF = false)
        {
            if (weapon == null) return null;

            // First check if the weapon name matches any override types
            if (weapon.Name?.String != null)
            {
                string weaponName = weapon.Name.String.ToLower();
                var overrideTypes = LoadWeaponTypeOverrideData();
                
                foreach (var type in overrideTypes)
                {
                    if (weaponName.Contains(type.name.ToLower()))
                    {
                        return type.keyword;
                    }
                }
            }

            // If no name match found, check keywords
            if (weapon.Keywords != null)
            {
                // Get all keywords
                var keywords = new List<string>();
                foreach (var keyword in weapon.Keywords)
                {
                    if (!linkCache.TryResolve(keyword, out var resolvedKeyword))
                    {
                        continue;
                    }
                    var keywordName = resolvedKeyword.EditorID;
                    if (keywordName != null)
                    {
                        keywords.Add(keywordName);
                    }
                }
                               
                // Load weapon type data
                var weaponTypes = LoadWeaponTypeData();
                
                foreach (var weaponType in weaponTypes)
                {
                    if (keywords.Contains(weaponType.keyword))
                    {
                        return weaponType.keyword;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Gets the weapon grip type (1h or 2h) based on keywords and name
        /// </summary>
        public static string? GetWeaponGrip(IWeaponGetter weapon, ILinkCache linkCache)
        {
            if (weapon == null) return null;

            // First check if the weapon name matches any override types
            if (weapon.Name?.String != null)
            {
                string weaponName = weapon.Name.String.ToLower();
                var overrideTypes = LoadWeaponTypeOverrideData();
                
                foreach (var type in overrideTypes)
                {
                    if (weaponName.Contains(type.keyword.ToLower()))
                    {
                        return type.grip;
                    }
                }
            }

            if (weapon.Keywords == null) return null;

            // Get all keywords
            var keywords = new List<string>();
            foreach (var keyword in weapon.Keywords)
            {
                if (!linkCache.TryResolve(keyword, out var resolvedKeyword)) continue;
                var keywordName = resolvedKeyword.EditorID;
                if (keywordName != null)
                    keywords.Add(keywordName);
            }
            
            // Load weapon type data
            var weaponTypes = LoadWeaponTypeData();
            
            // Find the first matching weapon type (prioritizing order in JSON)
            foreach (var weaponType in weaponTypes)
            {
                if (keywords.Contains(weaponType.keyword))
                {
                    return weaponType.grip;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Gets the weapon material based on keywords and name
        /// </summary>
        public static string? GetWeaponMaterial(IWeaponGetter weapon, ILinkCache linkCache, bool includeWACCF = false)
        {
            if (weapon == null) return null;

            // Check for special cases based on weapon EditorID first
            if (weapon.EditorID != null)
            {
                string editorId = weapon.EditorID;
                
                // Check for editor ID overrides
                var editorIdOverrides = LoadMaterialEditorIdOverrideData();
                var matchingOverride = editorIdOverrides.FirstOrDefault(m => m.keyword.Equals(editorId, StringComparison.OrdinalIgnoreCase));
                if (matchingOverride != null)
                {
                    return matchingOverride.keyword;
                }
                
                string editorIdLower = editorId.ToLower();
                
                // Check for mystic bound weapons
                if (editorIdLower.Contains("bound") && editorIdLower.Contains("mystic"))
                {
                    return "WeapMaterialMysticBound";
                }
                
                // Check for regular bound weapons
                if (editorIdLower.Contains("bound"))
                {
                    return "WeapMaterialBound";
                }
            }

            // Then check if the weapon name matches any override materials
            if (weapon.Name?.String != null)
            {
                string weaponName = weapon.Name.String.ToLower();
                var overrideMaterials = LoadMaterialOverrideData();
                
                foreach (var material in overrideMaterials)
                {
                    if (weaponName.Contains(material.keyword.ToLower()))
                    {
                        return material.keyword;
                    }
                }
            }

            if (weapon.Keywords == null) return null;

            // Get all keywords
            var keywords = new List<string>();
            foreach (var keyword in weapon.Keywords)
            {
                if (!linkCache.TryResolve(keyword, out var resolvedKeyword)) continue;
                var keywordName = resolvedKeyword.EditorID;
                if (keywordName != null)
                    keywords.Add(keywordName);
            }
            
            // Load material data
            var materials = LoadMaterialData();
            
            // Get weapon grip type
            string? gripType = GetWeaponGrip(weapon, linkCache);
            if (gripType == null) return null;
            
            // Find the material with the highest damage offset
            MaterialData? bestMatch = null;
            int highestOffset = int.MinValue;
            
            foreach (var material in materials)
            {
                if (keywords.Contains(material.keyword))
                {
                    int offset = gripType == "1h" 
                        ? (includeWACCF ? material.damage_offset_1h_waccf : material.damage_offset_1h)
                        : (includeWACCF ? material.damage_offset_2h_waccf : material.damage_offset_2h);
                    
                    if (offset > highestOffset)
                    {
                        highestOffset = offset;
                        bestMatch = material;
                    }
                }
            }
            
            return bestMatch?.keyword;
        }

        // New GetDamageOffset function using JSON data
        public static int GetDamageOffset(IWeaponGetter weapon, ILinkCache linkCache, string material, bool waccfMaterialTiers, string grip)
        {
            // Load the material data
            var materialData = LoadMaterialData();
            var materialDataOverride = LoadMaterialOverrideData();
            var materialDataEditorIdOverride = LoadMaterialEditorIdOverrideData();

            // Get base damage offset
            int baseOffset = 0;

            // Check editor ID override first
            if (weapon.EditorID != null)
            {
                var editorIdOverride = materialDataEditorIdOverride.FirstOrDefault(m => m.keyword.Equals(weapon.EditorID, StringComparison.OrdinalIgnoreCase));
                if (editorIdOverride != null)
                {
                    baseOffset = grip == "1h"
                        ? (waccfMaterialTiers ? editorIdOverride.damage_offset_1h_waccf : editorIdOverride.damage_offset_1h)
                        : (waccfMaterialTiers ? editorIdOverride.damage_offset_2h_waccf : editorIdOverride.damage_offset_2h);
                    return baseOffset;
                }
            }

            // Then check name override
            var overrideMaterial = materialDataOverride.FirstOrDefault(m => m.keyword.Equals(material, StringComparison.OrdinalIgnoreCase));
            if (overrideMaterial != null)
            {
                baseOffset = grip == "1h"
                    ? (waccfMaterialTiers ? overrideMaterial.damage_offset_1h_waccf : overrideMaterial.damage_offset_1h)
                    : (waccfMaterialTiers ? overrideMaterial.damage_offset_2h_waccf : overrideMaterial.damage_offset_2h);
            }
            else
            {
                // Finally check main data
                var mainMaterial = materialData.FirstOrDefault(m => m.keyword.Equals(material, StringComparison.OrdinalIgnoreCase));
                if (mainMaterial != null)
                {
                    baseOffset = grip == "1h"
                        ? (waccfMaterialTiers ? mainMaterial.damage_offset_1h_waccf : mainMaterial.damage_offset_1h)
                        : (waccfMaterialTiers ? mainMaterial.damage_offset_2h_waccf : mainMaterial.damage_offset_2h);
                }
            }

            // Check for Stalhrim war axes and maces bonus
            if (material == "DLC2WeaponMaterialStalhrim" && weapon.Keywords != null)
            {
                var keywords = new List<string>();
                foreach (var keyword in weapon.Keywords)
                {
                    if (!linkCache.TryResolve(keyword, out var resolvedKeyword)) continue;
                    var keywordName = resolvedKeyword.EditorID;
                    if (keywordName != null)
                        keywords.Add(keywordName);
                }

                if (keywords.Contains("WeapTypeWarAxe") || keywords.Contains("WeapTypeMace"))
                {
                    baseOffset += 1;
                }
            }

            return baseOffset;
        }
    }
} 