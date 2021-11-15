// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDKExamples.Il2Cpp;

using UnityEngine;

namespace Niantic.ARDKExamples.Gameboard
{
  internal static class Utils
  {
    /// Calculates the euclidean distance between two nodes.
    /// @notes Used during path finding.
    public static float EuclideanDistance(PathNode from, PathNode to)
    {
      return Vector2Int.Distance(from.Coordinates, to.Coordinates);
    }

    /// Calculates the manhattan distance between two nodes.
    /// @notes Used during path finding.
    public static int ManhattanDistance(PathNode from, PathNode to)
    {
      return Math.Abs
          (from.Coordinates.x - to.Coordinates.x) +
        Math.Abs(from.Coordinates.y - to.Coordinates.y);
    }

    /// Calculates the standard deviation of the provided sample.
    public static float CalculateStandardDeviation(IEnumerable<float> samples)
    {
      var m = 0.0f;
      var s = 0.0f;
      var k = 1;
      foreach (var value in samples)
      {
        var tmpM = m;
        m += (value - tmpM) / k;
        s += (value - tmpM) * (value - m);
        k++;
      }

      return Mathf.Sqrt(s / Mathf.Max(1, k - 1));
    }

    /// Fits a plane to best align with the specified set of points.
    public static void FitPlane
    (
      Vector3[] points,
      out Vector3 position,
      out Vector3 normal,
      int iterations = 100
    )
    {
      // Find the primary principal axis
      var primaryDirection = Vector3.forward;
      FitLine(points, out position, ref primaryDirection, iterations / 2);

      // Flatten the points along that axis
      var flattenedPoints = new Vector3[points.Length];
      Array.Copy(points, flattenedPoints, points.Length);
      var flattenedPointsLength = flattenedPoints.Length;
      for (var i = 0; i < flattenedPointsLength; i++)
      {
        flattenedPoints[i] = Vector3.ProjectOnPlane
            (points[i] - position, primaryDirection) +
          position;
      }

      // Find the secondary principal axis
      var secondaryDirection = Vector3.right;
      FitLine(flattenedPoints, out position, ref secondaryDirection, iterations / 2);

      normal = Vector3.Cross(primaryDirection, secondaryDirection).normalized;
    }

    // Compile this method without throwing any array exceptions.
    // This ultimately improves performance, since this method
    // is heavily used during GameBoard scans.
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    private static void FitLine
    (
      IList<Vector3> points,
      out Vector3 origin,
      ref Vector3 direction,
      int iterations = 100
    )
    {
      if (direction == Vector3.zero || float.IsNaN(direction.x) || float.IsInfinity(direction.x))
      {
        direction = Vector3.up;
      }

      // Calculate Average
      origin = Vector3.zero;
      var len = points.Count;
      for (var i = 0; i < len; i++)
      {
        origin += points[i];
      }

      origin /= len;

      // Step the optimal fitting line approximation:
      var newDirection = new Vector3(0.0f,0.0f, 0.0f);
      var zero = new Vector3(0.0f, 0.0f, 0.0f);
      var point = zero;
      for (var iter = 0; iter < iterations; iter++)
      {
        newDirection.x = 0.0f;
        newDirection.y = 0.0f;
        newDirection.z = 0.0f;

        for (var i = 0; i < len; i++)
        {
          var p = points[i];

          point.x = p.x - origin.x;
          point.y = p.y - origin.y;
          point.z = p.z - origin.z;

          var dot = (float)(direction.x * (double)point.x +
            direction.y * (double)point.y +
            direction.z * (double)point.z);

          newDirection = new Vector3
          (
            newDirection.x + point.x * dot,
            newDirection.y + point.y * dot,
            newDirection.z + point.z * dot
          );
        }

        var mag = Mathf.Sqrt
        (
          (float)(newDirection.x * (double)newDirection.x +
            newDirection.y * (double)newDirection.y +
            newDirection.z * (double)newDirection.z)
        );

        const double eps = 9.999999747378752E-06;
        direction = (double)mag > eps ? newDirection / mag : zero;
      }
    }

    /// Insert a value into an IList{T} that is presumed to be already sorted such that sort
    /// ordering is preserved.
    /// @notes https://www.jacksondunstan.com/articles/3189
    public static void InsertIntoSortedList<T>(this IList<T> list, T value, Comparison<T> comparison)
    {
      var startIndex = 0;
      var endIndex = list.Count;
      while (endIndex > startIndex)
      {
        var windowSize = endIndex - startIndex;
        var middleIndex = startIndex + (windowSize / 2);
        var middleValue = list[middleIndex];
        var compareToResult = comparison(middleValue, value);
        if (compareToResult == 0)
        {
          list.Insert(middleIndex, value);
          return;
        }

        if (compareToResult < 0)
        {
          startIndex = middleIndex + 1;
        }
        else
        {
          endIndex = middleIndex;
        }
      }
      list.Insert(startIndex, value);
    }
  }
}
