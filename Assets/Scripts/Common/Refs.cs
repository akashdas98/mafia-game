using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Refs : MonoBehaviour
{
  [SerializeField]
  private BoxCollider2D _interactionCollider;
  public BoxCollider2D InteractionCollider => _interactionCollider;

  [SerializeField]
  private PolygonCollider2D _hitCollider, _depthCollider;
  public PolygonCollider2D HitCollider => _hitCollider;
  public PolygonCollider2D DepthCollider => _depthCollider;

  [SerializeField]
  private Controller _controller;
  public Controller Controller => _controller;

  [SerializeField]
  private InputManager _inputManager;
  public InputManager InputManager => _inputManager;

  [SerializeField]
  private Interactable _interactable;
  public Interactable Interactable => _interactable;

  [SerializeField]
  private Inventory _inventory;
  public Inventory Inventory => _inventory;

  [SerializeField]
  private Animator _animator;
  public Animator Animator => _animator;

  [SerializeField]
  private SceneDetails _currentSceneDetails;
  public SceneDetails CurrentSceneDetails => _currentSceneDetails;

  [SerializeField]
  private InputHandler _inputHandler;
  public InputHandler InputHandler => _inputHandler;

  [SerializeField]
  private Target _aimTarget;
  public Target AimTarget => _aimTarget;

  public void AssignInputManager(InputManager cm)
  {
    _inputManager = cm;
  }

  public void SetCurrentScene(SceneDetails sceneDetails)
  {
    if (sceneDetails != null)
    {
      _currentSceneDetails = sceneDetails;
      Debug.Log("Current Scene: " + _currentSceneDetails.name);
    }
  }
}