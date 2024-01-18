using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities
{
  public static Vector3 Get8DirectionFromInput(float horizontal, float vertical)
  {
    // Ensure that at least one of the inputs is non-zero to calculate a direction
    if (horizontal != 0 || vertical != 0)
    {
      Vector3 launchDirection = new Vector3(horizontal, vertical, 0).normalized;
      return launchDirection;
    }
    else
    {
      return Vector3.zero; // No direction if there's no input
    }
  }
}