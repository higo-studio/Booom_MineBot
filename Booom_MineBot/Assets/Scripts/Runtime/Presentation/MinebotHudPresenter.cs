using Minebot.Bootstrap;
using Minebot.Progression;
using Minebot.WaveSurvival;
using UnityEngine;

namespace Minebot.UI
{
    [MinebotRuntimeTag(MinebotRuntimeTag.Consumer)]
    public sealed class MinebotHudPresenter : MonoBehaviour
    {
        private GameSessionService session;
        private PlayerVitals vitals;
        private PlayerEconomy economy;
        private WaveSurvivalService waves;
        private bool isSubscribed;

        public string LastSummary { get; private set; }

        private void OnEnable()
        {
            EnsureServices();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void InjectServices(
            GameSessionService injectedSession,
            PlayerVitals injectedVitals,
            PlayerEconomy injectedEconomy,
            WaveSurvivalService injectedWaves,
            BootstrapConfig config)
        {
            if (ReferenceEquals(session, injectedSession)
                && ReferenceEquals(vitals, injectedVitals)
                && ReferenceEquals(economy, injectedEconomy)
                && ReferenceEquals(waves, injectedWaves))
            {
                return;
            }

            Unsubscribe();
            session = injectedSession;
            vitals = injectedVitals;
            economy = injectedEconomy;
            waves = injectedWaves;
            Subscribe();
            Refresh();
        }

        public void Refresh()
        {
            LastSummary = BuildDebugSummary();
        }

        public string BuildDebugSummary()
        {
            if (vitals == null || economy == null || waves == null)
            {
                return "游戏服务尚未初始化。";
            }

            return $"生命 {vitals.CurrentHealth}/{vitals.MaxHealth} | 金属 {economy.Resources.Metal} | 能量 {economy.Resources.Energy} | 波次 {waves.CurrentWave}";
        }

        private void EnsureServices()
        {
            if (session != null && vitals != null && economy != null && waves != null)
            {
                return;
            }

            MinebotRuntimeDiscovery.TryInjectInto(this);
            Minebot.Presentation.MinebotGameplayPresentation presentation = GetComponentInParent<Minebot.Presentation.MinebotGameplayPresentation>();
            if (presentation != null && presentation.Services != null)
            {
                AdoptRegistry(presentation.Services);
            }
        }

        private void Subscribe()
        {
            if (isSubscribed || session == null)
            {
                return;
            }

            session.StateChanged += Refresh;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || session == null)
            {
                return;
            }

            session.StateChanged -= Refresh;
            isSubscribed = false;
        }

        private void AdoptRegistry(RuntimeServiceRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            session = registry.Session;
            vitals = registry.Vitals;
            economy = registry.Economy;
            waves = registry.Waves;
        }
    }
}
