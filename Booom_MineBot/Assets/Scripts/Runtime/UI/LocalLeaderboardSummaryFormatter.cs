using System.Collections.Generic;
using System.Text;
using Minebot.Progression;

namespace Minebot.UI
{
    public static class LocalLeaderboardSummaryFormatter
    {
        public static string Format(IReadOnlyList<LocalLeaderboardEntry> entries, string emptyText = "暂无成绩")
        {
            if (entries == null || entries.Count == 0)
            {
                return emptyText;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                LocalLeaderboardEntry entry = entries[i];
                builder.Append(i + 1)
                    .Append(". ")
                    .Append(entry.playerName)
                    .Append("  ")
                    .Append(entry.score)
                    .Append(" 分  波次 ")
                    .Append(entry.survivedWave);
            }

            return builder.ToString();
        }
    }
}
