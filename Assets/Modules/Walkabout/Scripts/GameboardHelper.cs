using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Niantic.ARDKExamples.Gameboard;

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// Constants, state, and helper methods for Gameboard used in Walkabout demo.
    /// Includes:
    ///  InstantiateGameboard()
    ///  PersistentScanRoutine() - periodically calls gameBoard.Scan() to update gameboard
    ///  GetClosestPointOnCurrentSurface()
    ///  RaycastToGameboard()
    ///  IsInnerGridNode()
    ///  DoesGameboardInnerNodeExist()
    ///  FindClosestInnerGameboardPoint()
    ///  CalculateLocomotionPath() - calls gameBoard.CalculatePath(_
    /// </summary>
    public class GameboardHelper : MonoBehaviour, ISceneDependency
    {
        // Defines a method to be used for selecting the surface to render
        public enum SurfaceSelectionMode
        {
            // Select the surface that is forward from the camera
            CameraForward,

            // Select the surface that is down from the actor on the gameBoard
            ActorDown
        }

        public const float baseTileSize = 0.2f;
        public const float baseTileHeight = 0.12f;
        private const int perimeterLayerIndex = 17;

        // don't allow NPCs to stand too close to the edge of the gameboard
        // e.g. for NPCs pushing objects that need a lot of clearance
        private float safeDistFromPerimeter = baseTileSize + 0.05f;

        public const int minNodesForValidGameboard = 20;
        public float tileSize;
        public float tileHeight;
        public const int kernelSize = 3;
        private const float maxSlope = 25f;
        private float scanInterval = 0.2f;
        private float scanRadius = 1.5f; //0.75f;
        public Vector3 raycastVectorOffset = Vector3.zero;

        Configuration gameBoardSettings = GameBoard.DefaultSettings;
        public static AppEvent<Surface> SurfaceUpdated = new AppEvent<Surface>();

        [SerializeField] private LayerMask raycastLayerMask;
        [SerializeField] private Camera _arCamera;

        private IGameBoard gameBoard = null;
        private Surface currentSurface = null;

        private Stack<GameObject> nodeTilePool = new Stack<GameObject>();
        private List<GameObject> activeTiles = new List<GameObject>();

        private Vector3 scanOrigin;
        private bool isScanning = false;

        private SurfaceSelectionMode surfaceSelectionMode = SurfaceSelectionMode.CameraForward;
        private Transform surfaceSelectionActorTransform;

        private WalkaboutManager walkaboutManager;


        void Awake()
        {
            walkaboutManager = SceneLookup.Get<WalkaboutManager>();

            // Assign the layer of the mesh
            gameBoardSettings.LayerMask = raycastLayerMask;

            gameBoardSettings.TileSize = baseTileSize;
            gameBoardSettings.MaxSlope = maxSlope;
            gameBoardSettings.KernelSize = kernelSize;

            // Our agent doesn't support jumping
            gameBoardSettings.AgentJumpDistance = 0;
            gameBoardSettings.JumpPenalty = int.MaxValue;

            // instantiate gameBoard
            tileSize = baseTileSize;
            tileHeight = baseTileHeight;

            InstantiateGameboard();

            SetIsScanning(false);
        }

        void OnEnable()
        {
            StartCoroutine(PersistentScanRoutine());
        }

        void OnDisable()
        {
        }


        private void InstantiateGameboard()
        {
            gameBoardSettings.TileSize = tileSize;

            Debug.Log("Instantiating gameBoard, tileSize " + tileSize);
            gameBoard = new GameBoard(gameBoardSettings);
        }


        // -----------------
        // Scanning to find gameBoard

        public void SetIsScanning(bool isScanning)
        {
            this.isScanning = isScanning;
        }

        public void SetSurfaceSelectionModeCameraForward()
        {
            surfaceSelectionMode = SurfaceSelectionMode.CameraForward;
        }

        public void SetSurfaceSelectionModeActorDown(Transform actorTransform)
        {
            surfaceSelectionMode = SurfaceSelectionMode.ActorDown;
            surfaceSelectionActorTransform = actorTransform;
        }

        // Get the surface selection ray based on the current SurfaceSelectionMode
        private Ray GetSurfaceSelectionRay()
        {
            // Get the surface selection ray based on the actor's down
            if (surfaceSelectionMode == SurfaceSelectionMode.ActorDown)
            {
                if (surfaceSelectionActorTransform != null)
                {
                    return new Ray(surfaceSelectionActorTransform.position, Vector3.down);
                }
                else
                {
                    Debug.LogWarning(name + " " + SurfaceSelectionMode.ActorDown +
                                        " got null actor. Defaulting to " + SurfaceSelectionMode.CameraForward);
                }
            }

            // If the method reaches this point, default to SurfaceSelectionMode.CameraForward
            var cameraPosition = _arCamera.transform.position;
            var cameraForward = _arCamera.transform.forward + raycastVectorOffset;
            return new Ray(cameraPosition, cameraForward);
        }


        // Periodically call gameBoard.Scan() to update gameboard
        // enabled/disabled by calling SetIsScanning() above
        private IEnumerator PersistentScanRoutine()
        {
            while (true)
            {
                if (isScanning)
                {
                    // Scan at camera reticle, from camera's height
                    if (walkaboutManager.cameraReticle.everFoundSurfaceForReticle)
                    {
                        scanOrigin = walkaboutManager.cameraReticle.transform.position;
                        scanOrigin.y = Camera.main.transform.position.y;
                    }

                    // Pre-reticle, scan from scanRadius forward from camera
                    else
                    {
                        Vector3 cameraForward = (Camera.main.transform.forward + raycastVectorOffset) * scanRadius;
                        scanOrigin = Camera.main.transform.position +
                                        Vector3.ProjectOnPlane(vector: cameraForward, planeNormal: Vector3.up).normalized;
                    }

                    // SCAN
                    gameBoard.Scan(scanOrigin, radius: scanRadius);


                    // Now with the latest surface(s) found,
                    // raycast from camera, to get the surface the camera is looking at
                    // OR if actor exists, raycast down from actor
                    gameBoard.RayCast(GetSurfaceSelectionRay(), out currentSurface, out Vector3 hit);

                    // If pointed at a surface, choose it as the surface to be rendering
                    if (currentSurface != null)
                    {
                        SurfaceUpdated.Invoke(currentSurface);
                    }
                }

                yield return new WaitForSeconds(scanInterval);
            }
        }

        public bool HasSurfaceToRender()
        {
            return currentSurface != null;
        }

        public int GetNumGameboardNodes()
        {
            int nodeCtr = 0;
            foreach (var surface in gameBoard.Surfaces)
            {
                foreach (var gridNode in surface.Elements)
                {
                    nodeCtr++;
                }
            }

            return nodeCtr;
        }

        public Surface GetCurrentSurface()
        {
            return currentSurface;
        }

        public Vector3 GetClosestPointOnCurrentSurface(Vector3 referencePoint)
        {
            if (gameBoard == null || currentSurface == null) return default;

            Vector3 surfacePoint =
                gameBoard.GetClosestPointOnSurface(currentSurface, referencePoint);

            return surfacePoint;
        }


        // -----------------
        // Player targeting of gameBoard

        public bool RaycastToGameboard(out float dist, out Vector3 gameBoardPt)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward + raycastVectorOffset);

            Surface surface;
            if (gameBoard.RayCast(ray, out surface, out gameBoardPt))
            {
                dist = Vector3.Distance(Camera.main.transform.position, gameBoardPt);
                return true;
            }

            dist = 0f;
            gameBoardPt = Vector3.zero;
            return false;
        }

        public bool IsInnerGridNode(GridNode gridNode)
        {
            Vector3 nodeWorldPosition = gameBoard.GridNodeToPosition(gridNode);
            return IsInnerGridNode(gridNode, nodeWorldPosition);
        }

        public bool IsInnerGridNode(GridNode gridNode, Vector3 nodeWorldPosition)
        {
            if (currentSurface == null || !currentSurface.ContainsElement(gridNode)) return false;

            bool isInnerGridNode =
                currentSurface.ContainsElement(new GridNode(gridNode.Coordinates + new Vector2Int(-1, +1))) &&      // NW
                currentSurface.ContainsElement(new GridNode(gridNode.Coordinates + new Vector2Int(+0, +1))) &&      // N
                currentSurface.ContainsElement(new GridNode(gridNode.Coordinates + new Vector2Int(+1, +1))) &&      // NE
                currentSurface.ContainsElement(new GridNode(gridNode.Coordinates + new Vector2Int(-1, +0))) &&      // W
                currentSurface.ContainsElement(new GridNode(gridNode.Coordinates + new Vector2Int(+1, +0))) &&      // E
                currentSurface.ContainsElement(new GridNode(gridNode.Coordinates + new Vector2Int(-1, -1))) &&      // SW
                currentSurface.ContainsElement(new GridNode(gridNode.Coordinates + new Vector2Int(+0, -1))) &&      // S
                currentSurface.ContainsElement(new GridNode(gridNode.Coordinates + new Vector2Int(+1, -1)));        // SE
            if (!isInnerGridNode) return false;

            // Also rule out inner grid nodes too close to a perimeter tile
            bool nearPerimeter = IsPointNearPerimeterTile(nodeWorldPosition, safeDistFromPerimeter);
            return !nearPerimeter;
        }


        public bool IsPointOnInnerGridNode(Vector3 gameBoardPt)
        {
            GridNode curGridNode = PositionToGridNode(gameBoardPt);
            return IsInnerGridNode(curGridNode, gameBoardPt);
        }

        // Converts a world position to a node on the game board.
        private GridNode PositionToGridNode(Vector3 worldPosition)
        {
            return new GridNode(PositionToTile(worldPosition));
        }

        // Converts a world position to grid coordinates.
        private Vector2Int PositionToTile(Vector3 position)
        {
            return new Vector2Int
            (
                Mathf.FloorToInt(position.x / tileSize),
                Mathf.FloorToInt(position.z / tileSize)
            );
        }


        // Is a point near a perimeter tile
        public bool IsPointNearPerimeterTile(Vector3 pos, float perimeterDist)
        {
            int layerMask = 1 << perimeterLayerIndex;
            Collider[] colliders = new Collider[1];
            int numPerimeterCollisions = Physics.OverlapSphereNonAlloc(pos, perimeterDist,
                                                                        colliders, layerMask,
                                                                        QueryTriggerInteraction.Collide);
            return numPerimeterCollisions > 0;
        }



        public bool DoesGameboardInnerNodeExist()
        {
            if (currentSurface == null) return false;

            foreach (GridNode gridNode in currentSurface.Elements)
            {
                if (IsInnerGridNode(gridNode))
                {
                    return true;
                }
            }

            return false;
        }


        public void FindClosestInnerGameboardPoint(Vector3 pos, out bool hasSurface,
                                                    out Vector3 validGameboardPt,
                                                    out Vector3 visibleValidGameboardPt,
                                                    bool allowGameboardEdgePoints = false)
        {
            // if there is no current surface, we're done
            if (currentSurface == null)
            {
                hasSurface = false;
                // Just put the original pos in these fields
                validGameboardPt = pos;
                visibleValidGameboardPt = pos;
                return;
            }

            hasSurface = true;
            Vector3 closestPosOnGameboard = gameBoard.GetClosestPointOnSurface(currentSurface, pos);

            bool isPosStillValid;
            if (!allowGameboardEdgePoints)
            {
                isPosStillValid = IsPointOnInnerGridNode(pos);
            }

            // if inner point not strictly required,
            // allow for points anywhere still on or near gameboard edge
            else
            {
                isPosStillValid = DemoUtil.GetXZDistance(pos, closestPosOnGameboard) < 0.1f;
            }

            // if the given point is still valid, we're done
            if (isPosStillValid)
            {
                validGameboardPt = pos;
                visibleValidGameboardPt = pos;
                return;
            }

            // otherwise search for the closest inner gameNode, preferring a visible one,
            // starting with the closest point on the gameboard to our given point
            pos = closestPosOnGameboard;

            // get camera's frustrum planes, to check for visibility
            Plane[] cameraFrustrumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

            bool foundInnerGridNode = false;
            float closestDist = 0f;
            Vector3 closestInnerGridNodePt = pos;

            bool foundVisibleInnerGridNode = false;
            float closestVisibleDist = 0f;
            Vector3 closestVisibleInnerGridNodePt = pos;

            foreach (GridNode gridNode in currentSurface.Elements)
            {
                if (IsInnerGridNode(gridNode))
                {
                    Vector3 gridNodePt = gameBoard.GridNodeToPosition(gridNode);
                    float dist = DemoUtil.GetXZDistance(pos, gridNodePt);

                    if (!foundInnerGridNode || dist < closestDist)
                    {
                        foundInnerGridNode = true;
                        closestDist = dist;
                        closestInnerGridNodePt = gridNodePt;
                    }

                    if (!foundVisibleInnerGridNode || dist < closestVisibleDist)
                    {
                        if (DemoUtil.IsPointVisibleBetweenFrustrumPlanes(gridNodePt, cameraFrustrumPlanes))
                        {
                            foundVisibleInnerGridNode = true;
                            closestVisibleDist = dist;
                            closestVisibleInnerGridNodePt = gridNodePt;
                        }
                    }
                }
            }

            // Prefer visible inner grid nodes, if any 
            // (even if farther from the original position than absolutely closest inner grid node)
            if (foundVisibleInnerGridNode)
            {
                foundInnerGridNode = foundVisibleInnerGridNode;
                closestDist = closestVisibleDist;
                closestInnerGridNodePt = closestVisibleInnerGridNodePt;
            }

            // Refine the position to be as close to the original point as possible,
            // staying within our inner gridNode
            if (foundInnerGridNode)
            {
                Vector3 vectorBetwPoints = pos - closestInnerGridNodePt;
                closestInnerGridNodePt = closestInnerGridNodePt + (vectorBetwPoints.normalized * (tileSize / 2f));
            }
            else
            {
                Debug.LogError("No inner grid nodes on gameBoard for placement/locomotion");
            }

            validGameboardPt = closestInnerGridNodePt;
            visibleValidGameboardPt = closestInnerGridNodePt;
        }


        // -----------------
        // Locomoting on gameBoard

        public List<Waypoint> CalculateLocomotionPath(Vector3 startPos, Vector3 endPos)
        {
            List<Waypoint> waypoints = gameBoard.CalculatePath(startPos, endPos, PathFindingBehaviour.SingleSurface);
            return waypoints;
        }



        // -----------------
        // Tiling gameBoard

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (isScanning)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawCube(scanOrigin, new Vector3(0.08f, 0.01f, 0.08f));
            }

            if (gameBoard == null) return;

            foreach (var surface in gameBoard.Surfaces)
            {
                foreach (var gridNode in surface.Elements)
                {
                    // is this an inner (non-edge) gridNode?
                    bool innerNode = IsInnerGridNode(gridNode);
                    Gizmos.color = innerNode ?
                        new Color(0.5f, 1f, 0.5f, 0.4f) :   // green
                        new Color(0.5f, 0.5f, 1f, 0.4f);    // blue

                    Vector3 nodeWorldPosition = gameBoard.GridNodeToPosition(gridNode);
                    Gizmos.DrawCube(nodeWorldPosition, new Vector3(tileSize * 0.9f, 0f, tileSize * 0.9f));
                }
            }
        }
#endif
    }
}
