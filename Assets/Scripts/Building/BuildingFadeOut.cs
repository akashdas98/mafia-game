using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingFadeOut : MonoBehaviour
{
  [SerializeField]
  private SpriteRenderer spriteRenderer;

  [SerializeField]
  private Collider2D buildingCollider;

  private float fadeLength, colliderFadePoint;

  void Start()
  {
    float bottomOfCollider = buildingCollider.bounds.min.y;
    float topOfCollider = buildingCollider.bounds.max.y;
    colliderFadePoint = bottomOfCollider;
    fadeLength = (topOfCollider - colliderFadePoint) - 2.5f;
  }

  void Update()
  {
    AdjustOpacityBasedOnCameraPosition();
  }

  void AdjustOpacityBasedOnCameraPosition()
  {
    float bottomOfScreen = Camera.main.ViewportToWorldPoint(new Vector2(0, 0)).y;

    float fadeStartPoint = bottomOfScreen;
    float fadeEndPoint = bottomOfScreen - fadeLength;
    float fadeAmount = Mathf.Clamp01(Mathf.InverseLerp(fadeEndPoint, fadeStartPoint, colliderFadePoint));

    Color spriteColor = spriteRenderer.color;
    spriteColor.a = fadeAmount;
    spriteRenderer.color = spriteColor;
  }
}