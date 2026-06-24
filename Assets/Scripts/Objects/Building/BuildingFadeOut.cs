using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class BuildingFadeOut : MonoBehaviour
{
  [SerializeField] private SpriteRenderer spriteRenderer;
  [SerializeField] private Collider2D buildingCollider;

  private CinemachineVirtualCamera cinemachineCam;
  private float fadeLength, colliderFadePoint;

  void Start()
  {
    if (buildingCollider == null)
    {
      buildingCollider = GetComponent<Collider2D>();
    }

    if (spriteRenderer == null)
    {
      spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    if (buildingCollider == null || spriteRenderer == null)
    {
      enabled = false;
      return;
    }

    float bottomOfCollider = buildingCollider.bounds.min.y;
    float topOfCollider = buildingCollider.bounds.max.y;
    fadeLength = (topOfCollider - bottomOfCollider);
    colliderFadePoint = bottomOfCollider;

    GameObject cinemachineCamObject = GameObject.FindWithTag("PlayerCinemachine");
    if (cinemachineCamObject != null)
    {
      cinemachineCam = cinemachineCamObject.GetComponent<CinemachineVirtualCamera>();
    }
  }

  void Update()
  {
    AdjustOpacityBasedOnCameraPosition();
  }

  void AdjustOpacityBasedOnCameraPosition()
  {
    if (cinemachineCam == null || cinemachineCam.Follow == null)
    {
      return;
    }

    Vector3 playerPosition = cinemachineCam.Follow.position;
    float playerViewportPosition = playerPosition.y - 2.5f;

    float fadeStartPoint = playerViewportPosition;
    float fadeEndPoint = playerViewportPosition - fadeLength;
    float fadeAmount = Mathf.Clamp01(Mathf.InverseLerp(fadeEndPoint, fadeStartPoint, colliderFadePoint));

    Color spriteColor = spriteRenderer.color;
    spriteColor.a = fadeAmount;
    spriteRenderer.color = spriteColor;
  }
}
