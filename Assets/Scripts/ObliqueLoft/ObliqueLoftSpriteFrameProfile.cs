using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ObliqueLoftSpriteFrameProfile
{
  [SerializeField] private Sprite sprite;
  [SerializeField] private SpriteRenderer renderer;
  [SerializeField] private string rendererPath;
  [SerializeField] private List<Vector2> footprint = new List<Vector2>();
  [SerializeField] private List<ObliqueLoftSlice> slices = new List<ObliqueLoftSlice>();

  public Sprite Sprite => sprite;
  public SpriteRenderer Renderer => renderer;
  public string RendererPath => rendererPath;
  public IReadOnlyList<Vector2> Footprint => footprint;
  public IReadOnlyList<ObliqueLoftSlice> Slices => slices;

  public bool Matches(SpriteRenderer candidateRenderer, Sprite candidateSprite, string candidateRendererPath)
  {
    if (candidateSprite == null || sprite != candidateSprite)
    {
      return false;
    }

    if (renderer != null && candidateRenderer != null)
    {
      return renderer == candidateRenderer;
    }

    if (!string.IsNullOrEmpty(rendererPath) && !string.IsNullOrEmpty(candidateRendererPath))
    {
      return rendererPath == candidateRendererPath;
    }

    return renderer == null && string.IsNullOrEmpty(rendererPath);
  }

  public bool MatchesSpriteOnly(Sprite candidateSprite)
  {
    return candidateSprite != null && sprite == candidateSprite;
  }

  public void SetKey(SpriteRenderer sourceRenderer, Sprite sourceSprite, string sourceRendererPath)
  {
    renderer = sourceRenderer;
    sprite = sourceSprite;
    rendererPath = sourceRendererPath;
  }

  public void Capture(IReadOnlyList<Vector2> sourceFootprint, IReadOnlyList<ObliqueLoftSlice> sourceSlices)
  {
    footprint = sourceFootprint != null ? new List<Vector2>(sourceFootprint) : new List<Vector2>();
    slices = new List<ObliqueLoftSlice>();
    if (sourceSlices == null)
    {
      return;
    }

    for (int i = 0; i < sourceSlices.Count; i++)
    {
      slices.Add(sourceSlices[i] != null ? sourceSlices[i].Clone() : new ObliqueLoftSlice());
    }
  }
}
