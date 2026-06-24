using UnityEngine;

public class AnimatorParameterRelay : MonoBehaviour
{
  [SerializeField] private Transform animatorRoot;
  [SerializeField] private bool includeInactive = true;
  [SerializeField] private bool autoRefreshOnAwake = true;
  [SerializeField] private bool warnOnMissingParameters = false;

  [Header("Debug")]
  [SerializeField] private Animator[] animators;

  private void Awake()
  {
    if (autoRefreshOnAwake)
      RefreshAnimators();
  }

  private void OnValidate()
  {
    if (!Application.isPlaying)
      RefreshAnimators();
  }

  [ContextMenu("Refresh Animators")]
  public void RefreshAnimators()
  {
    Transform root = animatorRoot != null
        ? animatorRoot
        : transform;

    animators = root.GetComponentsInChildren<Animator>(includeInactive);
  }

  public void SetBool(string parameterName, bool value)
  {
    int hash = Animator.StringToHash(parameterName);

    foreach (Animator animator in animators)
    {
      if (!CanSetParameter(
            animator,
            hash,
            parameterName,
            AnimatorControllerParameterType.Bool
          ))
      {
        continue;
      }

      animator.SetBool(hash, value);
    }
  }

  public void SetInteger(string parameterName, int value)
  {
    int hash = Animator.StringToHash(parameterName);

    foreach (Animator animator in animators)
    {
      if (!CanSetParameter(
            animator,
            hash,
            parameterName,
            AnimatorControllerParameterType.Int
          ))
      {
        continue;
      }

      animator.SetInteger(hash, value);
    }
  }

  public void SetFloat(string parameterName, float value)
  {
    int hash = Animator.StringToHash(parameterName);

    foreach (Animator animator in animators)
    {
      if (!CanSetParameter(
            animator,
            hash,
            parameterName,
            AnimatorControllerParameterType.Float
          ))
      {
        continue;
      }

      animator.SetFloat(hash, value);
    }
  }

  public void SetTrigger(string parameterName)
  {
    int hash = Animator.StringToHash(parameterName);

    foreach (Animator animator in animators)
    {
      if (!CanSetParameter(
            animator,
            hash,
            parameterName,
            AnimatorControllerParameterType.Trigger
          ))
      {
        continue;
      }

      animator.SetTrigger(hash);
    }
  }

  public void ResetTrigger(string parameterName)
  {
    int hash = Animator.StringToHash(parameterName);

    foreach (Animator animator in animators)
    {
      if (!CanSetParameter(
            animator,
            hash,
            parameterName,
            AnimatorControllerParameterType.Trigger
          ))
      {
        continue;
      }

      animator.ResetTrigger(hash);
    }
  }

  public void Rebind()
  {
    foreach (Animator animator in animators)
    {
      if (IsValid(animator))
        animator.Rebind();
    }
  }

  public void UpdateAnimators(float deltaTime)
  {
    foreach (Animator animator in animators)
    {
      if (IsValid(animator))
        animator.Update(deltaTime);
    }
  }

  private static bool IsValid(Animator animator)
  {
    return animator != null &&
           animator.enabled &&
           animator.runtimeAnimatorController != null;
  }

  private static bool CanSetParameter(
      Animator animator,
      int hash,
      string parameterName,
      AnimatorControllerParameterType type
  )
  {
    if (!IsValid(animator))
      return false;

    foreach (AnimatorControllerParameter parameter in animator.parameters)
    {
      if (parameter.nameHash == hash && parameter.type == type)
        return true;
    }

    AnimatorParameterRelay relay = animator.GetComponentInParent<AnimatorParameterRelay>();

    if (relay != null && relay.warnOnMissingParameters)
    {
      Debug.LogWarning(
          $"Animator '{animator.name}' does not have {type} parameter '{parameterName}'.",
          animator
      );
    }

    return false;
  }
}
