using Minebot.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Minebot.Bootstrap
{
    public sealed class BootstrapSceneLoader : MonoBehaviour
    {
        [SerializeField]
        private BootstrapConfig config;

        [SerializeField]
        private bool loadGameplayScene = true;

        [SerializeField]
        private bool showStartPage = false;

        [SerializeField]
        private MinebotBootstrapMenuView startPagePrefab;

        private bool gameplayLoadRequested;
        private MinebotBootstrapMenuView startPageView;
        private RectTransform startPageCanvasRoot;
        private MinebotRuntimeContext runtimeContext;

        public BootstrapConfig Config => config;
        public RuntimeServiceRegistry Services => runtimeContext != null ? runtimeContext.Services : null;
        public MinebotRuntimeContext RuntimeContext => runtimeContext;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            runtimeContext = GetComponent<MinebotRuntimeContext>();
            if (runtimeContext == null)
            {
                runtimeContext = gameObject.AddComponent<MinebotRuntimeContext>();
            }

            runtimeContext.Initialize(config);
            runtimeContext.InjectIntoScene(SceneManager.GetActiveScene());
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            if (!loadGameplayScene)
            {
                return;
            }

            if (showStartPage)
            {
                EnsureStartPageView();
                return;
            }

            if (SceneManager.GetActiveScene().name != GameplaySceneName)
            {
                SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Single);
            }
        }

        private string GameplaySceneName => config != null ? config.GameplaySceneName : "Gameplay";

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            runtimeContext?.InjectIntoScene(scene);

            if (!showStartPage || scene.name == GameplaySceneName)
            {
                DestroyStartPageView();
                return;
            }

            gameplayLoadRequested = false;
            EnsureStartPageView();
        }

        private void EnsureStartPageView()
        {
            if (SceneManager.GetActiveScene().name == GameplaySceneName)
            {
                return;
            }

            if (startPageView == null)
            {
                if (startPagePrefab == null)
                {
                    Debug.LogError("BootstrapSceneLoader 缺少 startPagePrefab。请在场景里显式引用 TitlePage.prefab。");
                    return;
                }

                EnsureStartPageCanvasRoot();
                startPageView = Instantiate(startPagePrefab, startPageCanvasRoot, false);
                if (startPageView.transform is RectTransform rectTransform)
                {
                    StretchToParent(rectTransform);
                }

                if (!startPageView.HasRequiredBindings(out string missingBindings))
                {
                    Debug.LogError($"TitlePage.prefab 缺少 MinebotBootstrapMenuView 必需引用：{missingBindings}");
                }
            }

            startPageView.BindButtons(HandleStartClicked, HandleQuitClicked);
            startPageView.SetVisible(true);
        }

        private void DestroyStartPageView()
        {
            if (startPageView == null)
            {
                DestroyStartPageCanvasRoot();
                return;
            }

            Destroy(startPageView.gameObject);
            startPageView = null;
            DestroyStartPageCanvasRoot();
        }

        private void EnsureStartPageCanvasRoot()
        {
            if (startPageCanvasRoot != null)
            {
                return;
            }

            var root = new GameObject("BootstrapUiRoot", typeof(RectTransform));
            root.layer = 5;
            root.transform.SetParent(transform, false);
            ConfigureCanvasRoot(root);
            startPageCanvasRoot = root.GetComponent<RectTransform>();
        }

        private void DestroyStartPageCanvasRoot()
        {
            if (startPageCanvasRoot == null)
            {
                return;
            }

            Destroy(startPageCanvasRoot.gameObject);
            startPageCanvasRoot = null;
        }

        private static void ConfigureCanvasRoot(GameObject target)
        {
            if (target.transform is RectTransform rootRect)
            {
                StretchToParent(rootRect);
            }

            Canvas canvas = GetOrAdd<Canvas>(target);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = GetOrAdd<CanvasScaler>(target);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAdd<GraphicRaycaster>(target);
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private void HandleStartClicked()
        {
            if (gameplayLoadRequested)
            {
                return;
            }

            gameplayLoadRequested = true;
            SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Single);
        }

        private static void HandleQuitClicked()
        {
            Debug.Log("[BootstrapSceneLoader] 收到退出请求。");
            Application.Quit();
        }

    }
}
