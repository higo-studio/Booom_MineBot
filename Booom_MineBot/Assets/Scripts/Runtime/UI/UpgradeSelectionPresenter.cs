using Minebot.Bootstrap;
using Minebot.Progression;
using UnityEngine;

namespace Minebot.UI
{
    public sealed class UpgradeSelectionPresenter : MonoBehaviour
    {
        [SerializeField]
        private int candidateCount = 3;

        public UpgradeDefinition[] CurrentCandidates { get; private set; } = System.Array.Empty<UpgradeDefinition>();
        public bool IsShowing { get; private set; }

        private void OnEnable()
        {
            if (MinebotServices.IsInitialized)
            {
                MinebotServices.Current.Session.StateChanged += Refresh;
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (MinebotServices.IsInitialized)
            {
                MinebotServices.Current.Session.StateChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            if (!MinebotServices.IsInitialized)
            {
                IsShowing = false;
                CurrentCandidates = System.Array.Empty<UpgradeDefinition>();
                return;
            }

            CurrentCandidates = MinebotServices.Current.Upgrades.GetCandidates(candidateCount);
            IsShowing = CurrentCandidates.Length > 0;
        }

        public bool Select(int index)
        {
            if (!IsShowing || index < 0 || index >= CurrentCandidates.Length)
            {
                return false;
            }

            bool selected = MinebotServices.Current.Upgrades.Select(CurrentCandidates[index]);
            Refresh();
            return selected;
        }
    }
}
