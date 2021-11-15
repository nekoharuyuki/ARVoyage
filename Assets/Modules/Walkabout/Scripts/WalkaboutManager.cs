using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using Niantic.ARDKExamples.Gameboard;

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// Constants, GameObjects, game state, and helper methods used by 
    /// various State classes in the Walkabout demo.
    /// </summary>
    public class WalkaboutManager : MonoBehaviour, ISceneDependency
    {
        public const int minVictorySteps = 10;

        public const float baseYetiSize = 1.6f;
        public const float origYetiSize = 3.5f;
        public const float yetiSizeDelta = 0.25f;
        public float yetiSize;
        public float baseYetiWalkSpeed;

        [Header("Gameboard")]
        private float dynamicGameboardPollTime = 0f;
        private const float dynamicGameboardPollPeriod = 0.5f;
        private int unstableGameboardCtrForYeti = 0;

        [Header("NPCs")]
        [SerializeField] public WalkaboutActor yetiAndSnowball;
        public Transform ActorCenterTransform => yetiAndSnowball.ActorCenterTransform;

        [SerializeField] private WalkaboutSnowman snowman;
        private float baseSnowballCloseToSnowmanDist = 0.5f * baseYetiSize / origYetiSize;

        [Header("Buttons")]
        [SerializeField] public GameObject placementButton;

        [Header("Reticle")]
        [SerializeField] public SurfaceReticle cameraReticle;
        [SerializeField] private GameObject destinationMarker;
        private Vector3 destinationPos;
        private bool destinationPointChanged = false;
        private float timeDestinationPointChanged = 0f;
        [HideInInspector] public const float reticleCloseToYetiDist = 0.4f;
        private const float snowmanHoverReticleDist = 0.3f;
        private bool showingSnowmanHover = false;

        [Header("Gauge")]
        [SerializeField] public Gauge progressGauge;

        [Header("State")]
        [SerializeField] StateGrowSnowball stateGrow;
        [SerializeField] StateBuildSnowman stateBuild;
        [HideInInspector] public bool inStateGrow = false;
        [HideInInspector] public bool inStateBuild = false;

        private GameboardHelper gameboardHelper;

        // manager state
        private bool everHadValidPlacement = false;
        private Vector3 lastValidPlacementPt;
        private List<Vector3> validGameboardPoints = new List<Vector3>();

        public const string invalidGameboardHint = "Keep scanning the ground!";

        void Awake()
        {
            gameboardHelper = SceneLookup.Get<GameboardHelper>();
        }

        void OnEnable()
        {
            cameraReticle.DisplayReticle(false);
            yetiSize = baseYetiSize;
            baseYetiWalkSpeed = yetiAndSnowball.yetiWalkSpeed;
            UpdateYetiSize();
        }

        public void UpdateYetiInitialPlacement()
        {
            // place yeti at exactly at current valid place on the gameboard, if any
            Vector3 placementPt;
            if (cameraReticle.isValidPlacementPt)
            {
                placementPt = cameraReticle.validPlacementPt;
            }

            // else place yeti as close as valid to reticle on gameboard, if any
            else if (cameraReticle.isReticleOnSurface || !everHadValidPlacement)
            {
                // if on surface and not near perimeter,
                // use the reticle position, but at previous good y pos
                bool nearPerimeter = gameboardHelper.IsPointNearPerimeterTile(cameraReticle.transform.position,
                                                                                yetiAndSnowball.GetYetiToSnowballDist());
                if (!nearPerimeter && cameraReticle.isReticleOnSurface)
                {
                    placementPt = cameraReticle.transform.position;
                    if (everHadValidPlacement)
                    {
                        placementPt.y = lastValidPlacementPt.y;
                    }
                }

                // otherwise look for a nearby safe placement point
                else
                {
                    bool hasSurface;
                    Vector3 visibleValidGameboardPt;
                    gameboardHelper.FindClosestInnerGameboardPoint(cameraReticle.transform.position,
                                                                    out hasSurface,
                                                                    out placementPt,
                                                                    out visibleValidGameboardPt);

                    // if we have a surface, 
                    // use the closest inner gameboard point, but at previous good y pos
                    if (hasSurface && everHadValidPlacement)
                    {
                        placementPt.y = lastValidPlacementPt.y;
                    }

                    // if no surface, keep yeti at last valid point,
                    // or this is the first time, use the reticle position
                    else
                    {
                        placementPt = everHadValidPlacement ? lastValidPlacementPt : cameraReticle.transform.position;
                    }
                }
            }

            // else leave yeti where they are on gameboard
            else
            {
                placementPt = lastValidPlacementPt;
            }

            // cache this
            lastValidPlacementPt = placementPt;
            everHadValidPlacement = true;

            // place yeti at chosen placement point
            yetiAndSnowball.transform.position = placementPt;

            // keep yeti's y rotation toward the camera
            DemoUtil.FaceNPCToPlayer(yetiAndSnowball.gameObject);

            // move yeti forward a bit along its orientation, 
            // since its center point is not under its feet (due to hidden snowball)
            float yetiOriginOffset = yetiAndSnowball.GetYetiToCenterDist();
            yetiAndSnowball.transform.position += yetiAndSnowball.transform.forward * yetiOriginOffset;

            // Be sure yeti is visible
            if (!yetiAndSnowball.gameObject.activeSelf)
            {
                // show translucent yeti, with no snowball yet
                yetiAndSnowball.DisplaySnowball(false);
                yetiAndSnowball.SetTransparent(true);
                DemoUtil.DisplayWithBubbleScale(yetiAndSnowball.gameObject, show: true,
                                                targetScale: baseYetiSize);
            }
        }


        public void UpdateYetiInitialPlacementDone()
        {
            // show opaque yeti with snowball
            yetiAndSnowball.SetTransparent(false);
            yetiAndSnowball.DisplaySnowball(true);
        }

        public bool RePlaceDoty()
        {
            if (inStateGrow)
            {
                stateGrow.RewindToPlacement();
                return true;
            }

            else if (inStateBuild)
            {
                stateBuild.RewindToPlacement();
                return true;
            }

            return false;
        }


        public void UpdateYetiLocomotion()
        {
            // If player set a new destination for yeti
            if (destinationPointChanged && gameboardHelper != null)
            {
                destinationPointChanged = false;
                timeDestinationPointChanged = Time.time;

                // Always start on a valid surface point
                Vector3 startPoint = gameboardHelper.GetClosestPointOnCurrentSurface(yetiAndSnowball.transform.position);

                // Calculate walk path to destination
                List<Waypoint> waypoints = gameboardHelper.CalculateLocomotionPath(
                    startPos: startPoint,
                    endPos: destinationMarker.transform.position);

                // Walk to destination
                if (waypoints != null && waypoints.Count > 0)
                {
                    // NOTE: this should be the actual worldspace position where we want to arrive
                    yetiAndSnowball.Move(waypoints, destinationMarker.transform.position);
                }
            }
        }

        public void CacheYetiGameboardPosition()
        {
            validGameboardPoints.Add(yetiAndSnowball.transform.position);
        }


        // Based on position of camera reticle, or a given requested destination position,
        // choose a valid locomotion destination for the yeti
        // Returns false if gameboard is invalid
        public bool SetYetiDestination(bool useCameraReticle = true, Vector3? requestedDestPos = null)
        {
            Vector3 requestedDestinationPos = requestedDestPos ?? Vector3.zero;

            if (useCameraReticle)
            {
                // don't allow use of reticle if it's not being displayed 
                // (e.g., reticle is plane above camera)
                if (!cameraReticle.isReticleDisplayable) return true;

                requestedDestinationPos = cameraReticle.transform.position;
            }


            // if using reticle, and reticle on surface and not near perimeter, 
            // then use the reticle position for destination point
            bool nearPerimeter = gameboardHelper.IsPointNearPerimeterTile(requestedDestinationPos,
                                                                            yetiAndSnowball.GetYetiToSnowballDist());
            if (useCameraReticle && !nearPerimeter && cameraReticle.isReticleOnSurface)
            {
                destinationPos = requestedDestinationPos;
            }

            // otherwise look for a nearby safe position for destination point
            else
            {
                bool hasSurface;
                Vector3 visibleValidGameboardPt;
                gameboardHelper.FindClosestInnerGameboardPoint(requestedDestinationPos,
                                                                out hasSurface,
                                                                out destinationPos,
                                                                out visibleValidGameboardPt);
                // bail if there is no surface
                if (!hasSurface)
                {
                    return false;
                }
            }

            destinationPointChanged = true;

            // Special case for targeting Snowman NPC:
            // if snowman is visible, and we choose a destination near the snowman, 
            // then lock onto the snowman
            bool destinationCloseToSnowman = snowman.gameObject.activeSelf &&
                DemoUtil.GetXZDistance(destinationPos, snowman.transform.position) < snowmanHoverReticleDist;
            if (destinationCloseToSnowman)
            {
                destinationPos.x = snowman.transform.position.x;
                destinationPos.z = snowman.transform.position.z;
            }

            // cache this destination point
            validGameboardPoints.Add(destinationPos);

            // put destination marker just ABOVE the reticle
            Vector3 destinationMarkerPt = destinationPos;
            float reticleY = Mathf.Max(cameraReticle.transform.position.y, cameraReticle.lastSurfaceY);
            destinationMarkerPt.y = reticleY + 0.005f;

            destinationMarker.gameObject.SetActive(true);
            destinationMarker.transform.position = destinationMarkerPt;
            destinationMarker.transform.rotation = Quaternion.identity;

            return true;
        }


        // Periodically poll if gameboard has dynamically disappeared underneath each NPC
        // If so, teleport NPC to closest valid gameboard tile
        public void HandleDynamicGameboard(bool includeSnowman = false)
        {
            // wait till next poll time
            if (Time.time < dynamicGameboardPollTime) return;
            dynamicGameboardPollTime = Time.time + dynamicGameboardPollPeriod;

            // for each NPC requested            
            for (int i = 0; i < (includeSnowman ? 2 : 1); i++)
            {
                GameObject npc = i == 0 ? yetiAndSnowball.gameObject : snowman.gameObject;

                // find preferably visible, valid gameboard position closest to NPC 
                // (hopefully where the NPC currently is, so we don't have to teleport)
                bool hasSurface;
                Vector3 validGameboardPt;
                Vector3 visibleValidGameboardPt;
                gameboardHelper.FindClosestInnerGameboardPoint(
                    npc.transform.position,
                    out hasSurface,
                    out validGameboardPt,
                    out visibleValidGameboardPt,
                    // inner points are too strict - results in too much teleportation
                    // instead, allow for points anywhere still on or near gameboard edge
                    allowGameboardEdgePoints: true);

                // bump it up to NPC's y
                validGameboardPt.y = npc.transform.position.y;

                // how far is it from the NPC?
                float dist = DemoUtil.GetXZDistance(npc.transform.position, validGameboardPt);

                // if too far, it means gameboard has disappeared under NPC
                bool unstableGameboard = dist > gameboardHelper.tileSize;
                if (unstableGameboard)
                {
                    // For yeti, require 2 unstable gameboard periods in a row
                    if (i == 0)
                    {
                        if (unstableGameboardCtrForYeti++ == 0)
                        {
                            Debug.Log("Unstable gameboard for " + npc + ", waiting another " + dynamicGameboardPollPeriod + "s before teleporting");
                            return;
                        }
                    }

                    // TELEPORT NPC
                    Debug.Log("Teleporting " + npc + " from " + npc.transform.position + " to " + validGameboardPt);
                    npc.transform.position = validGameboardPt;

                    // For yeti, re-path any locomotion, if snowman not complete
                    if (npc == yetiAndSnowball.gameObject && yetiAndSnowball.Rolling)
                    {
                        yetiAndSnowball.Stop();

                        // Old: give up on this destination
                        //destinationMarker.gameObject.SetActive(false);

                        // New: Re-path to existing destination,
                        //  after a short delay to allow teleport to happen
                        if (!yetiAndSnowball.SnowmanComplete)
                        {
                            StartCoroutine(SetYetiDestinationRoutine(destinationMarker.gameObject.transform.position));
                        }
                    }
                }

                // if gameboard is stable for yeti, reset ctr
                else
                {
                    if (i == 0)
                    {
                        unstableGameboardCtrForYeti = 0;
                    }
                }
            }
        }

        private IEnumerator SetYetiDestinationRoutine(Vector3 destinationPos, float initialDelay = 0.25f)
        {
            float waitTill = Time.time + initialDelay;
            while (Time.time < waitTill) yield return null;

            SetYetiDestination(useCameraReticle: false, requestedDestPos: destinationPos);
        }


        public int GetNumYetiSteps()
        {
            return yetiAndSnowball.Footsteps;
        }


        public bool CreateSnowman()
        {
            Surface currentSurface = gameboardHelper.GetCurrentSurface();
            if (currentSurface == null)
            {
                return false;
            }

            // choose a previous yeti position, farthest from current position
            int which = 0;
            float farthestDist = 0f;
            for (int i = 0; i < validGameboardPoints.Count; i++)
            {
                float dist = DemoUtil.GetXZDistance(yetiAndSnowball.transform.position,
                                                        validGameboardPoints[i]);
                if (dist > farthestDist)
                {
                    farthestDist = dist;
                    which = i;
                }
            }

            // place snowman, with its base revealed
            Vector3 snowmanPosition = validGameboardPoints[which];
            snowmanPosition.y = gameboardHelper.tileHeight + currentSurface.Elevation;
            snowman.transform.position = snowmanPosition;

            DemoUtil.FaceNPCToPlayer(snowman.gameObject);
            ShowSnowman(true);
            snowman.RevealBase();

            return true;
        }

        public void ShowSnowman(bool showSnowman)
        {
            if (!showSnowman)
            {
                snowman.Reset();
            }

            snowman.transform.localScale = Vector3.one * baseYetiSize;
            snowman.gameObject.SetActive(showSnowman);
        }


        public void UpdateSnowmanHoverVFX()
        {
            bool reticleCloseToSnowman = DemoUtil.GetXZDistance(cameraReticle.transform.position,
                                            snowman.transform.position) < snowmanHoverReticleDist;

            bool destinationCloseToSnowman = DemoUtil.GetXZDistance(destinationPos,
                                                snowman.transform.position) < snowmanHoverReticleDist;

            bool shouldShowHover = reticleCloseToSnowman && !destinationCloseToSnowman;

            if ((!showingSnowmanHover && shouldShowHover) ||
                (showingSnowmanHover && !shouldShowHover))
            {
                showingSnowmanHover = !showingSnowmanHover;
                snowman.SetHover(showingSnowmanHover);
            }
        }


        public bool IsSnowballNearSnowman()
        {
            float dist = DemoUtil.GetXZDistance(yetiAndSnowball.transform.position,
                                                    snowman.transform.position);
            return dist < (baseSnowballCloseToSnowmanDist * (yetiSize / baseYetiSize));
        }

        public void CompleteSnowman()
        {
            yetiAndSnowball.Complete();
            snowman.RevealBody();
        }

        public void RestartGame()
        {
            validGameboardPoints.Clear();
            destinationMarker.gameObject.SetActive(false);

            // hide yeti and snowman
            DemoUtil.DisplayWithBubbleScale(yetiAndSnowball.gameObject, show: false);
            DemoUtil.DisplayWithBubbleScale(snowman.gameObject, show: false);

            yetiAndSnowball.ResetProgress();
        }

        public void UpdateYetiSize()
        {
            yetiAndSnowball.transform.localScale = new Vector3(yetiSize, yetiSize, yetiSize);
            yetiAndSnowball.yetiDynamicHeight.transform.localPosition = new Vector3(0, gameboardHelper.tileHeight / yetiSize, 0);
            yetiAndSnowball.yetiWalkSpeed = baseYetiWalkSpeed * (yetiSize / baseYetiSize);
            snowman.transform.localScale = new Vector3(yetiSize, yetiSize, yetiSize);
        }
    }
}
