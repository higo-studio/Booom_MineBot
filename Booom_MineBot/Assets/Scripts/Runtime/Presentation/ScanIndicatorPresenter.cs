using System.Collections.Generic;
using Minebot.HazardInference;
using Minebot.UI;
using TMPro;
using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class ScanIndicatorPresenter : MonoBehaviour
    {
        private readonly List<ScanReading> readings = new List<ScanReading>();
        private readonly List<TextMeshPro> labels = new List<TextMeshPro>();
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
                TextMeshPro label = EnsureLabel(i);
                ScanReading reading = readings[i];
                label.gameObject.SetActive(true);
                label.text = reading.BombCount.ToString();
                label.fontSize = assets.ScanLabelFontSize;
                label.color = assets.ScanLabelColor;
                label.transform.position = (Vector3)ActorContactProbe.GridToWorldCenter(reading.WallPosition) + (Vector3)assets.ScanLabelOffset;
            }

            for (int i = readings.Count; i < labels.Count; i++)
            {
                labels[i].gameObject.SetActive(false);
            }
        }

        private TextMeshPro EnsureLabel(int index)
        {
            while (labels.Count <= index)
            {
                var labelObject = new GameObject($"Scan Label {labels.Count + 1}");
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localScale = Vector3.one * 0.12f;

                TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
                label.alignment = TextAlignmentOptions.Center;
                label.enableWordWrapping = false;
                label.font = MinebotHudFontUtility.GetDefaultFontAsset();

                MeshRenderer renderer = label.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = 35;
                }

                labels.Add(label);
            }

            return labels[index];
        }
    }
}
