using System;

namespace Minebot.Common
{
    public sealed class DeterministicRandom
    {
        private uint state;

        public DeterministicRandom(int seed)
        {
            state = seed == 0 ? 0x6d2b79f5u : unchecked((uint)seed);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            uint value = NextUInt();
            return minInclusive + (int)(value % (uint)(maxExclusive - minInclusive));
        }

        public float Value()
        {
            return (NextUInt() & 0x00ffffff) / 16777216f;
        }

        private uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }
    }
}
