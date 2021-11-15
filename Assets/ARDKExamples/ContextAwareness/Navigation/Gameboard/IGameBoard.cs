// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Collections.ObjectModel;

using UnityEngine;

namespace Niantic.ARDKExamples.Gameboard
{
  public interface IGameBoard
  {
    /// The number of distinct planes discovered.
    int NumberOfPlanes { get; }
    
    /// Creates a copy of the internal surface container to inspect.
    ReadOnlyCollection<Surface> Surfaces { get; }
    
    /// Searches for walkable areas in the environment.
    /// @param origin Origin of the scan in world position.
    /// @param radius Radius from origin to consider.
    void Scan(Vector3 origin, float radius);
    
    /// Removes all surfaces from the board.
    void Clear();

    /// Removes nodes outside the specified area.
    /// @param keepNodesFromOrigin Defines an origin in world position from which nodes will be retained.
    /// @param withinExtent Extent (metric) of the area where nodes need to be retained.
    void Clear(Vector3 keepNodesFromOrigin, float withinExtent);

    /// Checks whether an area is free to occupy (by a game object).
    /// @param center Origin of the area in world position.
    /// @param extent Width/Length (metric) of the object's estimated footprint.
    bool CanFitObject(Vector3 center, float extent);
    
    /// Calculates a walkable path between the two specified positions.
    /// @param fromPosition Start position.
    /// @param toPosition Destination position
    /// @param behaviour Path finding agent behaviour/algorithm to employ during the search.
    /// @returns A list of waypoints in world coordinates.
    List<Waypoint> CalculatePath
    (
      Vector3 fromPosition,
      Vector3 toPosition,
      PathFindingBehaviour behaviour
    );
    
    /// Raycasts the specified plane of the GameBoard.
    /// @param surface The surface within the game board to raycast.
    /// @param ray Ray to perform this function with.
    /// @param hitPoint Hit point in world coordinates, if any.
    /// @returns True if the ray hit a point on the target plane.
    bool RayCast(Surface surface, Ray ray, out Vector3 hitPoint);
    
    /// Raycasts the GameBoard.
    /// @param ray Ray to perform this function with.
    /// @param surface The surface hit by the ray, if any.
    /// @param hitPoint Hit point in world coordinates, if any.
    /// @returns True if the ray hit a point on any plane within the game board.
    bool RayCast(Ray ray, out Surface surface, out Vector3 hitPoint);
    
    /// Builds the geometry of the provided plane and copies it to the mesh.
    /// @note Any previous data stored in the mesh will be cleared.
    /// @param surface Surface to visualize.
    /// @param mesh A pre-allocated mesh.
    void UpdateSurfaceMesh(Surface surface, Mesh mesh);
    
    /// Returns the closest point on the specified surface to the reference point.
    /// @param surface The surface to find the closest point on to the reference.
    /// @param reference The reference point to find the closest point to.
    /// @returns The closest point to the reference in world coordinates.
    Vector3 GetClosestPointOnSurface(Surface surface, Vector3 reference);
    
    /// Converts a node on the game board to its corresponding world position.
    /// @param node A grid node acquired from an existing plane of the game board.
    /// @returns World position of the centroid of the node.
    Vector3 GridNodeToPosition(GridNode node);
  }
}
