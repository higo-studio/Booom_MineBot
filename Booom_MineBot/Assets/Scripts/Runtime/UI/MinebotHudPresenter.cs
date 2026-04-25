using Minebot.Bootstrap;
using UnityEngine;

namespace Minebot.UI
{
    public sealed class MinebotHudPresenter : MonoBehaviour
    {
        public string LastSummary { get; private set; }

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
            LastSummary = BuildDebugSummary();
        }

        public string BuildDebugSummary()
        {
            if (!MinebotServices.IsInitialized)
            {
                return "Minebot services are not initialized.";
            }

            RuntimeServiceRegistry services = MinebotServices.Current;
            return $"HP {services.Vitals.CurrentHealth}/{services.Vitals.MaxHealth} | Metal {services.Economy.Resources.Metal} | Energy {services.Economy.Resources.Energy} | Wave {services.Waves.CurrentWave}";
        }
    }
}
