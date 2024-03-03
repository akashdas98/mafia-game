using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class Utilities
{
  public static Vector2 GetDirectionFromInput(float horizontal, float vertical)
  {
    // Ensure that at least one of the inputs is non-zero to calculate a direction
    if (horizontal != 0 || vertical != 0)
    {
      Vector2 launchDirection = new Vector2(horizontal, vertical).normalized;
      return launchDirection;
    }
    else
    {
      return Vector2.zero; // No direction if there's no input
    }
  }

  public static int CircularShift(int current, int amount, int size)
  {
    int shift = (amount % size + size) % size;

    current = (current + shift) % size;

    return current;
  }

  public static Vector2 FindPointAtDistance(Vector2 source, Vector2 target, float distance)
  {
    // Calculate the direction vector from the source to the target
    Vector2 direction = target - source;

    // Normalize the direction vector
    direction.Normalize();

    // Scale the normalized direction by the desired distance
    Vector2 newPoint = source + direction * distance;

    return newPoint;
  }

  public static Vector2? FindVerticalProjectionIntersection(Vector2 src, Vector2 dest, Vector2 point)
  {
    float deltaY = dest.y - src.y;
    float deltaX = dest.x - src.x; // Corrected for consistency

    // Check for vertical src-dest line
    if (deltaX == 0)
    {
      if (point.x == src.x && point.y >= Mathf.Min(src.y, dest.y) && point.y <= Mathf.Max(src.y, dest.y))
      {
        return new Vector2(src.x, point.y); // Point's y on the vertical line
      }
      return null; // No intersection within the segment
    }

    float slope = deltaY / deltaX;
    float yIntercept = src.y - slope * src.x; // y = mx + b => b = y - mx

    // For a vertical projection, x remains the same, find y using the line equation
    float intersectionY = slope * point.x + yIntercept;

    // Ensure the intersection is within the segment's y bounds
    if (intersectionY >= Mathf.Min(src.y, dest.y) && intersectionY <= Mathf.Max(src.y, dest.y))
    {
      // Check if the projected point's x is within the x bounds of the src and dest
      if (point.x >= Mathf.Min(src.x, dest.x) && point.x <= Mathf.Max(src.x, dest.x))
      {
        return new Vector2(point.x, intersectionY);
      }
    }

    return null;
  }

  public static bool LinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2? intersection)
  {
    intersection = null;
    float d = (a2.x - a1.x) * (b2.y - b1.y) - (a2.y - a1.y) * (b2.x - b1.x);
    if (d == 0) return false; // Parallel lines

    float u = ((b1.x - a1.x) * (b2.y - b1.y) - (b1.y - a1.y) * (b2.x - b1.x)) / d;
    float v = ((b1.x - a1.x) * (a2.y - a1.y) - (b1.y - a1.y) * (a2.x - a1.x)) / d;

    if (u < 0 || u > 1 || v < 0 || v > 1) return false; // Intersection not within the segments

    intersection = a1 + u * (a2 - a1);
    return true;
  }

  public static float RoundToDecimalPlaces(float value, int decimalPlaces)
  {
    float scale = Mathf.Pow(10, decimalPlaces);
    return Mathf.Round(value * scale) / scale;
  }
}