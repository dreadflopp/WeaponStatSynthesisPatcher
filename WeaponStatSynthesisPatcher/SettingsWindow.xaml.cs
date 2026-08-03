using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
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

        private sealed class PendingTextSnapshot
        {
            public required string OriginalText { get; init; }
        }

        private enum NodeKind
        {
            Global,
            VariantContainer,
            CategoryContainer,
            WeaponCategory
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

            var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 2) };
            var restoreDefaultButton = new Button
            {
                Content = "Restore Default Settings",
                Width = 190,
                ToolTip = "Reload all settings from the bundled defaults."
            };
            restoreDefaultButton.Click += (_, _) =>
            {
                if (!ConfirmRestorePreset("default settings"))
                {
                    return;
                }
                RestoreFromBundledPreset("settings.json", "Restored default settings. Click Save to persist.");
            };
            buttonRow.Children.Add(restoreDefaultButton);
            _detailPanel.Children.Add(buttonRow);

            var props = typeof(Settings)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite
                    && p.PropertyType != typeof(WeaponCategory)
                    && p.PropertyType != typeof(VariantCategory)
                    && p.PropertyType != typeof(WeaponAttributeEnablers))
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
                TrackPendingText(box, box.Text);
                AddLabeledControlRow(_detailPanel, label, box, tooltip, topMargin: 8, labelWidth: 340);
                box.LostFocus += (_, _) =>
                {
                    if (TryParseFloat(box.Text, out var f))
                    {
                        prop.SetValue(_settings, f);
                    }
                    else
                    {
                        SetStatus($"{label} must be a number.", isError: true);
                    }
                };
            }
            else if (type == typeof(int))
            {
                var box = new TextBox { Text = ((int?)value ?? 0).ToString(CultureInfo.InvariantCulture), MinWidth = 180 };
                TrackPendingText(box, box.Text);
                AddLabeledControlRow(_detailPanel, label, box, tooltip, topMargin: 8, labelWidth: 340);
                box.LostFocus += (_, _) =>
                {
                    if (int.TryParse(box.Text, out var i))
                    {
                        prop.SetValue(_settings, i);
                    }
                    else
                    {
                        SetStatus($"{label} must be an integer.", isError: true);
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

        // ─── Variants ────────────────────────────────────────────────────────────

        private void RenderVariantContainer()
        {
            AddText("Variants", FontWeights.SemiBold, 20, Brushes.Black);
            AddText($"{_settings.Variants.Variants.Count} variant(s) defined.", FontWeights.Normal, 12, Brushes.DimGray);
            AddText("Each variant opens in its own tab.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 2);

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
            AddLabeledInfoRow(panel, "These values are added to the weapon's base stats after material offsets.", topMargin: 2);

            AddIntFieldToPanel(panel, "Additional Damage", variant.AdditionalDamage, "Damage offset (integer).", v => variant.AdditionalDamage = v);
            AddFloatFieldToPanel(panel, "Additional Reach", variant.AdditionalReach, "Reach offset.", v => variant.AdditionalReach = v);
            AddFloatFieldToPanel(panel, "Additional Speed", variant.AdditionalSpeed, "Speed offset.", v => variant.AdditionalSpeed = v);
            AddFloatFieldToPanel(panel, "Additional Stagger", variant.AdditionalStagger, "Stagger offset.", v => variant.AdditionalStagger = v);
            AddFloatFieldToPanel(panel, "Additional Crit Damage Offset", variant.AdditionalCriticalDamageOffset, "Critical damage flat offset.", v => variant.AdditionalCriticalDamageOffset = v);
            AddFloatFieldToPanel(panel, "Additional Crit Chance Multiplier", variant.AdditionalCriticalDamageChanceMultiplier, "Critical damage chance multiplier offset.", v => variant.AdditionalCriticalDamageChanceMultiplier = v);
            AddFloatFieldToPanel(panel, "Additional Crit Damage Multiplier", variant.AdditionalCriticalDamageMultiplier, "Critical damage multiplier offset.", v => variant.AdditionalCriticalDamageMultiplier = v);

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
            var count = GetWeaponCategoryProperties().Count();
            AddText($"{count} weapon categor{(count == 1 ? "y" : "ies")} defined.", FontWeights.Normal, 12, Brushes.DimGray);
            AddText("Select a category from the tree to edit its weapons.", FontWeights.Normal, 13, Brushes.DimGray, topMargin: 8);
        }

        private void RenderWeaponCategory(NodeRef node)
        {
            if (node.Category == null || node.CategoryProperty == null)
            {
                return;
            }

            AddText(GetPropertyLabel(node.CategoryProperty), FontWeights.SemiBold, 20, Brushes.Black);
            AddText($"{node.Category.Weapons.Count} weapon type(s) in this category.", FontWeights.Normal, 12, Brushes.DimGray);
            AddText("Each weapon type opens in its own tab.", FontWeights.Normal, 12, Brushes.DimGray, topMargin: 2);

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
                ? "Yes (built-in vanilla type priority applies)"
                : "No";
            var vanillaTypeText = new TextBlock
            {
                Text = vanillaTypeMessage,
                VerticalAlignment = VerticalAlignment.Center
            };
            AddLabeledControlRow(panel, "Vanilla Type", vanillaTypeText, "Vanilla weapon types have lower priority than non-vanilla types during matching. You can still disable the rule.");

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
            AddLabeledControlRow(panel, "Name IDs", nameIDsBox, "Semicolon-separated words to match against the weapon's name.");

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
            AddLabeledControlRow(panel, "Keyword IDs", keywordIDsBox, "Semicolon-separated keywords to match against the weapon's keywords.");

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

        private void AddIntField(string label, int current, string tooltip, Action<int> onChanged)
        {
            var box = new TextBox { Text = current.ToString(CultureInfo.InvariantCulture), MinWidth = 180 };
            TrackPendingText(box, box.Text);
            AddLabeledControlRow(_detailPanel, label, box, tooltip);
            box.LostFocus += (_, _) =>
            {
                if (int.TryParse(box.Text, out var v))
                {
                    onChanged(v);
                }
                else
                {
                    SetStatus($"{label} must be an integer.", isError: true);
                }
            };
        }

        private void AddFloatField(string label, float current, string tooltip, Action<float> onChanged)
        {
            var box = new TextBox { Text = current.ToString(CultureInfo.InvariantCulture), MinWidth = 180 };
            TrackPendingText(box, box.Text);
            AddLabeledControlRow(_detailPanel, label, box, tooltip);
            box.LostFocus += (_, _) =>
            {
                if (TryParseFloat(box.Text, out var v))
                {
                    onChanged(v);
                }
                else
                {
                    SetStatus($"{label} must be a number.", isError: true);
                }
            };
        }

        private static void AddUshortFieldToPanel(Panel panel, string label, ushort current, string tooltip, Action<ushort> onChanged)
        {
            var box = new TextBox { Text = current.ToString(CultureInfo.InvariantCulture), MinWidth = 180 };
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

        private static void AddIntFieldToPanel(Panel panel, string label, int current, string tooltip, Action<int> onChanged)
        {
            var box = new TextBox { Text = current.ToString(CultureInfo.InvariantCulture), MinWidth = 180 };
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

        private static void AddFloatFieldToPanel(Panel panel, string label, float current, string tooltip, Action<float> onChanged)
        {
            var box = new TextBox { Text = current.ToString(CultureInfo.InvariantCulture), MinWidth = 180 };
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

        private static string? GetPropertyTooltip(PropertyInfo property) => GetTooltipText(property);

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

        private static string? GetTooltipText(MemberInfo? member)
        {
            var attrData = member?
                .CustomAttributes
                .FirstOrDefault(a =>
                    string.Equals(a.AttributeType.Name, "Tooltip", StringComparison.Ordinal) ||
                    string.Equals(a.AttributeType.Name, "TooltipAttribute", StringComparison.Ordinal));

            if (attrData == null)
            {
                return null;
            }

            if (attrData.ConstructorArguments.Count > 0 && attrData.ConstructorArguments[0].Value is string constructorValue && !string.IsNullOrWhiteSpace(constructorValue))
            {
                return constructorValue;
            }

            return attrData.NamedArguments.Select(a => a.TypedValue.Value as string).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
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

        private void RestoreFromBundledPreset(string presetFileName, string successMessage)
        {
            try
            {
                var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrWhiteSpace(assemblyDirectory))
                {
                    SetStatus("Could not locate patcher directory for bundled presets.", isError: true);
                    return;
                }

                var presetPath = Path.Combine(assemblyDirectory, "Data", presetFileName);
                if (!File.Exists(presetPath))
                {
                    SetStatus($"Bundled preset was not found: {presetFileName}", isError: true);
                    return;
                }

                var json = File.ReadAllText(presetPath);
                JsonConvert.PopulateObject(json, _settings, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });

                BuildTree();
                if (_settingsTree.Items.Count > 0 && _settingsTree.Items[0] is TreeViewItem firstNode)
                {
                    firstNode.IsSelected = true;
                    firstNode.BringIntoView();
                }

                RenderSelectedNode();
                SetStatus(successMessage);
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to restore preset '{presetFileName}': {ex.Message}", isError: true);
            }
        }

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

        // ─── Save / close ─────────────────────────────────────────────────────────

        private void SetStatus(string message, bool isError = false)
        {
            _statusText.Text = message;
            _statusText.Foreground = isError ? Brushes.DarkRed : Brushes.DimGray;
        }

        private bool Save()
        {
            try
            {
                SettingsFileStore.Save(_settingsFolder, _settings);
                _lastSavedSettingsSnapshot = GetSettingsSnapshot();
                SetStatus("Saved settings.json.");
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
