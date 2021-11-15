using System;

using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Semantics;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Niantic.ARDK.Extensions;

using Random = UnityEngine.Random;

namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// Display and animation of environment resource particles (grass, trees, sky) in BuildAShip demo.
    /// </summary>
    class BuildAShipResourceRenderer : MonoBehaviour, ISceneDependency
    {
        AppEvent<int> ActiveItemCountUpdated = new AppEvent<int>();
        AppEvent<int> CollectedItemCountUpdated = new AppEvent<int>();

        [SerializeField] private ARSessionManager arSessionManager = default;
        [SerializeField] private ARSemanticSegmentationManager semanticSegmentationManager;

        [SerializeField] Transform particleContainer = default;
        [SerializeField] GameObject tilePrefab = default;

        [SerializeField] public int activeItemCount = 0;
        [SerializeField] public int collectedItemCount = 0;
        private int collectingItemCount = 0;

        [Header("Grid")]
        [SerializeField] int tilesPerUnit = 16;
        [SerializeField] int sampleCount = 16;
        [SerializeField] float bottomMarginPercent;
        [SerializeField] float topMarginPercent;

        float activeTileThreshold = .25f;

        [Header("Vacuum")]
        [SerializeField] RectTransform collectButton = default;
        [SerializeField] GameObject resourceVacuum = default;
        [SerializeField] float vacuumOffset = 1f;
        [SerializeField] float showDuration = .25f;
        [SerializeField] float hideDuration = .5f;
        private MeshRenderer[] resourceVacuumRenderers;

        [Header("Speed Settings")]
        [SerializeField] float collectDelay = 1;
        [SerializeField] float collectSpeed = 3.5f;
        private float lastCollectTime;

        [Header("Editor Defaults")]
        [SerializeField] EnvResource defaultResource;
        [SerializeField] Texture2D defaultTexture;

        private Texture2D semanticTexture;
        public Texture2D SemanticTexture { get { return semanticTexture; } }

        private bool semanticTextureUpdated = false;
        public bool SemanticTextureProcessedSinceBecomingVisible { get; private set; }
        private EnvResource currentResource;

        private int gridWidth;
        private int gridHeight;

        private List<BuildAShipResourceTile> tiles = new List<BuildAShipResourceTile>();
        private List<BuildAShipResourceTile> activeTiles = new List<BuildAShipResourceTile>();
        private List<BuildAShipResourceTile> collectingTiles = new List<BuildAShipResourceTile>();

        public bool Visible { get; private set; }
        private bool collecting = false;

        private AudioManager audioManager;
        private SegmentedAudioLoop vacuumAudioLoop;

        void Awake()
        {
            audioManager = SceneLookup.Get<AudioManager>();

            Debug.Log(Camera.main.pixelHeight);

            float aspectRatio = (float)Camera.main.pixelWidth / (float)Camera.main.pixelHeight;
            //float croppedHeight = 1 - ((bottomMarginInPixels + topMarginInPixels) / (float)Camera.main.pixelHeight);
            //float bottomOffset = (bottomMarginInPixels / (float)Camera.main.pixelHeight);

            float croppedHeight = 1 - (bottomMarginPercent + topMarginPercent);
            float bottomOffset = bottomMarginPercent;

            // Calculate grid.
            gridHeight = Mathf.CeilToInt(tilesPerUnit * croppedHeight);
            gridWidth = Mathf.CeilToInt(tilesPerUnit * aspectRatio);

            // Scale container to 1 world units tall.
            float inverseRatio = 1f / ((float)tilesPerUnit - 1);
            particleContainer.localScale = Vector3.one * inverseRatio;
            particleContainer.localPosition = new Vector3(aspectRatio * -.5f, -.5f + bottomOffset, 0);

            // Initialize particle grid.
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int index = (y * gridWidth) + x;
                    GameObject tileInstance = Instantiate(tilePrefab, particleContainer);
                    BuildAShipResourceTile tile = tileInstance.GetComponent<BuildAShipResourceTile>();

                    // Indent every other row.
                    float offset = (y % 2 == 0) ? 0 : .5f;

                    Vector3 position = new Vector3(x + offset, y, 0);
                    tile.transform.localPosition = position;

                    tiles.Add(tile);
                }
            }

            // SDK setup.
            semanticSegmentationManager.SemanticBufferUpdated += OnSemanticBufferUpdated;

            // Set defaults.
#if UNITY_EDITOR
            {
                SetResource(defaultResource);
                semanticTexture = defaultTexture;
                semanticTextureUpdated = true;
            }
#endif
        }

        private void Start()
        {
            arSessionManager.EnableFeatures();
            resourceVacuum.SetActive(false);

            // Keep resource vacuum positioned.
            Vector3 buttonPosition = collectButton.gameObject.transform.position;
            buttonPosition.z = vacuumOffset;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(buttonPosition);
            resourceVacuum.transform.position = worldPosition;

            // Get vacuum renderers.
            resourceVacuumRenderers = resourceVacuum.GetComponentsInChildren<MeshRenderer>();
        }

        void Update()
        {
            // Process texture as updates come in.
            if (semanticTextureUpdated)
            {
                ProcessTexture(semanticTexture);
#if !UNITY_EDITOR
                semanticTextureUpdated = false;
#endif
            }

            // Animate tile size based on current samples.
            {
                activeTiles.Clear();

                for (int i = 0; i < tiles.Count; i++)
                {
                    float scale = ((float)tiles[i].samples / (float)sampleCount);
                    float distance = Vector3.Distance(tiles[i].transform.localPosition, new Vector3(gridWidth / 2, gridHeight / 2, 0));
                    float maxScale = 1 - Mathf.Clamp01(distance / gridHeight);
                    tiles[i].transform.localScale = Vector3.one * scale * maxScale;

                    // Find active tiles.
                    if (scale > activeTileThreshold) activeTiles.Add(tiles[i]);

                    // Shink if we're not visible.
                    if (!Visible) tiles[i].samples = Mathf.Clamp(tiles[i].samples - 1, 0, sampleCount);

                }

                activeItemCount = activeTiles.Count;
            }

            /// Spawn a new tile for colletion.
            {
                Vector3 basePosition = resourceVacuum.transform.position;
                Vector3 mouthPosition = resourceVacuum.transform.position - (resourceVacuum.transform.forward * .25f);
                basePosition = particleContainer.InverseTransformPoint(basePosition);
                mouthPosition = particleContainer.InverseTransformPoint(mouthPosition);

                if (collecting && Time.time - lastCollectTime > collectDelay &&
                    activeTiles.Count > 0 &&
                    collectingItemCount < BuildAShipManager.numResourcesParticlesToCollect)
                {
                    BuildAShipResourceTile randomActiveTile = activeTiles[Random.Range(0, activeTiles.Count - 1)];
                    randomActiveTile.samples = 0;

                    GameObject tileInstance = Instantiate(tilePrefab,
                        randomActiveTile.transform.position,
                        randomActiveTile.transform.rotation,
                        particleContainer);

                    BuildAShipResourceTile tile = tileInstance.GetComponent<BuildAShipResourceTile>();
                    tile.SetSprite(currentResource.ResourceSprite);

                    tile.startTime = Time.time;
                    tile.endTime = tile.startTime + (Vector3.Distance(tile.transform.localPosition, mouthPosition) * 1 / collectSpeed);

                    tile.positionCurveX.keys = new Keyframe[] {
                        new Keyframe(0, tile.transform.localPosition.x),
                        new Keyframe(.75f, mouthPosition.x),
                        new Keyframe(1, basePosition.x)
                    };
                    tile.positionCurveX.SmoothTangents(1, 0);

                    tile.positionCurveY.keys = new Keyframe[] {
                        new Keyframe(0, tile.transform.localPosition.y),
                        new Keyframe(.75f, mouthPosition.y),
                        new Keyframe(1, basePosition.y)
                    };
                    tile.positionCurveY.SmoothTangents(1, 0);

                    tile.positionCurveZ.keys = new Keyframe[] {
                        new Keyframe(0, tile.transform.localPosition.z),
                        new Keyframe(.75f, mouthPosition.z),
                        new Keyframe(1, basePosition.z)
                    };
                    tile.positionCurveZ.SmoothTangents(1, 0);

                    tile.scaleCurve.keys = new Keyframe[] {
                        new Keyframe(0, 1),
                        new Keyframe(.75f, 5),
                        new Keyframe(1, 0)
                    };
                    tile.scaleCurve.SmoothTangents(1, 0);

                    tile.rotationCurve.keys = new Keyframe[] {
                        new Keyframe(0, 0),
                        new Keyframe(.75f, 0),
                        new Keyframe(1, Random.Range(-135, 135))
                    };
                    tile.rotationCurve.SmoothTangents(1, 0);

                    collectingTiles.Add(tile);
                    collectingItemCount++;

                    lastCollectTime = Time.time;
                }
            }

            // Animate tiles being collected.
            for (int i = collectingTiles.Count - 1; i >= 0; i--)
            {
                BuildAShipResourceTile tile = collectingTiles[i];
                float t = (Time.time - tile.startTime) / (tile.endTime - tile.startTime);
                if (t <= 1)
                {
                    Vector3 position = new Vector3(
                        tile.positionCurveX.Evaluate(t),
                        tile.positionCurveY.Evaluate(t),
                        tile.positionCurveZ.Evaluate(t)
                    );
                    Vector3 scale = Vector3.one * tile.scaleCurve.Evaluate(t);
                    float rotation = tile.rotationCurve.Evaluate(t);

                    tile.transform.localPosition = position;
                    tile.transform.localScale = scale;
                    tile.transform.localRotation = Quaternion.AngleAxis(rotation, Vector3.forward);
                }
                else
                {
                    collectingTiles.RemoveAt(i);
                    Destroy(tile.gameObject);

                    audioManager.PlayAudioNonSpatial(audioKey: AudioKeys.SFX_Resource_Vacuum);
                    collectedItemCount++;
                    CollectedItemCountUpdated.Invoke(collectedItemCount);
                }
            }

            // WIP events.
            ActiveItemCountUpdated.Invoke(activeItemCount);
        }

        public void SetVisible(bool visible)
        {
            activeItemCount = 0;
            this.Visible = visible;

            if (!visible)
            {
                SemanticTextureProcessedSinceBecomingVisible = false;
            }
        }

        public void SetCollecting(bool collecting)
        {
            ShowResourceVacuum(collecting);

            collectedItemCount = 0;
            collectingItemCount = 0;
            this.collecting = collecting;
        }

        private void ShowResourceVacuum(bool show)
        {
            float duration = (show) ? showDuration : hideDuration;
            float startAlpha = GetVaccuumAlpha();
            float endAlpha = (show) ? 1 : 0;
            InterpolationUtil.EasedInterpolation(gameObject, gameObject,
                InterpolationUtil.EaseInOutCubic,
                duration,
                onStart: () =>
                 {
                     resourceVacuum.SetActive(true);
                 },
                onUpdate: (t) =>
                 {
                     float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                     SetVacuumAlpha(alpha);
                     resourceVacuum.transform.localScale = new Vector3(.05f, .05f, .045f + (.005f * alpha));
                 },
                onComplete: () =>
                 {
                     if (!show) resourceVacuum.SetActive(false);
                 }
            );

            if (show)
            {
                if (vacuumAudioLoop == null)
                {
                    vacuumAudioLoop = audioManager.PlaySegmentedLoop(
                        AudioKeys.SFX_Vacuum_Start,
                        AudioKeys.SFX_Vacuum_LP,
                        AudioKeys.SFX_Vacuum_End);
                }
            }
            else
            {
                if (vacuumAudioLoop != null)
                {
                    vacuumAudioLoop.Stop();
                    vacuumAudioLoop = null;
                }
            }
        }

        private float GetVaccuumAlpha()
        {
            MeshRenderer renderer = resourceVacuumRenderers[0];
            return renderer.material.GetFloat("_MasterAlpha");
        }

        private void SetVacuumAlpha(float alpha)
        {
            for (int i = 0; i < resourceVacuumRenderers.Length; i++)
            {
                MeshRenderer renderer = resourceVacuumRenderers[i];
                renderer.material.SetFloat("_MasterAlpha", alpha);
                renderer.material.SetFloat("_MasterAlpha", alpha);
            }
        }

        private void OnSemanticBufferUpdated(ContextAwarenessStreamUpdatedArgs<ISemanticBuffer> args)
        {
            if (semanticSegmentationManager == null)
            {
                Debug.LogError("SemanticSegmentationManager is null!");
                return;
            }

            // Grab most recent buffer.
            var semanticProcessor = semanticSegmentationManager.SemanticBufferProcessor;
            ISemanticBuffer semanticBuffer = semanticProcessor.AwarenessBuffer;

            if (semanticBuffer == null)
            {
                Debug.LogError("ISemanticBuffer is null!");
                return;
            }

            if (currentResource == null)
            {
                Debug.LogError("currentResource is null!");
                return;
            }

            int featureChannel = -1;
            string channelName = currentResource.Channel.ToString();
            string[] channelNames = semanticBuffer.ChannelNames;

            for (int i = 0; i < channelNames.Length; i++)
            {
                //Debug.LogFormat("Checking Feature Channel: {0} {1}", i, channelNames[i]);
                if (channelName.ToUpper() == channelNames[i].ToUpper())
                {
                    //Debug.LogFormat("Matched Feature Channel: {0} {1}", i, channelNames[i]);
                    featureChannel = i;
                }
            }

            if (featureChannel == -1)
            {
                Debug.LogWarning("Could not find feature channel:" + channelName);
            }

            // Update semantic texture.
            semanticProcessor.CopyToAlignedTextureARGB32
            (
                featureChannel,
                ref semanticTexture,
                Screen.orientation
            );
            semanticTextureUpdated = true;

        }

        public void SetResource(EnvResource resource)
        {
            if (resource == null) return;

            // Swap in new icon sprite texture.
            Sprite sprite = resource.ResourceIcon;
            foreach (BuildAShipResourceTile tile in tiles)
            {
                tile.SetSprite(sprite);
            }

            currentResource = resource;
        }

        public void ProcessTexture(Texture2D segmentationTexture)
        {
            if (segmentationTexture != null && Visible)
            {
                int bottomMargin = Mathf.FloorToInt(semanticTexture.height * bottomMarginPercent);
                int totalMargin =
                    bottomMargin + Mathf.FloorToInt(semanticTexture.height * topMarginPercent);
                int segmentationSampleableHeight = segmentationTexture.height - totalMargin;
                NativeArray<Color32> pixelData = segmentationTexture.GetRawTextureData<Color32>();

                // Map tiles to pixel coordinates and sample the texture.
                for (int i = 0; i < tiles.Count; i++)
                {
                    // Tile space to normalized space.
                    float normalizedX = Mathf.Clamp01(tiles[i].transform.localPosition.x / (float)(gridWidth - 1));
                    float normalizedY = Mathf.Clamp01(tiles[i].transform.localPosition.y / (float)(gridHeight - 1));

                    // Normalized space to pixel space.
                    int pixelX = Mathf.FloorToInt(normalizedX * (segmentationTexture.width - 1));
                    int pixelY =
                        Mathf.FloorToInt(normalizedY * (segmentationSampleableHeight - 1))
                        + bottomMargin; 

                    // Pixel coordinates to pixel index.
                    int pixelIndex = (pixelY * (segmentationTexture.width)) + pixelX;
                    if (pixelIndex >= pixelData.Length) return;

                    // Sample and store current pixel data.
                    Color32 pixelColor = pixelData[pixelIndex];
                    tiles[i].samples += (pixelColor.r > .5) ? 1 : -1;
                    tiles[i].samples = Mathf.Clamp(tiles[i].samples, 0, sampleCount);
                }

                SemanticTextureProcessedSinceBecomingVisible = true;
            }
            else
            {
                // Auto shrink if there is no texture.
                for (int i = 0; i < tiles.Count; i++)
                {
                    tiles[i].samples = Mathf.Clamp(tiles[i].samples, 0, sampleCount);
                }
            }
        }

    }
}
