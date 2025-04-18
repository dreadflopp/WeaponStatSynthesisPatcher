# Synthesis Patcher Development Reference

## Core Concepts
- Synthesis allows modders to develop mods via code (patchers)
- Patchers are bundled into a single `Synthesis.esp` file
- Patchers should be rerun when mods are added/removed to adapt to new content

## Project Structure
```
project_root/
├── .gitignore
├── .editorconfig
├── Directory.Build.props
├── Program.cs
├── [ProjectName].sln
└── docs/
    ├── Synthesis/     # Synthesis-specific documentation
    └── Mutagen/       # Mutagen framework documentation
```

## Essential Files
1. `Directory.Build.props`:
```xml
<Project>
    <PropertyGroup>
        <Nullable>enable</Nullable>
        <WarningsAsErrors>nullable</WarningsAsErrors>
    </PropertyGroup>
</Project>
```

2. `Program.cs` Basic Structure:
```csharp
public static async Task<int> Main(string[] args)
{
    return await SynthesisPipeline.Instance
        .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
        .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
        .Run(args);
}

public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
{
    // Patcher logic here
}
```

## Key Documentation References

### Synthesis Documentation
Location: `docs/Synthesis/`
Key files:
- `index.md`: Overview of Synthesis functionality
- `Installation.md`: Setup instructions
- `Typical-Usage.md`: Common usage patterns
- `devs/Create-a-Patcher.md`: Development guide
- `devs/Coding-a-Patcher.md`: Coding best practices
- `devs/Running-and-Debugging.md`: Testing and debugging

### Mutagen Documentation
Location: `docs/Mutagen/`
Key files:
- `index.md`: Mutagen framework overview
- `Big-Cheat-Sheet.md`: Quick reference for common operations
- `Correctness.md`: Best practices for record handling
- `linkcache/`: Documentation for record linking and caching
- `loadorder/`: Load order management
- `best-practices/`: Development guidelines

## Important Concepts for AI Assistance

### Record Handling
- Always use `WinningOverrides()` when iterating through records
- Use `GetOrAddAsOverride()` when modifying records
- Handle nullability properly (enabled by default)
- Use `LinkCache` for record resolution

### State Management
- Access game data through `state.LoadOrder`
- Create patches using `state.PatchMod`
- Use `state.LinkCache` for record resolution

### Best Practices
1. Null Safety:
   - Enable nullable reference types
   - Handle null cases explicitly
   - Use null-conditional operators when appropriate

2. Record Modification:
   - Always create overrides, never modify original records
   - Use proper record type casting
   - Handle record presence checks

3. Performance:
   - Use efficient record iteration
   - Minimize record resolution operations
   - Cache frequently accessed data

### Common Patterns
1. Record Iteration:
```csharp
foreach (var record in state.LoadOrder.PriorityOrder.RecordType().WinningOverrides())
{
    var override = state.PatchMod.RecordType.GetOrAddAsOverride(record);
    // Modify override
}
```

2. Record Resolution:
```csharp
if (formLink.TryResolve(state.LinkCache, out var resolvedRecord))
{
    // Use resolvedRecord
}
```

3. Settings Management:
```csharp
static Lazy<Settings> Settings = null!;
// Initialize in Main or RunPatch
Settings = new Lazy<Settings>(() => state.GetSettings<Settings>());
```
# Skyrim Weapon Object Structure (Mutagen / Synthesis)

This document provides a comprehensive overview of the `Weapon` object structure used in Skyrim modding via Mutagen/Synthesis, as well as code examples for reading and writing weapon properties.

---

## 🔸 Basic Weapon Info

### General Fields

| Property      | Type                                      | Description                                                         |
| ------------- | ----------------------------------------- | ------------------------------------------------------------------- |
| `FormKey`     | `FormKey`                                 | Unique identifier for the weapon, including mod filename and FormID |
| `EditorID`    | `string`                                  | Internal ID used for scripting/modding                              |
| `Name`        | `TranslatedString`                        | In-game display name                                                |
| `Description` | `TranslatedString`                        | Optional description text                                           |
| `Keywords`    | `IReadOnlyList<FormLink<IKeywordGetter>>` | Tags for sorting, filtering, gameplay logic                         |

---

## 🗡 Model and Sound Fields

| Property           | Type                               | Description                   |
| ------------------ | ---------------------------------- | ----------------------------- |
| `Model`            | `Model`                            | Path to the weapon mesh (NIF) |
| `FirstPersonModel` | `Model`                            | Used in first-person view     |
| `EquipSound`       | `FormLink<ISoundDescriptorGetter>` | Sound when equipped           |
| `UnequipSound`     | `FormLink<ISoundDescriptorGetter>` | Sound when unequipped         |
| `AttackFailSound`  | `FormLink<ISoundDescriptorGetter>` | Played on failed attack       |

---

## ⚔ Stats

### Basic Stats

| Property | Type     | Description          |
| -------- | -------- | -------------------- |
| `Damage` | `int16`  | Base physical damage |
| `Weight` | `float`  | In-game weight       |
| `Value`  | `uint32` | Base gold value      |

### Weapon Data Block (`Data`)

| Field             | Type                  | Description                                                    |
| ----------------- | --------------------- | -------------------------------------------------------------- |
| `AnimationType`   | `WeaponAnimationType` | Determines animation used (e.g. TwoHandSword)                  |
| `Speed`           | `float`               | Affects attack speed (1.0 = normal)                            |
| `Reach`           | `float`               | Attack range (1.0 = default)                                   |
| `Stagger`         | `float`               | Multiplier to stagger chance                                   |
| `Skill`           | `ActorValue`          | Governing skill (e.g., TwoHanded, OneHanded)                   |
| `Flags`           | `WeaponData.Flag`     | Bitflags, e.g., `NonPlayable`, `IgnoresNormalWeaponResistance` |
| `AttackAnimation` | `AttackAnimation`     | Affects VATS animations (default: `Default`)                   |

### Weapon Flags

Examples:

- `None`
- `NonPlayable`
- `IgnoresNormalWeaponResistance`
- `BoundWeapon`

Use with:

```csharp
if (weapon.Data.Flags.HasFlag(WeaponData.Flag.NonPlayable)) { ... }
```

---

## 💥 Critical Damage

| Field                  | Type                            | Description               |
| ---------------------- | ------------------------------- | ------------------------- |
| `Critical.Damage`      | `int16`                         | Flat crit damage added    |
| `Critical.PercentMult` | `float`                         | Multiplier (1.0 = normal) |
| `Critical.Flags`       | `CriticalData.Flag`             | e.g., `OnDeath`           |
| `Critical.Effect`      | `FormLink<IEffectRecordGetter>` | Optional magic effect     |

---

## 🧩 Template and Equipment

| Property        | Description                                       |
| --------------- | ------------------------------------------------- |
| `Template`      | Weapon that this weapon inherits from             |
| `EquipmentType` | Determines weapon slot (e.g. OneHandedSword, Bow) |

---

## 🔉 Misc Fields

| Field                 | Description                      |
| --------------------- | -------------------------------- |
| `ImpactDataSet`       | Used for hit effects             |
| `DetectionSoundLevel` | How loud the weapon is when used |

---

## 📦 Example Weapon Output (From `art_RBladeGreatsword`)

```yaml
EditorID: art_RBladeGreatsword
Name: Ritevice
FormKey: 000802:Skyrim Weapons Expansion.esp
ModelPath: Meshes\Artsick\rblade\r_greatsword.nif
Damage: 22
Weight: 22
Value: 1440
Reach: 1.3
Speed: 0.75
Stagger: 1.1
Skill: TwoHanded
Critical:
  Damage: 11
  PercentMult: 1
  Flags: OnDeath
Flags: None
Keywords:
  - 01E71E:Skyrim.esm
  - 06D931:Skyrim.esm
  - 08F958:Skyrim.esm
EquipmentType: 013F45:Skyrim.esm
ImpactDataSet: 0949D5:Skyrim.esm
```

---

## 🧪 Sample Code: Read and Modify Weapon

```csharp
var weapons = State.LoadOrder.PriorityOrder.Weapon().WinningOverrides();

foreach (var weapon in weapons)
{
    if (weapon.IsDeleted) continue;

    Console.WriteLine($"Weapon: {weapon.EditorID}");

    var mutableWeapon = weapon.DeepCopy();

    // Example: scale damage by 10%
    if (mutableWeapon.Data != null)
    {
        mutableWeapon.Data.Damage = (short)(mutableWeapon.Data.Damage * 1.1);
        mutableWeapon.Data.Speed += 0.1f;
    }

    // Example: Add a keyword if missing
    var newKeyword = State.LinkCache.GetForm<IKeywordGetter>(FormKey.Factory("0006BBE9", "Skyrim.esm"));
    if (!mutableWeapon.Keywords.Any(k => k.FormKey == newKeyword.FormKey))
    {
        mutableWeapon.Keywords.Add(newKeyword.ToLink());
    }

    // Add modified weapon to patch
    State.PatchMod.Weapons.Set(mutableWeapon);
}
```

---

## ✅ Tips

- Always null-check `Data`, `BasicStats`, and nested fields.
- Use `.DeepCopy()` when modifying existing forms.
- Keyword checks should compare `FormKey`, not object references.
- For adding brand-new weapons, use `mod.Weapons.AddNew()` and set fields manually.

---

## Error Handling
- Use proper exception handling
- Log errors appropriately
- Handle missing records gracefully
- Validate input data

## Testing
- Use IDE for development and debugging
- Test with various load orders
- Verify record modifications
- Check for conflicts with other mods

## Additional Resources
- Full documentation available in `docs/` directory
- Synthesis Discord for community support
- Mutagen documentation for detailed API reference
- Best practices documentation for advanced patterns 