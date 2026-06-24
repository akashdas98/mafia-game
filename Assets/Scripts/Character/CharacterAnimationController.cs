using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
  [SerializeField] private AnimatorParameterRelay animatorRelay;
  [SerializeField] private MonoBehaviour[] animationAdapters;

  private readonly List<MonoBehaviour> adapterBehaviours = new();
  private readonly List<IAnimationParameterContributor> contributors = new();

  private AnimationParameterWriter writer;
  private bool adaptersCached;

  public void Initialize()
  {
    Initialize(null, null);
  }

  public void Initialize(CharacterMotor motor, WeaponUser weaponUser)
  {
    if (animatorRelay == null)
    {
      animatorRelay = GetComponentInChildren<AnimatorParameterRelay>(true);
    }

    RefreshAdapterListIfNeeded();
    InitializeKnownAdapters(motor, weaponUser);
  }

  public void Tick()
  {
    Initialize();

    if (animatorRelay == null)
    {
      return;
    }

    if (writer == null)
    {
      writer = new AnimationParameterWriter(animatorRelay);
    }
    else
    {
      writer.SetRelay(animatorRelay);
    }

    for (int i = 0; i < contributors.Count; i++)
    {
      MonoBehaviour adapter = adapterBehaviours[i];

      if (adapter != null && adapter.isActiveAndEnabled)
      {
        contributors[i].Contribute(writer);
      }
    }
  }

  private void Update()
  {
    Tick();
  }

  private void RefreshAdapterListIfNeeded()
  {
    if (adaptersCached)
    {
      return;
    }

    if (animationAdapters == null || animationAdapters.Length == 0)
    {
      animationAdapters = FindLocalAnimationAdapters();
    }

    adapterBehaviours.Clear();
    contributors.Clear();

    if (animationAdapters == null)
    {
      adaptersCached = true;
      return;
    }

    foreach (MonoBehaviour adapter in animationAdapters)
    {
      if (adapter is IAnimationParameterContributor contributor)
      {
        adapterBehaviours.Add(adapter);
        contributors.Add(contributor);
      }
    }

    adaptersCached = true;
  }

  private MonoBehaviour[] FindLocalAnimationAdapters()
  {
    MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
    List<MonoBehaviour> foundAdapters = new();

    foreach (MonoBehaviour behaviour in behaviours)
    {
      if (behaviour is IAnimationParameterContributor)
      {
        foundAdapters.Add(behaviour);
      }
    }

    return foundAdapters.ToArray();
  }

  private void InitializeKnownAdapters(CharacterMotor motor, WeaponUser weaponUser)
  {
    if (animationAdapters == null)
    {
      return;
    }

    foreach (MonoBehaviour adapter in animationAdapters)
    {
      if (adapter is CharacterMovementAnimationAdapter movementAdapter)
      {
        movementAdapter.Initialize(motor);
      }
      else if (adapter is CharacterAimAnimationAdapter aimAdapter)
      {
        aimAdapter.Initialize(weaponUser);
      }
    }
  }

  private void Reset()
  {
    animatorRelay = GetComponentInChildren<AnimatorParameterRelay>(true);
    animationAdapters = FindLocalAnimationAdapters();
    adaptersCached = false;
  }

  private void OnValidate()
  {
    if (animatorRelay == null)
    {
      animatorRelay = GetComponentInChildren<AnimatorParameterRelay>(true);
    }

    if (animationAdapters == null || animationAdapters.Length == 0)
    {
      animationAdapters = FindLocalAnimationAdapters();
    }

    adaptersCached = false;
  }
}
