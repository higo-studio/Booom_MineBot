using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Minebot.Automation;
using Minebot.Presentation;
using Minebot.UI;
using UnityEditor;
using UnityEngine;

namespace Minebot.Editor
{
    internal static class MinebotPrefabGameplayArtSupport
    {
        private const int ActorPixelsPerUnit = 32;
        private const int FxPixelsPerUnit = 16;
        private const int UiPixelsPerUnit = 32;
        private const string ActorSpriteDirectory = "Assets/Art/Minebot/Sprites/Actors/States";
        private const string PickupSpriteDirectory = "Assets/Art/Minebot/Sprites/Pickups";
        private const string FxSpriteDirectory = "Assets/Art/Minebot/Sprites/Effects";
        private const string HudSpriteDirectory = "Assets/Art/Minebot/Sprites/UI/HUD";
        private const string SequenceAssetDirectory = "Assets/Resources/Minebot/Presentation/Sequences";
        private const string ActorPrefabDirectory = "Assets/Resources/Minebot/Presentation/Actors";
        private const string PickupPrefabDirectory = "Assets/Resources/Minebot/Presentation/Pickups";
        private const string CellFxPrefabDirectory = "Assets/Resources/Minebot/Presentation/CellFx";
        private const string PromptPath = "Assets/Art/Minebot/Generated/Prompts/minebot-prefab-gameplay-art-batch-001.md";
        private const string ManifestPath = "Assets/Art/Minebot/Generated/Selected/minebot-prefab-gameplay-art-manifest-001.md";
        private const string RecordTemplatePath = "Assets/Art/Minebot/Docs/prefab-gameplay-art-record-template.md";
        private const string HudMockupSourcePath = "Assets/Art/Minebot/Generated/Selected/minebot-hud-uiux-mockup-source.png";

        private static readonly PresentationActorState[] ActorStates =
        {
            PresentationActorState.Idle,
            PresentationActorState.Moving,
            PresentationActorState.Mining,
            PresentationActorState.Blocked,
            PresentationActorState.Destroyed
        };

        private static readonly TextureSpec[] TextureSpecs = CreateTextureSpecs();

        public static void EnsureGeneratedFiles()
        {
            EnsureDirectory(ActorSpriteDirectory);
            EnsureDirectory(PickupSpriteDirectory);
            EnsureDirectory(FxSpriteDirectory);
            EnsureDirectory(HudSpriteDirectory);

            foreach (TextureSpec spec in TextureSpecs)
            {
                WriteTexture(spec.Path, spec.Factory());
            }

            WriteText(PromptPath, CreatePrompt());
            WriteText(ManifestPath, CreateManifest());
            WriteText(RecordTemplatePath, CreateRecordTemplate());
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        public static void EnsureTextureImporters()
        {
            foreach (TextureSpec spec in TextureSpecs)
            {
                ConfigureTextureImporter(spec.Path, spec.PixelsPerUnit);
            }
        }

        public static void ValidateImportSettings(List<string> errors)
        {
            foreach (TextureSpec spec in TextureSpecs)
            {
                ValidateTexture(spec.Path, spec.PixelsPerUnit, errors);
            }
        }

        public static void EnsureGeneratedAssets()
        {
            EnsureDirectory(SequenceAssetDirectory);
            EnsureDirectory(ActorPrefabDirectory);
            EnsureDirectory(PickupPrefabDirectory);
            EnsureDirectory(CellFxPrefabDirectory);

            foreach (TextureSpec spec in TextureSpecs)
            {
                ConfigureTextureImporter(spec.Path, spec.PixelsPerUnit);
            }

            EnsureSequenceAssets();
            EnsureActorPrefabs();
            EnsurePickupPrefabs();
            EnsureCellFxPrefabs();
            MinebotHudPrefabBuilder.CreateOrUpdatePrefabs();
        }

        public static void ConfigureArtSet(MinebotPresentationArtSet artSet)
        {
            if (artSet == null)
            {
                return;
            }

            artSet.ConfigureActorPresentation(
                AssetDatabase.LoadAssetAtPath<GameObject>($"{ActorPrefabDirectory}/PlayerActor.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>($"{ActorPrefabDirectory}/HelperRobotActor.prefab"),
                CreateActorStateSet("Player"),
                CreateActorStateSet("Robot"));

            artSet.ConfigurePickupPresentation(
                AssetDatabase.LoadAssetAtPath<GameObject>($"{PickupPrefabDirectory}/PickupMetal.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>($"{PickupPrefabDirectory}/PickupEnergy.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>($"{PickupPrefabDirectory}/PickupExperience.prefab"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{PickupSpriteDirectory}/pickup_metal.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{PickupSpriteDirectory}/pickup_energy.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{PickupSpriteDirectory}/pickup_experience.png"));

            artSet.ConfigureCellFxPresentation(
                AssetDatabase.LoadAssetAtPath<GameObject>($"{CellFxPrefabDirectory}/MiningCrackFx.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>($"{CellFxPrefabDirectory}/WallBreakFx.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>($"{CellFxPrefabDirectory}/ExplosionFx.prefab"),
                AssetDatabase.LoadAssetAtPath<SpriteSequenceAsset>($"{SequenceAssetDirectory}/Fx_MiningCrack.asset"),
                AssetDatabase.LoadAssetAtPath<SpriteSequenceAsset>($"{SequenceAssetDirectory}/Fx_WallBreak.asset"),
                AssetDatabase.LoadAssetAtPath<SpriteSequenceAsset>($"{SequenceAssetDirectory}/Fx_Explosion.asset"));

            artSet.ConfigureHudPresentation(
                AssetDatabase.LoadAssetAtPath<MinebotHudView>(MinebotHudView.PrefabAssetPath),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{HudSpriteDirectory}/hud_panel_background.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{HudSpriteDirectory}/hud_icon_status.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{HudSpriteDirectory}/hud_icon_interaction.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{HudSpriteDirectory}/hud_icon_feedback.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{HudSpriteDirectory}/hud_icon_warning.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{HudSpriteDirectory}/hud_icon_upgrade.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{HudSpriteDirectory}/hud_icon_build.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{HudSpriteDirectory}/hud_icon_building_interaction.png"));

            EditorUtility.SetDirty(artSet);
        }

        private static void EnsureSequenceAssets()
        {
            foreach (PresentationActorState state in ActorStates)
            {
                EnsureSequenceAsset(
                    $"{SequenceAssetDirectory}/Player_{state}.asset",
                    new[]
                    {
                        AssetDatabase.LoadAssetAtPath<Sprite>($"{ActorSpriteDirectory}/player_{ToToken(state)}_0.png"),
                        AssetDatabase.LoadAssetAtPath<Sprite>($"{ActorSpriteDirectory}/player_{ToToken(state)}_1.png")
                    },
                    state == PresentationActorState.Idle ? 0.22f : 0.12f,
                    state != PresentationActorState.Destroyed);

                EnsureSequenceAsset(
                    $"{SequenceAssetDirectory}/Robot_{state}.asset",
                    new[]
                    {
                        AssetDatabase.LoadAssetAtPath<Sprite>($"{ActorSpriteDirectory}/robot_{ToToken(state)}_0.png"),
                        AssetDatabase.LoadAssetAtPath<Sprite>($"{ActorSpriteDirectory}/robot_{ToToken(state)}_1.png")
                    },
                    state == PresentationActorState.Idle ? 0.22f : 0.12f,
                    state != PresentationActorState.Destroyed);
            }

            EnsureSequenceAsset(
                $"{SequenceAssetDirectory}/Fx_MiningCrack.asset",
                LoadSpriteRange("crack_mining", 4, FxSpriteDirectory),
                0.08f,
                true);
            EnsureSequenceAsset(
                $"{SequenceAssetDirectory}/Fx_WallBreak.asset",
                LoadSpriteRange("wall_break", 4, FxSpriteDirectory),
                0.07f,
                false);
            EnsureSequenceAsset(
                $"{SequenceAssetDirectory}/Fx_Explosion.asset",
                LoadSpriteRange("explosion", 5, FxSpriteDirectory),
                0.06f,
                false);
        }

        private static void EnsureActorPrefabs()
        {
            EnsurePrefab($"{ActorPrefabDirectory}/PlayerActor.prefab", "Player Actor", typeof(MinebotActorView), typeof(MinebotSpriteSequencePlayer));
            EnsurePrefab($"{ActorPrefabDirectory}/HelperRobotActor.prefab", "Helper Robot Actor", typeof(MinebotActorView), typeof(MinebotSpriteSequencePlayer), typeof(HelperRobotMotionController));
        }

        private static void EnsurePickupPrefabs()
        {
            EnsurePrefab($"{PickupPrefabDirectory}/PickupMetal.prefab", "Pickup Metal", typeof(MinebotPickupView));
            EnsurePrefab($"{PickupPrefabDirectory}/PickupEnergy.prefab", "Pickup Energy", typeof(MinebotPickupView));
            EnsurePrefab($"{PickupPrefabDirectory}/PickupExperience.prefab", "Pickup Experience", typeof(MinebotPickupView));
        }

        private static void EnsureCellFxPrefabs()
        {
            EnsurePrefab($"{CellFxPrefabDirectory}/MiningCrackFx.prefab", "Mining Crack Fx", typeof(MinebotCellFxView), typeof(MinebotSpriteSequencePlayer));
            EnsurePrefab($"{CellFxPrefabDirectory}/WallBreakFx.prefab", "Wall Break Fx", typeof(MinebotCellFxView), typeof(MinebotSpriteSequencePlayer));
            EnsurePrefab($"{CellFxPrefabDirectory}/ExplosionFx.prefab", "Explosion Fx", typeof(MinebotCellFxView), typeof(MinebotSpriteSequencePlayer));
        }

        private static void EnsurePrefab(string assetPath, string objectName, params Type[] componentTypes)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (root == null)
            {
                root = new GameObject(objectName, componentTypes);
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }

                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                contents.name = objectName;
                for (int i = 0; i < componentTypes.Length; i++)
                {
                    if (contents.GetComponent(componentTypes[i]) == null)
                    {
                        contents.AddComponent(componentTypes[i]);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static ActorStateSequenceSet CreateActorStateSet(string actorToken)
        {
            var set = new ActorStateSequenceSet();
            set.Configure(
                AssetDatabase.LoadAssetAtPath<SpriteSequenceAsset>($"{SequenceAssetDirectory}/{actorToken}_Idle.asset"),
                AssetDatabase.LoadAssetAtPath<SpriteSequenceAsset>($"{SequenceAssetDirectory}/{actorToken}_Moving.asset"),
                AssetDatabase.LoadAssetAtPath<SpriteSequenceAsset>($"{SequenceAssetDirectory}/{actorToken}_Mining.asset"),
                AssetDatabase.LoadAssetAtPath<SpriteSequenceAsset>($"{SequenceAssetDirectory}/{actorToken}_Blocked.asset"),
                AssetDatabase.LoadAssetAtPath<SpriteSequenceAsset>($"{SequenceAssetDirectory}/{actorToken}_Destroyed.asset"));
            return set;
        }

        private static Sprite[] LoadSpriteRange(string prefix, int count, string directory)
        {
            var sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{directory}/{prefix}_{i}.png");
            }

            return sprites;
        }

        private static void EnsureSequenceAsset(string assetPath, Sprite[] frames, float frameDuration, bool loop)
        {
            SpriteSequenceAsset asset = AssetDatabase.LoadAssetAtPath<SpriteSequenceAsset>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SpriteSequenceAsset>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.Configure(frames, frameDuration, loop);
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureTextureImporter(string assetPath, int pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                changed = true;
            }

            Vector4 expectedBorder = assetPath == $"{HudSpriteDirectory}/hud_panel_background.png"
                ? new Vector4(12f, 12f, 12f, 12f)
                : Vector4.zero;
            if (importer.spriteBorder != expectedBorder)
            {
                importer.spriteBorder = expectedBorder;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void ValidateTexture(string assetPath, int pixelsPerUnit, List<string> errors)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                errors.Add($"{assetPath}: missing texture importer");
                return;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                errors.Add($"{assetPath}: textureType should be Sprite");
            }

            if (importer.filterMode != FilterMode.Point)
            {
                errors.Add($"{assetPath}: filterMode should be Point");
            }

            if (importer.mipmapEnabled)
            {
                errors.Add($"{assetPath}: mipmapEnabled should be false");
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
            {
                errors.Add($"{assetPath}: pixelsPerUnit should be {pixelsPerUnit}");
            }
        }

        private static void EnsureDirectory(string assetDirectoryPath)
        {
            Directory.CreateDirectory(ToFullPath(assetDirectoryPath));
        }

        private static void WriteTexture(string assetPath, Texture2D texture)
        {
            try
            {
                File.WriteAllBytes(ToFullPath(assetPath), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void WriteText(string assetPath, string content)
        {
            File.WriteAllText(ToFullPath(assetPath), content, Encoding.UTF8);
        }

        private static string ToFullPath(string assetPath)
        {
            return assetPath.StartsWith("Assets/", StringComparison.Ordinal)
                ? Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length))
                : Path.GetFullPath(assetPath);
        }

        private static string ToToken(PresentationActorState state)
        {
            return state.ToString().ToLowerInvariant();
        }

        private static TextureSpec[] CreateTextureSpecs()
        {
            var specs = new List<TextureSpec>();
            for (int i = 0; i < ActorStates.Length; i++)
            {
                PresentationActorState state = ActorStates[i];
                for (int frame = 0; frame < 2; frame++)
                {
                    int capturedFrame = frame;
                    PresentationActorState capturedState = state;
                    specs.Add(new TextureSpec(
                        $"{ActorSpriteDirectory}/player_{ToToken(capturedState)}_{capturedFrame}.png",
                        ActorPixelsPerUnit,
                        () => CreateActorFrame(true, capturedState, capturedFrame)));
                    specs.Add(new TextureSpec(
                        $"{ActorSpriteDirectory}/robot_{ToToken(capturedState)}_{capturedFrame}.png",
                        ActorPixelsPerUnit,
                        () => CreateActorFrame(false, capturedState, capturedFrame)));
                }
            }

            specs.Add(new TextureSpec($"{PickupSpriteDirectory}/pickup_metal.png", UiPixelsPerUnit, CreateMetalPickupTexture));
            specs.Add(new TextureSpec($"{PickupSpriteDirectory}/pickup_energy.png", UiPixelsPerUnit, CreateEnergyPickupTexture));
            specs.Add(new TextureSpec($"{PickupSpriteDirectory}/pickup_experience.png", UiPixelsPerUnit, CreateExperiencePickupTexture));

            for (int i = 0; i < 4; i++)
            {
                int captured = i;
                specs.Add(new TextureSpec($"{FxSpriteDirectory}/crack_mining_{captured}.png", FxPixelsPerUnit, () => CreateMiningCrackTexture(captured)));
                specs.Add(new TextureSpec($"{FxSpriteDirectory}/wall_break_{captured}.png", FxPixelsPerUnit, () => CreateWallBreakTexture(captured)));
            }

            for (int i = 0; i < 5; i++)
            {
                int captured = i;
                specs.Add(new TextureSpec($"{FxSpriteDirectory}/explosion_{captured}.png", FxPixelsPerUnit, () => CreateExplosionTexture(captured)));
            }

            specs.Add(new TextureSpec($"{HudSpriteDirectory}/hud_panel_background.png", UiPixelsPerUnit, CreateHudPanelBackgroundTexture));
            specs.Add(new TextureSpec($"{HudSpriteDirectory}/hud_icon_status.png", UiPixelsPerUnit, CreateHudStatusIconTexture));
            specs.Add(new TextureSpec($"{HudSpriteDirectory}/hud_icon_interaction.png", UiPixelsPerUnit, CreateHudInteractionIconTexture));
            specs.Add(new TextureSpec($"{HudSpriteDirectory}/hud_icon_feedback.png", UiPixelsPerUnit, CreateHudFeedbackIconTexture));
            specs.Add(new TextureSpec($"{HudSpriteDirectory}/hud_icon_warning.png", UiPixelsPerUnit, CreateHudWarningIconTexture));
            specs.Add(new TextureSpec($"{HudSpriteDirectory}/hud_icon_upgrade.png", UiPixelsPerUnit, CreateHudUpgradeIconTexture));
            specs.Add(new TextureSpec($"{HudSpriteDirectory}/hud_icon_build.png", UiPixelsPerUnit, CreateHudBuildIconTexture));
            specs.Add(new TextureSpec($"{HudSpriteDirectory}/hud_icon_building_interaction.png", UiPixelsPerUnit, CreateHudBuildingInteractionIconTexture));
            return specs.ToArray();
        }

        private static Texture2D CreateActorFrame(bool player, PresentationActorState state, int frame)
        {
            Texture2D texture = CreateTransparentTexture(player ? "PlayerFrame" : "RobotFrame", 32, 32);
            Color shell = player ? new Color(0.94f, 0.82f, 0.28f, 1f) : new Color(0.45f, 0.92f, 0.46f, 1f);
            Color visor = new Color(0.12f, 0.82f, 0.98f, 1f);
            Color accent = state == PresentationActorState.Destroyed
                ? new Color(0.98f, 0.28f, 0.24f, 1f)
                : state == PresentationActorState.Blocked
                    ? new Color(1f, 0.8f, 0.24f, 1f)
                    : state == PresentationActorState.Mining
                        ? new Color(1f, 0.42f, 0.24f, 1f)
                        : new Color(0.82f, 0.92f, 1f, 1f);

            int bob = state == PresentationActorState.Moving ? (frame == 0 ? 0 : 1) : 0;
            for (int y = 8; y <= 23; y++)
            {
                for (int x = 9; x <= 22; x++)
                {
                    bool edge = x == 9 || x == 22 || y == 8 || y == 23;
                    texture.SetPixel(x, y + bob, edge ? accent : shell);
                }
            }

            for (int y = 15; y <= 18; y++)
            {
                for (int x = 11; x <= 20; x++)
                {
                    texture.SetPixel(x, y + bob, visor);
                }
            }

            if (state == PresentationActorState.Mining)
            {
                for (int i = 0; i < 6; i++)
                {
                    texture.SetPixel(23 + i, 12 + i / 2 + bob, accent);
                }
            }
            else if (state == PresentationActorState.Blocked)
            {
                for (int i = 0; i < 5; i++)
                {
                    texture.SetPixel(6 + i, 24 - i + bob, accent);
                    texture.SetPixel(25 - i, 24 - i + bob, accent);
                }
            }
            else if (state == PresentationActorState.Destroyed)
            {
                for (int i = 0; i < 8; i++)
                {
                    texture.SetPixel(10 + i, 10 + i / 2, accent);
                    texture.SetPixel(18 + i / 2, 10 + i, accent);
                }
            }
            else
            {
                int footOffset = frame == 0 ? 0 : 1;
                texture.SetPixel(12, 6 + footOffset, accent);
                texture.SetPixel(19, 6 + (1 - footOffset), accent);
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateMetalPickupTexture()
        {
            return CreateGemTexture("MetalPickup", new Color(0.92f, 0.84f, 0.58f, 1f), new Color(0.56f, 0.42f, 0.16f, 1f));
        }

        private static Texture2D CreateEnergyPickupTexture()
        {
            return CreateGemTexture("EnergyPickup", new Color(0.4f, 0.94f, 1f, 1f), new Color(0.08f, 0.44f, 0.78f, 1f));
        }

        private static Texture2D CreateExperiencePickupTexture()
        {
            return CreateGemTexture("ExperiencePickup", new Color(0.72f, 0.96f, 0.4f, 1f), new Color(0.2f, 0.52f, 0.12f, 1f));
        }

        private static Texture2D CreateMiningCrackTexture(int frame)
        {
            Texture2D texture = CreateTransparentTexture("MiningCrack", 16, 16);
            Color crack = new Color(1f, 0.82f, 0.74f, 0.95f);
            int length = 6 + frame * 2;
            for (int i = 0; i < length; i++)
            {
                int x = 3 + i;
                int y = 12 - i / 2;
                if (x >= 0 && x < 16 && y >= 0 && y < 16)
                {
                    texture.SetPixel(x, y, crack);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateWallBreakTexture(int frame)
        {
            Texture2D texture = CreateTransparentTexture("WallBreak", 16, 16);
            Color shard = new Color(1f, 0.74f, 0.46f, 0.95f);
            int radius = 2 + frame;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int dx = x - 7;
                    int dy = y - 7;
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) == radius || (frame >= 2 && dx * dx + dy * dy <= radius * radius / 2))
                    {
                        texture.SetPixel(x, y, shard);
                    }
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateExplosionTexture(int frame)
        {
            Texture2D texture = CreateTransparentTexture("Explosion", 16, 16);
            Color hot = new Color(1f, 0.56f, 0.28f, 0.95f);
            Color core = new Color(1f, 0.92f, 0.62f, 0.95f);
            int radius = 2 + frame;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int dx = x - 7;
                    int dy = y - 7;
                    int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (distance <= radius)
                    {
                        texture.SetPixel(x, y, distance <= radius / 2 ? core : hot);
                    }
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateHudPanelBackgroundTexture()
        {
            Texture2D texture = CreateHudPanelBackgroundFromMockup();
            if (texture != null)
            {
                return texture;
            }

            texture = CreateTransparentTexture("HudPanelBackground", 32, 32);
            Color fill = new Color(0.08f, 0.11f, 0.13f, 0.92f);
            Color border = new Color(0.3f, 0.72f, 0.82f, 1f);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool edge = x < 2 || y < 2 || x >= 30 || y >= 30;
                    texture.SetPixel(x, y, edge ? border : fill);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateHudStatusIconTexture()
        {
            return CreateHudMockupIconTexture("HudStatusIcon", new RectInt(18, 39, 54, 53))
                ?? CreateRingIcon("HudStatusIcon", new Color(0.82f, 0.96f, 1f, 1f), new Color(0.18f, 0.72f, 0.86f, 1f));
        }

        private static Texture2D CreateHudInteractionIconTexture()
        {
            return CreateHudMockupIconTexture("HudInteractionIcon", new RectInt(709, 37, 60, 55))
                ?? CreateArrowIcon("HudInteractionIcon", new Color(0.96f, 0.88f, 0.68f, 1f), new Color(0.84f, 0.48f, 0.2f, 1f));
        }

        private static Texture2D CreateHudFeedbackIconTexture()
        {
            return CreateHudMockupIconTexture("HudFeedbackIcon", new RectInt(1389, 785, 42, 42))
                ?? CreateWaveIcon("HudFeedbackIcon", new Color(0.84f, 0.98f, 1f, 1f), new Color(0.18f, 0.76f, 0.92f, 1f));
        }

        private static Texture2D CreateHudWarningIconTexture()
        {
            return CreateHudMockupIconTexture("HudWarningIcon", new RectInt(1256, 36, 56, 56))
                ?? CreateTriangleIcon("HudWarningIcon", new Color(1f, 0.84f, 0.76f, 1f), new Color(1f, 0.38f, 0.28f, 1f));
        }

        private static Texture2D CreateHudUpgradeIconTexture()
        {
            return CreateHudMockupIconTexture("HudUpgradeIcon", new RectInt(34, 185, 52, 46))
                ?? CreatePlusIcon("HudUpgradeIcon", new Color(0.84f, 1f, 0.66f, 1f), new Color(0.28f, 0.68f, 0.18f, 1f));
        }

        private static Texture2D CreateHudBuildIconTexture()
        {
            return CreateHudMockupIconTexture("HudBuildIcon", new RectInt(941, 806, 58, 58))
                ?? CreateGridIcon("HudBuildIcon", new Color(0.96f, 0.92f, 0.72f, 1f), new Color(0.74f, 0.54f, 0.22f, 1f));
        }

        private static Texture2D CreateHudBuildingInteractionIconTexture()
        {
            return CreateHudMockupIconTexture("HudBuildingInteractionIcon", new RectInt(1528, 408, 34, 40))
                ?? CreateCogIcon("HudBuildingInteractionIcon", new Color(0.92f, 0.88f, 1f, 1f), new Color(0.5f, 0.4f, 0.82f, 1f));
        }

        private static Texture2D CreateHudPanelBackgroundFromMockup()
        {
            Texture2D source = LoadHudMockupSourceTexture();
            if (source == null)
            {
                return null;
            }

            const int size = 48;
            const int cornerSize = 12;
            Texture2D texture = CreateTransparentTexture("HudPanelBackground", size, size);

            RectInt topLeft = new RectInt(1346, 392, cornerSize, cornerSize);
            RectInt topRight = new RectInt(1596, 392, cornerSize, cornerSize);
            RectInt bottomLeft = new RectInt(1346, 548, cornerSize, cornerSize);
            RectInt bottomRight = new RectInt(1596, 548, cornerSize, cornerSize);
            RectInt fillRect = new RectInt(1544, 484, 4, 4);

            CopyPatchTopLeft(source, topLeft, texture, 0, 0);
            CopyPatchTopLeft(source, topRight, texture, size - cornerSize, 0);
            CopyPatchTopLeft(source, bottomLeft, texture, 0, size - cornerSize);
            CopyPatchTopLeft(source, bottomRight, texture, size - cornerSize, size - cornerSize);

            TileVerticalStripTopLeft(source, topLeft.x + cornerSize - 2, topLeft.y, 2, cornerSize, texture, cornerSize, 0, size - cornerSize * 2);
            TileVerticalStripTopLeft(source, bottomLeft.x + cornerSize - 2, bottomLeft.y, 2, cornerSize, texture, cornerSize, size - cornerSize, size - cornerSize * 2);
            TileHorizontalStripTopLeft(source, topLeft.x, topLeft.y + cornerSize - 2, cornerSize, 2, texture, 0, cornerSize, size - cornerSize * 2);
            TileHorizontalStripTopLeft(source, topRight.x, topRight.y + cornerSize - 2, cornerSize, 2, texture, size - cornerSize, cornerSize, size - cornerSize * 2);
            TilePatchTopLeft(source, fillRect, texture, cornerSize, cornerSize, size - cornerSize * 2, size - cornerSize * 2);

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateHudMockupIconTexture(string name, RectInt sourceRect)
        {
            Texture2D source = LoadHudMockupSourceTexture();
            if (source == null)
            {
                return null;
            }

            Texture2D texture = CropTextureTopLeft(source, sourceRect, name);
            ClearEdgeConnectedBackground(texture);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateGemTexture(string name, Color fill, Color border)
        {
            Texture2D texture = CreateTransparentTexture(name, 32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int distance = Mathf.Abs(x - 15) + Mathf.Abs(y - 15);
                    if (distance > 11)
                    {
                        continue;
                    }

                    texture.SetPixel(x, y, distance > 8 ? border : fill);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateRingIcon(string name, Color fill, Color border)
        {
            Texture2D texture = CreateTransparentTexture(name, 32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int dx = x - 15;
                    int dy = y - 15;
                    int distance = dx * dx + dy * dy;
                    if (distance >= 70 && distance <= 120)
                    {
                        texture.SetPixel(x, y, border);
                    }
                    else if (distance < 60)
                    {
                        texture.SetPixel(x, y, fill);
                    }
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateArrowIcon(string name, Color fill, Color border)
        {
            Texture2D texture = CreateTransparentTexture(name, 32, 32);
            for (int y = 7; y < 25; y++)
            {
                texture.SetPixel(10, y, border);
                texture.SetPixel(11, y, fill);
            }

            for (int i = 0; i < 9; i++)
            {
                texture.SetPixel(11 + i, 16 + i / 2, border);
                texture.SetPixel(11 + i, 15 - i / 2, border);
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateWaveIcon(string name, Color fill, Color border)
        {
            Texture2D texture = CreateTransparentTexture(name, 32, 32);
            for (int x = 5; x < 27; x++)
            {
                int y = 16 + Mathf.RoundToInt(Mathf.Sin((x - 5) / 4f) * 5f);
                texture.SetPixel(x, y, border);
                if (y + 1 < 32)
                {
                    texture.SetPixel(x, y + 1, fill);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateTriangleIcon(string name, Color fill, Color border)
        {
            Texture2D texture = CreateTransparentTexture(name, 32, 32);
            for (int y = 6; y < 26; y++)
            {
                int width = (y - 6) / 2;
                for (int x = 15 - width; x <= 15 + width; x++)
                {
                    bool edge = x == 15 - width || x == 15 + width || y == 25;
                    texture.SetPixel(x, y, edge ? border : fill);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreatePlusIcon(string name, Color fill, Color border)
        {
            Texture2D texture = CreateTransparentTexture(name, 32, 32);
            for (int i = 8; i < 24; i++)
            {
                texture.SetPixel(15, i, border);
                texture.SetPixel(16, i, fill);
                texture.SetPixel(i, 15, border);
                texture.SetPixel(i, 16, fill);
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateGridIcon(string name, Color fill, Color border)
        {
            Texture2D texture = CreateTransparentTexture(name, 32, 32);
            for (int y = 7; y < 25; y += 6)
            {
                for (int x = 7; x < 25; x++)
                {
                    texture.SetPixel(x, y, border);
                }
            }

            for (int x = 7; x < 25; x += 6)
            {
                for (int y = 7; y < 25; y++)
                {
                    texture.SetPixel(x, y, fill);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateCogIcon(string name, Color fill, Color border)
        {
            Texture2D texture = CreateTransparentTexture(name, 32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int dx = x - 15;
                    int dy = y - 15;
                    int distance = dx * dx + dy * dy;
                    if (distance <= 28)
                    {
                        texture.SetPixel(x, y, fill);
                    }
                    else if (distance <= 90 && ((x + y) % 6 < 2))
                    {
                        texture.SetPixel(x, y, border);
                    }
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateTransparentTexture(string name, int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private static Texture2D LoadHudMockupSourceTexture()
        {
            string fullPath = ToFullPath(HudMockupSourcePath);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "HudUiUxMockupSource",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }

            return texture;
        }

        private static Texture2D CropTextureTopLeft(Texture2D source, RectInt rect, string name)
        {
            Texture2D texture = CreateTransparentTexture(name, rect.width, rect.height);
            CopyPatchTopLeft(source, rect, texture, 0, 0);
            texture.Apply(false, false);
            return texture;
        }

        private static void CopyPatchTopLeft(Texture2D source, RectInt sourceRect, Texture2D destination, int destinationLeft, int destinationTop)
        {
            for (int y = 0; y < sourceRect.height; y++)
            {
                for (int x = 0; x < sourceRect.width; x++)
                {
                    SetPixelTopLeft(destination, destinationLeft + x, destinationTop + y, GetPixelTopLeft(source, sourceRect.x + x, sourceRect.y + y));
                }
            }
        }

        private static void TilePatchTopLeft(Texture2D source, RectInt sourceRect, Texture2D destination, int destinationLeft, int destinationTop, int destinationWidth, int destinationHeight)
        {
            for (int y = 0; y < destinationHeight; y++)
            {
                for (int x = 0; x < destinationWidth; x++)
                {
                    int sourceX = sourceRect.x + (x % sourceRect.width);
                    int sourceY = sourceRect.y + (y % sourceRect.height);
                    SetPixelTopLeft(destination, destinationLeft + x, destinationTop + y, GetPixelTopLeft(source, sourceX, sourceY));
                }
            }
        }

        private static void TileVerticalStripTopLeft(Texture2D source, int sourceLeft, int sourceTop, int stripWidth, int stripHeight, Texture2D destination, int destinationLeft, int destinationTop, int destinationWidth)
        {
            TilePatchTopLeft(source, new RectInt(sourceLeft, sourceTop, stripWidth, stripHeight), destination, destinationLeft, destinationTop, destinationWidth, stripHeight);
        }

        private static void TileHorizontalStripTopLeft(Texture2D source, int sourceLeft, int sourceTop, int stripWidth, int stripHeight, Texture2D destination, int destinationLeft, int destinationTop, int destinationHeight)
        {
            TilePatchTopLeft(source, new RectInt(sourceLeft, sourceTop, stripWidth, stripHeight), destination, destinationLeft, destinationTop, stripWidth, destinationHeight);
        }

        private static Color32 GetPixelTopLeft(Texture2D texture, int x, int y)
        {
            return texture.GetPixel(x, texture.height - 1 - y);
        }

        private static void SetPixelTopLeft(Texture2D texture, int x, int y, Color32 color)
        {
            texture.SetPixel(x, texture.height - 1 - y, color);
        }

        private static void ClearEdgeConnectedBackground(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            Color32[] pixels = texture.GetPixels32();
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();

            for (int x = 0; x < width; x++)
            {
                TryQueueBackgroundPixel(x, 0, width, pixels, visited, queue);
                TryQueueBackgroundPixel(x, height - 1, width, pixels, visited, queue);
            }

            for (int y = 0; y < height; y++)
            {
                TryQueueBackgroundPixel(0, y, width, pixels, visited, queue);
                TryQueueBackgroundPixel(width - 1, y, width, pixels, visited, queue);
            }

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                pixels[index].a = 0;
                int x = index % width;
                int y = index / width;
                TryQueueBackgroundPixel(x - 1, y, width, pixels, visited, queue);
                TryQueueBackgroundPixel(x + 1, y, width, pixels, visited, queue);
                TryQueueBackgroundPixel(x, y - 1, width, pixels, visited, queue);
                TryQueueBackgroundPixel(x, y + 1, width, pixels, visited, queue);
            }

            texture.SetPixels32(pixels);
        }

        private static void TryQueueBackgroundPixel(int x, int y, int width, Color32[] pixels, bool[] visited, Queue<int> queue)
        {
            int height = pixels.Length / width;
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }

            int index = y * width + x;
            if (visited[index])
            {
                return;
            }

            visited[index] = true;
            if (!LooksLikeHudBackground(pixels[index]))
            {
                return;
            }

            queue.Enqueue(index);
        }

        private static bool LooksLikeHudBackground(Color32 color)
        {
            if (color.a < 12)
            {
                return true;
            }

            return color.r <= 52 && color.g <= 62 && color.b <= 62;
        }

        private static string CreatePrompt()
        {
            return
                "# Minebot Prefab Gameplay Art Batch 001\n\n" +
                "- Scope: actor state frames, pickup icons, wall interaction FX and graphical HUD icons.\n" +
                "- Style: chunky pixel silhouettes, high-contrast readable shapes, no internal brick borders on crack / wall-break frames.\n" +
                "- Runtime targets: actor prefabs, pickup prefabs, cell FX prefabs, HUD panel/icon chrome.\n";
        }

        private static string CreateManifest()
        {
            return
                "# Minebot Prefab Gameplay Art Manifest 001\n\n" +
                "| Group | Assets |\n" +
                "| --- | --- |\n" +
                "| actors | `Assets/Art/Minebot/Sprites/Actors/States/*` |\n" +
                "| pickups | `Assets/Art/Minebot/Sprites/Pickups/*` |\n" +
                "| cell-fx | `Assets/Art/Minebot/Sprites/Effects/*` |\n" +
                "| hud | `Assets/Art/Minebot/Sprites/UI/HUD/*`, source: `Assets/Art/Minebot/Generated/Selected/minebot-hud-uiux-mockup-source.png` |\n" +
                "| prefabs | `Assets/Resources/Minebot/Presentation/**` |\n";
        }

        private static string CreateRecordTemplate()
        {
            return
                "# Prefab Gameplay Art Record\n\n" +
                "## Prompt\n- batch:\n- intent:\n\n" +
                "## Selection Notes\n- chosen sheet / rationale:\n- continuity checks for crack / wall-break frames:\n\n" +
                "## Final Assets\n- actor frames:\n- pickup icons:\n- cell fx frames:\n- hud graphics:\n\n" +
                "## Runtime Usage\n- actor prefabs:\n- pickup prefabs:\n- cell fx prefabs:\n- hud panels / icons:\n";
        }

        private readonly struct TextureSpec
        {
            public TextureSpec(string path, int pixelsPerUnit, Func<Texture2D> factory)
            {
                Path = path;
                PixelsPerUnit = pixelsPerUnit;
                Factory = factory;
            }

            public string Path { get; }
            public int PixelsPerUnit { get; }
            public Func<Texture2D> Factory { get; }
        }
    }
}
