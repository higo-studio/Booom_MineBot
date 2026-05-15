using System.Collections.Generic;
using Minebot.HazardInference;
using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class ScanIndicatorPresenter : MonoBehaviour
    {
        private readonly List<ScanReading> readings = new List<ScanReading>();
        private readonly List<BitmapGlyphLabel> labels = new List<BitmapGlyphLabel>();
        private MinebotPresentationAssets assets;

        internal void Configure(MinebotPresentationAssets presentationAssets)
        {
            assets = presentationAssets;
        }

        public void ShowReadings(IReadOnlyList<ScanReading> scanReadings)
        {
            readings.Clear();
            if (scanReadings == null)
            {
                return;
            }

            for (int i = 0; i < scanReadings.Count; i++)
            {
                readings.Add(scanReadings[i]);
            }
        }

        public void Refresh()
        {
            if (assets == null)
            {
                return;
            }

            // 清理已销毁的标签
            labels.RemoveAll(label => label == null);

            for (int i = 0; i < readings.Count; i++)
            {
                BitmapGlyphLabel label = EnsureLabel(i);
                ScanReading reading = readings[i];
                if (label == null || label.gameObject == null)
                {
                    continue;
                }
                label.gameObject.SetActive(true);
                label.SetText(
                    reading.BombCount.ToString(),
                    assets.BitmapGlyphFont,
                    assets.ScanLabelColor,
                    assets.ScanLabelFontSize,
                    assets.ScanLabelSortingOrder);
                Vector3 basePosition = ActorContactProbe.GridToWorldCenter(reading.CellPosition);
                label.transform.position = basePosition + new Vector3(assets.ScanLabelOffset.x, assets.ScanLabelOffset.y, 0f);
            }

            for (int i = readings.Count; i < labels.Count; i++)
            {
                if (labels[i] != null && labels[i].gameObject != null)
                {
                    labels[i].gameObject.SetActive(false);
                }
            }
        }

        private BitmapGlyphLabel EnsureLabel(int index)
        {
            while (labels.Count <= index)
            {
                var labelObject = new GameObject($"Scan Label {labels.Count + 1}");
                labelObject.transform.SetParent(transform, false);
                labels.Add(labelObject.AddComponent<BitmapGlyphLabel>());
            }

            return labels[index];
        }
    }
}
