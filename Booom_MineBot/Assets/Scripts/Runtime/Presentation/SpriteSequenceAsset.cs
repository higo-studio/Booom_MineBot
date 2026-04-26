using System;
using UnityEngine;

namespace Minebot.Presentation
{
    [CreateAssetMenu(menuName = "Minebot/Presentation/Sprite Sequence")]
    public sealed class SpriteSequenceAsset : ScriptableObject
    {
        [SerializeField]
        private Sprite[] frames = Array.Empty<Sprite>();

        [SerializeField]
        private float frameDuration = 0.12f;

        [SerializeField]
        private bool loop = true;

        public Sprite[] Frames => frames ?? Array.Empty<Sprite>();
        public float FrameDuration => Mathf.Max(0.02f, frameDuration);
        public bool Loop => loop;

#if UNITY_EDITOR
        public void Configure(Sprite[] configuredFrames, float configuredFrameDuration, bool configuredLoop)
        {
            frames = configuredFrames ?? Array.Empty<Sprite>();
            frameDuration = Mathf.Max(0.02f, configuredFrameDuration);
            loop = configuredLoop;
        }
#endif
    }
}
