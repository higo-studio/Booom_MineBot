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

            for (int i = 0; i < readings.Count; i++)
            {
                BitmapGlyphLabel label = EnsureLabel(i);
                ScanReading reading = readings[i];
                label.gameObject.SetActive(true);
                label.SetText(
                    reading.BombCount.ToString(),
                    assets.BitmapGlyphFont,
                    assets.ScanLabelColor,
                    assets.ScanLabelFontSize,
                    assets.ScanLabelSortingOrder);
                label.transform.position = (Vector3)ActorContactProbe.GridToWorldCenter(reading.WallPosition) + (Vector3)assets.ScanLabelOffset;
            }

            for (int i = readings.Count; i < labels.Count; i++)
            {
                labels[i].gameObject.SetActive(false);
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
