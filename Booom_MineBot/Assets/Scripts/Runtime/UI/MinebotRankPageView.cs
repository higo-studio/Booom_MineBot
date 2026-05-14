using System;
using System.Collections.Generic;
using Minebot.Progression;
using TMPro;
using UnityEngine;

namespace Minebot.UI
{
    public sealed class MinebotRankPageView
    {
        private const string LayoutName = "Layout";
        private const string SlotName = "Slot";
        private const string NameTextName = "Name Text";
        private const string ScoreTextName = "Score Text";
        private const string WaveTextName = "Wave Text";

        private readonly GameObject root;
        private readonly RankRow[] rows;

        public MinebotRankPageView(GameObject root)
        {
            this.root = root;
            rows = ResolveRows(root != null ? root.transform : null);
        }

        public GameObject GameObject => root;
        public bool IsVisible => root != null && root.activeSelf;

        public bool HasRequiredBindings(out string missingBindings)
        {
            var missing = new List<string>();
            if (root == null)
            {
                missing.Add("root");
            }

            if (rows.Length == 0)
            {
                missing.Add("rank rows");
            }

            for (int i = 0; i < rows.Length; i++)
            {
                RankRow row = rows[i];
                if (row.NameText == null)
                {
                    missing.Add($"rows[{i}].nameText");
                }

                if (row.ScoreText == null)
                {
                    missing.Add($"rows[{i}].scoreText");
                }

                if (row.WaveText == null)
                {
                    missing.Add($"rows[{i}].waveText");
                }
            }

            missingBindings = missing.Count > 0 ? string.Join(", ", missing) : null;
            return missing.Count == 0;
        }

        public void SetEntries(IReadOnlyList<LocalLeaderboardEntry> entries)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                bool hasEntry = entries != null && i < entries.Count && entries[i] != null;
                LocalLeaderboardEntry entry = hasEntry ? entries[i] : null;
                SetText(rows[i].NameText, hasEntry ? entry.playerName : "---");
                SetText(rows[i].ScoreText, hasEntry ? Mathf.Max(0, entry.score).ToString() : "0");
                SetText(rows[i].WaveText, hasEntry ? $"Wave {Mathf.Max(0, entry.survivedWave)}" : "Wave -");
            }
        }

        public void SetVisible(bool visible)
        {
            if (root == null)
            {
                return;
            }

            if (visible)
            {
                root.transform.SetAsLastSibling();
            }

            root.SetActive(visible);
        }

        private static RankRow[] ResolveRows(Transform rootTransform)
        {
            Transform layout = FindDirectChild(rootTransform, LayoutName);
            if (layout == null)
            {
                return Array.Empty<RankRow>();
            }

            var rowCandidates = new List<RowCandidate>();
            for (int i = 0; i < layout.childCount; i++)
            {
                Transform child = layout.GetChild(i);
                if (!TryParseSlotIndex(child.name, out int index))
                {
                    continue;
                }

                rowCandidates.Add(new RowCandidate(index, child));
            }

            rowCandidates.Sort((left, right) => left.Index.CompareTo(right.Index));
            var resolvedRows = new RankRow[Mathf.Min(LocalLeaderboardService.MaxEntries, rowCandidates.Count)];
            for (int i = 0; i < resolvedRows.Length; i++)
            {
                Transform slot = rowCandidates[i].Transform;
                resolvedRows[i] = new RankRow(
                    FindDirectChild(slot, NameTextName)?.GetComponent<TMP_Text>(),
                    FindDirectChild(slot, ScoreTextName)?.GetComponent<TMP_Text>(),
                    FindDirectChild(slot, WaveTextName)?.GetComponent<TMP_Text>());
            }

            return resolvedRows;
        }

        private static bool TryParseSlotIndex(string objectName, out int index)
        {
            index = -1;
            if (string.Equals(objectName, SlotName, StringComparison.Ordinal))
            {
                index = 0;
                return true;
            }

            const string prefix = SlotName + " (";
            if (!objectName.StartsWith(prefix, StringComparison.Ordinal) || !objectName.EndsWith(")", StringComparison.Ordinal))
            {
                return false;
            }

            string suffix = objectName.Substring(prefix.Length, objectName.Length - prefix.Length - 1);
            return int.TryParse(suffix, out index);
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name.Trim(), childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private readonly struct RankRow
        {
            public RankRow(TMP_Text nameText, TMP_Text scoreText, TMP_Text waveText)
            {
                NameText = nameText;
                ScoreText = scoreText;
                WaveText = waveText;
            }

            public TMP_Text NameText { get; }
            public TMP_Text ScoreText { get; }
            public TMP_Text WaveText { get; }
        }

        private readonly struct RowCandidate
        {
            public RowCandidate(int index, Transform transform)
            {
                Index = index;
                Transform = transform;
            }

            public int Index { get; }
            public Transform Transform { get; }
        }
    }
}
