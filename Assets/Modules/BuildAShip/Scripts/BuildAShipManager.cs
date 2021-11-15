using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using Niantic.ARDKExamples.Gameboard;

namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// Constants, GameObjects, game state, and helper methods used by 
    /// various State classes in the BuildAShip demo.
    /// </summary>
    public class BuildAShipManager : MonoBehaviour, ISceneDependency
    {
        public const float baseYetiSize = 3.5f;

        [Header("ResourcesToCollectOrder")]
        [SerializeField] public EnvResource[] resourcesToCollectOrder;
        public const int numResourcesParticlesToCollect = 20;
        public int numResourcesCollected { get; set; } = 0;

        [Header("NPCs")]
        [SerializeField] public BuildAShipActor yetiActor;

        [Header("GUI")]
        [SerializeField] public Gauge progressGauge;

        [Header("Buttons")]
        [SerializeField] public GameObject collectButton;

        [Header("Reticle")]
        [SerializeField] public SurfaceReticle cameraReticle;

        [Header("State")]
        [SerializeField] public StateCollect stateCollect;
        [SerializeField] public StateCollectDone stateCollectDone;

        public void UpdateYetiInitialPlacement()
        {
            Vector3 placementPt;

            // If yeti NOT yet active
            if (!yetiActor.gameObject.activeSelf)
            {
                // if reticle is not yet on a surface, wait
                if (!cameraReticle.isReticleOnSurface) return;

                // first time placing yeti, place at reticle
                placementPt = cameraReticle.reticlePt;
            }

            // once yeti is already active, use valid placement point if any
            else if (cameraReticle.isValidPlacementPt)
            {
                placementPt = cameraReticle.validPlacementPt;
            }

            // else leave yeti where he is
            else
            {
                placementPt = yetiActor.gameObject.transform.position;
            }

            // place yeti at chosen placement point
            yetiActor.gameObject.transform.position = placementPt;

            // If yeti NOT yet active, show ghost yeti
            if (!yetiActor.gameObject.activeSelf)
            {
                yetiActor.SetTransparent(true);
                DemoUtil.DisplayWithBubbleScale(yetiActor.gameObject, show: true,
                                                targetScale: baseYetiSize);
            }

            // keep yeti's y rotation toward the camera
            DemoUtil.FaceNPCToPlayer(yetiActor.gameObject);
        }


        public void UpdateYetiInitialPlacementDone()
        {
            yetiActor.SetTransparent(false);
        }


        public EnvResource GetResourceToCollect()
        {
            if (numResourcesCollected < resourcesToCollectOrder.Length)
            {
                return resourcesToCollectOrder[numResourcesCollected];
            }

            return null;
        }

        public void RestartGame()
        {
            numResourcesCollected = 0;
        }

        public bool SkipCurrentStep()
        {
            if (stateCollect.running)
            {
                stateCollect.ExitStateEarly();
                return true;
            }

            return false;
        }

    }

}


