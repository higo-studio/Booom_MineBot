using System;
using UnityEngine;

namespace Minebot.Presentation
{
    [CreateAssetMenu(menuName = "Minebot/表现/精灵序列")]
    public sealed class SpriteSequenceAsset : ScriptableObject
    {
        [SerializeField]
        [InspectorLabel("帧列表")]
        private Sprite[] frames = Array.Empty<Sprite>();

        [SerializeField]
        [InspectorLabel("帧时长")]
        private float frameDuration = 0.12f;

        [SerializeField]
        [InspectorLabel("循环")]
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
