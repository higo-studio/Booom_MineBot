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
                return "游戏服务尚未初始化。";
            }

            RuntimeServiceRegistry services = MinebotServices.Current;
            return $"生命 {services.Vitals.CurrentHealth}/{services.Vitals.MaxHealth} | 金属 {services.Economy.Resources.Metal} | 能量 {services.Economy.Resources.Energy} | 波次 {services.Waves.CurrentWave}";
        }
    }
}
