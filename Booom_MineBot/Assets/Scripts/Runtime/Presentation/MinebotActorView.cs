using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class MinebotActorView : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer bodyRenderer;

        [SerializeField]
        private MinebotSpriteSequencePlayer sequencePlayer;

        [SerializeField]
        private Sprite fallbackSprite;

        public SpriteRenderer BodyRenderer => bodyRenderer;

        public void EnsureDefaultStructure(Sprite sprite, int sortingOrder)
        {
            fallbackSprite = sprite;
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
            bodyRenderer.sprite = bodyRenderer.sprite != null ? bodyRenderer.sprite : fallbackSprite;
            visual.localScale = new Vector3(0.82f, 0.82f, 1f);

            sequencePlayer = GetComponent<MinebotSpriteSequencePlayer>();
            if (sequencePlayer == null)
            {
                sequencePlayer = gameObject.AddComponent<MinebotSpriteSequencePlayer>();
            }

            sequencePlayer.TargetRenderer = bodyRenderer;
        }

        public void ApplyState(ActorStateSequenceSet states, PresentationActorState state, Sprite fallback, Color tint)
        {
            EnsureDefaultStructure(fallback, bodyRenderer != null ? bodyRenderer.sortingOrder : 40);
            fallbackSprite = fallback;
            bodyRenderer.color = tint;

            SpriteSequenceAsset sequence = states != null ? states.ForState(state) : null;
            if (sequence != null && sequence.Frames.Length > 0)
            {
                sequencePlayer.TargetRenderer = bodyRenderer;
                sequencePlayer.Play(sequence);
                return;
            }

            sequencePlayer.Stop();
            bodyRenderer.sprite = fallbackSprite;
        }
    }
}
