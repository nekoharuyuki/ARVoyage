using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Niantic.ARDK.Extensions.Meshing;

namespace Niantic.ARVoyage.SnowballToss
{
    /// <summary>
    /// State in SnowballToss that waits for the player to scan the environment, to create a mesh.
    /// It listens for mesh updates from ARMeshUpdater.MeshObjectsUpdated, to know that
    /// a mesh has been created. 
    /// Its next state (set via inspector) is StateCountdown.
    /// </summary>
    public class StateScanning : MonoBehaviour
    {
        private string[] scanningTextStr = { "Scan your surroundings", "Keep scanning!" };

        private SnowballTossManager snowballTossManager;
        private FeaturePointHelper featurePointHelper;

        [Header("State machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        protected float initialDelay = 1f;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private TMPro.TMP_Text scanningText;
        private Fader fader;

        private bool foundMesh = false;

        // Give FeaturePointRendering a chance to show up
        protected const float minScanningDuration = 5f; //DemoUtil.minUIDisplayDuration;


        void Awake()
        {
            // We're not the first state; start off disabled
            gameObject.SetActive(false);

            snowballTossManager = SceneLookup.Get<SnowballTossManager>();
            featurePointHelper = SceneLookup.Get<FeaturePointHelper>();

            // Listen for mesh updates
            if (snowballTossManager._ARMeshUpdater != null)
            {
                snowballTossManager._ARMeshUpdater.MeshObjectsUpdated += MeshObjectsUpdated;
            }

            fader = SceneLookup.Get<Fader>();
        }

        void OnDestroy()
        {
            if (snowballTossManager._ARMeshUpdater != null)
            {
                snowballTossManager._ARMeshUpdater.MeshObjectsUpdated -= MeshObjectsUpdated;
            }
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader, initialDelay: initialDelay));

            // Turn on feature point rendering, to visualize the room
            featurePointHelper.Tracking = true;
            featurePointHelper.Spawning = true;

            running = true;
        }



        void MeshObjectsUpdated(MeshObjectsUpdatedArgs args)
        {
            if (args.BlocksUpdated != null)
            {
                Debug.Log("StateScanning found mesh");
                foundMesh = true;
            }
        }


        void Update()
        {
            if (!running) return;

            if (exitState != null)
            {
                Exit(exitState);
                return;
            }

            // Once a mesh has been found, move on to next state
            if (foundMesh &&
                Time.time - timeStartedState > minScanningDuration)
            {
                exitState = nextState;
            }
        }


        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Turn off feature point rendering
            featurePointHelper.Tracking = false;
            featurePointHelper.Spawning = false;

            // Fade out GUI
            yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);
        }

    }
}
