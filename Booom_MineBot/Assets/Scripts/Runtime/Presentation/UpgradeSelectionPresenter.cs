using Minebot.Bootstrap;
using Minebot.Progression;
using UnityEngine;

namespace Minebot.UI
{
    [MinebotRuntimeTag(MinebotRuntimeTag.Consumer)]
    public sealed class UpgradeSelectionPresenter : MonoBehaviour
    {
        [SerializeField]
        private int candidateCount = 3;

        private RuntimeServiceRegistry services;
        private bool isSubscribed;

        public UpgradeDefinition[] CurrentCandidates { get; private set; } = System.Array.Empty<UpgradeDefinition>();
        public bool IsShowing { get; private set; }

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
            if (services == null)
            {
                IsShowing = false;
                CurrentCandidates = System.Array.Empty<UpgradeDefinition>();
                return;
            }

            CurrentCandidates = services.Upgrades.GetCandidates(candidateCount);
            IsShowing = CurrentCandidates.Length > 0;
        }

        public bool Select(int index)
        {
            if (!IsShowing || index < 0 || index >= CurrentCandidates.Length)
            {
                return false;
            }

            bool selected = services.Upgrades.Select(CurrentCandidates[index]);
            Refresh();
            return selected;
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
