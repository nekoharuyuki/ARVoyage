using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// AudioManager class. Includes:
    ///  Utility to play audio non-spatially.
    ///  Utility to play audio at a world position.
    ///  Utility to play audio attached to a GameObject.
    /// </summary>
    public class AudioManager : MonoBehaviour, ISceneDependency
    {
        [Tooltip("These clips will loop throughout the scene. They will automatically fade out when leaving the scene.")]
        [SerializeField] private List<ClipData> persistentAmbientClips;

        [Tooltip("The clips available to play in this scene.")]
        [SerializeField] private List<AudioClipList> audioClipLists;

        [Tooltip("Should the list of audio key constants be copied to the clipboard on run?")]
        [SerializeField] private bool buildKeyConstsToClipboardInEditor;

        private readonly Dictionary<string, AudioVariants> AudioDictionary = new Dictionary<string, AudioVariants>();

        private StringBuilder keyConstsStringBuilder;

        private List<AudioSource> audioSourcePool = new List<AudioSource>();
        private int currentAudioSourcePoolIndex;

        [Serializable]
        private struct ClipData
        {
            public AudioClip clip;
            public float volume;
        }


        /// <summary>
        /// Shuffle between varying clips for an AudioKey.
        /// </summary>
        private class AudioVariants
        {
            public List<AudioClip> clips = new List<AudioClip>();
            private int index;

            public void Shuffle()
            {
                int n = clips.Count;
                while (n > 1)
                {
                    n--;
                    int k = UnityEngine.Random.Range(0, n + 1);
                    AudioClip clip = clips[k];
                    clips[k] = clips[n];
                    clips[n] = clip;
                }
            }

            public AudioClip GetNext()
            {
                if (clips.Count == 1)
                {
                    return clips[0];
                }

                index++;

                if (index == clips.Count)
                {
                    index = 0;
                    Shuffle();
                }

                return clips[index];
            }
        }

        private void Awake()
        {
#if !UNITY_EDITOR
            buildKeyConstsToClipboardInEditor = false;
#endif
            BuildClipDictionary();

            // Add initial audio sources to pool
            for (int i = 0; i < 10; i++)
            {
                AddAudioSourceToPool();
            }

            Fader.FadingSceneOut.AddListener(OnFadingSceneOut);
            Fader.FadingSceneIn.AddListener(OnFadingSceneIn);
        }

        private void OnDestroy()
        {
            Fader.FadingSceneIn.RemoveListener(OnFadingSceneIn);
            Fader.FadingSceneOut.RemoveListener(OnFadingSceneOut);
        }

        private void BuildClipDictionary()
        {
            if (buildKeyConstsToClipboardInEditor)
            {
                keyConstsStringBuilder = new StringBuilder();
            }

            for (int i = 0; i < audioClipLists.Count; i++)
            {
                if (buildKeyConstsToClipboardInEditor)
                {
                    AddAudioClipsCategoryString(audioClipLists[i]);
                }

                List<AudioClip> audioClips = audioClipLists[i].clips;

                for (int j = 0; j < audioClips.Count; j++)
                {
                    AudioClip clip = audioClips[j];

                    // By default, the key is the full clip name
                    string audioKey = clip.name;

                    // If this clip name ends in a variation suffix, use the main portion of the name as the key
                    int lastUnderScoreIndex = audioKey.LastIndexOf('_');
                    if (lastUnderScoreIndex != -1 && lastUnderScoreIndex < audioKey.Length - 1)
                    {
                        string possibleVariationSuffix = audioKey.Substring(lastUnderScoreIndex + 1);
                        if (int.TryParse(possibleVariationSuffix, out int variation))
                        {
                            audioKey = audioKey.Substring(0, lastUnderScoreIndex);
                        }
                    }

                    // Debug.LogFormat("[Clip {0}] [Key {1}]", clip.name, audioKey);

                    AudioVariants variantsForKey = null;
                    AudioDictionary.TryGetValue(audioKey, out variantsForKey);

                    if (variantsForKey == null)
                    {
                        variantsForKey = new AudioVariants();
                        AudioDictionary.Add(audioKey, variantsForKey);

                        if (buildKeyConstsToClipboardInEditor)
                        {
                            AddAudioKeyConstString(audioKey);
                        }
                    }

                    variantsForKey.clips.Add(clip);
                }
            }

            // Shuffle all the variants
            foreach (AudioVariants variants in AudioDictionary.Values)
            {
                variants.Shuffle();
            }

            if (buildKeyConstsToClipboardInEditor)
            {
                GUIUtility.systemCopyBuffer = keyConstsStringBuilder?.ToString();
            }
        }

        private void OnFadingSceneIn(float fadeDuration)
        {
            for (int i = 0; i < persistentAmbientClips.Count; i++)
            {
                ClipData clipData = persistentAmbientClips[i];
                PlayAudioOnPoolSource(clipData.clip, targetTransform: this.transform, position: Vector3.zero, volume: clipData.volume, spatialBlend: 0f, loop: true, fadeInDuration: fadeDuration, dspDelay: 0);
            }
        }

        // When the scene is fading out, fade out all currently playing sources
        private void OnFadingSceneOut(float fadeDuration)
        {
            for (int i = 0; i < audioSourcePool.Count; i++)
            {
                AudioSource source = audioSourcePool[i];
                if (source != null && source.isPlaying && source.clip != null)
                {
                    Debug.Log("Fade out source playing " + source.clip.name);
                    FadeOutAudioSource(source, fadeDuration);
                }
            }
        }

        public Coroutine FadeInAudioSource(AudioSource source, float fadeDuration, float targetVolume, Func<float, float> easingFunc = null)
        {
            return FadeAudioSource(source, fadeDuration, targetVolume, easingFunc);
        }

        public Coroutine FadeOutAudioSource(AudioSource source, float fadeDuration, Func<float, float> easingFunc = null)
        {
            return FadeAudioSource(source, fadeDuration, targetVolume: 0f, easingFunc);
        }

        private Coroutine FadeAudioSource(AudioSource source, float fadeDuration, float targetVolume, Func<float, float> easingFunc)
        {
            if (source == null)
            {
                Debug.LogWarning("Ignoring attempt to fade null audio source");
                return null;
            }

            float startVolume = source.volume;

            if (easingFunc == null)
            {
                easingFunc = InterpolationUtil.EaseInOutCubic;
            }

            return InterpolationUtil.EasedInterpolation(
                target: source,
                // Pass the AudioManager as the operationKey so any other fades on this source will interrupt this one
                operationKey: this,
                easingFunc: easingFunc,
                duration: fadeDuration,
                onUpdate: (float progress) =>
                {
                    source.volume = Mathf.Lerp(startVolume, targetVolume, progress);
                },
                onComplete: () =>
                {
                    if (Mathf.Approximately(source.volume, 0.0f))
                    {
                        source.Stop();
                    }
                });
        }

        /// <summary>
        /// Utility to play audio non-spatially.
        /// </summary>
        /// <param name="audioKey">The audio key.</param>
        /// <param name="volume">Volume to use. Defaults to 1.</param>
        /// <param name="loop">Should the audio loop? Defaults to false.</param>
        /// <param name="fadeInDuration">How long to fade in? Defaults to 0.</param>
        /// <param name="dspDelay">How long to delay starting the clip, in DSP time. N.B. fade in doesn't wait for this delay.</param>
        /// <returns>The source playing the audio.</returns>
        public AudioSource PlayAudioNonSpatial(string audioKey, float volume = 1, bool loop = false, float fadeInDuration = 0, double dspDelay = 0)
        {
            return PlayAudioOnPoolSource(audioKey, targetTransform: transform, position: Vector3.zero, volume, spatialBlend: 0f, loop, fadeInDuration, dspDelay);
        }

        /// <summary>
        /// Utility to play audio at a world position.
        /// </summary>
        /// <param name="audioKey">The audio key.</param>
        /// <param name="position">The world position.</param>
        /// <param name="volume">Volume to use. Defaults to 1.</param>
        /// <param name="spatialBlend">The spatial blend to use. Defaults to fully spatial.</param>
        /// <param name="loop">Should the audio loop? Defaults to false.</param>
        /// <param name="fadeInDuration">How long to fade in? Defaults to 0.</param>
        /// <param name="dspDelay">How long to delay starting the clip, in DSP time. N.B. fade in doesn't wait for this delay.</param>

        /// <returns>The source playing the audio.</returns>
        public AudioSource PlayAudioAtPosition(string audioKey, Vector3 position, float volume = 1, float spatialBlend = 1, bool loop = false, float fadeInDuration = 0, double dspDelay = 0)
        {
            return PlayAudioOnPoolSource(audioKey, targetTransform: null, position, volume, spatialBlend, loop, fadeInDuration, dspDelay);
        }

        /// <summary>
        /// Utility to play audio attached to a GameObject.
        /// </summary>
        /// <param name="audioKey">The audio key.</param>
        /// <param name="targetObject">The GameObject to attach the audio to.</param>
        /// <param name="volume">Volume to use. Defaults to 1.</param>
        /// <param name="spatialBlend">The spatial blend to use. Defaults to fully spatial.</param>
        /// <param name="loop">Should the audio loop? Defaults to false.</param>
        /// <param name="fadeInDuration">How long to fade in? Defaults to 0.</param>
        /// <param name="dspDelay">How long to delay starting the clip, in DSP time. N.B. fade in doesn't wait for this delay.</param>
        /// <returns>The source playing the audio.</returns>
        public AudioSource PlayAudioOnObject(string audioKey, GameObject targetObject, float volume = 1, float spatialBlend = 1, bool loop = false, float fadeInDuration = 0, double dspDelay = 0)
        {
            return PlayAudioOnPoolSource(audioKey, targetTransform: targetObject.transform, Vector3.zero, volume, spatialBlend, loop, fadeInDuration, dspDelay);
        }

        /// <summary>
        /// Utility to play a loop with an in, loop and out. Currently only supports non-spatial playback.
        /// </summary>
        /// <param name="audioKeyIn">The audio key for the "in" clip.</param>
        /// <param name="audioKeyLoop">The audio key for the "loop" clip. This clip should stitch perfectly to the end of the "in" clip.</param>
        /// <param name="audioKeyOut">The audio key for the "out" clip.</param>
        /// <param name="volume">Volume to use. Defaults to 1.</param>
        /// <param name="stopCrossfadeDuration">How long to crossfade when stopping.</param>
        /// <returns>The SegmentedAudioLoop playing the audio.</returns>
        public SegmentedAudioLoop PlaySegmentedLoop(string audioKeyIn, string audioKeyLoop, string audioKeyOut, float volume = 1, float stopCrossfadeDuration = .5f)
        {
            return SegmentedAudioLoop.CreateAndStart(this, audioKeyIn, audioKeyLoop, audioKeyOut, volume, stopCrossfadeDuration);
        }

        // Wrapper that takes an audioKey
        private AudioSource PlayAudioOnPoolSource(string audioKey, Transform targetTransform, Vector3 position, float volume, float spatialBlend, bool loop, float fadeInDuration, double dspDelay)
        {
            AudioClip clip = GetClipForKey(audioKey);

            // If the clip is null, return a non-playing audio source
            if (clip == null)
            {
                return GetAudioSourceToUse();
            }

            return PlayAudioOnPoolSource(clip, targetTransform, position, volume, spatialBlend, loop, fadeInDuration, dspDelay);
        }

        public AudioSource PlayAudioOnPoolSource(AudioClip clip, Transform targetTransform, Vector3 position, float volume, float spatialBlend, bool loop, float fadeInDuration, double dspDelay)
        {
            Debug.LogFormat("{0} PlayAudioOnPoolSource: [clip {1}] [target {2}] [volume {3}] [spatialBlend {4}] [loop {5}]",
                this,
                clip.name,
                targetTransform != null ? targetTransform.name : position.ToString(),
                volume,
                spatialBlend,
                loop);

            AudioSource source = GetAudioSourceToUse();
            source.gameObject.SetActive(true);
            source.enabled = true;

            // If a transform is provided, play directly on it
            if (targetTransform != null)
            {
                source.transform.parent = targetTransform;
                source.transform.localPosition = Vector3.zero;
            }
            // Otherwise, ensure the source is attached to this transform and move the source to the specified world position
            else
            {
                if (source.transform.parent != this.transform)
                {
                    source.transform.parent = this.transform;
                }
                source.transform.position = position;
            }

            if (source.isPlaying)
            {
                Debug.LogErrorFormat("Got playing source from pool [source {0}] [clip {1}]", source?.name, clip?.name);
            }

            source.spatialBlend = spatialBlend;
            source.clip = clip;
            source.loop = loop;

            if (dspDelay == 0)
            {
                source.Play();
            }
            else
            {
                source.PlayScheduled(AudioSettings.dspTime + dspDelay);
            }

            if (fadeInDuration > 0)
            {
                source.volume = 0;
                FadeInAudioSource(source, fadeInDuration, volume);
            }
            else
            {
                source.volume = volume;
            }

            Debug.LogFormat("Playing clip [{0}] on source [{1}]", clip.name, source.name);

            return source;
        }

        public AudioSource GetAudioSourceToUse()
        {
            // When recursing in the case that the current source was destroyed, it is possible for
            // currentAudioSourcePoolIndex to be out of bounds. Reset it.
            if (currentAudioSourcePoolIndex >= audioSourcePool.Count)
            {
                currentAudioSourcePoolIndex = 0;
            }

            // If there are no sources in the pool, reset the index and add one to use
            if (audioSourcePool.Count == 0)
            {
                currentAudioSourcePoolIndex = 0;
                return AddAudioSourceToPool();
            }

            // If there are sources in the pool, find one to use

            // First try using the current source
            AudioSource source = audioSourcePool[currentAudioSourcePoolIndex];

            // If the current source or source's object has been destroyed, remove it from the pool and recurse
            if (source == null || source.gameObject == null)
            {
                // Debug.Log(this + " pool source destroyed. Removing source and recursing.");
                audioSourcePool.RemoveAt(currentAudioSourcePoolIndex);
                return GetAudioSourceToUse();
            }

            // If current source is playing, move onto the next one, which will be the one least recently played
            if (source.isPlaying)
            {
                currentAudioSourcePoolIndex = (currentAudioSourcePoolIndex + 1) % audioSourcePool.Count;
                source = audioSourcePool[currentAudioSourcePoolIndex];
                // Debug.Log(this + " current pool source is playing. Moving to least recently used source: " + currentAudioSourcePoolIndex);

                // If the least recently used source or its object has been destroyed, remove it from the pool and recurse
                if (source == null || source.gameObject == null)
                {
                    // Debug.Log(this + " least recently used pool source destroyed. Removing source and recursing.");
                    audioSourcePool.RemoveAt(currentAudioSourcePoolIndex);
                    return GetAudioSourceToUse();
                }

                // If that source is also playing, add a new source at the current index
                if (source.isPlaying)
                {
                    // Debug.Log(this + " least recently used pool source is also still playing. Adding new source.");
                    return AddAudioSourceToPool();
                }
            }

            return source;
        }

        public AudioClip GetClipForKey(string audioKey)
        {
            if (AudioDictionary.TryGetValue(audioKey, out AudioVariants variantsForKey))
            {
                return variantsForKey.GetNext();
            }

            Debug.LogError("Didn't find audio key " + audioKey);
            return null;
        }

        private AudioSource AddAudioSourceToPool()
        {
            GameObject audioSourceGO = new GameObject("AudioManagerPool_" + audioSourcePool.Count);
            audioSourceGO.transform.parent = transform;
            audioSourceGO.transform.localPosition = Vector3.zero;
            audioSourceGO.transform.localRotation = Quaternion.identity;
            AudioSource source = audioSourceGO.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1;
            audioSourcePool.Insert(currentAudioSourcePoolIndex, source);
            Debug.Log("Added audio source to pool: " + audioSourceGO.name);
            return source;
        }

        private void AddAudioKeyConstString(string key)
        {
            keyConstsStringBuilder?.AppendLine("public const string " + key + " = \"" + key + "\";");
        }

        private void AddAudioClipsCategoryString(AudioClipList clipList)
        {
            keyConstsStringBuilder?.AppendLine("");
            keyConstsStringBuilder?.AppendLine("// " + clipList.category);
        }
    }
}
