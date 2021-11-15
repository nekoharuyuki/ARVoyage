using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Niantic.ARDKExamples.Gameboard;

namespace Niantic.ARVoyage.Walkabout
{
    public enum MeshTileType
    {
        Fill,
        Corner,
        Notch,
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Creates a mesh of snow-tiles on the gameboard in the Walkabout demo.
    /// </summary>
    public class WalkaboutTileBuilder : MonoBehaviour, ISceneDependency
    {
        [SerializeField] List<WalkaboutMeshTile> meshTilePrefabs;
        [SerializeField] Transform tileContainer;

        private GameboardHelper gameboardHelper;
        private HashSet<GridNode> lastSurfaceElements;

        Dictionary<MeshTileType, Pooler<GameObject>> meshTilePools =
            new Dictionary<MeshTileType, Pooler<GameObject>>() {
                {MeshTileType.Fill, new Pooler<GameObject>()},
                {MeshTileType.Corner, new Pooler<GameObject>()},
                {MeshTileType.Notch, new Pooler<GameObject>()},
                {MeshTileType.Horizontal, new Pooler<GameObject>()},
                {MeshTileType.Vertical, new Pooler<GameObject>()}
            };

        void Awake()
        {
            GameboardHelper.SurfaceUpdated.AddListener(BuildMesh);
            gameboardHelper = SceneLookup.Get<GameboardHelper>();

            WalkaboutMeshTile GetTileByType(MeshTileType tileType)
            {
                return meshTilePrefabs.FirstOrDefault(tile => tile.type == tileType);
            }

            foreach (MeshTileType meshTileType in meshTilePools.Keys)
            {
                meshTilePools[meshTileType].Initialize(() =>
                {
                    WalkaboutMeshTile meshTile = GetTileByType(meshTileType);
                    GameObject instance = Instantiate(meshTile.gameObject, transform);

                    instance.transform.SetParent(tileContainer);
                    instance.SetActive(false);

                    return instance;
                }, 128);
            }
        }

        void OnDestroy()
        {
            GameboardHelper.SurfaceUpdated.RemoveListener(BuildMesh);
        }

        public void BuildMesh(Surface surface)
        {
            if (surface == null) return;

            // Create new tiles, but skip existing ones.
            {
                HashSet<GridNode> surfaceElements = new HashSet<GridNode>(surface.Elements);

                // Only update if the element set isn't identical.
                if (lastSurfaceElements != null && surfaceElements.SetEquals(lastSurfaceElements)) return;
                lastSurfaceElements = surfaceElements;

                // Repool all existing tiles.
                foreach (MeshTileType meshTileType in meshTilePools.Keys)
                {
                    meshTilePools[meshTileType].ReturnAll((GameObject instance) =>
                    {
                        instance.SetActive(false);
                    });
                }

                int createCount = 0;
                foreach (GridNode node in surface.Elements)
                {

                    float tileSize = gameboardHelper.tileSize;
                    float halfSize = tileSize / 2.0f;
                    float tileHeight = gameboardHelper.tileHeight;

                    Vector3 origin = new Vector3(
                        node.Coordinates.x * tileSize + halfSize,
                        surface.Elevation,
                        node.Coordinates.y * tileSize + halfSize
                    );

                    int x = node.Coordinates.x;
                    int y = node.Coordinates.y;

                    bool SW = surface.ContainsElement(new GridNode(new Vector2Int(x - 1, y - 1)));
                    bool S = surface.ContainsElement(new GridNode(new Vector2Int(x, y - 1)));
                    bool SE = surface.ContainsElement(new GridNode(new Vector2Int(x + 1, y - 1)));
                    bool W = surface.ContainsElement(new GridNode(new Vector2Int(x - 1, y)));
                    bool E = surface.ContainsElement(new GridNode(new Vector2Int(x + 1, y)));
                    bool NW = surface.ContainsElement(new GridNode(new Vector2Int(x - 1, y + 1)));
                    bool N = surface.ContainsElement(new GridNode(new Vector2Int(x, y + 1)));
                    bool NE = surface.ContainsElement(new GridNode(new Vector2Int(x + 1, y + 1)));

                    void CreateTile(string prefix, MeshTileType tileType, Vector3 position, Quaternion rotation)
                    {
                        GameObject instance = meshTilePools[tileType].BorrowItem();
                        if (instance == null) return;

                        instance.transform.localPosition = origin + position;
                        instance.transform.localRotation = rotation;
                        instance.name = prefix + "_" + tileType.ToString();
                        instance.SetActive(true);

                        Vector3 scale = new Vector3(tileSize / .24f, tileHeight / .07688f, tileSize / .24f);
                        instance.transform.localScale = scale;
                    }

                    // SW
                    {
                        Vector3 position = Vector3.zero;
                        Quaternion rotation = Quaternion.identity;

                        if (!W && !S)
                            CreateTile("SW", MeshTileType.Corner, position, rotation);
                        else if (W && S && !SW)
                            CreateTile("SW", MeshTileType.Notch, position, rotation);
                        else if (W && !S)
                            CreateTile("SW", MeshTileType.Horizontal, position, rotation);
                        else if (!W && S)
                            CreateTile("SW", MeshTileType.Vertical, position, rotation);
                        else
                            CreateTile("SW", MeshTileType.Fill, position, rotation);
                    }

                    // SE
                    {
                        Vector3 position = Vector3.zero;
                        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, -Vector3.right);

                        if (!E && !S)
                            CreateTile("SE", MeshTileType.Corner, position, rotation);
                        else if (E && S && !SE)
                            CreateTile("SE", MeshTileType.Notch, position, rotation);
                        else if (E && !S)
                            CreateTile("SE", MeshTileType.Vertical, position, rotation);
                        else if (!E && S)
                            CreateTile("SE", MeshTileType.Horizontal, position, rotation);
                        else
                            CreateTile("SE", MeshTileType.Fill, position, rotation);
                    }

                    // NW
                    {
                        Vector3 position = Vector3.zero;
                        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, Vector3.right);

                        if (!W && !N)
                            CreateTile("NW", MeshTileType.Corner, position, rotation);
                        else if (W && N && !NW)
                            CreateTile("NW", MeshTileType.Notch, position, rotation);
                        else if (W && !N)
                            CreateTile("NW", MeshTileType.Vertical, position, rotation);
                        else if (!W && N)
                            CreateTile("NW", MeshTileType.Horizontal, position, rotation);
                        else
                            CreateTile("NW", MeshTileType.Fill, position, rotation);
                    }

                    // NE
                    {
                        Vector3 position = Vector3.zero;
                        Quaternion rotation = Quaternion.AngleAxis(180, Vector3.up);

                        if (!E && !N)
                            CreateTile("NE", MeshTileType.Corner, position, rotation);
                        else if (E && N && !NE)
                            CreateTile("NE", MeshTileType.Notch, position, rotation);
                        else if (E && !N)
                            CreateTile("NE", MeshTileType.Horizontal, position, rotation);
                        else if (!E && N)
                            CreateTile("NE", MeshTileType.Vertical, position, rotation);
                        else
                            CreateTile("NE", MeshTileType.Fill, position, rotation);
                    }

                    createCount++;
                }

                // if (duplicateCount > 0) Debug.Log("Build Mesh Duplicate: " + duplicateCount);
                if (createCount > 0) Debug.Log("Build Mesh Create: " + createCount);
            }
        }
    }
}