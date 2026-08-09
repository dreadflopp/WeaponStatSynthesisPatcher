using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace WeaponStatSynthesisPatcher
{
    public partial class SettingsWindow : Window
    {
        private readonly Settings _settings;
        private readonly string? _settingsFolder;
        private readonly TreeView _settingsTree;
        private readonly StackPanel _detailPanel;
        private readonly TextBlock _statusText;
        private readonly Button _closeButton;
        private readonly Button _saveButton;
        private string _lastSavedSettingsSnapshot = string.Empty;
        private readonly HashSet<string> _defaultWeaponMaterialTitles;
        private readonly Dictionary<TextBox, Func<string, string?>> _fieldValidators = new();
        private readonly Dictionary<TextBox, object?> _validationTooltips = new();
        private static readonly Brush ValidationErrorBrush = Brushes.Firebrick;

        private sealed class PendingTextSnapshot
        {
            public required string OriginalText { get; init; }
        }

        private sealed class RestoreSelection
        {
            public bool RestoreGlobalSettings { get; set; }
            public bool RestoreVariants { get; set; }
            public bool RestoreWeaponMaterials { get; set; }
            public bool RestoreUniqueWeapons { get; set; }
            public bool RestoreAllWeaponCategories { get; set; }
        }

        private enum NodeKind
        {
            Global,
            WeaponMaterials,
            VariantContainer,
            SpecialWeapons,
            CategoryContainer,
            WeaponCategory
        }

        private sealed class SpecialWeaponListItem
        {
            public required int SourceIndex { get; init; }
            public required SpecialWeaponData Data { get; init; }
            public required string EditorId { get; init; }
            public required string FormKey { get; init; }
            public required string ModName { get; init; }
            public int? DamageOffset { get; init; }
            public float? SpeedOffset { get; init; }
            public float? ReachOffset { get; init; }
            public float? StaggerOffset { get; init; }
            public float? CriticalDamageOffset { get; init; }
            public float? CriticalDamageChanceMultiplierOffset { get; init; }
        }

        private sealed class NodeRef
        {
            public NodeKind Kind { get; init; }

            // WeaponCategory nodes
            public PropertyInfo? CategoryProperty { get; init; }
            public WeaponCategory? Category { get; init; }
        }

        private sealed class EnumOption<TEnum> where TEnum : struct, Enum
        {
            public required TEnum Value { get; init; }
            public required string Label { get; init; }
        }

        private sealed class EnumOptionItem
        {
            public required object Value { get; init; }
            public required string Label { get; init; }
        }

        private static readonly HashSet<string> ExpandableMultilineGlobalFields = new(StringComparer.Ordinal)
        {
            nameof(Settings.PluginIncludeList),
            nameof(Settings.PluginExcludeList),
            nameof(Settings.IgnoredWeaponFormKeys)
        };

        private static readonly IReadOnlyDictionary<(Type Owner, string Property), string> PropertyTooltips =
            new Dictionary<(Type Owner, string Property), string>
            {
                [(typeof(Settings), nameof(Settings.DebugMode))] = "Enable debug output",
                [(typeof(Settings), nameof(Settings.PluginFilter))] = "Choose which plugins to process",
                [(typeof(Settings), nameof(Settings.PluginIncludeList))] = "List of plugins to include (semicolon separated)",
                [(typeof(Settings), nameof(Settings.PluginExcludeList))] = "List of plugins to exclude (semicolon separated)",
                [(typeof(Settings), nameof(Settings.IgnoredWeaponFormKeys))] = "List of weapon form keys to ignore (semicolon separated)",
                [(typeof(Settings), nameof(Settings.StalhrimStaggerBonus))] = "Enable support for Stalhrim stagger bonus, if current stagger is greater than 0",
                [(typeof(Settings), nameof(Settings.StalhrimDamageBonus))] = "Enable support for Stalhrim War Axe and Mace damage bonus",
                [(typeof(Settings), nameof(Settings.BoundWeaponParsing))] = "Choose how to parse bound weapons",
                [(typeof(WeaponAttributeEnablers), nameof(WeaponAttributeEnablers.EnableDamage))] = "Enable or disable damage attribute edits",
                [(typeof(WeaponAttributeEnablers), nameof(WeaponAttributeEnablers.EnableReach))] = "Enable or disable reach attribute edits",
                [(typeof(WeaponAttributeEnablers), nameof(WeaponAttributeEnablers.EnableSpeed))] = "Enable or disable speed attribute edits",
                [(typeof(WeaponAttributeEnablers), nameof(WeaponAttributeEnablers.EnableStagger))] = "Enable or disable stagger attribute edits",
                [(typeof(WeaponAttributeEnablers), nameof(WeaponAttributeEnablers.EnableCriticalDamage))] = "Enable or disable critical damage edits (damage and multiplier)",
                [(typeof(WeaponAttributeEnablers), nameof(WeaponAttributeEnablers.EnableCriticalDamageChanceMultiplier))] = "Enable or disable critical damage chance multiplier edits"
            };

        private static readonly Regex FormKeyPattern = new(
            @"^[0-9A-F]{6}:[^\\/:*?""<>|\r\n]+\.(?:esp|esl|esm)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public SettingsWindow(Settings settings, string? settingsFolder)
        {
            LoadComponent();
            _settings = settings;
            _settingsFolder = settingsFolder;

            _settingsTree = GetRequiredControl<TreeView>("SettingsTree");
            _detailPanel = GetRequiredControl<StackPanel>("DetailPanel");
            _statusText = GetRequiredControl<TextBlock>("StatusText");
            _closeButton = GetRequiredControl<Button>("CloseButton");
            _saveButton = GetRequiredControl<Button>("SaveButton");

            _defaultWeaponMaterialTitles = Settings.LoadBundledWeaponMaterials()
                .Where(m => !string.IsNullOrWhiteSpace(m.Title))
                .Select(m => m.Title)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _settingsTree.SelectedItemChanged += (_, _) => RenderSelectedNode();
            _saveButton.Click += (_, _) => Save();
            _closeButton.Click += (_, _) => Close();
            Closing += OnWindowClosing;

            _lastSavedSettingsSnapshot = GetSettingsSnapshot();

            BuildTree();
            SetStatus("Loaded settings.");
        }

        private void BuildTree()
        {
            // Remember which nodes were expanded so we can restore expansion state.
            var expandedHeaders = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in EnumerateTreeNodes(_settingsTree.Items))
            {
                if (node.IsExpanded && node.Header is string header)
                {
                    expandedHeaders.Add(header);
                }
            }

            _settingsTree.Items.Clear();

            var globalNode = new TreeViewItem
            {
                Header = "Global Settings",
                Tag = new NodeRef { Kind = NodeKind.Global }
            };
            _settingsTree.Items.Add(globalNode);

            var variantsNode = new TreeViewItem
            {
                Header = "Variants",
                Tag = new NodeRef { Kind = NodeKind.VariantContainer },
                IsExpanded = expandedHeaders.Count == 0 || expandedHeaders.Contains("Variants")
            };
            _settingsTree.Items.Add(variantsNode);

            var materialsNode = new TreeViewItem
            {
                Header = "Weapon Materials",
                Tag = new NodeRef { Kind = NodeKind.WeaponMaterials }
            };
            _settingsTree.Items.Add(materialsNode);

            var specialWeaponsNode = new TreeViewItem
            {
                Header = "Unique Weapons",
                Tag = new NodeRef { Kind = NodeKind.SpecialWeapons }
            };
            _settingsTree.Items.Add(specialWeaponsNode);

            var categoriesNode = new TreeViewItem
            {
                Header = "Weapon Categories",
                Tag = new NodeRef { Kind = NodeKind.CategoryContainer },
                IsExpanded = expandedHeaders.Count == 0 || expandedHeaders.Contains("Weapon Categories")
            };

            foreach (var prop in GetWeaponCategoryProperties())
            {
                if (prop.GetValue(_settings) is not WeaponCategory category)
                {
                    continue;
                }

                categoriesNode.Items.Add(new TreeViewItem
                {
                    Header = GetPropertyLabel(prop),
                    Tag = new NodeRef
                    {
                        Kind = NodeKind.WeaponCategory,
                        CategoryProperty = prop,
                        Category = category
                    }
                });
            }
            _settingsTree.Items.Add(categoriesNode);

            if (_settingsTree.Items.Count > 0 && _settingsTree.Items[0] is TreeViewItem first)
            {
                first.IsSelected = true;
            }
        }

        private IEnumerable<PropertyInfo> GetWeaponCategoryProperties()
        {
            return typeof(Settings)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(WeaponCategory))
                .OrderBy(p => p.MetadataToken);
        }

        private void RenderSelectedNode()
        {
            _fieldValidators.Clear();
            _validationTooltips.Clear();
            _detailPanel.Children.Clear();

            if (_settingsTree.SelectedItem is not TreeViewItem item || item.Tag is not NodeRef node)
            {
                AddText("Select a node from the tree.", FontWeights.Normal, 14, Brushes.DimGray);
                return;
            }

            switch (node.Kind)
            {
                case NodeKind.Global:
                    RenderGlobalSettings();
                    break;
                case NodeKind.VariantContainer:
                    RenderVariantContainer();
                    break;
                case NodeKind.WeaponMaterials:
                    RenderWeaponMaterials();
                    break;
                case NodeKind.SpecialWeapons:
                    RenderSpecialWeapons();
                    break;
                case NodeKind.CategoryContainer:
                    RenderCategoryContainer();
                    break;
                case NodeKind.WeaponCategory:
                    RenderWeaponCategory(node);
                    break;
            }
        }

        // ─── Global Settings ─────────────────────────────────────────────────────

        private void RenderGlobalSettings()
        {
            AddText("Global Settings", FontWeights.SemiBold, 20, Brushes.Black);

           AddText("This patcher is preconfigured with sensible defaults. Most users do not need to change anything.", FontWeights.Normal, 12, Brushes.DimGray);
           AddText("If you use mods that alter material tiers, like WACCF, CC Rebalance or the Restless Dead, you need to adjust the Weaponm Material settings accordingly.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 2);

            var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 2) };
            var restoreDefaultButton = new Button
            {
                Content = "Restore Default Settings",
                Width = 190,
                ToolTip = "Restore selected settings sections from bundled defaults."
            };
            restoreDefaultButton.Click += (_, _) =>
            {
                RestoreDefaultSettingsWithSelection();
            };
            buttonRow.Children.Add(restoreDefaultButton);
            _detailPanel.Children.Add(buttonRow);

            var props = typeof(Settings)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite
                    && p.PropertyType != typeof(WeaponCategory)
                    && p.PropertyType != typeof(VariantCategory)
                    && p.PropertyType != typeof(WeaponAttributeEnablers)
                    && p.Name != nameof(Settings.SettingsVersion)
                    && p.Name != nameof(Settings.WeaponMaterials))
                .OrderBy(p => p.MetadataToken)
                .ToList();

            foreach (var prop in props)
            {
                RenderGlobalProperty(prop);
            }

            // WeaponAttributeEnablers as a grouped section
            AddText("Weapon Attribute Enablers", FontWeights.SemiBold, 15, Brushes.Black, topMargin: 16);
            AddText("Enable or disable individual stat edits globally.", FontWeights.Normal, 12, Brushes.DimGray);
            foreach (var prop in typeof(WeaponAttributeEnablers).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType == typeof(bool)).OrderBy(p => p.MetadataToken))
            {
                var value = (bool?)prop.GetValue(_settings.WeaponAttributeEnablers) ?? false;
                var label = GetPropertyLabel(prop);
                var tooltip = GetPropertyTooltip(prop) ?? $"Configure {label}.";
                var check = new CheckBox { IsChecked = value };
                check.Checked += (_, _) => prop.SetValue(_settings.WeaponAttributeEnablers, true);
                check.Unchecked += (_, _) => prop.SetValue(_settings.WeaponAttributeEnablers, false);
                AddLabeledControlRow(_detailPanel, label, check, tooltip, topMargin: 6, labelWidth: 340);
            }
        }

        private void RenderGlobalProperty(PropertyInfo prop)
        {
            var type = prop.PropertyType;
            var value = prop.GetValue(_settings);
            var label = GetPropertyLabel(prop);
            var tooltip = GetPropertyTooltip(prop) ?? $"Configure {label}.";

            if (type == typeof(bool))
            {
                var check = new CheckBox { IsChecked = (bool?)value ?? false };
                check.Checked += (_, _) => prop.SetValue(_settings, true);
                check.Unchecked += (_, _) => prop.SetValue(_settings, false);
                AddLabeledControlRow(_detailPanel, label, check, tooltip, topMargin: 8, labelWidth: 340);
            }
            else if (type.IsEnum)
            {
                var combo = new ComboBox
                {
                    ItemsSource = GetEnumOptions(type),
                    DisplayMemberPath = nameof(EnumOptionItem.Label),
                    SelectedValuePath = nameof(EnumOptionItem.Value),
                    SelectedValue = value,
                    MinWidth = 220
                };
                combo.SelectionChanged += (_, _) =>
                {
                    if (combo.SelectedValue != null)
                    {
                        prop.SetValue(_settings, combo.SelectedValue);
                    }
                };
                AddLabeledControlRow(_detailPanel, label, combo, tooltip, topMargin: 8, labelWidth: 340);
            }
            else if (type == typeof(float))
            {
                var box = new TextBox { Text = ((float?)value ?? 0f).ToString(CultureInfo.InvariantCulture), MinWidth = 180 };
                RegisterValidation(box, RequiredFloatError(label));
                TrackPendingText(box, box.Text);
                AddLabeledControlRow(_detailPanel, label, box, tooltip, topMargin: 8, labelWidth: 340);
                box.LostFocus += (_, _) =>
                {
                    if (TryParseFloat(box.Text, out var f))
                    {
                        prop.SetValue(_settings, f);
                    }
                };
            }
            else if (type == typeof(int))
            {
                var box = new TextBox { Text = ((int?)value ?? 0).ToString(CultureInfo.InvariantCulture), MinWidth = 180 };
                RegisterValidation(box, RequiredIntError(label));
                TrackPendingText(box, box.Text);
                AddLabeledControlRow(_detailPanel, label, box, tooltip, topMargin: 8, labelWidth: 340);
                box.LostFocus += (_, _) =>
                {
                    if (int.TryParse(box.Text, out var i))
                    {
                        prop.SetValue(_settings, i);
                    }
                };
            }
            else if (type == typeof(string))
            {
                if (ExpandableMultilineGlobalFields.Contains(prop.Name))
                {
                    var expander = CreateExpandableTextEditor(value?.ToString() ?? string.Empty, text => prop.SetValue(_settings, text));
                    AddLabeledControlRow(_detailPanel, label, expander, tooltip, topMargin: 8, labelWidth: 340);
                }
                else
                {
                    var box = new TextBox { Text = value?.ToString() ?? string.Empty, MinWidth = 360 };
                    box.TextChanged += (_, _) => prop.SetValue(_settings, box.Text);
                    AddLabeledControlRow(_detailPanel, label, box, tooltip, topMargin: 8, labelWidth: 340);
                }
            }
        }

        // ─── Weapon Materials ───────────────────────────────────────────────────

        private void RenderWeaponMaterials()
        {
            AddText("Weapon Materials", FontWeights.SemiBold, 20, Brushes.Black);
            AddText("Weapon material is matched by name and keywords, with name taking priority.", FontWeights.Normal, 12, Brushes.DimGray);

            var addButton = new Button
            {
                Content = "Add Material",
                Width = 130,
                Margin = new Thickness(0, 10, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
                ToolTip = "Add a custom material row to the local settings file."
            };
            addButton.Click += (_, _) =>
            {
                _settings.WeaponMaterials.Add(new WeaponMaterialSetting
                {
                    Title = GenerateUniqueKey(_settings.WeaponMaterials.Select(m => m.Title), "New Material"),
                    Enabled = true
                });
                RenderSelectedNode();
                SetStatus("Added material row.");
            };
            var actionPanel = new WrapPanel
            {
                Margin = new Thickness(0, 2, 0, 0),
                Orientation = Orientation.Horizontal
            };

            var restoreButton = new Button
            {
                Content = "Restore Default Materials",
                Width = 210,
                Margin = new Thickness(0, 0, 8, 8),
                ToolTip = "Replace current material settings with InternalData/material_data.json."
            };
            restoreButton.Click += (_, _) => RestoreWeaponMaterialsFromInternal();
            actionPanel.Children.Add(restoreButton);

            var applyCcButton = new Button
            {
                Content = "Apply CC Debuff",
                Width = 190,
                Margin = new Thickness(0, 0, 8, 8),
                ToolTip = "This slightly debuffs the damage of weapons of materials added by the official CC addons."
            };
            applyCcButton.Click += (_, _) => ApplyWeaponMaterialAddon("material_data addon_debuff_cc.json", "Applied CC Debuff addon values.");
            actionPanel.Children.Add(applyCcButton);

            var applyWaccfButton = new Button
            {
                Content = "Apply WACCF changes",
                Width = 170,
                Margin = new Thickness(0, 0, 8, 8),
                ToolTip = "Switches material offsets according to WACCF values. This is required if you use WACCF and want to keep the same material offsets."
            };
            applyWaccfButton.Click += (_, _) => ApplyWeaponMaterialAddon("material_data__addon_waccf.json", "Applied WACCF addon values.");
            actionPanel.Children.Add(applyWaccfButton);

            var applyRestlessButton = new Button
            {
                Content = "Apply The Restless Dead debuffs",
                Width = 210,
                Margin = new Thickness(0, 0, 8, 8),
                ToolTip = "Apply debuffs to Draugr weapons according to the \"The Restless Dead\" mod."
            };
            applyRestlessButton.Click += (_, _) => ApplyWeaponMaterialAddon("material_data_addon_restless_dead.json", "Applied Restless Dead addon values.");
            actionPanel.Children.Add(applyRestlessButton);

            _detailPanel.Children.Add(actionPanel);
            _detailPanel.Children.Add(addButton);

            if (_settings.WeaponMaterials.Count == 0)
            {
                AddText("No weapon materials configured.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 8);
                return;
            }

            var listPanel = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            foreach (var material in _settings.WeaponMaterials
                .OrderBy(m => m.DamageOffset1h)
                .ThenBy(m => m.DamageOffset2h)
                .ThenBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
                .ToList())
            {
                listPanel.Children.Add(CreateWeaponMaterialRow(material));
            }

            _detailPanel.Children.Add(listPanel);
        }

        private FrameworkElement CreateWeaponMaterialRow(WeaponMaterialSetting material)
        {
            var isDefaultMaterial = _defaultWeaponMaterialTitles.Contains(material.Title);

            var row = new Border
            {
                BorderBrush = Brushes.Gainsboro,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var contentPanel = new StackPanel
            {
                Margin = new Thickness(10, 8, 10, 10)
            };

            var editorRow = new Grid();
            editorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) }); // Enabled
            for (var column = 0; column < 4; column++)
            {
                editorRow.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star),
                    MinWidth = 70
                });
            }
            editorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Delete

            var enabledPanel = CreateMaterialFieldPanel("Enabled");

            var enabledCheck = new CheckBox
            {
                IsChecked = material.Enabled,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 3, 0, 0),
                ToolTip = "Disable to keep this material but skip matching and damage offset application."
            };
            enabledCheck.Checked += (_, _) => material.Enabled = true;
            enabledCheck.Unchecked += (_, _) => material.Enabled = false;
            enabledPanel.Children.Add(enabledCheck);
            Grid.SetColumn(enabledPanel, 0);
            editorRow.Children.Add(enabledPanel);

            var titlePanel = CreateMaterialFieldPanel(isDefaultMaterial ? "Title · Locked" : "Title", leftMargin: 8);

            var titleBox = new TextBox
            {
                Text = material.Title,
                IsReadOnly = isDefaultMaterial,
                IsTabStop = !isDefaultMaterial,
                Background = isDefaultMaterial ? SystemColors.ControlBrush : SystemColors.WindowBrush,
                Foreground = SystemColors.ControlTextBrush,
                ToolTip = isDefaultMaterial
                    ? "Default material titles are locked because title is the addon key."
                    : "Material title. Addon files use this value as their key."
            };
            if (!isDefaultMaterial)
            {
                RegisterValidation(titleBox, text =>
                {
                    var title = text.Trim();
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        return "Material title is required.";
                    }

                    return _settings.WeaponMaterials.Any(other => !ReferenceEquals(other, material)
                        && string.Equals(other.Title.Trim(), title, StringComparison.OrdinalIgnoreCase))
                            ? $"A material named '{title}' already exists."
                            : null;
                });
            }
            if (!isDefaultMaterial)
            {
                titleBox.TextChanged += (_, _) => material.Title = titleBox.Text;
            }
            titlePanel.Children.Add(titleBox);
            Grid.SetColumn(titlePanel, 1);
            editorRow.Children.Add(titlePanel);

            var identifiersPanel = CreateMaterialFieldPanel("Identifiers", leftMargin: 8);

            var identifiersBox = new TextBox
            {
                Text = string.Join(";", material.Identifiers ?? new List<string>()),
                ToolTip = "Semicolon-separated identifiers used for both name and keyword material matching."
            };
            identifiersBox.TextChanged += (_, _) =>
            {
                material.Identifiers = identifiersBox.Text
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            };
            identifiersPanel.Children.Add(identifiersBox);
            Grid.SetColumn(identifiersPanel, 2);
            editorRow.Children.Add(identifiersPanel);

            var oneHandedPanel = CreateMaterialFieldPanel("1H Damage Offset", leftMargin: 8);

            var oneHandedBox = new TextBox
            {
                Text = material.DamageOffset1h?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                ToolTip = "Damage offset applied to one-handed weapons."
            };
            RegisterValidation(oneHandedBox, OptionalIntError("1H Damage Offset"));
            oneHandedBox.LostFocus += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(oneHandedBox.Text))
                {
                    material.DamageOffset1h = null;
                }
                else if (int.TryParse(oneHandedBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    || int.TryParse(oneHandedBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
                {
                    material.DamageOffset1h = value;
                }
            };
            oneHandedPanel.Children.Add(oneHandedBox);
            Grid.SetColumn(oneHandedPanel, 3);
            editorRow.Children.Add(oneHandedPanel);

            var twoHandedPanel = CreateMaterialFieldPanel("2H Damage Offset", leftMargin: 8);

            var twoHandedBox = new TextBox
            {
                Text = material.DamageOffset2h?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                ToolTip = "Damage offset applied to two-handed weapons."
            };
            RegisterValidation(twoHandedBox, OptionalIntError("2H Damage Offset"));
            twoHandedBox.LostFocus += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(twoHandedBox.Text))
                {
                    material.DamageOffset2h = null;
                }
                else if (int.TryParse(twoHandedBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    || int.TryParse(twoHandedBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
                {
                    material.DamageOffset2h = value;
                }
            };
            twoHandedPanel.Children.Add(twoHandedBox);
            Grid.SetColumn(twoHandedPanel, 4);
            editorRow.Children.Add(twoHandedPanel);

            var deleteButton = new Button
            {
                Content = "Delete",
                Width = 80,
                Margin = new Thickness(10, 17, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                IsEnabled = !isDefaultMaterial,
                ToolTip = isDefaultMaterial
                    ? "Default materials cannot be removed. Disable them instead."
                    : "Delete this custom material row."
            };
            deleteButton.Click += (_, _) =>
            {
                _settings.WeaponMaterials.Remove(material);
                RenderSelectedNode();
                SetStatus("Deleted custom material row.");
            };
            Grid.SetColumn(deleteButton, 5);
            editorRow.Children.Add(deleteButton);

            contentPanel.Children.Add(editorRow);

            row.Child = contentPanel;
            return row;
        }

        private static StackPanel CreateMaterialFieldPanel(string label, double leftMargin = 0)
        {
            var panel = new StackPanel { Margin = new Thickness(leftMargin, 0, 0, 0) };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = Brushes.DimGray,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 2),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            return panel;
        }

        private void RestoreWeaponMaterialsFromInternal()
        {
            if (!ConfirmRestorePreset("default weapon materials"))
            {
                return;
            }

            var defaults = Settings.LoadBundledWeaponMaterials();
            if (defaults.Count == 0)
            {
                SetStatus("Failed to load default materials from InternalData/material_data.json.", isError: true);
                return;
            }

            _settings.WeaponMaterials = defaults;
            RenderSelectedNode();
            SetStatus("Restored default weapon materials. Click Save to persist.");
        }

        private void ApplyWeaponMaterialAddon(string addonFileName, string successMessage)
        {
            var addonMaterials = LoadBundledWeaponMaterialsFile(addonFileName);
            if (addonMaterials.Count == 0)
            {
                SetStatus($"Failed to load addon file: {addonFileName}", isError: true);
                return;
            }

            var currentByTitle = new Dictionary<string, WeaponMaterialSetting>(StringComparer.OrdinalIgnoreCase);
            foreach (var material in _settings.WeaponMaterials)
            {
                var key = material.Title?.Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                // Keep the last entry in case duplicates already exist.
                currentByTitle[key] = material;
            }

            foreach (var addonMaterial in addonMaterials)
            {
                var addonKey = addonMaterial.Title?.Trim();
                if (string.IsNullOrWhiteSpace(addonKey))
                {
                    continue;
                }

                if (currentByTitle.TryGetValue(addonKey, out var existing))
                {
                    existing.Enabled = addonMaterial.Enabled;
                    existing.Identifiers = addonMaterial.Identifiers?.ToList() ?? new List<string>();
                    existing.DamageOffset1h = addonMaterial.DamageOffset1h;
                    existing.DamageOffset2h = addonMaterial.DamageOffset2h;
                }
                else
                {
                    addonMaterial.Title = addonKey;
                    _settings.WeaponMaterials.Add(addonMaterial);
                    currentByTitle[addonKey] = addonMaterial;
                }
            }

            RenderSelectedNode();
            SetStatus(successMessage + " Click Save to persist.");
        }

        private static List<WeaponMaterialSetting> LoadBundledWeaponMaterialsFile(string fileName)
        {
            try
            {
                var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrWhiteSpace(assemblyDirectory))
                {
                    return new List<WeaponMaterialSetting>();
                }

                var filePath = Path.Combine(assemblyDirectory, "InternalData", fileName);
                if (!File.Exists(filePath))
                {
                    return new List<WeaponMaterialSetting>();
                }

                var json = File.ReadAllText(filePath);
                var materials = JsonConvert.DeserializeObject<List<WeaponMaterialSetting>>(json);
                return materials?.Select(material => new WeaponMaterialSetting
                {
                    Title = material.Title,
                    Enabled = material.Enabled,
                    Identifiers = material.Identifiers?.ToList() ?? new List<string>(),
                    DamageOffset1h = material.DamageOffset1h,
                    DamageOffset2h = material.DamageOffset2h
                }).ToList() ?? new List<WeaponMaterialSetting>();
            }
            catch
            {
                return new List<WeaponMaterialSetting>();
            }
        }

        // ─── Variants ────────────────────────────────────────────────────────────

        private void RenderVariantContainer()
        {
            AddText("Variants", FontWeights.SemiBold, 20, Brushes.Black);
            AddText("Variants are for additional customization of weapons, like rusty variants or cyrodyllic variants. The weapon name is scanned for identifiers and matched against the available variants.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 2);
            AddText("The weapon may be modified using multipliers and/or flat offsets. The recommended method is to use multipliers.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 2);
            AddText("Multipliers are applied before flat offsets for each stat. Example: final damage = (current damage * multiplier) + flat offset.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 2);
            AddText("Since damage is an integer, it is rounded after multiplying, with a minimum change of ±1.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 2);

            var addButton = new Button { Content = "Add Variant", Width = 140, Margin = new Thickness(0, 12, 0, 0) };
            addButton.Click += (_, _) =>
            {
                var key = GenerateUniqueKey(_settings.Variants.Variants.Keys, "New Variant");
                _settings.Variants.Variants[key] = new VariantSettings();
                BuildTree();
                SelectVariantContainerNode();
                SetStatus($"Added variant '{key}'.");
            };
            _detailPanel.Children.Add(addButton);

            if (_settings.Variants.Variants.Count == 0)
            {
                AddText("No variants defined yet.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 12);
                return;
            }

            var tabs = new TabControl
            {
                Margin = new Thickness(0, 12, 0, 0)
            };

            foreach (var variantEntry in _settings.Variants.Variants.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                tabs.Items.Add(new TabItem
                {
                    Header = variantEntry.Key,
                    Content = CreateVariantCardContent(variantEntry.Key, variantEntry.Value)
                });
            }

            _detailPanel.Children.Add(tabs);
        }

        // ─── Special Weapons ────────────────────────────────────────────────────

        private void RenderSpecialWeapons()
        {
            AddText("Unique Weapons", FontWeights.SemiBold, 20, Brushes.Black);

            var specialWeapons = GetSpecialWeapons();
            AddText("This is a list of unique weapons that do not follow the standard weapon tier list.", FontWeights.Normal, 12, Brushes.DimGray);
            AddText("Weapon stats are calculated by comparing them to an iron weapon of the same type the stats in the settings.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 2);
            AddText("Manual offsets can be applied if the calculated result needs adjustment. Manual offsets override the calculated values.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 2);

            var addButton = new Button { Content = "Add Unique Weapon", Width = 170, Margin = new Thickness(0, 10, 0, 0) };
            addButton.Click += (_, _) =>
            {
                _settings.UniqueWeapons.Add(new SpecialWeaponData());
                RenderSelectedNode();
                SetStatus("Added unique weapon entry.");
            };
            _detailPanel.Children.Add(addButton);

            if (specialWeapons.Count == 0)
            {
                AddText("No unique weapons are currently defined.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 10);
                return;
            }

            var listPanel = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0)
            };

            foreach (var weapon in specialWeapons)
            {
                listPanel.Children.Add(CreateSpecialWeaponRow(weapon));
            }

            _detailPanel.Children.Add(listPanel);
        }

        private List<SpecialWeaponListItem> GetSpecialWeapons()
        {
            return _settings.UniqueWeapons
                .Select((weapon, index) => new SpecialWeaponListItem
                {
                    SourceIndex = index,
                    Data = weapon,
                    EditorId = weapon.EditorID,
                    FormKey = weapon.FormKey,
                    ModName = GetFormKeyModName(weapon.FormKey),
                    DamageOffset = weapon.DamageOffset,
                    SpeedOffset = weapon.SpeedOffset,
                    ReachOffset = weapon.ReachOffset,
                    StaggerOffset = weapon.StaggerOffset,
                    CriticalDamageOffset = weapon.CriticalDamageOffset,
                    CriticalDamageChanceMultiplierOffset = weapon.CriticalDamageChanceMultiplierOffset
                })
                .OrderBy(weapon => !string.IsNullOrWhiteSpace(weapon.EditorId)
                    && !string.IsNullOrWhiteSpace(weapon.FormKey))
                .ThenBy(weapon => weapon.ModName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(weapon => weapon.EditorId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void DeleteSpecialWeapon(int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= _settings.UniqueWeapons.Count)
            {
                return;
            }

            _settings.UniqueWeapons.RemoveAt(sourceIndex);
            RenderSelectedNode();
            SetStatus("Deleted unique weapon entry.");
        }

        private static string GetFormKeyModName(string? formKey)
        {
            if (string.IsNullOrWhiteSpace(formKey))
            {
                return string.Empty;
            }

            var separatorIndex = formKey.IndexOf(':');
            if (separatorIndex < 0 || separatorIndex + 1 >= formKey.Length)
            {
                return string.Empty;
            }

            return formKey[(separatorIndex + 1)..].Trim();
        }

        private FrameworkElement CreateSpecialWeaponRow(SpecialWeaponListItem weapon)
        {
            var row = new Border
            {
                BorderBrush = Brushes.Gainsboro,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var contentPanel = new StackPanel
            {
                Margin = new Thickness(10, 6, 10, 10)
            };

            var topRow = new DockPanel { LastChildFill = false };
            var deleteButton = new Button
            {
                Content = "Delete",
                Width = 80,
                Margin = new Thickness(0, 0, 0, 4)
            };
            deleteButton.Click += (_, _) => DeleteSpecialWeapon(weapon.SourceIndex);
            DockPanel.SetDock(deleteButton, Dock.Right);
            topRow.Children.Add(deleteButton);
            contentPanel.Children.Add(topRow);

            var editorIdBox = new TextBox { Text = weapon.EditorId, MinWidth = 360 };
            editorIdBox.TextChanged += (_, _) => weapon.Data.EditorID = editorIdBox.Text;
            AddLabeledControlRow(
                contentPanel,
                "Editor ID",
                editorIdBox,
                "Optional reference label for you. Parsing matches by Form Key, not Editor ID.",
                topMargin: 4,
                labelWidth: 140);

            var formKeyBox = new TextBox { Text = weapon.FormKey, MinWidth = 360 };
            RegisterValidation(formKeyBox, text => FormKeyPattern.IsMatch(text)
                ? null
                : "Form Key must contain six hexadecimal digits, a colon, and an .esp, .esl, or .esm filename (for example, ABC123:Plugin.esp)."
            );
            formKeyBox.TextChanged += (_, _) => weapon.Data.FormKey = formKeyBox.Text;
            AddLabeledControlRow(
                contentPanel,
                "Form Key",
                formKeyBox,
                "The exact form key for this weapon, including source plugin.",
                topMargin: 4,
                labelWidth: 140);

            var expander = new Expander
            {
                IsExpanded = false,
                Content = contentPanel
            };

            bool HasManualOffsets()
            {
                return weapon.Data.DamageOffset.HasValue
                    || weapon.Data.SpeedOffset.HasValue
                    || weapon.Data.ReachOffset.HasValue
                    || weapon.Data.StaggerOffset.HasValue
                    || weapon.Data.CriticalDamageOffset.HasValue
                    || weapon.Data.CriticalDamageChanceMultiplierOffset.HasValue;
            }

            void UpdateHeader()
            {
                var editor = string.IsNullOrWhiteSpace(editorIdBox.Text) ? "<empty editor id>" : editorIdBox.Text;
                var form = string.IsNullOrWhiteSpace(formKeyBox.Text) ? "<empty form key>" : formKeyBox.Text;
                var status = HasManualOffsets() ? "Manual Offsets" : "Calculated";
                expander.Header = $"{editor} | {form} [{status}]";
            }

            AddNullableIntEditor(contentPanel, "Damage Offset", weapon.DamageOffset, value => weapon.Data.DamageOffset = value, "Adds or subtracts flat base damage from this unique weapon.", UpdateHeader);
            AddNullableFloatEditor(contentPanel, "Speed Offset", weapon.SpeedOffset, value => weapon.Data.SpeedOffset = value, "Adds or subtracts attack speed from this unique weapon.", UpdateHeader);
            AddNullableFloatEditor(contentPanel, "Reach Offset", weapon.ReachOffset, value => weapon.Data.ReachOffset = value, "Adds or subtracts weapon reach.", UpdateHeader);
            AddNullableFloatEditor(contentPanel, "Stagger Offset", weapon.StaggerOffset, value => weapon.Data.StaggerOffset = value, "Adds or subtracts stagger value.", UpdateHeader);
            AddNullableFloatEditor(contentPanel, "Critical Damage Offset", weapon.CriticalDamageOffset, value => weapon.Data.CriticalDamageOffset = value, "Adds or subtracts critical damage offset.", UpdateHeader);
            AddNullableFloatEditor(contentPanel, "Critical Chance Multiplier Offset", weapon.CriticalDamageChanceMultiplierOffset, value => weapon.Data.CriticalDamageChanceMultiplierOffset = value, "Adds or subtracts the critical chance multiplier.", UpdateHeader);

            editorIdBox.TextChanged += (_, _) => UpdateHeader();
            formKeyBox.TextChanged += (_, _) => UpdateHeader();
            UpdateHeader();

            row.Child = expander;
            return row;
        }

        private void AddNullableIntEditor(Panel panel, string label, int? currentValue, Action<int?> onChanged, string tooltip, Action? onValueCommitted = null)
        {
            var box = new TextBox { Text = currentValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, MinWidth = 180 };
            RegisterValidation(box, OptionalIntError(label));
            TrackPendingText(box, box.Text);
            var editor = CreateWatermarkedTextBox(box, "Calculated");
            AddLabeledControlRow(panel, label, editor, $"{tooltip} Leave empty to use calculated value.", topMargin: 4, labelWidth: 220);
            box.LostFocus += (_, _) =>
            {
                var trimmed = box.Text.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    onChanged(null);
                    onValueCommitted?.Invoke();
                    return;
                }

                if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                    || int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
                {
                    onChanged(parsed);
                    onValueCommitted?.Invoke();
                }
            };
        }

        private void AddNullableFloatEditor(Panel panel, string label, float? currentValue, Action<float?> onChanged, string tooltip, Action? onValueCommitted = null)
        {
            var box = new TextBox { Text = currentValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, MinWidth = 180 };
            RegisterValidation(box, OptionalFloatError(label));
            TrackPendingText(box, box.Text);
            var editor = CreateWatermarkedTextBox(box, "Calculated");
            AddLabeledControlRow(panel, label, editor, $"{tooltip} Leave empty to use calculated value.", topMargin: 4, labelWidth: 220);
            box.LostFocus += (_, _) =>
            {
                var trimmed = box.Text.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    onChanged(null);
                    onValueCommitted?.Invoke();
                    return;
                }

                if (TryParseFloat(trimmed, out var parsed))
                {
                    onChanged(parsed);
                    onValueCommitted?.Invoke();
                }
            };
        }

        private static FrameworkElement CreateWatermarkedTextBox(TextBox textBox, string watermarkText)
        {
            var container = new Grid();

            var watermark = new TextBlock
            {
                Text = watermarkText,
                Foreground = Brushes.Gray,
                Margin = new Thickness(6, 2, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            void UpdateWatermark()
            {
                watermark.Visibility = string.IsNullOrWhiteSpace(textBox.Text) && !textBox.IsKeyboardFocusWithin
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            textBox.TextChanged += (_, _) => UpdateWatermark();
            textBox.GotKeyboardFocus += (_, _) => UpdateWatermark();
            textBox.LostKeyboardFocus += (_, _) => UpdateWatermark();

            container.Children.Add(textBox);
            container.Children.Add(watermark);
            UpdateWatermark();

            return container;
        }

        private FrameworkElement CreateVariantCardContent(string variantKey, VariantSettings variant)
        {
            var card = new Border
            {
                BorderBrush = Brushes.Gainsboro,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 10, 0, 0)
            };

            var outerGrid = new Grid();
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var panel = new StackPanel();
            Grid.SetColumn(panel, 0);
            outerGrid.Children.Add(panel);

            var deleteButton = new Button
            {
                Content = "Delete",
                Width = 80,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            deleteButton.Click += (_, _) =>
            {
                _settings.Variants.Variants.Remove(variantKey);
                BuildTree();
                SelectVariantContainerNode();
                SetStatus($"Deleted variant '{variantKey}'.");
            };
            Grid.SetColumn(deleteButton, 1);
            outerGrid.Children.Add(deleteButton);

            card.Child = outerGrid;

            var keyBox = new TextBox { Text = variantKey, MinWidth = 360 };
            RegisterValidation(keyBox, text =>
            {
                var name = text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    return "Variant name is required.";
                }

                return !string.Equals(name, variantKey, StringComparison.OrdinalIgnoreCase)
                    && _settings.Variants.Variants.Keys.Any(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        ? $"A variant named '{name}' already exists."
                        : null;
            });
            TrackPendingText(keyBox, variantKey);
            AddLabeledControlRow(panel, "Name", keyBox, "Display name for this variant. Used only for organization.", topMargin: 0);
            keyBox.LostFocus += (_, _) => RenameVariant(variantKey, keyBox.Text.Trim());

            AddSectionHeader(panel, "Matching", topMargin: 14);

            var nameIDsBox = new TextBox { Text = variant.NameIDs, MinWidth = 360 };
            AddLabeledControlRow(panel, "Name IDs", nameIDsBox, "Semicolon-separated words; weapon name or keywords must contain one of these for the variant to apply.");
            nameIDsBox.TextChanged += (_, _) => variant.NameIDs = nameIDsBox.Text;

            var excludeIDsBox = new TextBox { Text = variant.ExcludeIDs, MinWidth = 360 };
            AddLabeledControlRow(panel, "Exclude IDs", excludeIDsBox, "Semicolon-separated words; if the weapon name or keywords match any of these, the variant is skipped.");
            excludeIDsBox.TextChanged += (_, _) => variant.ExcludeIDs = excludeIDsBox.Text;

            var skillCombo = new ComboBox
            {
                ItemsSource = GetEnumOptions<WeaponSkill>(),
                DisplayMemberPath = nameof(EnumOption<WeaponSkill>.Label),
                SelectedValuePath = nameof(EnumOption<WeaponSkill>.Value),
                SelectedValue = variant.Skill,
                MinWidth = 180
            };
            AddLabeledControlRow(panel, "Skill", skillCombo, "Only apply this variant to weapons using this skill.");
            skillCombo.SelectionChanged += (_, _) =>
            {
                if (skillCombo.SelectedValue is WeaponSkill skill)
                {
                    variant.Skill = skill;
                }
            };

            AddSectionHeader(panel, "Stat Offsets", topMargin: 14);
            AddLabeledInfoRow(panel, "Each final stat uses this order: (base stat + material offsets) * variant multiplier + variant flat offset.", topMargin: 2);
            AddLabeledInfoRow(panel, "Damage exception: multiplier result is rounded, and if a decrease/increase would round to no change, it still applies at least -1/+1 before flat damage offset.", topMargin: 2);

            AddVariantStatRowWithIntFlat(panel, "Damage", "Damage multiplier and flat offset. Multiplier is applied first.", variant.DamageMultiplier, variant.AdditionalDamage, v => variant.DamageMultiplier = v, v => variant.AdditionalDamage = v);
            AddVariantStatRowWithFloatFlat(panel, "Reach", "Reach multiplier and flat offset. Multiplier is applied first.", variant.ReachMultiplier, variant.AdditionalReach, v => variant.ReachMultiplier = v, v => variant.AdditionalReach = v);
            AddVariantStatRowWithFloatFlat(panel, "Speed", "Speed multiplier and flat offset. Multiplier is applied first.", variant.SpeedMultiplier, variant.AdditionalSpeed, v => variant.SpeedMultiplier = v, v => variant.AdditionalSpeed = v);
            AddVariantStatRowWithFloatFlat(panel, "Stagger", "Stagger multiplier and flat offset. Multiplier is applied first.", variant.StaggerMultiplier, variant.AdditionalStagger, v => variant.StaggerMultiplier = v, v => variant.AdditionalStagger = v);
            AddVariantStatRowWithFloatFlat(panel, "Crit Damage Offset", "Critical damage offset multiplier and flat offset. Multiplier is applied first.", variant.CriticalDamageOffsetMultiplier, variant.AdditionalCriticalDamageOffset, v => variant.CriticalDamageOffsetMultiplier = v, v => variant.AdditionalCriticalDamageOffset = v);
            AddVariantStatRowWithFloatFlat(panel, "Crit Chance Multiplier", "Critical chance multiplier multiplier and flat offset. Multiplier is applied first.", variant.CriticalDamageChanceMultiplierMultiplier, variant.AdditionalCriticalDamageChanceMultiplier, v => variant.CriticalDamageChanceMultiplierMultiplier = v, v => variant.AdditionalCriticalDamageChanceMultiplier = v);
            AddVariantStatRowWithFloatFlat(panel, "Crit Damage Multiplier", "Critical damage multiplier multiplier and flat offset. Multiplier is applied first.", variant.CriticalDamageMultiplierMultiplier, variant.AdditionalCriticalDamageMultiplier, v => variant.CriticalDamageMultiplierMultiplier = v, v => variant.AdditionalCriticalDamageMultiplier = v);

            return card;
        }

        private void RenameVariant(string oldKey, string newKey)
        {
            if (!_settings.Variants.Variants.TryGetValue(oldKey, out var variant))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(newKey) || string.Equals(newKey, oldKey, StringComparison.Ordinal))
            {
                return;
            }

            if (_settings.Variants.Variants.ContainsKey(newKey))
            {
                SetStatus($"Variant name '{newKey}' already exists.", isError: true);
                return;
            }

            _settings.Variants.Variants.Remove(oldKey);
            _settings.Variants.Variants[newKey] = variant;
            BuildTree();
            SelectVariantContainerNode();
            SetStatus($"Renamed variant to '{newKey}'.");
        }

        // ─── Weapon Categories ───────────────────────────────────────────────────

        private void RenderCategoryContainer()
        {
            AddText("Weapon Categories", FontWeights.SemiBold, 20, Brushes.Black);
            AddText("Select a category from the tree to edit its weapons.", FontWeights.Normal, 13, Brushes.DimGray);
        }

        private void RenderWeaponCategory(NodeRef node)
        {
            if (node.Category == null || node.CategoryProperty == null)
            {
                return;
            }

            //AddText(GetPropertyLabel(node.CategoryProperty), FontWeights.SemiBold, 20, Brushes.Black);
            //AddText("Each weapon type opens in its own tab.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 2);

            var addButton = new Button { Content = "Add Weapon Type", Width = 160, Margin = new Thickness(0, 10, 0, 0) };
            addButton.Click += (_, _) =>
            {
                var key = GenerateUniqueKey(node.Category.Weapons.Keys, "New Weapon");
                node.Category.Weapons[key] = new WeaponSettings();
                BuildTree();
                SelectCategoryNode(node.CategoryProperty);
                SetStatus($"Added weapon type '{key}'.");
            };
            _detailPanel.Children.Add(addButton);

            if (node.Category.Weapons.Count == 0)
            {
                AddText("No weapon types defined for this category yet.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 12);
                return;
            }

            var tabs = new TabControl
            {
                Margin = new Thickness(0, 12, 0, 0)
            };

            foreach (var weaponEntry in node.Category.Weapons.ToList())
            {
                tabs.Items.Add(new TabItem
                {
                    Header = weaponEntry.Key,
                    Content = CreateWeaponCardContent(node, weaponEntry.Key, weaponEntry.Value)
                });
            }

            _detailPanel.Children.Add(tabs);
        }

        private FrameworkElement CreateWeaponCardContent(NodeRef categoryNode, string weaponKey, WeaponSettings weapon)
        {
            var card = new Border
            {
                BorderBrush = Brushes.Gainsboro,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 10, 0, 0)
            };

            var outerGrid = new Grid();
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var panel = new StackPanel();
            Grid.SetColumn(panel, 0);
            outerGrid.Children.Add(panel);

            var deleteButton = new Button
            {
                Content = "Delete",
                Width = 80,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            deleteButton.Click += (_, _) =>
            {
                categoryNode.Category!.Weapons.Remove(weaponKey);
                BuildTree();
                SelectCategoryNode(categoryNode.CategoryProperty!);
                SetStatus($"Deleted weapon type '{weaponKey}'.");
            };
            Grid.SetColumn(deleteButton, 1);
            outerGrid.Children.Add(deleteButton);

            card.Child = outerGrid;

            // Name (rename on LostFocus)
            var nameBox = new TextBox { Text = weaponKey, MinWidth = 360 };
            RegisterValidation(nameBox, text =>
            {
                var name = text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    return "Weapon type name is required.";
                }

                return !string.Equals(name, weaponKey, StringComparison.OrdinalIgnoreCase)
                    && categoryNode.Category!.Weapons.Keys.Any(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        ? $"A weapon type named '{name}' already exists."
                        : null;
            });
            TrackPendingText(nameBox, weaponKey);
            AddLabeledControlRow(panel, "Name", nameBox, "Display name used for matching in settings only.", topMargin: 0);
            nameBox.LostFocus += (_, _) => RenameWeapon(categoryNode, weaponKey, nameBox.Text.Trim());

            // Enabled
            var enabledCheck = new CheckBox { IsChecked = weapon.Enabled };
            AddLabeledControlRow(panel, "Enabled", enabledCheck, "Enable or disable this weapon type.");
            enabledCheck.Checked += (_, _) => weapon.Enabled = true;
            enabledCheck.Unchecked += (_, _) => weapon.Enabled = false;

            // Vanilla marker (used by resolver priority buckets)
            string vanillaTypeMessage = VanillaWeaponTypes.IsVanillaWeaponType(weaponKey)
                ? "Yes (built-in vanilla type has lower priority in the matching algorithm)"
                : "No";
            var vanillaTypeText = new TextBlock
            {
                Text = vanillaTypeMessage,
                VerticalAlignment = VerticalAlignment.Center
            };
            AddLabeledControlRow(panel, "Vanilla Type", vanillaTypeText, "Vanilla weapon types have lower priority than non-vanilla types during matching.");

            // Stats section
            AddSectionHeader(panel, "Stats", topMargin: 10);

            AddUshortFieldToPanel(panel, "Damage", weapon.Damage, "Base damage.", v => weapon.Damage = v);
            AddIntFieldToPanel(panel, "Bound Weapon Additional Damage", weapon.BoundWeaponAdditionalDamage, "Added to base damage for bound weapons.", v => weapon.BoundWeaponAdditionalDamage = v);
            AddIntFieldToPanel(panel, "Bound Mystic Weapon Additional Damage", weapon.BoundMysticWeaponAdditionalDamage, "Added to base damage for bound mystic weapons.", v => weapon.BoundMysticWeaponAdditionalDamage = v);
            AddFloatFieldToPanel(panel, "Reach", weapon.Reach, "Weapon reach.", v => weapon.Reach = v);
            AddFloatFieldToPanel(panel, "Speed", weapon.Speed, "Attack speed.", v => weapon.Speed = v);
            AddFloatFieldToPanel(panel, "Stagger", weapon.Stagger, "Stagger multiplier.", v => weapon.Stagger = v);
            AddFloatFieldToPanel(panel, "Crit Damage Offset", weapon.CriticalDamageOffset, "Flat offset added to critical damage calculation.", v => weapon.CriticalDamageOffset = v);
            AddFloatFieldToPanel(panel, "Crit Chance Multiplier", weapon.CriticalDamageChanceMultiplier, "Critical damage chance multiplier.", v => weapon.CriticalDamageChanceMultiplier = v);
            AddFloatFieldToPanel(panel, "Crit Damage Multiplier", weapon.CriticalDamageMultiplier, "Critical damage multiplier.", v => weapon.CriticalDamageMultiplier = v);

            // Match Logic section
            AddSectionHeader(panel, "Match Logic", topMargin: 10);

            var matchSkillCombo = new ComboBox
            {
                ItemsSource = GetEnumOptions<WeaponSkill>(),
                DisplayMemberPath = nameof(EnumOption<WeaponSkill>.Label),
                SelectedValuePath = nameof(EnumOption<WeaponSkill>.Value),
                SelectedValue = weapon.MatchLogicSettings.Skill,
                MinWidth = 180
            };
            AddLabeledControlRow(panel, "Skill", matchSkillCombo, "Weapon skill required to match this type.");
            matchSkillCombo.SelectionChanged += (_, _) =>
            {
                if (matchSkillCombo.SelectedValue is WeaponSkill skill)
                {
                    weapon.MatchLogicSettings.Skill = skill;
                }
            };

            var nameIDsBox = new TextBox { Text = weapon.MatchLogicSettings.NamedIDs, MinWidth = 360 };
            AddLabeledControlRow(panel, "Name IDs", nameIDsBox, "Semicolon-separated identifiers. The weapon's name must contain one of these for the type to match. Can be empty");

            var logicCombo = new ComboBox
            {
                ItemsSource = GetEnumOptions<LogicOperator>(),
                DisplayMemberPath = nameof(EnumOption<LogicOperator>.Label),
                SelectedValuePath = nameof(EnumOption<LogicOperator>.Value),
                SelectedValue = weapon.MatchLogicSettings.SearchLogic,
                MinWidth = 120
            };
            AddLabeledControlRow(panel, "AND / OR", logicCombo, "AND: both Name IDs and Keyword IDs must match. OR: either is sufficient. If Name IDs or Keyword IDs is empty, this operator is ignored.");
            logicCombo.SelectionChanged += (_, _) =>
            {
                if (logicCombo.SelectedValue is LogicOperator op)
                {
                    weapon.MatchLogicSettings.SearchLogic = op;
                }
            };

            var keywordIDsBox = new TextBox { Text = weapon.MatchLogicSettings.KeywordIDs, MinWidth = 360 };
            AddLabeledControlRow(panel, "Keyword IDs", keywordIDsBox, "Semicolon-separated identifiers to match against the weapon's keywords. The weapon must have at least one of these keywords for the type to match. Can be empty");

            void RefreshMatchLogicControlState()
            {
                bool hasNames = HasDelimitedEntries(nameIDsBox.Text);
                bool hasKeywords = HasDelimitedEntries(keywordIDsBox.Text);
                bool allowOrSelection = hasNames && hasKeywords;

                if (!allowOrSelection)
                {
                    weapon.MatchLogicSettings.SearchLogic = LogicOperator.AND;
                    logicCombo.SelectedValue = LogicOperator.AND;
                    logicCombo.IsEnabled = false;
                }
                else
                {
                    logicCombo.IsEnabled = true;
                }
            }

            nameIDsBox.TextChanged += (_, _) =>
            {
                weapon.MatchLogicSettings.NamedIDs = nameIDsBox.Text;
                RefreshMatchLogicControlState();
            };
            keywordIDsBox.TextChanged += (_, _) =>
            {
                weapon.MatchLogicSettings.KeywordIDs = keywordIDsBox.Text;
                RefreshMatchLogicControlState();
            };

            RefreshMatchLogicControlState();

            return card;
        }

        private void RenameWeapon(NodeRef categoryNode, string oldKey, string newKey)
        {
            if (categoryNode.Category == null || categoryNode.CategoryProperty == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(newKey) || string.Equals(newKey, oldKey, StringComparison.Ordinal))
            {
                return;
            }

            if (!categoryNode.Category.Weapons.TryGetValue(oldKey, out var weapon))
            {
                return;
            }

            if (categoryNode.Category.Weapons.ContainsKey(newKey))
            {
                SetStatus($"Weapon type name '{newKey}' already exists.", isError: true);
                return;
            }

            categoryNode.Category.Weapons.Remove(oldKey);
            categoryNode.Category.Weapons[newKey] = weapon;
            BuildTree();
            SelectCategoryNode(categoryNode.CategoryProperty);
            SetStatus($"Renamed weapon type to '{newKey}'.");
        }

        // ─── Helpers: field builders ─────────────────────────────────────────────

        private static bool HasDelimitedEntries(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Any(entry => !string.IsNullOrWhiteSpace(entry));
        }

        private void AddUshortFieldToPanel(Panel panel, string label, ushort current, string tooltip, Action<ushort> onChanged)
        {
            var box = new TextBox { Text = current.ToString(CultureInfo.InvariantCulture), MinWidth = 180 };
            RegisterValidation(box, text => ushort.TryParse(text, out _) ? null : $"{label} must be a whole number from 0 to {ushort.MaxValue}.");
            TrackPendingText(box, box.Text);
            AddLabeledControlRow(panel, label, box, tooltip);
            box.LostFocus += (_, _) =>
            {
                if (ushort.TryParse(box.Text, out var v))
                {
                    onChanged(v);
                }
            };
        }

        private void AddIntFieldToPanel(Panel panel, string label, int current, string tooltip, Action<int> onChanged)
        {
            var box = new TextBox { Text = current.ToString(CultureInfo.InvariantCulture), MinWidth = 180 };
            RegisterValidation(box, RequiredIntError(label));
            TrackPendingText(box, box.Text);
            AddLabeledControlRow(panel, label, box, tooltip);
            box.LostFocus += (_, _) =>
            {
                if (int.TryParse(box.Text, out var v))
                {
                    onChanged(v);
                }
            };
        }

        private void AddFloatFieldToPanel(Panel panel, string label, float current, string tooltip, Action<float> onChanged)
        {
            var box = new TextBox { Text = current.ToString(CultureInfo.InvariantCulture), MinWidth = 180 };
            RegisterValidation(box, RequiredFloatError(label));
            TrackPendingText(box, box.Text);
            AddLabeledControlRow(panel, label, box, tooltip);
            box.LostFocus += (_, _) =>
            {
                if (TryParseFloat(box.Text, out var v))
                {
                    onChanged(v);
                }
            };
        }

        private void AddVariantStatRowWithIntFlat(Panel panel, string label, string tooltip, decimal multiplier, int flatOffset, Action<decimal> onMultiplierChanged, Action<int> onFlatOffsetChanged)
        {
            var editorGrid = new Grid();
            editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var multiplierPanel = new StackPanel();
            multiplierPanel.Children.Add(new TextBlock { Text = "Multiplier", Foreground = Brushes.DimGray, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });
            var multiplierBox = new TextBox { Text = multiplier.ToString(CultureInfo.InvariantCulture), MinWidth = 120 };
            RegisterValidation(multiplierBox, RequiredDecimalError($"{label} multiplier"));
            TrackPendingText(multiplierBox, multiplierBox.Text);
            multiplierBox.LostFocus += (_, _) =>
            {
                if (TryParseDecimal(multiplierBox.Text, out var value))
                {
                    onMultiplierChanged(value);
                }
            };
            multiplierPanel.Children.Add(multiplierBox);
            Grid.SetColumn(multiplierPanel, 0);

            var offsetPanel = new StackPanel();
            offsetPanel.Children.Add(new TextBlock { Text = "Flat Offset", Foreground = Brushes.DimGray, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });
            var offsetBox = new TextBox { Text = flatOffset.ToString(CultureInfo.InvariantCulture), MinWidth = 120 };
            RegisterValidation(offsetBox, RequiredIntError($"{label} flat offset"));
            TrackPendingText(offsetBox, offsetBox.Text);
            offsetBox.LostFocus += (_, _) =>
            {
                if (int.TryParse(offsetBox.Text, out var value))
                {
                    onFlatOffsetChanged(value);
                }
            };
            offsetPanel.Children.Add(offsetBox);
            Grid.SetColumn(offsetPanel, 2);

            editorGrid.Children.Add(multiplierPanel);
            editorGrid.Children.Add(offsetPanel);

            AddLabeledControlRow(panel, label, editorGrid, tooltip);
        }

        private void AddVariantStatRowWithFloatFlat(Panel panel, string label, string tooltip, decimal multiplier, float flatOffset, Action<decimal> onMultiplierChanged, Action<float> onFlatOffsetChanged)
        {
            var editorGrid = new Grid();
            editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var multiplierPanel = new StackPanel();
            multiplierPanel.Children.Add(new TextBlock { Text = "Multiplier", Foreground = Brushes.DimGray, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });
            var multiplierBox = new TextBox { Text = multiplier.ToString(CultureInfo.InvariantCulture), MinWidth = 120 };
            RegisterValidation(multiplierBox, RequiredDecimalError($"{label} multiplier"));
            TrackPendingText(multiplierBox, multiplierBox.Text);
            multiplierBox.LostFocus += (_, _) =>
            {
                if (TryParseDecimal(multiplierBox.Text, out var value))
                {
                    onMultiplierChanged(value);
                }
            };
            multiplierPanel.Children.Add(multiplierBox);
            Grid.SetColumn(multiplierPanel, 0);

            var offsetPanel = new StackPanel();
            offsetPanel.Children.Add(new TextBlock { Text = "Flat Offset", Foreground = Brushes.DimGray, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });
            var offsetBox = new TextBox { Text = flatOffset.ToString(CultureInfo.InvariantCulture), MinWidth = 120 };
            RegisterValidation(offsetBox, RequiredFloatError($"{label} flat offset"));
            TrackPendingText(offsetBox, offsetBox.Text);
            offsetBox.LostFocus += (_, _) =>
            {
                if (TryParseFloat(offsetBox.Text, out var value))
                {
                    onFlatOffsetChanged(value);
                }
            };
            offsetPanel.Children.Add(offsetBox);
            Grid.SetColumn(offsetPanel, 2);

            editorGrid.Children.Add(multiplierPanel);
            editorGrid.Children.Add(offsetPanel);

            AddLabeledControlRow(panel, label, editorGrid, tooltip);
        }

        private static void AddSectionHeader(Panel panel, string text, double topMargin = 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                FontSize = 16,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, topMargin, 0, 2)
            });
        }

        private static void AddLabeledInfoRow(Panel panel, string text, double topMargin = 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Normal,
                FontSize = 12,
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, topMargin, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }

        // ─── Tree selection helpers ───────────────────────────────────────────────

        private void SelectVariantContainerNode()
        {
            foreach (var node in EnumerateTreeNodes(_settingsTree.Items))
            {
                if (node.Tag is NodeRef tag
                    && tag.Kind == NodeKind.VariantContainer)
                {
                    node.IsSelected = true;
                    node.BringIntoView();
                    return;
                }
            }
        }

        private void SelectCategoryNode(PropertyInfo categoryProperty)
        {
            foreach (var node in EnumerateTreeNodes(_settingsTree.Items))
            {
                if (node.Tag is NodeRef tag
                    && tag.Kind == NodeKind.WeaponCategory
                    && tag.CategoryProperty?.Name == categoryProperty.Name)
                {
                    node.IsSelected = true;
                    node.BringIntoView();
                    return;
                }
            }
        }

        private static IEnumerable<TreeViewItem> EnumerateTreeNodes(ItemCollection items)
        {
            foreach (var item in items)
            {
                if (item is not TreeViewItem treeItem)
                {
                    continue;
                }

                yield return treeItem;

                foreach (var child in EnumerateTreeNodes(treeItem.Items))
                {
                    yield return child;
                }
            }
        }

        // ─── Enum option helpers ─────────────────────────────────────────────────

        private static List<EnumOption<TEnum>> GetEnumOptions<TEnum>() where TEnum : struct, Enum
        {
            return Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Select(value => new EnumOption<TEnum> { Value = value, Label = GetEnumLabel(value) })
                .ToList();
        }

        private static List<EnumOptionItem> GetEnumOptions(Type enumType)
        {
            return Enum.GetValues(enumType)
                .Cast<object>()
                .Select(value => new EnumOptionItem
                {
                    Value = value,
                    Label = GetEnumLabel(enumType, value.ToString() ?? string.Empty)
                })
                .ToList();
        }

        private static string GetEnumLabel<TEnum>(TEnum value) where TEnum : struct, Enum =>
            GetEnumLabel(typeof(TEnum), value.ToString() ?? string.Empty);

        private static string GetEnumLabel(Type enumType, string enumMemberName)
        {
            var member = enumType.GetMember(enumMemberName).FirstOrDefault();
            var settingName = GetSettingName(member);
            return string.IsNullOrWhiteSpace(settingName) ? SplitPascalCase(enumMemberName) : settingName;
        }

        // ─── Reflection helpers ───────────────────────────────────────────────────

        private static string GetPropertyLabel(PropertyInfo property)
        {
            var settingName = GetSettingName(property);
            return string.IsNullOrWhiteSpace(settingName) ? SplitPascalCase(property.Name) : settingName;
        }

        private static string? GetPropertyTooltip(PropertyInfo property)
        {
            var owner = property.DeclaringType;
            return owner != null && PropertyTooltips.TryGetValue((owner, property.Name), out var tooltip)
                ? tooltip
                : null;
        }

        private static string? GetSettingName(MemberInfo? member)
        {
            var attr = member?
                .GetCustomAttributes(inherit: false)
                .FirstOrDefault(a =>
                    string.Equals(a.GetType().Name, "SettingName", StringComparison.Ordinal) ||
                    string.Equals(a.GetType().Name, "SettingNameAttribute", StringComparison.Ordinal));

            if (attr == null)
            {
                return null;
            }

            return attr.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(attr) as string;
        }

        private static string SplitPascalCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var chars = new List<char>(value.Length + 5);
            chars.Add(value[0]);
            for (var i = 1; i < value.Length; i++)
            {
                if (char.IsUpper(value[i]) && !char.IsUpper(value[i - 1]))
                {
                    chars.Add(' ');
                }
                chars.Add(value[i]);
            }

            return new string(chars.ToArray());
        }

        // ─── Layout helpers ───────────────────────────────────────────────────────

        private static void AddLabeledControlRow(Panel panel, string label, FrameworkElement control, string? tooltip = null, double topMargin = 8, double labelWidth = 200)
        {
            var row = new Grid { Margin = new Thickness(0, topMargin, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelWidth) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 10, 0)
            };

            if (!string.IsNullOrWhiteSpace(tooltip))
            {
                labelBlock.ToolTip = tooltip;
                control.ToolTip = tooltip;
            }

            control.VerticalAlignment = VerticalAlignment.Top;
            control.HorizontalAlignment = HorizontalAlignment.Stretch;

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(control, 1);
            row.Children.Add(labelBlock);
            row.Children.Add(control);

            panel.Children.Add(row);
        }

        private void AddText(string text, FontWeight weight, double size, Brush color, double topMargin = 0)
        {
            _detailPanel.Children.Add(new TextBlock
            {
                Text = text,
                FontWeight = weight,
                FontSize = size,
                Foreground = color,
                Margin = new Thickness(0, topMargin, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }

        private static Expander CreateExpandableTextEditor(string value, Action<string> onChanged)
        {
            var textBox = new TextBox
            {
                Text = value,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MinWidth = 360,
                MinHeight = 72
            };
            textBox.TextChanged += (_, _) => onChanged(textBox.Text);

            var expander = new Expander { Header = "Expand", IsExpanded = false, Content = textBox };
            expander.Expanded += (_, _) => expander.Header = "Collapse";
            expander.Collapsed += (_, _) => expander.Header = "Expand";
            return expander;
        }

        private static string GenerateUniqueKey(IEnumerable<string> existingKeys, string baseKey)
        {
            var existing = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);
            if (!existing.Contains(baseKey))
            {
                return baseKey;
            }

            var index = 2;
            while (existing.Contains($"{baseKey} {index}"))
            {
                index++;
            }

            return $"{baseKey} {index}";
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static bool TryParseDecimal(string text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private void RegisterValidation(TextBox textBox, Func<string, string?> validator)
        {
            _fieldValidators[textBox] = validator;
            textBox.TextChanged += (_, _) => ValidateField(textBox, force: false);
            textBox.LostKeyboardFocus += (_, _) => ValidateField(textBox, force: true);
        }

        private bool ValidateField(TextBox textBox, bool force)
        {
            if (!_fieldValidators.TryGetValue(textBox, out var validator))
            {
                return true;
            }

            var error = validator(textBox.Text);
            if (!force && IsIntermediateNumericInput(textBox.Text))
            {
                error = null;
            }

            if (string.IsNullOrWhiteSpace(error))
            {
                textBox.ClearValue(Control.BorderBrushProperty);
                textBox.ClearValue(Control.BorderThicknessProperty);
                if (_validationTooltips.Remove(textBox, out var originalTooltip))
                {
                    textBox.ToolTip = originalTooltip;
                }
                return true;
            }

            if (!_validationTooltips.ContainsKey(textBox))
            {
                _validationTooltips[textBox] = textBox.ToolTip;
            }
            textBox.BorderBrush = ValidationErrorBrush;
            textBox.BorderThickness = new Thickness(2);
            textBox.ToolTip = error;
            return false;
        }

        private bool ValidateVisibleFields()
        {
            TextBox? firstInvalid = null;
            foreach (var textBox in _fieldValidators.Keys.Where(box => box.IsVisible).ToList())
            {
                if (!ValidateField(textBox, force: true) && firstInvalid == null)
                {
                    firstInvalid = textBox;
                }
            }

            if (firstInvalid == null)
            {
                return true;
            }

            firstInvalid.BringIntoView();
            firstInvalid.Focus();
            return false;
        }

        private static bool IsIntermediateNumericInput(string text)
        {
            var trimmed = text.Trim();
            return string.IsNullOrEmpty(trimmed)
                || trimmed is "+" or "-"
                || trimmed.EndsWith(".", StringComparison.Ordinal)
                || trimmed.EndsWith(",", StringComparison.Ordinal);
        }

        private static Func<string, string?> RequiredIntError(string label) =>
            text => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                || int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out _)
                    ? null
                    : $"{label} must be an integer.";

        private static Func<string, string?> OptionalIntError(string label) =>
            text => string.IsNullOrWhiteSpace(text) ? null : RequiredIntError(label)(text);

        private static Func<string, string?> RequiredFloatError(string label) =>
            text => TryParseFloat(text, out _) ? null : $"{label} must be a number.";

        private static Func<string, string?> OptionalFloatError(string label) =>
            text => string.IsNullOrWhiteSpace(text) ? null : RequiredFloatError(label)(text);

        private static Func<string, string?> RequiredDecimalError(string label) =>
            text => TryParseDecimal(text, out _) ? null : $"{label} must be a decimal number.";

        // ─── Pending-text change detection ────────────────────────────────────────

        private static void TrackPendingText(TextBox textBox, string originalText)
        {
            textBox.Tag = new PendingTextSnapshot { OriginalText = originalText };
        }

        private bool HasPendingTextBoxChanges()
        {
            foreach (var textBox in GetVisualDescendants<TextBox>(_detailPanel))
            {
                if (textBox.Tag is PendingTextSnapshot snapshot && !string.Equals(textBox.Text, snapshot.OriginalText, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<T> GetVisualDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var descendant in GetVisualDescendants<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        // ─── Restore presets ─────────────────────────────────────────────────────

        private bool ConfirmRestorePreset(string presetLabel)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to restore {presetLabel}?\n\nThis will replace your current settings.\n\nClick Save afterward to persist the restored settings to your settings file.",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            return result == MessageBoxResult.Yes;
        }

        private void RestoreDefaultSettingsWithSelection()
        {
            var selection = PromptRestoreSelection();
            if (selection == null)
            {
                return;
            }

            var bundledDefaults = new Settings();
            var restoredSections = new List<string>();

            if (selection.RestoreGlobalSettings)
            {
                RestoreGlobalSettingsFromDefaults(bundledDefaults);
                restoredSections.Add("Global Settings");
            }

            if (selection.RestoreVariants)
            {
                _settings.Variants = CloneByJson(bundledDefaults.Variants);
                restoredSections.Add("Variants");
            }

            if (selection.RestoreWeaponMaterials)
            {
                _settings.WeaponMaterials = bundledDefaults.WeaponMaterials
                    .Select(CloneMaterial)
                    .ToList();
                restoredSections.Add("Weapon Materials");
            }

            if (selection.RestoreUniqueWeapons)
            {
                _settings.UniqueWeapons = bundledDefaults.UniqueWeapons
                    .Select(CloneSpecialWeapon)
                    .ToList();
                restoredSections.Add("Unique Weapons");
            }

            var categoryProperties = GetWeaponCategoryProperties().ToList();
            foreach (var categoryProperty in categoryProperties)
            {
                if (!selection.RestoreAllWeaponCategories)
                {
                    continue;
                }

                if (categoryProperty.GetValue(bundledDefaults) is WeaponCategory defaultCategory)
                {
                    categoryProperty.SetValue(_settings, CloneByJson(defaultCategory));
                    restoredSections.Add(GetPropertyLabel(categoryProperty));
                }
            }

            BuildTree();
            RenderSelectedNode();

            if (restoredSections.Count == 0)
            {
                SetStatus("No sections were selected for restore.");
                return;
            }

            SetStatus($"Restored: {string.Join(", ", restoredSections.Distinct(StringComparer.OrdinalIgnoreCase))}. Click Save to persist.");
        }

        private void RestoreGlobalSettingsFromDefaults(Settings bundledDefaults)
        {
            var properties = typeof(Settings)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.CanWrite)
                .Where(property =>
                    property.PropertyType != typeof(WeaponCategory)
                    && property.PropertyType != typeof(VariantCategory)
                    && property.PropertyType != typeof(List<SpecialWeaponData>)
                    && property.PropertyType != typeof(List<WeaponMaterialSetting>)
                    && property.Name != nameof(Settings.SettingsVersion));

            foreach (var property in properties)
            {
                var defaultValue = property.GetValue(bundledDefaults);
                if (property.PropertyType == typeof(WeaponAttributeEnablers) && defaultValue is WeaponAttributeEnablers enablers)
                {
                    property.SetValue(_settings, CloneByJson(enablers));
                }
                else
                {
                    property.SetValue(_settings, defaultValue);
                }
            }
        }

        private RestoreSelection? PromptRestoreSelection()
        {
            var dialog = new Window
            {
                Title = "Restore Default Settings",
                Width = 520,
                Height = 680,
                MinWidth = 460,
                MinHeight = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.CanResize,
                ShowInTaskbar = false
            };

            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var infoText = new TextBlock
            {
                Text = "Select what to restore from bundled defaults. This can reset all settings, including Weapon Materials, Unique Weapons, Variants, and Weapon Categories.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(infoText, 0);
            root.Children.Add(infoText);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            var content = new StackPanel();
            scroll.Content = content;

            var globalCheck = new CheckBox { Content = "Global Settings", IsChecked = true, Margin = new Thickness(0, 0, 0, 6) };
            var variantsCheck = new CheckBox { Content = "Variants", IsChecked = true, Margin = new Thickness(0, 0, 0, 6) };
            var materialsCheck = new CheckBox { Content = "Weapon Materials", IsChecked = true, Margin = new Thickness(0, 0, 0, 6) };
            var uniqueCheck = new CheckBox { Content = "Unique Weapons", IsChecked = true, Margin = new Thickness(0, 0, 0, 6) };
            var categoriesAllCheck = new CheckBox { Content = "Weapon Categories", IsChecked = true, Margin = new Thickness(0, 0, 0, 6) };

            content.Children.Add(globalCheck);
            content.Children.Add(variantsCheck);
            content.Children.Add(materialsCheck);
            content.Children.Add(uniqueCheck);
            content.Children.Add(categoriesAllCheck);

            var buttonRow = new DockPanel
            {
                LastChildFill = false,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(buttonRow, 2);
            root.Children.Add(buttonRow);

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 110,
                Margin = new Thickness(8, 0, 0, 0)
            };
            cancelButton.Click += (_, _) => dialog.DialogResult = false;
            DockPanel.SetDock(cancelButton, Dock.Right);
            buttonRow.Children.Add(cancelButton);

            var restoreButton = new Button
            {
                Content = "Restore",
                Width = 110
            };
            restoreButton.Click += (_, _) => dialog.DialogResult = true;
            DockPanel.SetDock(restoreButton, Dock.Right);
            buttonRow.Children.Add(restoreButton);

            dialog.Content = root;

            bool? dialogResult = dialog.ShowDialog();
            if (dialogResult != true)
            {
                return null;
            }

            var result = new RestoreSelection
            {
                RestoreGlobalSettings = globalCheck.IsChecked == true,
                RestoreVariants = variantsCheck.IsChecked == true,
                RestoreWeaponMaterials = materialsCheck.IsChecked == true,
                RestoreUniqueWeapons = uniqueCheck.IsChecked == true,
                RestoreAllWeaponCategories = categoriesAllCheck.IsChecked == true
            };

            return result;
        }

        private static T CloneByJson<T>(T source)
        {
            var json = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(json)!;
        }

        private static WeaponMaterialSetting CloneMaterial(WeaponMaterialSetting source)
        {
            return new WeaponMaterialSetting
            {
                Title = source.Title,
                Enabled = source.Enabled,
                Identifiers = source.Identifiers?.ToList() ?? new List<string>(),
                DamageOffset1h = source.DamageOffset1h,
                DamageOffset2h = source.DamageOffset2h
            };
        }

        private static SpecialWeaponData CloneSpecialWeapon(SpecialWeaponData source)
        {
            return new SpecialWeaponData
            {
                EditorID = source.EditorID,
                FormKey = source.FormKey,
                DamageOffset = source.DamageOffset,
                SpeedOffset = source.SpeedOffset,
                ReachOffset = source.ReachOffset,
                StaggerOffset = source.StaggerOffset,
                CriticalDamageOffset = source.CriticalDamageOffset,
                CriticalDamageChanceMultiplierOffset = source.CriticalDamageChanceMultiplierOffset
            };
        }

        // ─── Save / close ─────────────────────────────────────────────────────────

        private void SetStatus(string message, bool isError = false)
        {
            _statusText.Text = message;
            _statusText.Foreground = isError ? Brushes.DarkRed : Brushes.DimGray;
        }

        private bool Save()
        {
            CommitPendingInputEdits();
            if (!ValidateVisibleFields())
            {
                return false;
            }

            try
            {
                SettingsFileStore.Save(_settingsFolder, _settings);
                _lastSavedSettingsSnapshot = GetSettingsSnapshot();
                SetStatus($"Saved {SettingsFileStore.GetUserSettingsPath(_settingsFolder)}.");
                return true;
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to save settings: {ex.Message}", isError: true);
                return false;
            }
        }

        private string GetSettingsSnapshot() => JsonConvert.SerializeObject(_settings, Formatting.None);

        private bool HasUnsavedChanges() =>
            HasPendingTextBoxChanges() ||
            !string.Equals(_lastSavedSettingsSnapshot, GetSettingsSnapshot(), StringComparison.Ordinal);

        private void CommitPendingInputEdits()
        {
            if (Keyboard.FocusedElement is not UIElement focusedElement)
            {
                return;
            }

            _saveButton.Focus();
            focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            CommitPendingInputEdits();

            if (!HasUnsavedChanges())
            {
                return;
            }

            var result = MessageBox.Show(
                "You have unsaved changes.\n\nDo you want to save before closing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.Yes);

            if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (result == MessageBoxResult.Yes)
            {
                if (!Save())
                {
                    e.Cancel = true;
                }
            }
        }

        // ─── XAML loading ─────────────────────────────────────────────────────────

        private void LoadComponent()
        {
            var resourceLocater = new Uri("/WeaponStatSynthesisPatcher;component/settingswindow.xaml", UriKind.Relative);
            Application.LoadComponent(this, resourceLocater);
        }

        private TControl GetRequiredControl<TControl>(string name) where TControl : class
        {
            return FindName(name) as TControl
                ?? throw new XamlParseException($"Required control '{name}' was not found in SettingsWindow.xaml.");
        }
    }
}
