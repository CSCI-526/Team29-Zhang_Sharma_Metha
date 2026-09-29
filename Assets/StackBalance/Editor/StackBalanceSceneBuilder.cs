#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StackBalance.Editor
{
    public static class StackBalanceSceneBuilder
    {
        private const string SceneDirectory = "Assets/StackBalance/Scenes";
        private const string ScenePath = SceneDirectory + "/StackBalance.unity";
        private const string GeneratedDirectory = "Assets/StackBalance/Generated";
        private const string SquareSpritePath = GeneratedDirectory + "/WhiteSquare.png";

        private static Sprite squareSprite;

        [MenuItem("Tools/Stack Balance/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                    "Replace Stack Balance scene?",
                    "The scene already exists. Replace it with a newly generated prototype scene?",
                    "Replace", "Cancel"))
            {
                return;
            }

            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "StackBalance";
            squareSprite = EnsureSquareSpriteAsset();

            Camera camera = CreateCamera();
            CreateWorldDecor();

            GameObject systems = new GameObject("Game Systems");
            TowerBalanceSystem balanceSystem = systems.AddComponent<TowerBalanceSystem>();

            GameObject towerRootObject = new GameObject("Tower");

            Canvas canvas = CreateCanvas();
            CreateEventSystem();
            UIReferences ui = CreateUI(canvas.transform);

            GameUIController gameUI = canvas.gameObject.AddComponent<GameUIController>();
            gameUI.Configure(ui.score, ui.height, ui.weight, ui.feedback,
                ui.gameOverPanel, ui.gameOverReason, ui.finalScore, ui.restartButton,
                ui.endGameButton);

            BalanceMeterUI meterUI = ui.meterRoot.gameObject.AddComponent<BalanceMeterUI>();
            meterUI.Configure(ui.pointer, ui.meterTrack, ui.balanceValue);

            TowerCameraController cameraController = camera.gameObject.AddComponent<TowerCameraController>();
            cameraController.Configure(0f, 2f, 2.5f);

            StackGameManager manager = systems.AddComponent<StackGameManager>();
            manager.Configure(towerRootObject.transform, balanceSystem, meterUI, gameUI, cameraController);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = systems;
            EditorUtility.DisplayDialog("Stack Balance",
                "Prototype scene created and added to Build Settings. Open StackBalance.unity and press Play.", "OK");
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.14f);
            return camera;
        }

        private static void CreateWorldDecor()
        {
            CreateWorldRectangle("Ground", new Vector2(0f, -3.75f),
                new Vector2(9f, 0.35f), new Color(0.18f, 0.2f, 0.27f), 0);
            GameObject pivot = CreateWorldRectangle("Central Pivot", new Vector2(0f, -3.48f),
                new Vector2(0.38f, 0.38f), new Color(0.95f, 0.95f, 0.95f), 1);
            pivot.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private static GameObject CreateWorldRectangle(string name, Vector2 position,
            Vector2 scale, Color color, int sortingOrder)
        {
            GameObject item = new GameObject(name);
            item.transform.position = position;
            item.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return item;
        }

        private static Sprite EnsureSquareSpriteAsset()
        {
            if (!Directory.Exists(GeneratedDirectory))
            {
                Directory.CreateDirectory(GeneratedDirectory);
            }

            if (!File.Exists(SquareSpritePath))
            {
                Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[64];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(SquareSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SquareSpritePath);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SquareSpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 8f;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Game UI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static UIReferences CreateUI(Transform canvas)
        {
            UIReferences ui = new UIReferences();
            ui.score = CreateText(canvas, "Score", "Score: 0", 34, TextAnchor.MiddleLeft,
                new Vector2(30f, -25f), new Vector2(310f, 60f), AnchorPreset.TopLeft);
            ui.height = CreateText(canvas, "Height", "Height: 0", 34, TextAnchor.MiddleLeft,
                new Vector2(30f, -85f), new Vector2(310f, 60f), AnchorPreset.TopLeft);
            ui.weight = CreateText(canvas, "Current Weight", "Current: Normal", 34, TextAnchor.MiddleRight,
                new Vector2(-30f, -25f), new Vector2(390f, 60f), AnchorPreset.TopRight);
            ui.feedback = CreateText(canvas, "Feedback", string.Empty, 46, TextAnchor.MiddleCenter,
                new Vector2(0f, -150f), new Vector2(500f, 70f), AnchorPreset.TopCenter);
            ui.feedback.color = new Color(1f, 0.85f, 0.2f);

            Text instructions = CreateText(canvas, "Instructions",
                "Click or press SPACE to drop", 27, TextAnchor.MiddleCenter,
                new Vector2(0f, 30f), new Vector2(540f, 50f), AnchorPreset.BottomCenter);
            instructions.color = new Color(0.85f, 0.87f, 0.92f);

            ui.meterRoot = CreateRect(canvas, "Balance Meter", new Vector2(0f, -35f),
                new Vector2(420f, 90f), AnchorPreset.TopCenter);
            ui.balanceValue = CreateText(ui.meterRoot, "Balance Value", "Balance: SAFE  (0.0)", 25,
                TextAnchor.MiddleCenter, new Vector2(0f, 30f), new Vector2(420f, 35f), AnchorPreset.Center);
            ui.meterTrack = CreateRect(ui.meterRoot, "Track", new Vector2(0f, -18f),
                new Vector2(360f, 26f), AnchorPreset.Center);
            CreateImage(ui.meterTrack, "Left Danger", new Color(0.85f, 0.18f, 0.18f),
                new Vector2(-120f, 0f), new Vector2(120f, 26f));
            CreateImage(ui.meterTrack, "Safe", new Color(0.18f, 0.72f, 0.35f),
                Vector2.zero, new Vector2(120f, 26f));
            CreateImage(ui.meterTrack, "Right Danger", new Color(0.85f, 0.18f, 0.18f),
                new Vector2(120f, 0f), new Vector2(120f, 26f));
            ui.pointer = CreateImage(ui.meterTrack, "Pointer", Color.white,
                Vector2.zero, new Vector2(8f, 42f)).rectTransform;

            ui.gameOverPanel = CreatePanel(canvas, "Game Over Panel",
                new Color(0.035f, 0.045f, 0.07f, 0.96f), Vector2.zero, new Vector2(650f, 390f));
            ui.gameOverReason = CreateText(ui.gameOverPanel.transform, "Reason", "Game Over", 48,
                TextAnchor.MiddleCenter, new Vector2(0f, 95f), new Vector2(590f, 100f), AnchorPreset.Center);
            ui.finalScore = CreateText(ui.gameOverPanel.transform, "Final Score", "Final score: 0", 36,
                TextAnchor.MiddleCenter, new Vector2(0f, 20f), new Vector2(500f, 70f), AnchorPreset.Center);
            ui.restartButton = CreateButton(ui.gameOverPanel.transform, "Restart Button", "Restart",
                new Vector2(-145f, -90f), new Vector2(240f, 75f));
            ui.endGameButton = CreateButton(ui.gameOverPanel.transform, "End Game Button", "End Game",
                new Vector2(145f, -90f), new Vector2(240f, 75f));
            ui.gameOverPanel.SetActive(false);
            return ui;
        }

        private static Text CreateText(Transform parent, string name, string content, int size,
            TextAnchor alignment, Vector2 position, Vector2 dimensions, AnchorPreset anchor)
        {
            RectTransform rect = CreateRect(parent, name, position, dimensions, anchor);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color,
            Vector2 position, Vector2 dimensions)
        {
            RectTransform rect = CreateRect(parent, name, position, dimensions, AnchorPreset.Center);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect.gameObject;
        }

        private static Image CreateImage(Transform parent, string name, Color color,
            Vector2 position, Vector2 dimensions)
        {
            RectTransform rect = CreateRect(parent, name, position, dimensions, AnchorPreset.Center);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 position, Vector2 dimensions)
        {
            RectTransform rect = CreateRect(parent, name, position, dimensions, AnchorPreset.Center);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.1f, 0.62f, 0.75f);
            Button button = rect.gameObject.AddComponent<Button>();
            Text text = CreateText(rect, "Label", label, 34, TextAnchor.MiddleCenter,
                Vector2.zero, dimensions, AnchorPreset.Center);
            text.raycastTarget = false;
            return button;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 position,
            Vector2 dimensions, AnchorPreset anchor)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetAnchor(rect, anchor);
            rect.sizeDelta = dimensions;
            rect.anchoredPosition = position;
            return rect;
        }

        private static void SetAnchor(RectTransform rect, AnchorPreset anchor)
        {
            Vector2 value;
            switch (anchor)
            {
                case AnchorPreset.TopLeft: value = new Vector2(0f, 1f); break;
                case AnchorPreset.TopCenter: value = new Vector2(0.5f, 1f); break;
                case AnchorPreset.TopRight: value = new Vector2(1f, 1f); break;
                case AnchorPreset.BottomCenter: value = new Vector2(0.5f, 0f); break;
                default: value = new Vector2(0.5f, 0.5f); break;
            }
            rect.anchorMin = value;
            rect.anchorMax = value;
            rect.pivot = value;
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private enum AnchorPreset { TopLeft, TopCenter, TopRight, BottomCenter, Center }

        private sealed class UIReferences
        {
            public Text score;
            public Text height;
            public Text weight;
            public Text feedback;
            public RectTransform meterRoot;
            public RectTransform meterTrack;
            public RectTransform pointer;
            public Text balanceValue;
            public GameObject gameOverPanel;
            public Text gameOverReason;
            public Text finalScore;
            public Button restartButton;
            public Button endGameButton;
        }
    }
}
#endif
