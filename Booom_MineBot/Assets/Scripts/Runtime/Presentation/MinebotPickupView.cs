using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class MinebotPickupView : MonoBehaviour
    {
        private const float HoverScale = 0.72f;
        private const float AbsorbRootScale = 0.25f;
        private const float AbsorbDurationSeconds = 0.16f;

        [SerializeField]
        private SpriteRenderer bodyRenderer;

        private float absorbElapsed;
        private Vector3 absorbStart;
        private Vector3 absorbTarget;

        public void EnsureDefaultStructure(Sprite sprite, int sortingOrder)
        {
            Transform visual = transform.Find("Visual");
            if (visual == null)
            {
                visual = new GameObject("Visual").transform;
                visual.SetParent(transform, false);
            }

            bodyRenderer = visual.GetComponent<SpriteRenderer>();
            if (bodyRenderer == null)
            {
                bodyRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
            }

            bodyRenderer.sprite = sprite;
            bodyRenderer.sortingOrder = sortingOrder;
            bodyRenderer.color = Color.white;
            visual.localScale = new Vector3(HoverScale, HoverScale, 1f);
        }

        public void ShowHoverVisual(Sprite icon, Vector3 worldPosition, int sortingOrder)
        {
            gameObject.SetActive(true);
            EnsureDefaultStructure(icon, sortingOrder);
            if (bodyRenderer != null)
            {
                bodyRenderer.sprite = icon;
                bodyRenderer.sortingOrder = sortingOrder;
                bodyRenderer.color = Color.white;
            }

            transform.position = worldPosition;
            transform.localScale = Vector3.one;
        }

        public void BeginAbsorbVisual(Sprite icon, Vector3 startWorldPosition, Vector3 targetWorldPosition, int sortingOrder)
        {
            gameObject.SetActive(true);
            EnsureDefaultStructure(icon, sortingOrder);
            if (bodyRenderer != null)
            {
                bodyRenderer.sprite = icon;
                bodyRenderer.sortingOrder = sortingOrder;
                bodyRenderer.color = Color.white;
            }

            absorbElapsed = 0f;
            absorbStart = startWorldPosition;
            absorbTarget = targetWorldPosition;
            transform.position = startWorldPosition;
            transform.localScale = Vector3.one;
        }

        public bool TickAbsorb(float deltaTime)
        {
            absorbElapsed += Mathf.Max(0f, deltaTime);
            float progress = Mathf.Clamp01(absorbElapsed / AbsorbDurationSeconds);
            transform.position = Vector3.Lerp(absorbStart, absorbTarget, progress);
            transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(AbsorbRootScale, AbsorbRootScale, 1f), progress);
            if (bodyRenderer != null)
            {
                Color color = bodyRenderer.color;
                color.a = 1f - progress;
                bodyRenderer.color = color;
            }

            return progress >= 1f;
        }

        public void HideForPool()
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = Color.white;
            }

            gameObject.SetActive(false);
        }
    }
}
