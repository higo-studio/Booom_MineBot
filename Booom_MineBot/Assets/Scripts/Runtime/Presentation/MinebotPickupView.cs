using Minebot.Progression;
using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class MinebotPickupView : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer bodyRenderer;

        private int pickupId;
        private bool absorbing;
        private float absorbElapsed;
        private Vector3 absorbStart;
        private Vector3 absorbTarget;
        private float age;
        private Vector2 drift;
        private Vector3 originWorld;

        public int PickupId => pickupId;
        public bool IsAbsorbingVisual => absorbing;

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
            visual.localScale = new Vector3(0.72f, 0.72f, 1f);
        }

        public void Bind(WorldPickupState pickup, Sprite icon, Vector3 baseWorldPosition, int sortingOrder)
        {
            pickupId = pickup.Id;
            age = pickup.Age;
            drift = pickup.Drift;
            originWorld = baseWorldPosition;
            EnsureDefaultStructure(icon, sortingOrder);
            bodyRenderer.sortingOrder = sortingOrder;
            bodyRenderer.sprite = icon;
            if (!absorbing)
            {
                transform.position = ComputeHoverPosition();
            }
        }

        public void BeginAbsorb(Vector3 playerWorldPosition)
        {
            if (absorbing)
            {
                return;
            }

            absorbing = true;
            absorbElapsed = 0f;
            absorbStart = transform.position;
            absorbTarget = playerWorldPosition;
        }

        private void Update()
        {
            if (!absorbing)
            {
                transform.position = ComputeHoverPosition();
                return;
            }

            absorbElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(absorbElapsed / 0.16f);
            transform.position = Vector3.Lerp(absorbStart, absorbTarget, progress);
            transform.localScale = Vector3.Lerp(new Vector3(0.72f, 0.72f, 1f), new Vector3(0.18f, 0.18f, 1f), progress);
            if (bodyRenderer != null)
            {
                Color color = bodyRenderer.color;
                color.a = 1f - progress;
                bodyRenderer.color = color;
            }

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private Vector3 ComputeHoverPosition()
        {
            float launch = Mathf.Clamp01(age / 0.24f);
            float hover = 0.16f + Mathf.Sin((age + pickupId * 0.17f) * 6.2f) * 0.04f;
            return originWorld + new Vector3(drift.x, Mathf.Lerp(0.02f, 0.28f, launch) + hover, 0f);
        }
    }
}
