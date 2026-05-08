using Minebot.GridMining;

namespace Minebot.Progression
{
    public sealed class ScoreService
    {
        private readonly ScoreConfig config;

        public ScoreService(ScoreConfig config)
        {
            this.config = config;
        }

        public int CurrentScore { get; private set; }

        public void AddManualWallBreak(HardnessTier hardnessTier)
        {
            CurrentScore += config != null
                ? config.ScoreForWall(hardnessTier)
                : DefaultWallScore(hardnessTier);
        }

        public void AddEarthquakeBombs(int bombCount)
        {
            if (bombCount <= 0)
            {
                return;
            }

            int perBomb = config != null
                ? config.EarthquakeBombScore
                : ScoreConfig.DefaultEarthquakeBombScore;
            CurrentScore += perBomb * bombCount;
        }

        public void AddWaveSurvived()
        {
            CurrentScore += config != null
                ? config.WaveSurvivedScore
                : ScoreConfig.DefaultWaveSurvivedScore;
        }

        public void AddBuildingConstructed(string buildingId)
        {
            CurrentScore += config != null
                ? config.ScoreForBuilding(buildingId)
                : ScoreConfig.DefaultBuildingScore;
        }

        private static int DefaultWallScore(HardnessTier hardnessTier)
        {
            return hardnessTier switch
            {
                HardnessTier.Soil => ScoreConfig.DefaultSoilWallScore,
                HardnessTier.Stone => ScoreConfig.DefaultStoneWallScore,
                HardnessTier.HardRock => ScoreConfig.DefaultHardRockWallScore,
                HardnessTier.UltraHard => ScoreConfig.DefaultUltraHardWallScore,
                _ => ScoreConfig.DefaultSoilWallScore
            };
        }
    }
}
