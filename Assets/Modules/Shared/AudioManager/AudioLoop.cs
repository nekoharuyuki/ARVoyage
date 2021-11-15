using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Class for managing a segmented audio loop with an in, loop and out.
    /// </summary>
    public class SegmentedAudioLoop
    {
        private readonly AudioManager AudioManager;
        private readonly string AudioKeyIn;
        private readonly string AudioKeyLoop;
        private readonly string AudioKeyOut;
        private readonly float Volume;
        private readonly float StopCrossFadeDuration;

        private bool stopped;

        private Coroutine startRoutine;

        private AudioSource sourceIn;
        private AudioSource sourceLoop;
        private AudioSource sourceOut;

        public static SegmentedAudioLoop CreateAndStart(AudioManager audioManager, string audioKeyIn, string audioKeyLoop, string audioKeyOut, float volume, float stopCrossFadeDuration)
        {
            SegmentedAudioLoop loop = new SegmentedAudioLoop(audioManager, audioKeyIn, audioKeyLoop, audioKeyOut, volume, stopCrossFadeDuration);
            loop.Start();
            return loop;
        }

        private SegmentedAudioLoop(AudioManager audioManager, string audioKeyIn, string audioKeyLoop, string audioKeyOut, float volume, float crossFadeDuration)
        {
            this.AudioManager = audioManager;
            this.AudioKeyIn = audioKeyIn;
            this.AudioKeyLoop = audioKeyLoop;
            this.AudioKeyOut = audioKeyOut;
            this.Volume = volume;
            this.StopCrossFadeDuration = crossFadeDuration;
        }

        private void Start()
        {
            // Play the in clip and schedule the loop clip to begin as it ends
            sourceIn = AudioManager.PlayAudioNonSpatial(AudioKeyIn, Volume);
            double inClipDuration = (double)sourceIn.clip.samples / sourceIn.clip.frequency;
            sourceLoop = AudioManager.PlayAudioNonSpatial(AudioKeyLoop, Volume, loop: true, dspDelay: inClipDuration);
        }

        public void Stop()
        {
            if (stopped)
            {
                return;
            }

            stopped = true;

            // End the startRoutine if it's still running
            if (startRoutine != null)
            {
                AudioManager.StopCoroutine(startRoutine);
            }

            // Fade out the in audio if it's playing
            if (sourceIn != null && sourceIn.isPlaying)
            {
                AudioManager.FadeOutAudioSource(sourceIn, StopCrossFadeDuration);
            }

            // Fade out the loop audio if it's playing
            if (sourceLoop != null && sourceLoop.isPlaying)
            {
                AudioManager.FadeOutAudioSource(sourceLoop, StopCrossFadeDuration);
            }

            // Play the out audio
            AudioManager.PlayAudioNonSpatial(AudioKeyOut, Volume, fadeInDuration: StopCrossFadeDuration);
        }
    }
}
