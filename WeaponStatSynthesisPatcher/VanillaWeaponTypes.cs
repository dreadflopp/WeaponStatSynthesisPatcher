using System;
using System.Collections.Generic;

namespace WeaponStatSynthesisPatcher
{
    internal static class VanillaWeaponTypes
    {
        // Canonical vanilla categories used for match-priority grouping.
        private static readonly HashSet<string> VanillaWeaponKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "Dagger",
            "Sword",
            "Mace",
            "War Axe",
            "Greatsword",
            "Warhammer",
            "Battleaxe"
        };

        public static bool IsVanillaWeaponType(string? weaponTypeKey)
        {
            return !string.IsNullOrWhiteSpace(weaponTypeKey) && VanillaWeaponKeys.Contains(weaponTypeKey);
        }
    }
}