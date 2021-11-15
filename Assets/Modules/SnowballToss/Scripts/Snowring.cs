using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Niantic.ARVoyage.SnowballToss
{
    /// <summary>
    /// A snowring is a target for the player to throw a snowball through for points.
    /// (The SnowballTossManager class instantiates multiple snowrings at a time.)
    /// InitSnowring() searches for a good position in the world to place its snowring.
    /// Success() is called by SnowringThruRing, a collider just behind the snowring's open center,
    /// triggered when a snowball is thrown through the snowring.
    /// Update() periodically detects if the snowring now intersects with newly-discovered
    /// environment mesh, and auto-expires the snowring if so.
    /// </summary>
    public class Snowring : MonoBehaviour
    {
        // dev flags
        private bool editorDebugButtons = false;
        private bool extraDebugLogging = false;

        private SnowballTossManager snowballTossManager = null;
        private SnowringThruRing snowringThruRing = null;

        public static float minLifetimeSecs = 15f;
        public static float maxLifetimeSecs = 17f;
        private float minPlacementDist = 1f;
        private float maxPlacementDist = 2.75f;
        private float minXAxisAnge = 0;
        private float maxXAxisAngle = 15f;
        private const float maxScale = 0.5f;

        private int currentSector = 0;

        private const int numPlacementSectors = 360 / 30;
        private const float degreesPerSector = 360f / (float)numPlacementSectors;
        private List<int> sectorSearchOrder = new List<int>();
        private int sectorSearchCtr = 0;
        private int numVectorsSearched = 0;
        private float timeStartedSampling = 0f;
        private int numSamplesTried = 0;
        private const int tooManySamplesTried = 300;
        private const float secsTillRecheckForNewlyFoundMesh = 0.5f;
        private float recheckMeshOverlapTime = 0f;

        private const float spacingRadius = 0.4f;
        private const float samplingDistIncrement = spacingRadius / 2f;
        private Collider[] overlappingColliders = new Collider[10];

        private float startTime = 0f;
        private float endTime = 0f;
        private bool isPlaced = false;
        private bool isSuccess = false;
        private bool isExpiring = false;

        private AudioManager audioManager;

        #region Visual Effects

        [Header("Visual Effects")]
        [SerializeField] GameObject ringBurstPrefab;

        private Material ringMaterial;

        #endregion // Visual Effects

        #region Animation Parameters

        [Header("Animation Parameters")]
        [SerializeField] AnimationCurve revealCurve;
        [SerializeField] AnimationCurve successCurve;
        [SerializeField] AnimationCurve expireCurve;

        // Animation state durations.
        private float revealDuration = 20f / 30f;
        private float revealDelay = 0f;
        private float successDuration = 20f / 30f;
        private float successDelay = .1f;
        private float expireDuration = 20f / 30f;
        private float expireDelay = 0f;

        #endregion // Animation Parameters


        private void Awake()
        {
            // Verify valid duration and distance values
            maxLifetimeSecs = Mathf.Max(minLifetimeSecs, maxLifetimeSecs);
            maxPlacementDist = Mathf.Max(minPlacementDist, maxPlacementDist);

            timeStartedSampling = Time.time;

            // Find material for score effect.
            MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer) ringMaterial = meshRenderer.material;

            audioManager = SceneLookup.Get<AudioManager>();
        }


        // Possibly called multiple times, no more than once per frame, 
        // until a valid ring placement is found
        public bool InitSnowring(SnowballTossManager snowballTossManager)
        {
            this.snowballTossManager = snowballTossManager;

            // Init child thru-ring collider
            if (snowringThruRing == null)
            {
                snowringThruRing = GetComponentInChildren<SnowringThruRing>();
                if (snowringThruRing != null)
                {
                    snowringThruRing.snowring = this;
                }
            }

            // Scaled to 0 at first
            this.transform.localScale = Vector3.zero;

            // Search another vector, for a valid ring placement
            isPlaced = SearchVectorForValidRingPlacement();

            // If the above search failed, return early;
            // game manager will call this method again on next frame, to try again
            if (!isPlaced)
            {
                return false;
            }

            // Set lifespan
            startTime = Time.time;
            endTime = Time.time + UnityEngine.Random.Range(minLifetimeSecs, maxLifetimeSecs);

            // Set recheck mesh overlap time
            recheckMeshOverlapTime = Time.time + secsTillRecheckForNewlyFoundMesh;

            // Start reveal animation.
            Reveal();

            return true;
        }


        private void Update()
        {
            // until the ring is placed, it's not ready
            if (!isPlaced) return;

            // Time to check for overlap with newly found mesh?
            if (!isSuccess && !isExpiring && Time.time > recheckMeshOverlapTime)
            {
                recheckMeshOverlapTime = Time.time + secsTillRecheckForNewlyFoundMesh;

                string collisionName;
                int numOverlappingColliders = GetNumOverlappingColliders(spacingRadius, out collisionName);
                if (numOverlappingColliders > 0)
                {
                    Debug.Log("Snowring recheck collision with " + collisionName + "; expiring, age " + (Time.time - startTime) + "s");
                    Expire();
                }
            }

            // Time to autonomously end this ring?
            if (!isSuccess && !isExpiring && Time.time > endTime)
            {
                Expire();
            }
        }


        // called by manager just before instantiating another snowring,
        // to try to avoid the new ring being placed in our sector
        public void UpdateCurrentSector()
        {
            Vector3 diff = Camera.main.transform.position - this.transform.position;
            float curYRotation = Camera.main.transform.eulerAngles.y;
            float angle = -((Mathf.Atan2(diff.z, diff.x) * 360f / (2f * Mathf.PI)) + 90f + curYRotation);
            angle = DemoUtil.NormalizeAngle(angle);
            // Rotate sectors by such that sector 0 is +/- degreesPerSector/2
            float sectorAngle = DemoUtil.NormalizeAngle(angle + (degreesPerSector / 2f));
            currentSector = (int)(sectorAngle / degreesPerSector);
            //Debug.Log("UpdateCurrentSector angle " + angle + " sectorAngle " + sectorAngle + " sector " + currentSector);
        }

        public int GetCurrentSector()
        {
            return currentSector;
        }


        // Pick a vector in a sector, preferring sectors with no existing rings.
        // Search along that vector for a position to place this ring,
        // such that there is empty space (no environment mesh) between player and the ring
        private bool SearchVectorForValidRingPlacement()
        {
            Vector3 validPosition = Vector3.zero;
            float validPositionDist = 0f;
            string collisionName;

            // First time here, choose a sector search order
            if (sectorSearchOrder.Count == 0)
            {
                ChooseSectorSearchOrder();
            }

            // if we've already tried all sectors once, 
            // then loosen criteria (allow multiple rings per sector)
            bool allowMultipleRingsInSameSector = false;
            if (++numVectorsSearched > numPlacementSectors)
            {
                allowMultipleRingsInSameSector = true;
            }

            // if we're constraining the search to sectors without existing rings,
            // get the next sector on the search list without an existing ring
            int sectorToSearch = 0;
            bool choseSector = false;
            while (!choseSector)
            {
                // iterate to the next sector in our sectorSearchOrder, looping
                sectorToSearch = sectorSearchOrder[sectorSearchCtr++ % sectorSearchOrder.Count];

                // if we're allowing multiple rings per sector, we will try this sector
                choseSector = allowMultipleRingsInSameSector;

                // otherwise, check if this sector already has a ring, 
                // or if the last destroyed ring was in this sector
                if (!choseSector)
                {
                    bool sectorAlreadyHasSnowring = false;
                    List<Snowring> currentSnowrings = snowballTossManager.snowrings;
                    for (int i = 0; i < currentSnowrings.Count && !sectorAlreadyHasSnowring; i++)
                    {
                        sectorAlreadyHasSnowring = currentSnowrings[i].GetCurrentSector() == sectorToSearch;
                    }

                    bool lastDestroyedRingInThisSector = snowballTossManager.lastDestroyedSnowringSector == sectorToSearch;

                    // if the sector already has a ring, 
                    // or if the last destroyed ring was in this sector,
                    // then don't choose this sector
                    choseSector = !(sectorAlreadyHasSnowring || lastDestroyedRingInThisSector);
                }
            }


            // choose an y-axis angle within the chosen sector
            float yAngleRel = DemoUtil.NormalizeAngle((sectorToSearch * degreesPerSector) +
                UnityEngine.Random.Range(-degreesPerSector / 5f, degreesPerSector / 5f));

            // get the absolute angle                                                
            float yAngle = DemoUtil.NormalizeAngle(Camera.main.transform.eulerAngles.y + yAngleRel);
            if (extraDebugLogging) Debug.Log("Searching sector " + sectorToSearch + " yAngleRel " + yAngleRel + " yAngle " + yAngle);

            // choose an x-axis angle
            // N.B.: negative x angle is up
            float xAngle = -UnityEngine.Random.Range(minXAxisAnge, maxXAxisAngle);

            this.transform.rotation = Quaternion.Euler(xAngle, yAngle, 0f);

            // Choose a distance within a desired range, starting farther out than minPlacementDist, 
            // since ultimately we'll be checking for minPlacementDist in XZ
            float initialMinPlacementDist = (minPlacementDist + maxPlacementDist) / 3f;
            float snowringDist = UnityEngine.Random.Range(initialMinPlacementDist * 1.5f, maxPlacementDist);
            snowringDist = Mathf.Min(snowringDist, maxPlacementDist);

            // Along the one chosen orientation vector and a max distance,
            // iterate through a series of OverlapSphereNonAlloc() calls,
            // using OverlapSphereNonAlloc, which is more performant than SphereCast,
            // sampling progressively-closer distances along that vector, all the up way to the camera,
            // ensuring there is empty space between us and the chosen position
            bool imperfectFindAfterOverlyLongSearch = false;
            while (snowringDist >= spacingRadius && !imperfectFindAfterOverlyLongSearch)
            {
                // position to sample
                this.transform.position = Camera.main.transform.position;
                this.transform.position += this.transform.forward * snowringDist;

                // test for any collisions within spacingRadius
                int numOverlappingColliders = GetNumOverlappingColliders(spacingRadius, out collisionName);
                //Debug.Log("numOverlappingColliders " + numOverlappingColliders + " for ring dist " + snowringDist);

                // keep track of number of samples tried
                ++numSamplesTried;

                // did we find a ring position that doesn't collide with anything?
                if (numOverlappingColliders == 0)
                {
                    if (validPosition == Vector3.zero &&
                        DemoUtil.GetXZDistance(Camera.main.transform.position, this.transform.position) >= minPlacementDist)
                    {
                        if (extraDebugLogging) Debug.Log("Search found valid ring position, dist " + snowringDist);
                        validPosition = this.transform.position;
                        validPositionDist = snowringDist;
                    }

                    // Eventually take any valid ring position,
                    // though it may be in the same sector as another ring (ie, overlapping rings from player's POV),
                    // or have mesh between the player and the ring (ie, ring is placed behind a room wall)
                    imperfectFindAfterOverlyLongSearch = allowMultipleRingsInSameSector || IsOverlyLongSearch();
                }

                // otherwise, if ring position collides with something, clear our results
                else
                {
                    if (validPosition != Vector3.zero)
                    {
                        if (extraDebugLogging) Debug.Log("Search found invalid ring position, dist " + snowringDist + ", " + overlappingColliders[0]);
                        validPosition = Vector3.zero;
                    }
                }

                // Iterate progressively closer to the camera
                // If we have not found a valid ring position, jump (slightly) closer to the camera
                // If we have found a valid ring position, jump by the (larger) spacing radius
                snowringDist -= validPosition != Vector3.zero ? spacingRadius : samplingDistIncrement;
            }

            // Did we find a valid position for the snowring?
            if (validPosition != Vector3.zero)
            {
                // Put the snowring at the valid position
                Debug.Log("New snowring, dist " + validPositionDist +
                            ", num samples tried " + numSamplesTried +
                            " over " + (Time.time - timeStartedSampling) + "s");
                this.transform.position = validPosition;

                // Rotate the ring to face the camera
                Vector3 vectorToCamera = Camera.main.transform.position - this.transform.position;
                Quaternion snowringRotation = Quaternion.LookRotation(vectorToCamera);
                this.transform.rotation = snowringRotation;

                return true;
            }

            return false;
        }


        // Fan out, preferring sectors closest to in-front-of-camera
        private void ChooseSectorSearchOrder()
        {
            sectorSearchOrder.Clear();
            sectorSearchOrder.Add(0);
            int ctr = 1;
            while (ctr < numPlacementSectors / 2)
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    sectorSearchOrder.Add(ctr);
                    sectorSearchOrder.Add(numPlacementSectors - ctr);
                }
                else
                {
                    sectorSearchOrder.Add(numPlacementSectors - ctr);
                    sectorSearchOrder.Add(ctr);
                }
                ++ctr;
            }
            if (sectorSearchOrder.Count == numPlacementSectors - 1)
            {
                sectorSearchOrder.Add(numPlacementSectors / 2);
            }

            if (extraDebugLogging)
            {
                string searchOrderStr = "Search order:";
                foreach (int s in sectorSearchOrder) searchOrderStr += " " + s;
                Debug.Log(searchOrderStr);
            }
        }


        private bool IsOverlyLongSearch()
        {
            return numVectorsSearched > numPlacementSectors * 2 ||
                    numSamplesTried > tooManySamplesTried;
        }


        private int GetNumOverlappingColliders(float radius, out string collisionName)
        {
            int arMeshLayerIndex = 9;
            int layerMask = 1 << arMeshLayerIndex;
            int numOverlappingColliders = Physics.OverlapSphereNonAlloc(this.transform.position,
                                                                        radius, overlappingColliders,
                                                                        layerMask, QueryTriggerInteraction.Ignore);
            int finalCount = numOverlappingColliders;
            int collisionIndex = -1;
            collisionName = null;
            GameObject colliderGameObject;

            // NOTE: Since we're now filtering by layer, shouldn't need to manually filter below.
            if (numOverlappingColliders > 0)
            {
                for (int i = 0; i < numOverlappingColliders; i++)
                {
                    colliderGameObject = overlappingColliders[i].gameObject;

                    // Exclude my own colliders
                    if (colliderGameObject == this.gameObject ||
                         colliderGameObject == snowringThruRing.gameObject)
                    {
                        --finalCount;
                    }

                    // Always exclude snowballs and snowrings
                    else if (colliderGameObject.name.Contains("Snow") ||
                            colliderGameObject.name.Contains(this.gameObject.name) ||
                             colliderGameObject.name.Contains(snowringThruRing.gameObject.name))
                    {
                        --finalCount;
                    }

                    // else we found a collision
                    else
                    {
                        collisionIndex = i;
                    }
                }
            }

            // Logging
            if (collisionIndex >= 0)
            {
                collisionName = overlappingColliders[collisionIndex].gameObject.name;

                if (startTime > 0f)
                {
                    if (extraDebugLogging) Debug.Log("Snowring recheck collision: " + collisionName);
                }
                else
                {
                    if (extraDebugLogging) Debug.Log("Snowring collision: " + collisionName);
                }
            }

            return finalCount;
        }


        private void Reveal()
        {
            StartCoroutine(RevealRoutine(revealDuration, revealDelay));

            // SFX
            audioManager.PlayAudioAtPosition(AudioKeys.SFX_IceRing_Spawn, this.gameObject.transform.position);
        }


        // Called from child thru-ring collider
        public void Success()
        {
            if (isSuccess) return;

            Debug.Log("Snowring succeeding due to snowball");
            isSuccess = true;

            // SFX
            audioManager.PlayAudioAtPosition(AudioKeys.SFX_RingScore_Indicator, this.gameObject.transform.position);

            // Destroy is called at end of routine.
            StartCoroutine(SuccessRoutine(successDuration, successDelay));
        }


        public void Expire()
        {
            //Debug.Log("Snowring expiring");
            isExpiring = true;

            // Destroy is called at end of routine.
            StartCoroutine(ExpireRoutine(expireDuration, expireDelay));
        }


        private void DestroySnowring()
        {
            UpdateCurrentSector();

            // tell manager we're being destroyed
            if (snowballTossManager)
            {
                snowballTossManager.SnowRingDestroyed(this, currentSector);
                Destroy(gameObject);
            }
        }


        public IEnumerator RevealRoutine(float duration = 1f, float delay = 0f)
        {
            System.Action<float> onUpdate = (float t) =>
            {
                transform.localScale = Vector3.one * maxScale * revealCurve.Evaluate(t);
            };

            yield return InterpolationUtil.LinearInterpolation(gameObject, gameObject,
                duration, delay, onUpdate: onUpdate);
        }

        public IEnumerator SuccessRoutine(float duration = 1f, float delay = 0f)
        {
            System.Action onStart = () =>
            {
                snowballTossManager?.SnowRingSucceeded();

                // Instantiate particle burst.
                if (ringBurstPrefab)
                {
                    GameObject ringBurstInstance = Instantiate(ringBurstPrefab, transform.position, transform.rotation);
                    ringBurstInstance.transform.localScale = transform.localScale;
                }
            };

            System.Action<float> onUpdate = (float t) =>
            {
                // Adjust albedo boost. 
                float albedoBoostAmount = .5f;
                float currentBoost = albedoBoostAmount * (1 - t);
                if (ringMaterial) ringMaterial.SetFloat("_AlbedoBoost", 1 + currentBoost);

                // Adjust ring scale;
                transform.localScale = Vector3.one * maxScale * successCurve.Evaluate(t);
            };

            System.Action onComplete = () =>
            {
                DestroySnowring();
            };

            yield return InterpolationUtil.LinearInterpolation(gameObject, gameObject,
                duration, delay, postWait: 0f,
                onStart, onUpdate, onComplete);
        }

        public IEnumerator ExpireRoutine(float duration = 1, float delay = 0)
        {
            System.Action<float> onUpdate = (float t) =>
            {
                transform.localScale = Vector3.one * maxScale * expireCurve.Evaluate(t);
            };

            System.Action onComplete = () =>
            {
                DestroySnowring();
            };

            yield return InterpolationUtil.LinearInterpolation(gameObject, gameObject,
                duration, delay, postWait: 0,
                null, onUpdate, onComplete);
        }


        #region Debug

        void OnGUI()
        {
            if (editorDebugButtons == true)
            {
                if (GUILayout.Button("Reveal"))
                {
                    StartCoroutine(RevealRoutine(revealDuration, revealDelay));
                }
                if (GUILayout.Button("Success"))
                {
                    StartCoroutine(SuccessRoutine(successDuration, successDelay));
                }
                if (GUILayout.Button("Expire"))
                {
                    StartCoroutine(ExpireRoutine(expireDuration, expireDelay));
                }
            }
        }

        #endregion // Debug

    }
}
