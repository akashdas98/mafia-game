using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Highlighter : MonoBehaviour
{
  [SerializeField]
  private Renderer renderer; // Reference to the SpriteRenderer to be highlighted

  private Color originalColor; // To store the original color of the renderer
  private Color highlightColor = new Color(1f, 1f, 0.2f, 0.8f);

  private Color MultiplyColors(Color colorA, Color colorB)
  {
    // Directly multiply the color components
    return new Color(colorA.r * colorB.r, colorA.g * colorB.g, colorA.b * colorB.b, Mathf.Min(colorA.a, colorB.a));
  }

  // Call this method to apply the highlight effect
  public void Highlight()
  {
    ResetHighlight();
    if (renderer != null)
    {
      // Change the renderer's color to the highlight color
      renderer.material.color = MultiplyColors(originalColor, highlightColor);
    }
  }

  // Call this method to remove the highlight effect and revert to the original color
  public void ResetHighlight()
  {
    if (renderer != null)
    {
      // Revert the renderer's color to its original
      renderer.material.color = originalColor;
    }
  }

  void Start()
  {
    originalColor = renderer.material.color;
  }
}