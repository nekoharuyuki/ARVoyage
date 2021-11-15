
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using Niantic.ARDKExamples.Gameboard;
using Niantic.ARVoyage.Walkabout;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Draw a reticle (sprite) on a horizontal xz plane found by raycasting from the camera into the world.
    /// If a Gameboard exists, it raycasts to that; otherwise it raycasts to find an AR plane.
    /// If the camera is pointing at the gameboard / AR plane, color the reticle as "valid", e.g. green.
    /// Otherwise color the reticle as "invalid", e.g. orange.
    /// </summary>
    public class SurfaceReticle : MonoBehaviour, ISceneDependency
    {
        private float reticleAngleDegOffset = 10f;

        // used for non-gameboard floor planes (GameboardHelper has its own safeDistFromPerimeter)
        private float safeDistFromFloorplanePerimeter = 1f;

        [SerializeField] private GameObject reticleSprite;
        [SerializeField] private SpriteRenderer reticleSpriteRenderer;

        private GameboardHelper gameboardHelper;
        private ARPlaneHelper arPlaneHelper;

        public Vector3 reticlePt { get; private set; }
        Vector3 raycastVectorOffset;
        public bool isReticleOnSurface { get; private set; } = false;
        public bool isReticleDisplayable { get; private set; } = false;
        public bool everFoundSurfaceForReticle { get; set; } = false;
        public float lastSurfaceY { get; private set; } = 0f;
        private float reticleTooCloseDist = 0.3f;
        private float reticleTooFarDist = 10f;

        public Vector3 validPlacementPt { get; private set; }
        public bool isValidPlacementPt { get; private set; } = false;
        private Color validReticleColor = new Color(20f / 256f, 168f / 256f, 85f / 256f, 1f);
        private Color invalidReticleColor = new Color(1f, 0.5f, 0f, 1f);

        public bool isTooCloseToGameObject { get; private set; } = false;


        void Awake()
        {
            arPlaneHelper = SceneLookup.Get<ARPlaneHelper>(warnIfNotFound: false);

            // GameboardHelper may or may not exist in the demo scene (it's optional)
            gameboardHelper = SceneLookup.Get<GameboardHelper>(warnIfNotFound: false);

            UpdateRaycastVectorOffset();
        }


        public void DisplayReticle(bool val)
        {
            reticleSprite.gameObject.SetActive(val);
        }


        public void UpdateReticle(GameObject gameObjectForProximityCheck = null,
                                    float closeToGameObjectDist = 0f)
        {
            float rayDist;
            Vector3 surfacePt;
            float surfaceYOffset = 0;

            // ###
            // If the demo scene has a GAMEBOARD
            if (gameboardHelper != null)
            {
                surfaceYOffset = gameboardHelper.tileHeight;

                // get current surface height
                Surface gameboardSurface = gameboardHelper.GetCurrentSurface();
                if (gameboardSurface != null)
                {
                    lastSurfaceY = gameboardSurface.Elevation + surfaceYOffset;
                }

                // raycast to gameboard
                isReticleOnSurface = gameboardHelper.RaycastToGameboard(out rayDist, out surfacePt);
                if (isReticleOnSurface)
                {
                    // elevate the point to gameboard surface (e.g., if it has snow on it)
                    surfacePt.y = lastSurfaceY;

                    isValidPlacementPt = gameboardHelper.IsPointOnInnerGridNode(surfacePt);

                    everFoundSurfaceForReticle = true;
                }
                else
                {
                    isValidPlacementPt = false;
                }
            }

            // ###
            // otherwise raycast into the world to find an AR FLOOR PLANE
            else
            {
                RaycastHit hit;
                isReticleOnSurface = Physics.Raycast(
                                        Camera.main.transform.position,
                                        Camera.main.transform.TransformDirection(Vector3.forward + raycastVectorOffset),
                                        out hit);
                surfacePt = hit.point;

                if (isReticleOnSurface)
                {
                    everFoundSurfaceForReticle = true;
                    lastSurfaceY = surfacePt.y;
                }

                isValidPlacementPt = isReticleOnSurface &&
                                        IsPointWithinARPlanePerimeter(surfacePt, safeDistFromFloorplanePerimeter);

                rayDist = isValidPlacementPt ? Vector3.Distance(Camera.main.transform.position, surfacePt) : 0f;
            }


            // Check if too close to given game object
            if (gameObjectForProximityCheck != null)
            {
                isTooCloseToGameObject = DemoUtil.GetXZDistance(gameObjectForProximityCheck.transform.position,
                                                                    this.transform.position) < closeToGameObjectDist;
                isValidPlacementPt = isValidPlacementPt && !isTooCloseToGameObject;
            }
            else
            {
                isTooCloseToGameObject = false;
            }

            // Draw a reticle once we've ever found a surface (using cached ray length as needed)
            if (everFoundSurfaceForReticle)
            {
                float cameraXAngle = Camera.main.transform.eulerAngles.x + reticleAngleDegOffset;
                if (cameraXAngle > 180f) cameraXAngle -= 360f;

                // Put reticle at surface point if we have one
                if (isReticleOnSurface)
                {
                    this.transform.position = surfacePt;
                }

                // or at the end of the player's ray, using last best-known surface y position
                else
                {
                    // determine the distance from camera along its ray to the last best-known surface y position
                    float cameraToSurfaceHeight = Camera.main.transform.position.y - lastSurfaceY;
                    if (cameraXAngle == 0f) cameraXAngle = 1f;  // avoid divide by 0
                    rayDist = cameraToSurfaceHeight / Mathf.Sin(cameraXAngle * Mathf.Deg2Rad);

                    // place reticle there
                    this.transform.rotation = Camera.main.transform.rotation;
                    this.transform.position = Camera.main.transform.position;
                    this.transform.position += (this.transform.forward + raycastVectorOffset) * rayDist;

                    // ensure reticle height is as least lastSurfaceY
                    Vector3 finalPos = this.transform.position;
                    finalPos.y = Mathf.Max(finalPos.y, lastSurfaceY);
                    this.transform.position = finalPos;
                }

                this.transform.rotation = Quaternion.identity;
                reticlePt = this.transform.position;

                // hide reticle if camera is pointed upward
                isReticleDisplayable = cameraXAngle > 0f;

                // hide reticle if it's too close or too far to/from camera
                if (isReticleDisplayable)
                {
                    float dist = Vector3.Distance(Camera.main.transform.position, reticlePt);
                    isReticleDisplayable = dist > reticleTooCloseDist && dist < reticleTooFarDist;
                }

                // show/hide reticle, set color
                DisplayReticle(isReticleDisplayable);
                reticleSpriteRenderer.color = isValidPlacementPt ? validReticleColor : invalidReticleColor;
            }

            if (isValidPlacementPt)
            {
                validPlacementPt = this.transform.position + (Vector3.up * -surfaceYOffset);
            }
        }


        public void UpdateRaycastVectorOffset()
        {
            raycastVectorOffset = Vector3.down * reticleAngleDegOffset * Mathf.Deg2Rad;

            if (gameboardHelper != null)
            {
                gameboardHelper.raycastVectorOffset = raycastVectorOffset;
            }
        }


        private bool IsPointWithinARPlanePerimeter(Vector3 pos, float safeDistFromPerimeter)
        {
            if (arPlaneHelper != null)
            {
                // Get latest list of AR planes
                List<ARPlane> arPlanes = arPlaneHelper.GetPlanes();

                if (arPlanes.Count > 0)
                {
                    // Find which AR plane the pos is on
                    ARPlane myARPlane = null;
                    bool found = false;
                    foreach (ARPlane arPlane in arPlanes)
                    {
                        if (Mathf.Abs(arPlane.gameObject.transform.position.y - pos.y) < 0.01f)
                        {
                            myARPlane = arPlane;
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        myARPlane = arPlanes[0];
                    }

                    // Get the arPlane's box collider
                    BoxCollider arPlaneBoxCollider = myARPlane.GetComponentInChildren<BoxCollider>();
                    if (arPlaneBoxCollider == null)
                    {
                        return false;
                    }

                    // Place the position to test in the box collider's space
                    Vector3 posBoxSpace = arPlaneBoxCollider.transform.InverseTransformPoint(pos);

                    // Reduce the bounds by safeDistFromPerimeter
                    Vector3 size = arPlaneBoxCollider.size;
                    size.x = Mathf.Max((size.x - safeDistFromPerimeter * 2f), 0f);
                    size.z = Mathf.Max((size.z - safeDistFromPerimeter * 2f), 0f);
                    Bounds bounds = new Bounds(arPlaneBoxCollider.center, size);

                    // Check if point is within the reduced bounds
                    bool isPointWithin = bounds.Contains(posBoxSpace);
                    return isPointWithin;
                }
            }

            return false;
        }


        // ------
        // Debug

        public const float reticleAngleOffsetDelta = 5f;

        public void DebugMenuIncrementReticleAngleOffsetButton()
        {
            reticleAngleDegOffset += reticleAngleOffsetDelta;
            UpdateRaycastVectorOffset();
        }

        public void DebugMenuDecrementReticleAngleOffsetButton()
        {
            reticleAngleDegOffset -= reticleAngleOffsetDelta;
            UpdateRaycastVectorOffset();
        }

    }

}


