using JSAM;
using Minebot.Bootstrap;
using Minebot.Progression;
using UnityEngine;

namespace Minebot.Presentation
{
    internal sealed class MinebotGameplayAudioController
    {
        private readonly MinebotAudioConfig config;
        private MusicFileObject currentMusic;
        private SoundChannelHelper playerMiningLoopHelper;
        private Transform playerMiningLoopAnchor;
        private SoundChannelHelper robotMiningLoopHelper;
        private Transform robotMiningLoopAnchor;
        private bool lastPendingUpgrade;
        private bool lastIsDead;
        private bool lastWarningWindowActive;
        private bool lastWaveResolutionActive;
        private WaveResolutionPhase lastWavePhase = WaveResolutionPhase.None;

        public MinebotGameplayAudioController(MinebotAudioConfig config)
        {
            this.config = config;
        }

        public void SyncState(RuntimeServiceRegistry services, Transform playerAnchor, Transform robotMiningAnchor)
        {
            if (!TryEnsureAudioRuntime() || services == null)
            {
                return;
            }

            bool isDead = services.Vitals != null && services.Vitals.IsDead;
            bool pendingUpgrade = services.Experience != null && services.Experience.HasPendingUpgrade;
            bool waveResolutionActive = services.Session != null && services.Session.IsWaveResolutionActive;
            bool warningWindowActive = !waveResolutionActive && services.Waves != null && services.Waves.IsWarningWindowActive;
            WaveResolutionPhase phase = waveResolutionActive
                ? services.Session.CurrentWaveResolutionState.Phase
                : WaveResolutionPhase.None;

            if (!lastPendingUpgrade && pendingUpgrade)
            {
                PlaySound(config.PickupAndGrowth.UpgradeAvailable);
            }

            if (!lastIsDead && isDead)
            {
                PlaySound(config.WaveAndFailure.GameOver);
            }

            if (!lastWarningWindowActive && warningWindowActive)
            {
                PlaySound(config.WaveAndFailure.WaveWarningStart);
            }

            if (waveResolutionActive && (!lastWaveResolutionActive || phase != lastWavePhase))
            {
                switch (phase)
                {
                    case WaveResolutionPhase.DetonatePerimeterBombs:
                        if (services.Session.CurrentWaveResolutionState.TotalPerimeterBombs > 0)
                        {
                            PlaySound(config.PlayerAndTerrain.HazardBombExplosion, playerAnchor);
                        }
                        break;
                    case WaveResolutionPhase.ReevaluateDangerZones:
                        PlaySound(config.WaveAndFailure.WaveDangerRefresh);
                        break;
                    case WaveResolutionPhase.CollapseDangerZones:
                        PlaySound(config.WaveAndFailure.WaveCollapse);
                        break;
                }
            }

            if (lastWaveResolutionActive && !waveResolutionActive)
            {
                if (services.Session.LastWaveResolution.RobotsDestroyed > 0)
                {
                    PlaySound(config.Robots.RobotDestroyed, playerAnchor);
                }

                if (!isDead && !services.Session.LastWaveResolution.PlayerKilled)
                {
                    PlaySound(config.WaveAndFailure.WaveSurvived);
                }
            }

            UpdateMusic(waveResolutionActive, warningWindowActive);
            UpdateLoop(
                config.Robots.RobotMiningLoop,
                robotMiningAnchor,
                robotMiningAnchor != null,
                ref robotMiningLoopHelper,
                ref robotMiningLoopAnchor);

            lastPendingUpgrade = pendingUpgrade;
            lastIsDead = isDead;
            lastWarningWindowActive = warningWindowActive;
            lastWaveResolutionActive = waveResolutionActive;
            lastWavePhase = phase;
        }

        public void StopTransientLoops()
        {
            StopLoop(ref playerMiningLoopHelper, ref playerMiningLoopAnchor);
            StopLoop(ref robotMiningLoopHelper, ref robotMiningLoopAnchor);
        }

        public void PlayMarkerModeToggle() => PlaySound(config.ModeAndUi.ModeMarkerToggle);
        public void PlayBuildModeToggle() => PlaySound(config.ModeAndUi.ModeBuildToggle);
        public void PlayBuildingSelect() => PlaySound(config.ModeAndUi.BuildingSelect);
        public void PlayMarkerSet() => PlaySound(config.ModeAndUi.MarkerSet);
        public void PlayMarkerClear() => PlaySound(config.ModeAndUi.MarkerClear);
        public void PlayActionDenied() => PlaySound(config.ModeAndUi.ActionDenied);
        public void PlayPlayerMove() => PlaySound(config.PlayerAndTerrain.PlayerMove);
        public void PlayPlayerBlock() => PlaySound(config.PlayerAndTerrain.PlayerBlock);
        public void PlayPlayerMiningWeak() => PlaySound(config.PlayerAndTerrain.PlayerMiningWeak);
        public void PlayPlayerDamage() => PlaySound(config.PlayerAndTerrain.PlayerDamage);
        public void PlayUpgradeApply() => PlaySound(config.PickupAndGrowth.UpgradeApply);
        public void PlayRepairSuccess() => PlaySound(config.BaseOps.RepairSuccess);
        public void PlayRobotBuildSuccess() => PlaySound(config.BaseOps.RobotBuildSuccess);
        public void PlayBuildPlaceSuccess() => PlaySound(config.BaseOps.BuildPlaceSuccess);

        public void StartPlayerMiningLoop(Transform anchor)
        {
            UpdateLoop(
                config.PlayerAndTerrain.PlayerMiningLoop,
                anchor,
                anchor != null,
                ref playerMiningLoopHelper,
                ref playerMiningLoopAnchor);
        }

        public void StopPlayerMiningLoop()
        {
            StopLoop(ref playerMiningLoopHelper, ref playerMiningLoopAnchor);
        }

        public void PlayTerrainWallBreak(Vector3 position)
        {
            PlaySound(config.PlayerAndTerrain.TerrainWallBreak, position);
        }

        public void PlayExplosion(Vector3 position)
        {
            PlaySound(config.PlayerAndTerrain.HazardBombExplosion, position);
        }

        public void PlayRobotWallBreak(Vector3 position)
        {
            PlaySound(config.Robots.RobotWallBreak, position);
        }

        public void PlayRobotDestroyed(Vector3 position)
        {
            PlaySound(config.Robots.RobotDestroyed, position);
        }

        public void PlayPickupAbsorb(WorldPickupType type)
        {
            switch (type)
            {
                case WorldPickupType.Metal:
                    PlaySound(config.PickupAndGrowth.PickupMetalAbsorb);
                    break;
                case WorldPickupType.Energy:
                    PlaySound(config.PickupAndGrowth.PickupEnergyAbsorb);
                    break;
                case WorldPickupType.Experience:
                    PlaySound(config.PickupAndGrowth.PickupExpAbsorb);
                    break;
            }
        }

        private void UpdateMusic(bool waveResolutionActive, bool warningWindowActive)
        {
            MusicFileObject desiredMusic = null;
            if (waveResolutionActive)
            {
                desiredMusic = config.Music.WaveResolution;
            }
            else if (warningWindowActive)
            {
                desiredMusic = config.Music.WaveWarning;
            }
            else
            {
                desiredMusic = config.Music.GameplayLoop;
            }

            if (desiredMusic == currentMusic || !HasPlayableClip(desiredMusic))
            {
                return;
            }

            AudioManager.PlayMusic(desiredMusic, true);
            currentMusic = desiredMusic;
        }

        private void UpdateLoop(
            SoundFileObject loopCue,
            Transform anchor,
            bool shouldPlay,
            ref SoundChannelHelper helper,
            ref Transform activeAnchor)
        {
            if (!shouldPlay || !HasPlayableClip(loopCue))
            {
                StopLoop(ref helper, ref activeAnchor);
                return;
            }

            bool sameAnchor = activeAnchor == anchor;
            if (helper != null && helper.isActiveAndEnabled && sameAnchor && helper.AudioFile == loopCue)
            {
                return;
            }

            helper = AudioManager.PlaySound(loopCue, anchor, helper);
            activeAnchor = anchor;
        }

        private static void StopLoop(ref SoundChannelHelper helper, ref Transform anchor)
        {
            if (helper != null)
            {
                helper.Stop();
                helper = null;
            }

            anchor = null;
        }

        private void PlaySound(SoundFileObject sound, Transform anchor = null)
        {
            if (!TryEnsureAudioRuntime() || !HasPlayableClip(sound))
            {
                return;
            }

            AudioManager.PlaySound(sound, anchor);
        }

        private void PlaySound(SoundFileObject sound, Vector3 position)
        {
            if (!TryEnsureAudioRuntime() || !HasPlayableClip(sound))
            {
                return;
            }

            AudioManager.PlaySound(sound, position);
        }

        private bool TryEnsureAudioRuntime()
        {
            if (config == null || !Application.isPlaying)
            {
                return false;
            }

            if (Resources.Load<JSAMSettings>(nameof(JSAMSettings)) == null)
            {
                return false;
            }

            if (Object.FindAnyObjectByType<AudioManager>() == null)
            {
                var audioManagerObject = new GameObject("Audio Manager");
                audioManagerObject.AddComponent<AudioManager>();
            }

            return Object.FindAnyObjectByType<AudioManager>() != null;
        }

        private static bool HasPlayableClip(BaseAudioFileObject audioFile)
        {
            if (audioFile == null || audioFile.Files == null)
            {
                return false;
            }

            for (int i = 0; i < audioFile.Files.Count; i++)
            {
                if (audioFile.Files[i] != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
