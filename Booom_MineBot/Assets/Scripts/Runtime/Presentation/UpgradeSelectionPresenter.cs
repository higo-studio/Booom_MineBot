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

        private UpgradeSelectionService upgrades;
        private GameSessionService session;
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

        public void InjectServices(
            UpgradeSelectionService injectedUpgrades,
            GameSessionService injectedSession,
            BootstrapConfig config)
        {
            if (ReferenceEquals(upgrades, injectedUpgrades) && ReferenceEquals(session, injectedSession))
            {
                return;
            }

            Unsubscribe();
            upgrades = injectedUpgrades;
            session = injectedSession;
            Subscribe();
            Refresh();
        }

        public void Refresh()
        {
            if (upgrades == null)
            {
                IsShowing = false;
                CurrentCandidates = System.Array.Empty<UpgradeDefinition>();
                return;
            }

            CurrentCandidates = upgrades.GetCandidates(candidateCount);
            IsShowing = CurrentCandidates.Length > 0;
        }

        public bool Select(int index)
        {
            if (!IsShowing || index < 0 || index >= CurrentCandidates.Length)
            {
                return false;
            }

            bool selected = upgrades.Select(CurrentCandidates[index]);
            Refresh();
            return selected;
        }

        private void EnsureServices()
        {
            if (upgrades != null && session != null)
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

            upgrades = registry.Upgrades;
            session = registry.Session;
        }
    }
}
