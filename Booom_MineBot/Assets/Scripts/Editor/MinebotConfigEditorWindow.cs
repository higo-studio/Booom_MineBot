using System;
using System.Collections.Generic;
using Minebot.Bootstrap;
using Minebot.GridMining;
using Minebot.Progression;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Minebot.Editor
{
    public sealed class MinebotConfigEditorWindow : EditorWindow
    {
        private readonly List<ConfigCategory> categories = new();

        private ObjectField bootstrapField;
        private Label statusLabel;
        private ListView categoryListView;
        private ScrollView detailScrollView;
        private BootstrapConfig currentBootstrapConfig;
        private int selectedCategoryIndex;

        [MenuItem("Minebot/配置/配置编辑器")]
        public static void OpenWindow()
        {
            MinebotConfigEditorWindow window = GetWindow<MinebotConfigEditorWindow>();
            window.minSize = new Vector2(860f, 540f);
            window.titleContent = new GUIContent("Minebot 配置");
        }

        public void CreateGUI()
        {
            titleContent = new GUIContent("Minebot 配置");
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.paddingLeft = 12f;
            rootVisualElement.style.paddingRight = 12f;
            rootVisualElement.style.paddingTop = 12f;
            rootVisualElement.style.paddingBottom = 12f;
            rootVisualElement.style.backgroundColor = new Color(0.11f, 0.12f, 0.14f);

            BuildToolbar(rootVisualElement);
            BuildMainArea(rootVisualElement);
            RebuildCategories();
            RefreshCurrentBootstrap(MinebotConfigAssetUtility.GetOrCreateBootstrapConfig());
        }

        private void BuildToolbar(VisualElement root)
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Column;
            toolbar.style.marginBottom = 12f;
            toolbar.style.paddingBottom = 10f;
            toolbar.style.borderBottomWidth = 1f;
            toolbar.style.borderBottomColor = new Color(0.22f, 0.24f, 0.28f);
            root.Add(toolbar);

            var title = new Label("Minebot 配置编辑器");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 18f;
            title.style.color = new Color(0.94f, 0.95f, 0.97f);
            title.style.marginBottom = 8f;
            toolbar.Add(title);

            var controlsRow = new VisualElement();
            controlsRow.style.flexDirection = FlexDirection.Row;
            controlsRow.style.alignItems = Align.Center;
            toolbar.Add(controlsRow);

            bootstrapField = new ObjectField("Bootstrap 配置")
            {
                objectType = typeof(BootstrapConfig),
                allowSceneObjects = false
            };
            bootstrapField.style.flexGrow = 1f;
            bootstrapField.style.marginRight = 8f;
            bootstrapField.RegisterValueChangedCallback(evt =>
            {
                RefreshCurrentBootstrap(evt.newValue as BootstrapConfig);
            });
            controlsRow.Add(bootstrapField);

            Button syncButton = CreateToolbarButton("补齐引用", () =>
            {
                if (currentBootstrapConfig == null)
                {
                    RefreshCurrentBootstrap(MinebotConfigAssetUtility.GetOrCreateBootstrapConfig());
                    return;
                }

                MinebotConfigAssetUtility.EnsureManagedAssets(currentBootstrapConfig);
                RefreshCurrentBootstrap(currentBootstrapConfig);
            });
            syncButton.style.marginRight = 8f;
            controlsRow.Add(syncButton);

            Button pingButton = CreateToolbarButton("定位资产", () =>
            {
                if (currentBootstrapConfig == null)
                {
                    return;
                }

                Selection.activeObject = currentBootstrapConfig;
                EditorGUIUtility.PingObject(currentBootstrapConfig);
            });
            controlsRow.Add(pingButton);

            statusLabel = new Label();
            statusLabel.style.marginTop = 6f;
            statusLabel.style.color = new Color(0.72f, 0.75f, 0.8f);
            toolbar.Add(statusLabel);
        }

        private void BuildMainArea(VisualElement root)
        {
            var mainArea = new VisualElement();
            mainArea.style.flexDirection = FlexDirection.Row;
            mainArea.style.flexGrow = 1f;
            root.Add(mainArea);

            var leftPane = new VisualElement();
            leftPane.style.width = 220f;
            leftPane.style.flexShrink = 0f;
            leftPane.style.paddingTop = 8f;
            leftPane.style.paddingBottom = 8f;
            leftPane.style.paddingLeft = 8f;
            leftPane.style.paddingRight = 8f;
            leftPane.style.backgroundColor = new Color(0.14f, 0.15f, 0.18f);
            leftPane.style.borderTopLeftRadius = 8f;
            leftPane.style.borderBottomLeftRadius = 8f;
            leftPane.style.borderTopRightRadius = 8f;
            leftPane.style.borderBottomRightRadius = 8f;
            leftPane.style.marginRight = 12f;
            mainArea.Add(leftPane);

            var leftTitle = new Label("配置分类");
            leftTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            leftTitle.style.color = new Color(0.93f, 0.94f, 0.96f);
            leftTitle.style.marginBottom = 8f;
            leftPane.Add(leftTitle);

            categoryListView = new ListView
            {
                selectionType = SelectionType.Single,
                fixedItemHeight = 36f
            };
            categoryListView.makeItem = () =>
            {
                var label = new Label();
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.paddingLeft = 8f;
                label.style.paddingRight = 8f;
                label.style.color = new Color(0.9f, 0.92f, 0.95f);
                return label;
            };
            categoryListView.bindItem = (element, index) =>
            {
                if (element is Label label && index >= 0 && index < categories.Count)
                {
                    label.text = categories[index].Name;
                }
            };
            categoryListView.selectionChanged += OnCategorySelectionChanged;
            categoryListView.style.flexGrow = 1f;
            leftPane.Add(categoryListView);

            detailScrollView = new ScrollView(ScrollViewMode.Vertical);
            detailScrollView.style.flexGrow = 1f;
            detailScrollView.style.paddingTop = 8f;
            detailScrollView.style.paddingBottom = 8f;
            detailScrollView.style.paddingLeft = 12f;
            detailScrollView.style.paddingRight = 12f;
            detailScrollView.style.backgroundColor = new Color(0.15f, 0.16f, 0.19f);
            detailScrollView.style.borderTopLeftRadius = 8f;
            detailScrollView.style.borderBottomLeftRadius = 8f;
            detailScrollView.style.borderTopRightRadius = 8f;
            detailScrollView.style.borderBottomRightRadius = 8f;
            mainArea.Add(detailScrollView);
        }

        private void RefreshCurrentBootstrap(BootstrapConfig bootstrapConfig)
        {
            currentBootstrapConfig = bootstrapConfig ?? MinebotConfigAssetUtility.GetOrCreateBootstrapConfig();

            if (currentBootstrapConfig != null)
            {
                MinebotConfigAssetUtility.EnsureManagedAssets(currentBootstrapConfig);
            }

            bootstrapField?.SetValueWithoutNotify(currentBootstrapConfig);
            UpdateStatus();
            categoryListView.itemsSource = categories;
            categoryListView.Rebuild();

            if (categories.Count == 0)
            {
                detailScrollView.Clear();
                return;
            }

            selectedCategoryIndex = Mathf.Clamp(selectedCategoryIndex, 0, categories.Count - 1);
            categoryListView.selectedIndex = selectedCategoryIndex;
            RefreshDetailPane();
        }

        private void UpdateStatus()
        {
            if (statusLabel == null)
            {
                return;
            }

            if (currentBootstrapConfig == null)
            {
                statusLabel.text = "当前没有可用的 Bootstrap 配置。";
                return;
            }

            statusLabel.text = $"当前配置: {AssetDatabase.GetAssetPath(currentBootstrapConfig)}";
        }

        private void RebuildCategories()
        {
            categories.Clear();
            categories.Add(new ConfigCategory("启动与场景", "维护场景入口与输入资源。", BuildBootstrapCategory));
            categories.Add(new ConfigCategory("地图", "管理 authored 地图引用与程序地图参数。", BuildMapCategory));
            categories.Add(new ConfigCategory("成长与经济", "管理玩家成长、资源掉落与升级池。", BuildProgressionCategory));
            categories.Add(new ConfigCategory("挖掘与风险", "管理挖掘规则和炸药感知规则。", BuildMiningAndHazardCategory));
            categories.Add(new ConfigCategory("波次", "管理地震波节奏与回收奖励。", BuildWaveCategory));
            categories.Add(new ConfigCategory("建筑", "自动同步建筑定义列表，并直接编辑每个建筑资产。", BuildBuildingCategory));
        }

        private void OnCategorySelectionChanged(IEnumerable<object> selection)
        {
            foreach (object item in selection)
            {
                selectedCategoryIndex = categories.IndexOf(item as ConfigCategory);
                break;
            }

            if (selectedCategoryIndex < 0)
            {
                selectedCategoryIndex = 0;
            }

            RefreshDetailPane();
        }

        private void RefreshDetailPane()
        {
            detailScrollView.Clear();

            if (currentBootstrapConfig == null)
            {
                detailScrollView.Add(new HelpBox("未找到 Bootstrap 配置，无法展示编辑内容。", HelpBoxMessageType.Warning));
                return;
            }

            if (selectedCategoryIndex < 0 || selectedCategoryIndex >= categories.Count)
            {
                return;
            }

            ConfigCategory category = categories[selectedCategoryIndex];
            detailScrollView.Add(CreateSectionTitle(category.Name, category.Description));
            category.Build(currentBootstrapConfig, detailScrollView);
        }

        private void BuildBootstrapCategory(BootstrapConfig bootstrapConfig, VisualElement parent)
        {
            parent.Add(new HelpBox("这里保留聚合入口本身的少量字段；子配置 SO 会在其它大类中自动托管。", HelpBoxMessageType.Info));
            parent.Add(CreateBootstrapPropertySection(bootstrapConfig, "启动设置", "gameplaySceneName", "inputActions"));
        }

        private void BuildMapCategory(BootstrapConfig bootstrapConfig, VisualElement parent)
        {
            parent.Add(new HelpBox("保留默认地图为空时，运行时会继续走程序生成地图。只有在需要 authored 地图时才创建并引用 MapDefinition。", HelpBoxMessageType.Info));
            parent.Add(CreateBootstrapPropertySection(bootstrapConfig, "地图入口", "defaultMap", "generatedMapConfig"));

            var buttonRow = CreateButtonRow();
            buttonRow.Add(CreateActionButton("创建/引用地图配置", () =>
            {
                MapDefinition map = MinebotConfigAssetUtility.AssignOrCreateDefaultMap(bootstrapConfig);
                if (map != null)
                {
                    Selection.activeObject = map;
                    EditorGUIUtility.PingObject(map);
                }

                RefreshCurrentBootstrap(bootstrapConfig);
            }));

            if (GetBootstrapReference<MapDefinition>(bootstrapConfig, "defaultMap") != null)
            {
                buttonRow.Add(CreateActionButton("定位地图资源", () =>
                {
                    MapDefinition map = GetBootstrapReference<MapDefinition>(bootstrapConfig, "defaultMap");
                    if (map == null)
                    {
                        return;
                    }

                    Selection.activeObject = map;
                    EditorGUIUtility.PingObject(map);
                }));
            }

            parent.Add(buttonRow);
        }

        private void BuildProgressionCategory(BootstrapConfig bootstrapConfig, VisualElement parent)
        {
            parent.Add(CreateManagedObjectSection(
                "数值配置",
                "经济、生命、机器人和四档掉落范围。",
                GetBootstrapReference<ScriptableObject>(bootstrapConfig, "balanceConfig")));
            parent.Add(CreateManagedObjectSection(
                "升级池",
                "升级列表与权重。",
                GetBootstrapReference<ScriptableObject>(bootstrapConfig, "upgradePool")));
        }

        private void BuildMiningAndHazardCategory(BootstrapConfig bootstrapConfig, VisualElement parent)
        {
            parent.Add(CreateManagedObjectSection(
                "挖掘规则",
                "墙体生命、防御、攻击和中断宽限。",
                GetBootstrapReference<ScriptableObject>(bootstrapConfig, "miningRules")));
            parent.Add(CreateManagedObjectSection(
                "炸药规则",
                "炸药生成、被动感知和伤害参数。",
                GetBootstrapReference<ScriptableObject>(bootstrapConfig, "hazardRules")));
        }

        private void BuildWaveCategory(BootstrapConfig bootstrapConfig, VisualElement parent)
        {
            parent.Add(CreateManagedObjectSection(
                "地震波配置",
                "波次节奏、危险区成长与机器人回收收益。",
                GetBootstrapReference<ScriptableObject>(bootstrapConfig, "waveConfig")));
        }

        private void BuildBuildingCategory(BootstrapConfig bootstrapConfig, VisualElement parent)
        {
            parent.Add(new HelpBox(
                $"建筑定义会自动收集当前 Bootstrap 目录下的 BuildingDefinition 资产，并同步回引用数组。\n托管目录: {MinebotConfigAssetUtility.GetManagedBuildingsFolder(bootstrapConfig)}",
                HelpBoxMessageType.Info));

            var buttonRow = CreateButtonRow();
            buttonRow.Add(CreateActionButton("新增建筑配置", () =>
            {
                BuildingDefinition definition = MinebotConfigAssetUtility.CreateBuildingDefinitionAsset(bootstrapConfig);
                if (definition != null)
                {
                    Selection.activeObject = definition;
                    EditorGUIUtility.PingObject(definition);
                }

                RefreshCurrentBootstrap(bootstrapConfig);
            }));
            buttonRow.Add(CreateActionButton("同步建筑引用", () =>
            {
                MinebotConfigAssetUtility.SyncBuildingDefinitions(bootstrapConfig);
                RefreshCurrentBootstrap(bootstrapConfig);
            }));
            parent.Add(buttonRow);

            IReadOnlyList<BuildingDefinition> definitions = MinebotConfigAssetUtility.GetBuildingDefinitions(bootstrapConfig);
            if (definitions.Count == 0)
            {
                parent.Add(new HelpBox("当前没有建筑配置。点击“新增建筑配置”后会自动创建并引用。", HelpBoxMessageType.Warning));
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                BuildingDefinition definition = definitions[i];
                var foldout = new Foldout
                {
                    text = ResolveBuildingTitle(definition),
                    value = i == 0
                };
                foldout.style.marginTop = 8f;
                foldout.style.marginBottom = 8f;
                foldout.Add(CreateManagedObjectBody(definition, includeTitleBar: true));
                parent.Add(foldout);
            }
        }

        private VisualElement CreateBootstrapPropertySection(BootstrapConfig bootstrapConfig, string title, params string[] propertyNames)
        {
            var container = CreateCard();
            container.Add(CreateCardHeader(title, null, bootstrapConfig));
            container.Add(CreatePropertyFields(new SerializedObject(bootstrapConfig), propertyNames));
            return container;
        }

        private VisualElement CreateManagedObjectSection(string title, string description, ScriptableObject target)
        {
            var container = CreateCard();
            container.Add(CreateCardHeader(title, description, target));
            container.Add(CreateManagedObjectBody(target, includeTitleBar: false));
            return container;
        }

        private VisualElement CreateManagedObjectBody(UnityEngine.Object target, bool includeTitleBar)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;

            if (target == null)
            {
                container.Add(new HelpBox("资源引用为空，点击窗口顶部“补齐引用”后会自动创建并回填。", HelpBoxMessageType.Warning));
                return container;
            }

            if (includeTitleBar)
            {
                container.Add(CreateInlineAssetToolbar(target));
            }

            container.Add(CreateInspector(new SerializedObject(target)));
            return container;
        }

        private VisualElement CreateInlineAssetToolbar(UnityEngine.Object target)
        {
            var row = CreateButtonRow();
            row.style.marginTop = 4f;
            row.style.marginBottom = 6f;
            row.Add(CreateActionButton("定位资源", () =>
            {
                Selection.activeObject = target;
                EditorGUIUtility.PingObject(target);
            }));
            row.Add(CreateActionButton("选中资源", () =>
            {
                Selection.activeObject = target;
            }));
            return row;
        }

        private VisualElement CreateCardHeader(string title, string description, UnityEngine.Object target)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Column;
            header.style.marginBottom = 8f;

            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.alignItems = Align.Center;
            topRow.style.justifyContent = Justify.SpaceBetween;
            header.Add(topRow);

            var titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 14f;
            titleLabel.style.color = new Color(0.94f, 0.95f, 0.97f);
            topRow.Add(titleLabel);

            if (target != null)
            {
                var buttonRow = new VisualElement();
                buttonRow.style.flexDirection = FlexDirection.Row;
                buttonRow.Add(CreateMiniButton("定位", () =>
                {
                    Selection.activeObject = target;
                    EditorGUIUtility.PingObject(target);
                }));
                buttonRow.Add(CreateMiniButton("选中", () =>
                {
                    Selection.activeObject = target;
                }));
                topRow.Add(buttonRow);
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                var descriptionLabel = new Label(description);
                descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
                descriptionLabel.style.color = new Color(0.72f, 0.75f, 0.8f);
                descriptionLabel.style.marginTop = 4f;
                header.Add(descriptionLabel);
            }

            if (target != null)
            {
                var pathLabel = new Label(AssetDatabase.GetAssetPath(target));
                pathLabel.style.whiteSpace = WhiteSpace.Normal;
                pathLabel.style.color = new Color(0.6f, 0.64f, 0.7f);
                pathLabel.style.marginTop = 2f;
                header.Add(pathLabel);
            }

            return header;
        }

        private VisualElement CreatePropertyFields(SerializedObject serializedObject, params string[] propertyNames)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;

            for (int i = 0; i < propertyNames.Length; i++)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyNames[i]);
                if (property == null)
                {
                    continue;
                }

                var field = new PropertyField(property.Copy());
                field.style.marginBottom = 6f;
                container.Add(field);
            }

            container.Bind(serializedObject);
            return container;
        }

        private VisualElement CreateInspector(SerializedObject serializedObject)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;

            SerializedProperty iterator = serializedObject.GetIterator();
            if (!iterator.NextVisible(true))
            {
                return container;
            }

            do
            {
                if (string.Equals(iterator.propertyPath, "m_Script", StringComparison.Ordinal))
                {
                    continue;
                }

                var field = new PropertyField(iterator.Copy());
                field.style.marginBottom = 6f;
                container.Add(field);
            }
            while (iterator.NextVisible(false));

            container.Bind(serializedObject);
            return container;
        }

        private VisualElement CreateSectionTitle(string title, string description)
        {
            var container = new VisualElement();
            container.style.marginBottom = 12f;

            var titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 17f;
            titleLabel.style.color = new Color(0.96f, 0.97f, 0.98f);
            container.Add(titleLabel);

            if (!string.IsNullOrWhiteSpace(description))
            {
                var descriptionLabel = new Label(description);
                descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
                descriptionLabel.style.marginTop = 4f;
                descriptionLabel.style.color = new Color(0.74f, 0.77f, 0.82f);
                container.Add(descriptionLabel);
            }

            return container;
        }

        private VisualElement CreateCard()
        {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Column;
            card.style.marginBottom = 12f;
            card.style.paddingTop = 10f;
            card.style.paddingBottom = 10f;
            card.style.paddingLeft = 12f;
            card.style.paddingRight = 12f;
            card.style.backgroundColor = new Color(0.18f, 0.19f, 0.23f);
            card.style.borderTopLeftRadius = 8f;
            card.style.borderBottomLeftRadius = 8f;
            card.style.borderTopRightRadius = 8f;
            card.style.borderBottomRightRadius = 8f;
            card.style.borderLeftWidth = 1f;
            card.style.borderRightWidth = 1f;
            card.style.borderTopWidth = 1f;
            card.style.borderBottomWidth = 1f;
            card.style.borderLeftColor = new Color(0.25f, 0.28f, 0.33f);
            card.style.borderRightColor = new Color(0.25f, 0.28f, 0.33f);
            card.style.borderTopColor = new Color(0.25f, 0.28f, 0.33f);
            card.style.borderBottomColor = new Color(0.25f, 0.28f, 0.33f);
            return card;
        }

        private VisualElement CreateButtonRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginTop = 8f;
            row.style.marginBottom = 8f;
            return row;
        }

        private Button CreateActionButton(string text, Action action)
        {
            var button = new Button(action)
            {
                text = text
            };
            button.style.height = 28f;
            button.style.marginRight = 8f;
            button.style.marginBottom = 8f;
            return button;
        }

        private Button CreateToolbarButton(string text, Action action)
        {
            var button = CreateActionButton(text, action);
            button.style.minWidth = 84f;
            return button;
        }

        private Button CreateMiniButton(string text, Action action)
        {
            var button = new Button(action)
            {
                text = text
            };
            button.style.height = 22f;
            button.style.minWidth = 48f;
            button.style.marginRight = 6f;
            return button;
        }

        private static T GetBootstrapReference<T>(BootstrapConfig bootstrapConfig, string propertyName)
            where T : UnityEngine.Object
        {
            if (bootstrapConfig == null)
            {
                return null;
            }

            SerializedObject serializedObject = new SerializedObject(bootstrapConfig);
            return serializedObject.FindProperty(propertyName)?.objectReferenceValue as T;
        }

        private static string ResolveBuildingTitle(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return "空建筑配置";
            }

            return string.IsNullOrWhiteSpace(definition.DisplayName)
                ? definition.name
                : $"{definition.DisplayName} ({definition.name})";
        }

        private sealed class ConfigCategory
        {
            public ConfigCategory(string name, string description, Action<BootstrapConfig, VisualElement> build)
            {
                Name = name;
                Description = description;
                Build = build;
            }

            public string Name { get; }
            public string Description { get; }
            public Action<BootstrapConfig, VisualElement> Build { get; }
        }
    }
}
