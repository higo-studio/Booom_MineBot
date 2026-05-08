using Minebot.Bootstrap;
using UnityEngine;

namespace Minebot.UI
{
    [MinebotRuntimeTag(MinebotRuntimeTag.Consumer)]
    public sealed class MinebotHudPresenter : MonoBehaviour
    {
        private RuntimeServiceRegistry services;
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

        public void InjectServices(RuntimeServiceRegistry injectedServices, BootstrapConfig config)
        {
            if (ReferenceEquals(services, injectedServices))
            {
                return;
            }

            Unsubscribe();
            services = injectedServices;
            Subscribe();
            Refresh();
        }

        public void Refresh()
        {
            LastSummary = BuildDebugSummary();
        }

        public string BuildDebugSummary()
        {
            if (services == null)
            {
                return "游戏服务尚未初始化。";
            }

            return $"生命 {services.Vitals.CurrentHealth}/{services.Vitals.MaxHealth} | 金属 {services.Economy.Resources.Metal} | 能量 {services.Economy.Resources.Energy} | 波次 {services.Waves.CurrentWave}";
        }

        private void EnsureServices()
        {
            if (services != null)
            {
                return;
            }

            Minebot.Presentation.MinebotGameplayPresentation presentation = GetComponentInParent<Minebot.Presentation.MinebotGameplayPresentation>();
            if (presentation != null && presentation.Services != null)
            {
                services = presentation.Services;
                return;
            }

            if (MinebotRuntimeDiscovery.TryResolveRuntimeServices(out RuntimeServiceRegistry runtimeServices, out _))
            {
                services = runtimeServices;
            }
        }

        private void Subscribe()
        {
            if (isSubscribed || services?.Session == null)
            {
                return;
            }

            services.Session.StateChanged += Refresh;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || services?.Session == null)
            {
                return;
            }

            services.Session.StateChanged -= Refresh;
            isSubscribed = false;
        }
    }
}
