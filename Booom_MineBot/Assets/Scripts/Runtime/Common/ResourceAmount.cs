using System;
using UnityEngine;

namespace Minebot.Common
{
    [Serializable]
    public struct ResourceAmount
    {
        [SerializeField]
        private int metal;

        [SerializeField]
        private int energy;

        [SerializeField]
        private int experience;

        public ResourceAmount(int metal, int energy, int experience)
        {
            this.metal = Math.Max(0, metal);
            this.energy = Math.Max(0, energy);
            this.experience = Math.Max(0, experience);
        }

        public int Metal => metal;
        public int Energy => energy;
        public int Experience => experience;

        public static ResourceAmount Zero => new ResourceAmount(0, 0, 0);

        public static ResourceAmount operator +(ResourceAmount left, ResourceAmount right)
        {
            return new ResourceAmount(
                left.Metal + right.Metal,
                left.Energy + right.Energy,
                left.Experience + right.Experience);
        }

        public bool CanAfford(ResourceAmount cost)
        {
            return Metal >= cost.Metal && Energy >= cost.Energy && Experience >= cost.Experience;
        }

        public ResourceAmount Spend(ResourceAmount cost)
        {
            if (!CanAfford(cost))
            {
                throw new InvalidOperationException("Insufficient resources.");
            }

            return new ResourceAmount(Metal - cost.Metal, Energy - cost.Energy, Experience - cost.Experience);
        }
    }
}
