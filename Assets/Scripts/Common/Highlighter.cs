using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


public class Highlighter : MonoBehaviour
{
  private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

  [SerializeField]
  [FormerlySerializedAs("renderer")]
  private Renderer targetRenderer; // Reference to the SpriteRenderer to be highlighted

  private Color originalColor; // To store the original color of the renderer
  private Color highlightColor = new Color(1f, 1f, 0.2f, 0.8f);
  private Color obliqueBlockedColor = new Color(0.35f, 1f, 0.35f, 0.85f);
  private bool hasOriginalColor;
  private MaterialPropertyBlock propertyBlock;

  private Color MultiplyColors(Color colorA, Color colorB)
  {
    // Directly multiply the color components
    return new Color(colorA.r * colorB.r, colorA.g * colorB.g, colorA.b * colorB.b, Mathf.Min(colorA.a, colorB.a));
  }

  // Call this method to apply the highlight effect
  public void Highlight()
  {
    EnsureRenderer();
    ResetHighlight();
    if (targetRenderer != null)
    {
      // Change the renderer's color to the highlight color
      SetRendererColor(MultiplyColors(originalColor, highlightColor));
    }
  }

  public void HighlightObliqueBlocked()
  {
    EnsureRenderer();
    ResetHighlight();
    if (targetRenderer != null)
    {
      SetRendererColor(Color.Lerp(originalColor, obliqueBlockedColor, 0.6f));
    }
  }

  // Call this method to remove the highlight effect and revert to the original color
  public void ResetHighlight()
  {
    EnsureRenderer();
    if (targetRenderer != null)
    {
      // Revert the renderer's color to its original
      SetRendererColor(originalColor);
    }
  }

  private void OnValidate()
  {
    EnsureRenderer();
  }

  void Start()
  {
    EnsureRenderer();
  }

  private void EnsureRenderer()
  {
    if (targetRenderer == null)
    {
      targetRenderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>() ?? GetComponentInParent<Renderer>();
    }

    if (targetRenderer != null && !hasOriginalColor)
    {
      originalColor = GetRendererColor();
      hasOriginalColor = true;
    }
  }

  private Color GetRendererColor()
  {
    if (targetRenderer is SpriteRenderer spriteRenderer)
    {
      return spriteRenderer.color;
    }

    Material sharedMaterial = targetRenderer != null ? targetRenderer.sharedMaterial : null;
    if (sharedMaterial != null && sharedMaterial.HasProperty(ColorPropertyId))
    {
      return sharedMaterial.color;
    }

    return Color.white;
  }

  private void SetRendererColor(Color color)
  {
    if (targetRenderer is SpriteRenderer spriteRenderer)
    {
      spriteRenderer.color = color;
      return;
    }

    if (targetRenderer == null)
    {
      return;
    }

    if (propertyBlock == null)
    {
      propertyBlock = new MaterialPropertyBlock();
    }

    targetRenderer.GetPropertyBlock(propertyBlock);
    propertyBlock.SetColor(ColorPropertyId, color);
    targetRenderer.SetPropertyBlock(propertyBlock);
  }
}
