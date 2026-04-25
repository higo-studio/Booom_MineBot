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

            string sceneName = config != null ? config.GameplaySceneName : "Gameplay";
            if (SceneManager.GetActiveScene().name != sceneName)
            {
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
        }
    }
}
