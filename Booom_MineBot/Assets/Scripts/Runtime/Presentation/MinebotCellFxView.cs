using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class MinebotCellFxView : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer bodyRenderer;

        [SerializeField]
        private MinebotSpriteSequencePlayer sequencePlayer;

        private bool persistent;
        private float lastRefreshTime;
        private SpriteSequenceAsset chainedSequence;
        private float chainedDelay;
        private float chainedElapsed;

        public void EnsureDefaultStructure(int sortingOrder)
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

            bodyRenderer.sortingOrder = sortingOrder;
            bodyRenderer.color = Color.white;
            //bodyRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
            sequencePlayer = GetComponent<MinebotSpriteSequencePlayer>();
            if (sequencePlayer == null)
            {
                sequencePlayer = gameObject.AddComponent<MinebotSpriteSequencePlayer>();
            }

            sequencePlayer.TargetRenderer = bodyRenderer;
        }

        public void RefreshPersistent(SpriteSequenceAsset sequence, Vector3 worldPosition, int sortingOrder, float totalDuration)
        {
            EnsureDefaultStructure(sortingOrder);
            persistent = true;
            transform.position = worldPosition;
            lastRefreshTime = Time.time;
            bodyRenderer.color = Color.white;
            float frameDuration = ComputeFrameDuration(sequence, totalDuration);
            sequencePlayer.PlayWithDuration(sequence, frameDuration);
        }

        public void PlayOneShot(SpriteSequenceAsset primarySequence, Vector3 worldPosition, int sortingOrder, SpriteSequenceAsset secondarySequence = null, float secondarySequenceDelay = 0.08f)
        {
            EnsureDefaultStructure(sortingOrder);
            persistent = false;
            transform.position = worldPosition;
            bodyRenderer.color = Color.white;
            chainedSequence = secondarySequence;
            chainedDelay = Mathf.Max(0f, secondarySequenceDelay);
            chainedElapsed = 0f;
            sequencePlayer.Play(primarySequence, restartIfSame: true);
        }

        private static float ComputeFrameDuration(SpriteSequenceAsset sequence, float totalDuration)
        {
            if (sequence == null || sequence.Frames == null || sequence.Frames.Length == 0)
            {
                return 0.1f;
            }

            int frameCount = sequence.Frames.Length;
            return totalDuration / frameCount;
        }

        private void Update()
        {
            if (persistent)
            {
                float idleTime = Time.time - lastRefreshTime;
                if (idleTime <= 0.05f)
                {
                    return;
                }

                float alpha = 1f - Mathf.Clamp01((idleTime - 0.05f) / 0.22f);
                if (bodyRenderer != null)
                {
                    Color color = bodyRenderer.color;
                    color.a = alpha;
                    bodyRenderer.color = color;
                }

                if (alpha <= 0f)
                {
                    Destroy(gameObject);
                }

                return;
            }

            if (chainedSequence != null)
            {
                chainedElapsed += Time.deltaTime;
                if (chainedElapsed >= chainedDelay)
                {
                    SpriteSequenceAsset next = chainedSequence;
                    chainedSequence = null;
                    sequencePlayer.Play(next, restartIfSame: true);
                }

                return;
            }

            if (sequencePlayer != null && sequencePlayer.IsComplete)
            {
                Destroy(gameObject);
            }
        }
    }
}
