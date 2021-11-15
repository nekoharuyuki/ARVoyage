// Comment in to test map path percent positions
// #define TEST_PATH_PERCENT

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Niantic.ARVoyage.Map
{
    /// <summary>
    /// Manages the Yeti character's functionality in the map scene
    /// </summary>
    public class MapActor : MonoBehaviour, ISceneDependency
    {
        private const string KeyRotationInterpolation = "Rotation";
        private const string KeyMovementInterpolation = "Movement";

        public static AppEvent<Level> ActorJumpedToLevel = new AppEvent<Level>();
        public static AppEvent<Level> ActorStartingWalkToLevel = new AppEvent<Level>();
        public static AppEvent<Level, float> ActorPathPercentFromLevelChanged = new AppEvent<Level, float>();

        [SerializeField] private GameObject yetiModel;
        [SerializeField] Animator animator;
        [SerializeField] ParentConstraint parentConstraint;

        [SerializeField] AnimationClip mapClip;
        [SerializeField] GameObject mapNull;
        [SerializeField] PlayableDirector introPlayableDirector;

        [SerializeField] GameObject poof;

        [SerializeField] private float fullPathDuration = 48f;
        [SerializeField] private float turnAroundDuration = .5f;

        [SerializeField] private float stepSFXInterval = .25f;
        [SerializeField] private float stepSFXVolume = .65f;

#if TEST_PATH_PERCENT
        [SerializeField] private float testPathPercent = 0;
#endif

        private AudioManager audioManager;

        private Action onIntroComplete = null;
        private bool readyToWalk;
        private Level currentLevel = Level.None;
        private float currentPathPercent = 0f;
        private Coroutine walkRoutine;
        private float lastStepSFXTime;

        private float yetiStartScale = 1.5f;
        private Dictionary<Level, float> levelPathAnimationPercentMap = new Dictionary<Level, float>();

        private void Awake()
        {
            // Cache the starting scale for the yeti
            yetiStartScale = yetiModel.transform.localScale.x;

            // Initialize the level path animation map
            levelPathAnimationPercentMap.Add(Level.SnowballToss, 0.166f);
            levelPathAnimationPercentMap.Add(Level.Walkabout, 0.403f);
            levelPathAnimationPercentMap.Add(Level.SnowballFight, 0.817f);
            levelPathAnimationPercentMap.Add(Level.BuildAShip, 1f);

            audioManager = SceneLookup.Get<AudioManager>();
        }

        private void OnEnable()
        {
            // Subscribe to events
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            introPlayableDirector.stopped -= OnIntroCompleted;
        }

        private void Start()
        {
            // If the map intro has previously completed, then spawn the yeti at the proper position
            if (SaveUtil.HasMapIntroEverCompleted())
            {
                // Change the actor's constraint to the map path null
                ConstrainActorToMapPathNull();

                yetiModel.transform.localScale = Vector3.zero;
            }
        }

        public void WalkToLevel(Level level)
        {
            if (level != Level.None && level != currentLevel)
            {
                currentLevel = level;

                // If ready to walk, start walking
                // otherwise the actor will start walking to this level once able
                if (readyToWalk)
                {
                    Debug.Log(name + " walking to level " + level);

                    // Stop any running walk
                    StopWalk();

                    float targetPathPercent = levelPathAnimationPercentMap[level];

                    if (!Mathf.Approximately(currentPathPercent, targetPathPercent))
                    {
                        walkRoutine = StartCoroutine(WalkRoutine(level, targetPathPercent));
                    }
                }
            }
        }

        public void PlayPoof(int frameDelay = 0)
        {
            StartCoroutine(PlayPoofRoutine(frameDelay));
        }

        private IEnumerator PlayPoofRoutine(int frameDelay)
        {
            // Wait for frame delay
            for (int i = 0; i < frameDelay; i++)
            {
                yield return null;
            }

            poof.SetActive(true);

            // Play combo audio for poof
            audioManager.PlayAudioOnObject(AudioKeys.SFX_SnowmanBuild,
                this.gameObject,
                spatialBlend: .5f);
            audioManager.PlayAudioOnObject(AudioKeys.SFX_Success_Magic,
                this.gameObject,
                spatialBlend: .5f);
        }

        public void JumpToLevel(Level level)
        {
            if (level != Level.None)
            {
                currentLevel = level;
                // Stop any running walk
                StopWalk();
                SetMapPathPercent(levelPathAnimationPercentMap[level]);

                ActorJumpedToLevel.Invoke(level);
            }
        }

        private void StopWalk(bool stopWalkingAnim = true)
        {
            if (walkRoutine != null)
            {
                InterpolationUtil.StopRunningInterpolation(mapNull, KeyMovementInterpolation);
                InterpolationUtil.StopRunningInterpolation(yetiModel, KeyRotationInterpolation);

                StopCoroutine(walkRoutine);
                walkRoutine = null;
            }

            if (stopWalkingAnim)
            {
                SetWalking(false);
            }
        }

        public Coroutine BubbleScaleUp()
        {
            return BubbleScaleUtil.ScaleUp(yetiModel, yetiStartScale);
        }

        public Coroutine BubbleScaleDown()
        {
            return BubbleScaleUtil.ScaleDown(yetiModel, 0);
        }

        public void SetWalking(bool value)
        {
            animator.SetBool("Walking", value);
        }

        // Called by StateSelectLevle if intro has never been completed
        public void PlayIntro(Action onComplete = null)
        {
            // Listen for the intro to stop, and trigger its play
            onIntroComplete = onComplete;
            introPlayableDirector.stopped += OnIntroCompleted;
            introPlayableDirector.Play();
        }

        private void OnIntroCompleted(PlayableDirector playableDirector)
        {
            // unsubscribe
            introPlayableDirector.stopped -= OnIntroCompleted;

            // Save that the map intro has completed
            SaveUtil.SaveMapIntroCompleted();

            // Invoke the dynamic oncomplete
            onIntroComplete?.Invoke();

            // Change the actor's constraint to the map path null
            ConstrainActorToMapPathNull();
        }

        private void ConstrainActorToMapPathNull()
        {
            ConstraintSource introSource = parentConstraint.GetSource(0);
            introSource.weight = 0;
            parentConstraint.SetSource(0, introSource);

            ConstraintSource mapSource = parentConstraint.GetSource(1);
            mapSource.weight = 1;
            parentConstraint.SetSource(1, mapSource);

            // The actor is ready to walk once constrained to the path null
            readyToWalk = true;

            // If there is already a level, start walking immediately
            if (currentLevel != Level.None)
            {
                WalkToLevel(currentLevel);
            }
        }

        private void SetMapPathPercent(float percent)
        {
            float clampedPercent = Mathf.Clamp01(percent);
            mapClip.SampleAnimation(mapNull, clampedPercent * mapClip.length);
            currentPathPercent = clampedPercent;
        }

        private IEnumerator WalkRoutine(Level level, float targetPathPercent)
        {
            float startPathPercent = currentPathPercent;

            bool isForwards = targetPathPercent > startPathPercent;
            float targetY = isForwards ? 0 : 180;

            SetWalking(true);
            ActorStartingWalkToLevel.Invoke(level);

            // Rotate based on walk direction if needed
            yield return RotateRoutine(targetY);

            float pathPercentDifference = Mathf.Abs(startPathPercent - targetPathPercent);
            float duration = pathPercentDifference * fullPathDuration;

            yield return InterpolationUtil.LinearInterpolation(
                target: mapNull,
                operationKey: KeyMovementInterpolation,
                duration: duration,
                onUpdate: (percent) =>
                {
                    float animationPercent = Mathf.Lerp(startPathPercent, targetPathPercent, percent);
                    SetMapPathPercent(animationPercent);
                    ActorPathPercentFromLevelChanged.Invoke(level, Mathf.Abs(targetPathPercent - animationPercent));
                    PlayStepSFXIfTime();
                }
            );

            // Rotate back to 0 if needed
            yield return RotateRoutine(0);

            SetWalking(false);

            walkRoutine = null;
        }

        private IEnumerator RotateRoutine(float targetY)
        {
            Quaternion startRotation = yetiModel.transform.localRotation;
            Quaternion targetRotation = Quaternion.Euler(0, targetY, 0);
            float angle = Quaternion.Angle(startRotation, targetRotation);

            if (angle > 10f)
            {
                float percentTurnAround = angle / 180f;
                float turnDuration = percentTurnAround * turnAroundDuration;

                yield return InterpolationUtil.LinearInterpolation(
                    target: yetiModel,
                    operationKey: KeyRotationInterpolation,
                    duration: turnDuration,
                    onUpdate: (percent) =>
                    {
                        yetiModel.transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, percent);
                        PlayStepSFXIfTime();
                    }
                );
            }
        }

        private void PlayStepSFXIfTime()
        {
            if (Time.time - lastStepSFXTime > stepSFXInterval)
            {
                lastStepSFXTime = Time.time;
                // Use PlayAudioAtPosition, since PlayAudioOnObject is taken up with rollingSFX
                audioManager.PlayAudioOnObject(AudioKeys.SFX_Yeti_Footstep_Snow,
                    this.gameObject,
                    volume: stepSFXVolume,
                    spatialBlend: .5f);
            }
        }

#if TEST_PATH_PERCENT
        private void Update()
        {
            mapClip.SampleAnimation(mapNull, Mathf.Clamp01(testPathPercent) * mapClip.length);
        }
#endif

    }
}
