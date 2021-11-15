using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using Niantic.ARDKExamples.Gameboard;

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// Yeti NPC and the snowball, used in Walkabout demo.
    /// Includes rolling the snowball while locomoting.
    /// </summary>
    public class WalkaboutActor : MonoBehaviour
    {
        public bool Rolling { get; private set; } = false;
        private List<Waypoint> gizmoPathWaypoints = new List<Waypoint>();

        [SerializeField] TransparentDepthHelper transparentDepthHelper;
        [SerializeField] [Range(0.0f, 1.0f)] float progress = 0f;

        [Header("Yeti")]
        [SerializeField] public Transform yetiTransform;
        [SerializeField] Animator yetiAnimator = default;
        [SerializeField] public float yetiWalkSpeed = .5f;
        [SerializeField] PathAnimation yetiAnimation = default;
        [SerializeField] float yetiWaveDelay = 1.5f;
        public Transform yetiDynamicHeight;

        [Header("Snowball")]
        [SerializeField] Transform snowballScaleTransform = default;
        [SerializeField] Transform snowballRotationTransform = default;
        [SerializeField] float snowballInitialScale = .45f;
        [SerializeField] float snowballMaximumScale = 1f;
        [SerializeField] float snowballRollDelay = .5f;
        private Vector3 lastPosition = Vector3.zero;

        /*[SerializeField]*/
        private float snowballRollTimeRequired = 10f;
        public bool SnowmanComplete { get; private set; } = false;

        [SerializeField] AnimationCurve turnAnimationCurve;

        public Transform ActorCenterTransform => snowballRotationTransform;
        public int Footsteps { get; private set; } = 0;
        public static AppEvent EventFootstep = new AppEvent();

        private AudioManager audioManager;
        private AudioSource rollingSFX = null;
        private const float rollingSFXFadeDuration = 0.5f;
        private const float footstepSFXVolume = 0.5f;

        public bool IsTransparent { get; private set; }

        void OnEnable()
        {
            // Subscribe to events
            EventFootstep.AddListener(OnEventFootstep);
            audioManager = SceneLookup.Get<AudioManager>();
        }

        void Start()
        {
            progress = 0;
            lastPosition = transform.position;
            SnowmanComplete = false;
        }

        void OnDisable()
        {
            // Unsubscribe from events
            EventFootstep.RemoveListener(OnEventFootstep);
        }

        public void SetTransparent(bool transparent)
        {
            transparentDepthHelper.SetTransparent(transparent);
            IsTransparent = transparent;
        }

        public float GetYetiToSnowballDist()
        {
            return DemoUtil.GetXZDistance(yetiTransform.position, snowballScaleTransform.position);
        }

        public float GetYetiToCenterDist()
        {
            float dist = DemoUtil.GetXZDistance(yetiTransform.position, this.gameObject.transform.position);
            return dist;
        }

        public void DisplaySnowball(bool displaySnowball)
        {
            if (snowballScaleTransform == null) return;

            snowballScaleTransform.gameObject.SetActive(displaySnowball);

            // When first displaying the snowball, set its scale to to-be-rolled
            if (displaySnowball)
            {
                snowballScaleTransform.localScale = Vector3.one * snowballInitialScale;
            }
        }

        public void ResetProgress()
        {
            progress = 0f;
            SnowmanComplete = false;
            Footsteps = 0;
        }

        private void OnEventFootstep()
        {
            ++Footsteps;

            // SFX
            // Use PlayAudioAtPosition, since PlayAudioOnObject is taken up with rollingSFX
            audioManager.PlayAudioAtPosition(AudioKeys.SFX_Yeti_Footstep_Snow,
                                            this.gameObject.transform.position,
                                            volume: footstepSFXVolume);
        }

        void Update()
        {
            // Handle walking/rolling.
            if (Rolling)
            {
                // Snowball scale/progress is spent on time spent in the walking state.
                UpdateSnowballScale();

                // Snowball movement is based on position change.
                {
                    float delta = Vector3.Distance(transform.position, lastPosition);
                    snowballRotationTransform.Rotate(Vector3.right * delta * 360);
                    lastPosition = transform.position;
                }
            }
        }

        // Snowball scale/progress is spent on time spent in the walking state.
        public void UpdateSnowballScale()
        {
            progress = Mathf.Clamp01(progress + (Time.deltaTime / snowballRollTimeRequired));
            snowballScaleTransform.localScale = Vector3.one *
                (((snowballMaximumScale - snowballInitialScale) * progress) + snowballInitialScale);
            yetiAnimator.SetFloat("SnowballScale", progress);
        }


        public void Stop()
        {
            StopAllCoroutines();
            Rolling = false;
            yetiAnimator.SetBool("Walking", false);
            StopRollingSFX();
        }

        // Make yeti's snowball vanish, complete the nearby snowman, and wave at the snowman
        public void Complete()
        {
            Stop();
            StartCoroutine(CompleteRoutine());
        }

        public IEnumerator CompleteRoutine()
        {
            // Make snowball vanish.
            snowballScaleTransform.gameObject.SetActive(false);

            SnowmanComplete = true;

            // Wait for a moment and then wave.
            yield return new WaitForSeconds(yetiWaveDelay);
            yetiAnimator.SetTrigger("Wave");
        }

        public void Move(IList<Waypoint> waypoints, Vector3 finalDestination)
        {
            List<Vector3> path = new List<Vector3>();
            gizmoPathWaypoints.Clear();

            // Always start with our current position.
            path.Add(transform.position);

            // Add points for each waypoint.
            foreach (Waypoint waypoint in waypoints)
            {
                path.Add(waypoint.WorldPosition);
                gizmoPathWaypoints.Add(waypoint);
            }

            // Modify final waypoint to match reticle-set destination's xz
            finalDestination.y = path[path.Count - 1].y;
            path[path.Count - 1] = finalDestination;

            StopAllCoroutines();
            yetiAnimation.SetPath(path, yetiWalkSpeed);
            StartCoroutine(MoveRoutine());
        }

        private IEnumerator MoveRoutine()
        {
            Vector3 zeroY = new Vector3(1, 0, 1);

            // Initial orientation.
            {
                Vector3 startSample = yetiAnimation.Evaluate(0);
                Vector3 lookaheadSample = yetiAnimation.Evaluate(.1f);

                Vector3 startDirection = (lookaheadSample - startSample).normalized;

                float angle = Vector3.SignedAngle(Vector3.Scale(transform.forward, zeroY),
                                                Vector3.Scale(startDirection, zeroY), Vector3.up);

                float rotationSpeed = 8f / 360f;
                float duration = Mathf.Abs(angle) * rotationSpeed;

                Debug.LogFormat("MoveRoutine: Begin Orientation: Angle: {0} Duration: {1}", angle, duration);

                if (Mathf.Abs(angle) > 0)
                {

                    float startAngle = Vector3.SignedAngle(Vector3.Scale(Vector3.forward, zeroY),
                                                Vector3.Scale(transform.forward, zeroY), Vector3.up);
                    float endAngle = Vector3.SignedAngle(Vector3.Scale(Vector3.forward, zeroY),
                                                Vector3.Scale(startDirection, zeroY), Vector3.up);

                    Debug.DrawRay(transform.position, Vector3.forward * .1f, Color.cyan, 4);
                    Debug.DrawRay(transform.position, transform.forward * .1f, Color.magenta, 4);
                    Debug.DrawRay(transform.position, startDirection * .1f, Color.yellow, 4);

                    Debug.LogFormat("MoveRoutine: Animate Orientation: Start: {0} End: {1}", startAngle, endAngle);

                    // Create animation for turn direction.
                    float blendTime = duration / 10f;
                    float turnDirection = (angle > 0) ? 1 : -1;

                    turnAnimationCurve = new AnimationCurve();
                    turnAnimationCurve.AddKey(0, 0);
                    turnAnimationCurve.AddKey(blendTime, turnDirection);
                    turnAnimationCurve.AddKey(duration - blendTime, turnDirection);
                    turnAnimationCurve.AddKey(duration, 0);

                    // Start walking and wait for Yeti to lean forward.
                    Rolling = false;
                    yetiAnimator.SetBool("Walking", true);
                    StopRollingSFX();
                    //yetiAnimator.SetFloat("TurnAngle", turnDirection);
                    yield return new WaitForSeconds(snowballRollDelay);

                    // Animate.
                    float startTime = Time.time;
                    float endTime = startTime + duration;
                    while (Time.time < endTime)
                    {
                        float t = (Time.time - startTime) / (endTime - startTime);
                        float currentAngle = Mathf.LerpAngle(startAngle, endAngle, t);
                        transform.rotation = Quaternion.AngleAxis(currentAngle, Vector3.up);

                        Debug.DrawRay(transform.position, transform.forward * .1f, Color.green, 4);

                        yetiAnimator.SetFloat("TurnAngle", turnAnimationCurve.Evaluate(Time.time - startTime));

                        yield return null;
                    }

                    transform.rotation = Quaternion.AngleAxis(endAngle, Vector3.up);
                    yetiAnimator.SetFloat("TurnAngle", 0);
                }
            }

            // Play the full waypoint animation.
            {
                float startTime = Time.time;
                float endTime = startTime + yetiAnimation.TotalDuration;

                Rolling = true;
                yetiAnimator.SetBool("Walking", true);
                StartRollingSFX();

                while (Time.time < endTime)
                {
                    float elapsed = Time.time - startTime;
                    transform.position = yetiAnimation.Evaluate(elapsed);

                    Vector3 nextPosition = yetiAnimation.Evaluate(elapsed + .25f);

                    Vector3 newDirection = (nextPosition - transform.position).normalized;
                    float angle = Vector3.SignedAngle(Vector3.Scale(transform.forward, zeroY),
                                                    Vector3.Scale(newDirection, zeroY), Vector3.up);

                    float lookAngle = Vector3.SignedAngle(Vector3.Scale(Vector3.forward, zeroY),
                                                Vector3.Scale(newDirection, zeroY), Vector3.up);
                    transform.rotation = Quaternion.AngleAxis(lookAngle, Vector3.up);

                    // Handle turn animation;
                    float currentTurnAngle = yetiAnimator.GetFloat("TurnAngle");
                    yetiAnimator.SetFloat("TurnAngle", Mathf.Lerp(currentTurnAngle, angle, .1f));

                    Debug.DrawRay(nextPosition, Vector3.up, Color.blue, .1f);
                    Debug.DrawRay(transform.position, (nextPosition - transform.position).normalized * .1f, Color.green, 30f);

                    yield return null;
                }

                Rolling = false;
                yetiAnimator.SetBool("Walking", false);
                StopRollingSFX();
            }
        }

        public float GetProgress()
        {
            return progress;
        }

        private void StartRollingSFX()
        {
            // start looping rolling SFX, if not already playing
            if (rollingSFX == null)
            {
                rollingSFX = audioManager.PlayAudioOnObject(AudioKeys.SFX_Snowball_Roll_LP,
                                                            targetObject: this.gameObject,
                                                            loop: true,
                                                            fadeInDuration: rollingSFXFadeDuration);
            }
        }

        private void StopRollingSFX()
        {
            // stop looping rolling SFX if already playing
            if (rollingSFX != null)
            {
                audioManager.FadeOutAudioSource(rollingSFX, fadeDuration: rollingSFXFadeDuration);
                rollingSFX = null;
            }
        }


        #region Editor Debug

#if UNITY_EDITOR && FALSE

        private void OnGUI()
        {
            if (GUILayout.Button("Transparent"))
            {
                SetTransparent(true);
            }

            if (GUILayout.Button("Opaque"))
            {
                SetTransparent(false);
            }

            if (GUILayout.Button("Snowball"))
            {
                DisplaySnowball(true);
            }

            if (GUILayout.Button("Move"))
            {
                List<Waypoint> waypoints = new List<Waypoint>() {
                    new Waypoint(null, transform.position ,Waypoint.MovementType.Walk),
                    new Waypoint(null, transform.position + new Vector3(0,0,1f),Waypoint.MovementType.Walk),
                    new Waypoint(null, transform.position + new Vector3(1,0,2f),Waypoint.MovementType.Walk),
                    new Waypoint(null, transform.position + new Vector3(-1f,0,4f),Waypoint.MovementType.Walk),
                    new Waypoint(null, transform.position + new Vector3(-2,0,-1),Waypoint.MovementType.Walk),
                    new Waypoint(null, transform.position + new Vector3(0,0,0),Waypoint.MovementType.Walk)
                };
                Move(waypoints);
            }

            if (GUILayout.Button("Stop"))
            {
                Stop();
            }

            if (GUILayout.Button("Complete"))
            {
                Complete();
            }
        }


        private void OnDrawGizmos()
        {
            if (gizmoPathWaypoints == null) return;
            Gizmos.color = Color.yellow;
            if (gizmoPathWaypoints.Count > 0)
            {
                foreach (Waypoint waypoint in gizmoPathWaypoints)
                {
                    Gizmos.DrawSphere(waypoint.WorldPosition, 0.02f);
                }
            }
        }

#endif

        #endregion // Editor Debug
    }
}