// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDKExamples.Gameboard {
  public struct Configuration {

    /// Metric size of a grid cell.
    public float TileSize;

    /// The size of the kernel used to compute areal properties for each cell.
    /// @note This needs to be an odd integer.
    public int KernelSize;

    /// The standard deviation tolerance value to use when determining node noise within a cell,
    /// outside of which the cell is considered too noisy to be walkable.
    public float KernelStdDevTol;

    /// The maximum amount two cells can differ in elevation to be considered on the same plane.
    public float AgentStepHeight;

    /// The maximum distance an agent can jump in meters.
    public float AgentJumpDistance;

    /// Determines the cost of jumping.
    /// @note Low value prefers jumping on top of obstacles, high value makes the agent go around.
    public int JumpPenalty;

    /// Maximum slope angle (degrees) of an area to be considered flat.
    public float MaxSlope;

    /// Minimum elevation (meters) a GridNode is expected to have in order to be walkable
    public float MinElevation;

    /// Specifies the layer of the environment to raycast.
    public LayerMask LayerMask;
  }
}