using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minebot.Progression
{
    [Serializable]
    public sealed class LocalLeaderboardEntry
    {
        public string playerName;
        public int score;
        public int survivedWave;
        public string savedAtUtc;
    }

    [Serializable]
    internal sealed class LocalLeaderboardData
    {
        public List<LocalLeaderboardEntry> entries = new List<LocalLeaderboardEntry>();
    }

    public static class LocalLeaderboardService
    {
        public const int MaxEntries = 10;
        private const string LeaderboardPrefsKey = "minebot.local_leaderboard.v1";

        public static IReadOnlyList<LocalLeaderboardEntry> GetEntries()
        {
            return LoadData().entries;
        }

        public static bool WouldQualify(int score)
        {
            List<LocalLeaderboardEntry> entries = LoadData().entries;
            if (entries.Count < MaxEntries)
            {
                return true;
            }

            return score > entries[entries.Count - 1].score;
        }

        public static bool TryAddEntry(string playerName, int score, int survivedWave, out int rank)
        {
            LocalLeaderboardData data = LoadData();
            data.entries.Add(new LocalLeaderboardEntry
            {
                playerName = SanitizeName(playerName),
                score = Mathf.Max(0, score),
                survivedWave = Mathf.Max(0, survivedWave),
                savedAtUtc = DateTime.UtcNow.ToString("O")
            });

            data.entries.Sort(CompareEntries);
            if (data.entries.Count > MaxEntries)
            {
                data.entries.RemoveRange(MaxEntries, data.entries.Count - MaxEntries);
            }

            SaveData(data);
            rank = FindRank(data.entries, score, playerName);
            return rank >= 0 && rank < MaxEntries;
        }

        private static int FindRank(List<LocalLeaderboardEntry> entries, int score, string playerName)
        {
            string sanitized = SanitizeName(playerName);
            for (int i = 0; i < entries.Count; i++)
            {
                LocalLeaderboardEntry entry = entries[i];
                if (entry.score == Mathf.Max(0, score)
                    && string.Equals(entry.playerName, sanitized, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static LocalLeaderboardData LoadData()
        {
            string json = PlayerPrefs.GetString(LeaderboardPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new LocalLeaderboardData();
            }

            LocalLeaderboardData data = JsonUtility.FromJson<LocalLeaderboardData>(json);
            return data ?? new LocalLeaderboardData();
        }

        private static void SaveData(LocalLeaderboardData data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(LeaderboardPrefsKey, json);
            PlayerPrefs.Save();
        }

        private static int CompareEntries(LocalLeaderboardEntry left, LocalLeaderboardEntry right)
        {
            int scoreCompare = right.score.CompareTo(left.score);
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            return string.CompareOrdinal(left.savedAtUtc, right.savedAtUtc);
        }

        private static string SanitizeName(string playerName)
        {
            string trimmed = string.IsNullOrWhiteSpace(playerName) ? "PLAYER" : playerName.Trim();
            return trimmed.Length > 16 ? trimmed.Substring(0, 16) : trimmed;
        }
    }
}
