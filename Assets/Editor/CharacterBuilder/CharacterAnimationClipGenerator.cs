using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CharacterAnimationClipGenerator
{
  public static AnimationClip CreateOrUpdateSpriteClip(
      string assetPath,
      Sprite[] frames,
      float frameRate,
      bool loop
  )
  {
    AnimationClip clip =
        AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

    if (clip == null)
    {
      clip = new AnimationClip
      {
        frameRate = frameRate
      };

      AssetDatabase.CreateAsset(clip, assetPath);
    }

    clip.frameRate = frameRate;

    EditorCurveBinding binding = new EditorCurveBinding
    {
      type = typeof(SpriteRenderer),
      path = "",
      propertyName = "m_Sprite"
    };

    ObjectReferenceKeyframe[] keyframes = frames
        .Select((sprite, index) => new ObjectReferenceKeyframe
        {
          time = index / frameRate,
          value = sprite
        })
        .ToArray();

    AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

    AnimationClipSettings settings =
        AnimationUtility.GetAnimationClipSettings(clip);

    settings.loopTime = loop;

    AnimationUtility.SetAnimationClipSettings(clip, settings);

    EditorUtility.SetDirty(clip);

    return clip;
  }

  public static AnimationClip CreateOrUpdateEmptySlotClip(string assetPath)
  {
    AnimationClip clip =
        AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

    if (clip == null)
    {
      clip = new AnimationClip
      {
        frameRate = CharacterBuilderConstants.DefaultSampleRate
      };

      AssetDatabase.CreateAsset(clip, assetPath);
    }

    EditorUtility.SetDirty(clip);
    return clip;
  }

  public static AnimationClip CreateOrUpdateSlotClip(
    string assetPath,
    string slotName,
    float duration,
    float sampleRate,
    bool loop
)
  {
    AnimationClip clip =
        AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

    if (clip == null)
    {
      clip = new AnimationClip();
      AssetDatabase.CreateAsset(clip, assetPath);
    }

    sampleRate = Mathf.Max(1f, sampleRate);

    clip.frameRate = sampleRate;

    duration = Mathf.Max(duration, 1f / sampleRate);

    ClearCurves(clip);

    AnimationClipSettings settings =
        AnimationUtility.GetAnimationClipSettings(clip);

    settings.loopTime = loop;

    AnimationUtility.SetAnimationClipSettings(clip, settings);

    EditorUtility.SetDirty(clip);
    return clip;
  }

  private static void ClearCurves(AnimationClip clip)
  {
    foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
      AnimationUtility.SetEditorCurve(clip, binding, null);

    foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
      AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
  }
}
