using Minebot.Common;

namespace Minebot.Progression
{
    public sealed class PlayerEconomy
    {
        public PlayerEconomy(ResourceAmount startingResources)
        {
            Resources = startingResources;
        }

        public ResourceAmount Resources { get; private set; }

        public void Add(ResourceAmount amount)
        {
            Resources += amount;
        }

        public bool TrySpend(ResourceAmount cost)
        {
            if (!Resources.CanAfford(cost))
            {
                return false;
            }

            Resources = Resources.Spend(cost);
            return true;
        }
    }
}
