using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Minebot.Presentation
{
    internal static class DualGridFogBurst
    {
        [BurstCompile]
        private struct ClassifyFogBandsJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<byte> SolidMask;

            [ReadOnly]
            public NativeArray<byte> RevealedMask;

            [WriteOnly]
            public NativeArray<byte> NearMask;

            [WriteOnly]
            public NativeArray<byte> DeepMask;

            public int Width;
            public int Height;

            public void Execute(int index)
            {
                if (SolidMask[index] == 0)
                {
                    NearMask[index] = 0;
                    DeepMask[index] = 0;
                    return;
                }

                int x = index % Width;
                int y = index / Width;
                int minX = x > 0 ? x - 1 : 0;
                int maxX = x < Width - 1 ? x + 1 : Width - 1;
                int minY = y > 0 ? y - 1 : 0;
                int maxY = y < Height - 1 ? y + 1 : Height - 1;

                byte near = 0;
                for (int neighborY = minY; neighborY <= maxY && near == 0; neighborY++)
                {
                    int rowStart = neighborY * Width;
                    for (int neighborX = minX; neighborX <= maxX; neighborX++)
                    {
                        if (neighborX == x && neighborY == y)
                        {
                            continue;
                        }

                        if (RevealedMask[rowStart + neighborX] != 0)
                        {
                            near = 1;
                            break;
                        }
                    }
                }

                NearMask[index] = near;
                DeepMask[index] = near == 0 ? (byte)1 : (byte)0;
            }
        }

        public static void ClassifyAll(
            int width,
            int height,
            NativeArray<byte> solidMask,
            NativeArray<byte> revealedMask,
            NativeArray<byte> nearMask,
            NativeArray<byte> deepMask)
        {
            int cellCount = width * height;
            if (cellCount <= 0)
            {
                return;
            }

            var job = new ClassifyFogBandsJob
            {
                SolidMask = solidMask,
                RevealedMask = revealedMask,
                NearMask = nearMask,
                DeepMask = deepMask,
                Width = width,
                Height = height
            };

            job.Run(cellCount);
        }
    }
}
