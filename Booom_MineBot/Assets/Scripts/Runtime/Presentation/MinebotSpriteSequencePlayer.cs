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
        private float sequencePlayerFrameDuration;

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
            isComplete = sequence == null || sequence.Frames.Length == 0;
            ApplyCurrentFrame(forceFirstFrame: true);
        }

        public void PlayWithDuration(SpriteSequenceAsset sequence, float frameDuration)
        {
            if (sequence == null || sequence.Frames.Length == 0)
            {
                currentSequence = sequence;
                elapsed = 0f;
                isComplete = true;
                return;
            }

            currentSequence = sequence;
            elapsed = 0f;
            isComplete = false;
            if (sequencePlayerFrameDuration != frameDuration)
            {
                sequencePlayerFrameDuration = frameDuration;
            }

            ApplyCurrentFrame(forceFirstFrame: true);
        }

        public void Stop()
        {
            currentSequence = null;
            elapsed = 0f;
            isComplete = true;
        }

        private void LateUpdate()
        {
            if (currentSequence == null || currentSequence.Frames.Length == 0 || targetRenderer == null)
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
            // 优先使用动态帧时长，否则使用序列配置的帧时长
            float frameDuration = sequencePlayerFrameDuration > 0f ? sequencePlayerFrameDuration : currentSequence.FrameDuration;
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

            targetRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frameCount - 1)];
        }
    }
}
