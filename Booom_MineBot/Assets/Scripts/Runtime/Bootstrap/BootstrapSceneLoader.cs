using System.Collections.Generic;
using Minebot.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        private bool gameplayLoadRequested;

        public BootstrapConfig Config => config;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            MinebotServices.Initialize(config);
        }

        private void Start()
        {
            if (!loadGameplayScene)
            {
                return;
            }

            if (showStartPage)
            {
                return;
            }

            string sceneName = config != null ? config.GameplaySceneName : "Gameplay";
            if (SceneManager.GetActiveScene().name != sceneName)
            {
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
        }

        private void OnGUI()
        {
            string gameplaySceneName = config != null ? config.GameplaySceneName : "Gameplay";
            if (!showStartPage || SceneManager.GetActiveScene().name == gameplaySceneName)
            {
                return;
            }

            Rect panel = new Rect(
                Mathf.Max(24f, (Screen.width - 520f) * 0.5f),
                Mathf.Max(24f, (Screen.height - 420f) * 0.5f),
                Mathf.Min(520f, Screen.width - 48f),
                380f);

            GUILayout.BeginArea(panel, GUI.skin.window);
            GUILayout.Label("BOOOM MINEBOT");
            GUILayout.Label("启动页");
            GUILayout.Space(12f);

            using (new GUILayout.HorizontalScope())
            {
                GUI.enabled = !gameplayLoadRequested;
                if (GUILayout.Button("开始游戏", GUILayout.Height(40f)))
                {
                    gameplayLoadRequested = true;
                    SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
                }

                GUI.enabled = true;
                if (GUILayout.Button("退出游戏", GUILayout.Height(40f)))
                {
                    Debug.Log("[BootstrapSceneLoader] 收到退出请求。");
                    Application.Quit();
                }
            }

            GUILayout.Space(16f);
            GUILayout.Label("本地前十");
            IReadOnlyList<LocalLeaderboardEntry> entries = LocalLeaderboardService.GetEntries();
            if (entries.Count == 0)
            {
                GUILayout.Label("暂无成绩");
            }
            else
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    LocalLeaderboardEntry entry = entries[i];
                    GUILayout.Label($"{i + 1}. {entry.playerName}  {entry.score} 分  波次 {entry.survivedWave}");
                }
            }

            GUILayout.EndArea();
        }
    }
}
