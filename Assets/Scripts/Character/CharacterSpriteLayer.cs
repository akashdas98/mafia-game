using UnityEngine;

public class CharacterSpriteLayer : MonoBehaviour
{
  [SerializeField] private CharacterPartGroup partGroup;
  [SerializeField] private SpriteRenderer spriteRenderer;
  [SerializeField] private Animator animator;

  public CharacterPartGroup PartGroup => partGroup;
  public SpriteRenderer SpriteRenderer => spriteRenderer;
  public Animator Animator => animator;

  private void Reset()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();
    animator = GetComponent<Animator>();
  }

  private void OnValidate()
  {
    if (spriteRenderer == null)
      spriteRenderer = GetComponent<SpriteRenderer>();

    if (animator == null)
      animator = GetComponent<Animator>();
  }

  public void ApplyAnimated(RuntimeAnimatorController controller)
  {
    if (animator == null)
      return;

    if (controller == null)
    {
      Clear();
      return;
    }

    bool isActiveInHierarchy = animator.gameObject.activeInHierarchy;

    bool preserveState =
        Application.isPlaying &&
        isActiveInHierarchy &&
        animator.isActiveAndEnabled &&
        animator.runtimeAnimatorController != null;

    int stateHash = 0;
    float normalizedTime = 0f;

    if (preserveState)
    {
      AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
      stateHash = stateInfo.fullPathHash;
      normalizedTime = stateInfo.normalizedTime;
    }

    animator.enabled = true;
    animator.runtimeAnimatorController = controller;

    if (!isActiveInHierarchy)
    {
#if UNITY_EDITOR
          UnityEditor.EditorUtility.SetDirty(animator);

          if (spriteRenderer != null)
              UnityEditor.EditorUtility.SetDirty(spriteRenderer);
#endif
      return;
    }

    if (Application.isPlaying)
    {
      if (preserveState)
        animator.Play(stateHash, 0, normalizedTime);

      animator.Update(0f);
    }
    else
    {
      animator.Rebind();
      animator.Update(0f);

#if UNITY_EDITOR
          UnityEditor.EditorUtility.SetDirty(animator);

          if (spriteRenderer != null)
              UnityEditor.EditorUtility.SetDirty(spriteRenderer);
#endif
    }
  }

  public void ApplyStatic(Sprite sprite)
  {
    if (animator != null)
      animator.enabled = false;

    if (spriteRenderer != null)
      spriteRenderer.sprite = sprite;
  }

  public void Clear()
  {
    if (animator != null)
      animator.enabled = false;

    if (spriteRenderer != null)
      spriteRenderer.sprite = null;
  }

  public void SetSorting(string sortingLayerName, int sortingOrder)
  {
    if (spriteRenderer == null)
      return;

    spriteRenderer.sortingLayerName = sortingLayerName;
    spriteRenderer.sortingOrder = sortingOrder;
  }
}