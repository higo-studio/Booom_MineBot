using System.Collections.Generic;
using Minebot.Progression;
using Minebot.UI;
using UnityEngine;
using UnityEngine.EventSystems;
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

            EnsureEventSystem();

            if (startPageView == null)
            {
                MinebotBootstrapMenuView prefab = startPagePrefab != null
                    ? startPagePrefab
                    : Resources.Load<MinebotBootstrapMenuView>(MinebotHudDefaults.BootstrapMenuResourcePath);
                startPageView = prefab != null
                    ? Instantiate(prefab)
                    : new GameObject(MinebotHudDefaults.BootstrapMenuObjectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MinebotBootstrapMenuView)).GetComponent<MinebotBootstrapMenuView>();
                startPageView.name = MinebotHudDefaults.BootstrapMenuObjectName;
            }

            startPageView.EnsureDefaultStructure(MinebotHudFontUtility.GetDefaultFontAsset(), MinebotHudDefaults.BootstrapMenu);
            startPageView.BindButtons(HandleStartClicked, HandleQuitClicked);
            startPageView.SetLeaderboardSummary(BuildLeaderboardSummary());
            startPageView.SetLoadingState(gameplayLoadRequested);
            startPageView.SetVisible(true);
        }

        private void DestroyStartPageView()
        {
            if (startPageView == null)
            {
                return;
            }

            Destroy(startPageView.gameObject);
            startPageView = null;
        }

        private void HandleStartClicked()
        {
            if (gameplayLoadRequested)
            {
                return;
            }

            gameplayLoadRequested = true;
            startPageView?.SetLoadingState(true);
            SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Single);
        }

        private static void HandleQuitClicked()
        {
            Debug.Log("[BootstrapSceneLoader] 收到退出请求。");
            Application.Quit();
        }

        private static string BuildLeaderboardSummary()
        {
            IReadOnlyList<LocalLeaderboardEntry> entries = LocalLeaderboardService.GetEntries();
            return LocalLeaderboardSummaryFormatter.Format(entries);
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
