using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class MinebotSpriteSequencePlayer : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer targetRenderer;

        private SpriteSequenceAsset currentSequence;
        private float elapsed;
        private bool isComplete;
        private bool isHoldingFrame;

        public SpriteRenderer TargetRenderer
        {
            get => targetRenderer;
            set => targetRenderer = value;
        }

        public SpriteSequenceAsset CurrentSequence => currentSequence;
        public bool IsComplete => isComplete;

        public void Play(SpriteSequenceAsset sequence, bool restartIfSame = false)
        {
            if (!restartIfSame && currentSequence == sequence)
            {
                return;
            }

            currentSequence = sequence;
            elapsed = 0f;
            isHoldingFrame = false;
            isComplete = sequence == null || sequence.Frames.Length == 0;
            ApplyCurrentFrame(forceFirstFrame: true);
        }

        public void ShowFrame(SpriteSequenceAsset sequence, int frameIndex)
        {
            currentSequence = sequence;
            elapsed = 0f;
            isHoldingFrame = true;
            isComplete = sequence == null || sequence.Frames.Length == 0;
            ApplyFrame(frameIndex);
        }

        public void Stop()
        {
            currentSequence = null;
            elapsed = 0f;
            isHoldingFrame = false;
            isComplete = true;
        }

        private void LateUpdate()
        {
            if (isHoldingFrame || currentSequence == null || currentSequence.Frames.Length == 0 || targetRenderer == null)
            {
                return;
            }

            elapsed += Time.deltaTime;
            ApplyCurrentFrame(forceFirstFrame: false);
        }

        private void ApplyCurrentFrame(bool forceFirstFrame)
        {
            if (targetRenderer == null || currentSequence == null || currentSequence.Frames.Length == 0)
            {
                return;
            }

            Sprite[] frames = currentSequence.Frames;
            int frameCount = frames.Length;
            float frameDuration = currentSequence.FrameDuration;
            int frameIndex = forceFirstFrame ? 0 : Mathf.FloorToInt(elapsed / frameDuration);

            if (currentSequence.Loop)
            {
                frameIndex = frameIndex % frameCount;
            }
            else if (frameIndex >= frameCount)
            {
                frameIndex = frameCount - 1;
                isComplete = true;
            }
            else
            {
                isComplete = false;
            }

            ApplyFrame(frameIndex);
        }

        private void ApplyFrame(int frameIndex)
        {
            if (targetRenderer == null || currentSequence == null || currentSequence.Frames.Length == 0)
            {
                return;
            }

            Sprite[] frames = currentSequence.Frames;
            targetRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
        }
    }
}
