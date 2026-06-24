// Assets/Editor/BlendTreeOptionsClipboard.cs

using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEditor.ShortcutManagement;

public static class BlendTreeOptionsClipboard
{
  private static BlendTreeCopyData copiedData;

  [Serializable]
  private class BlendTreeCopyData
  {
    public string sourceName;

    public BlendTreeType blendType;
    public string blendParameter;
    public string blendParameterY;

    public bool useAutomaticThresholds;
    public float minThreshold;
    public float maxThreshold;

    public ChildCopyData[] children;
  }

  [Serializable]
  private class ChildCopyData
  {
    public Motion motion;

    public float threshold;
    public Vector2 position;
    public float timeScale;
    public float cycleOffset;
    public string directBlendParameter;
    public bool mirror;

    public BlendTreeCopyData nestedBlendTree;
  }

  // ------------------------------------------------------------
  // Assets menu / Project-window right click menu
  //
  // Shortcuts:
  // Copy  = Ctrl + Alt + C   / Cmd + Option + C on macOS
  // Paste = Ctrl + Alt + V   / Cmd + Option + V on macOS
  // ------------------------------------------------------------

  private static void CopyOptionsMenu()
  {
    BlendTree source = GetSelectedBlendTree();

    if (source == null)
    {
      Debug.LogError("Select a BlendTree first.");
      return;
    }

    copiedData = CaptureBlendTree(source);
    Debug.Log($"Copied full BlendTree setup from '{source.name}'.");
  }

  private static bool ValidateCopyOptionsMenu()
  {
    return GetSelectedBlendTree() != null;
  }

  private static void PasteOptionsMenu()
  {
    BlendTree target = GetSelectedBlendTree();

    if (target == null)
    {
      Debug.LogError("Select a BlendTree first.");
      return;
    }

    if (copiedData == null)
    {
      Debug.LogError("No copied BlendTree setup found.");
      return;
    }

    PasteBlendTree(target, copiedData);
  }

  private static bool ValidatePasteOptionsMenu()
  {
    return GetSelectedBlendTree() != null && copiedData != null;
  }

  // ------------------------------------------------------------
  // Tools fallback menu
  // ------------------------------------------------------------

  [MenuItem("Tools/Animation/Blend Tree/Copy Options")]
  private static void CopyOptionsToolsMenu()
  {
    BlendTree source = GetSelectedBlendTree();

    if (source == null)
    {
      Debug.LogError("Select a BlendTree first.");
      return;
    }

    copiedData = CaptureBlendTree(source);
    Debug.Log($"Copied full BlendTree setup from '{source.name}'.");
  }

  [MenuItem("Tools/Animation/Blend Tree/Paste Options")]
  private static void PasteOptionsToolsMenu()
  {
    BlendTree target = GetSelectedBlendTree();

    if (target == null)
    {
      Debug.LogError("Select a BlendTree first.");
      return;
    }

    if (copiedData == null)
    {
      Debug.LogError("No copied BlendTree setup found.");
      return;
    }

    PasteBlendTree(target, copiedData);
  }

  // ------------------------------------------------------------
  // Copy
  // ------------------------------------------------------------

  private static BlendTreeCopyData CaptureBlendTree(BlendTree source)
  {
    ChildMotion[] sourceChildren = source.children;

    BlendTreeCopyData data = new BlendTreeCopyData
    {
      sourceName = source.name,

      blendType = source.blendType,
      blendParameter = source.blendParameter,
      blendParameterY = source.blendParameterY,

      useAutomaticThresholds = source.useAutomaticThresholds,
      minThreshold = source.minThreshold,
      maxThreshold = source.maxThreshold,

      children = new ChildCopyData[sourceChildren.Length]
    };

    for (int i = 0; i < sourceChildren.Length; i++)
    {
      ChildMotion sourceChild = sourceChildren[i];

      ChildCopyData childData = new ChildCopyData
      {
        motion = sourceChild.motion,

        threshold = sourceChild.threshold,
        position = sourceChild.position,
        timeScale = sourceChild.timeScale,
        cycleOffset = sourceChild.cycleOffset,
        directBlendParameter = sourceChild.directBlendParameter,
        mirror = sourceChild.mirror
      };

      if (sourceChild.motion is BlendTree nestedBlendTree)
      {
        childData.nestedBlendTree = CaptureBlendTree(nestedBlendTree);
      }

      data.children[i] = childData;
    }

    return data;
  }

  // ------------------------------------------------------------
  // Paste
  // ------------------------------------------------------------

  private static void PasteBlendTree(BlendTree target, BlendTreeCopyData data)
  {
    string targetAssetPath = AssetDatabase.GetAssetPath(target);

    if (string.IsNullOrEmpty(targetAssetPath))
    {
      Debug.LogError($"Could not find asset path for target BlendTree '{target.name}'.");
      return;
    }

    Undo.RecordObject(target, "Paste Full BlendTree Setup");

    ApplyBlendTreeData(target, data, targetAssetPath);

    EditorUtility.SetDirty(target);
    AssetDatabase.SaveAssets();
    AssetDatabase.ImportAsset(targetAssetPath);

    Debug.Log(
        $"Pasted full BlendTree setup from '{data.sourceName}' to '{target.name}'. " +
        $"Target now has {target.children.Length} children."
    );
  }

  private static void ApplyBlendTreeData(
      BlendTree target,
      BlendTreeCopyData data,
      string targetAssetPath
  )
  {
    target.blendType = data.blendType;
    target.blendParameter = data.blendParameter;
    target.blendParameterY = data.blendParameterY;

    target.minThreshold = data.minThreshold;
    target.maxThreshold = data.maxThreshold;

    ChildMotion[] newChildren = new ChildMotion[data.children.Length];

    for (int i = 0; i < data.children.Length; i++)
    {
      ChildCopyData copiedChild = data.children[i];

      Motion childMotion = copiedChild.motion;

      if (copiedChild.nestedBlendTree != null)
      {
        childMotion = CreateNestedBlendTreeCopy(
            copiedChild.nestedBlendTree,
            targetAssetPath
        );
      }

      newChildren[i] = new ChildMotion
      {
        motion = childMotion,

        threshold = copiedChild.threshold,
        position = copiedChild.position,
        timeScale = copiedChild.timeScale,
        cycleOffset = copiedChild.cycleOffset,
        directBlendParameter = copiedChild.directBlendParameter,
        mirror = copiedChild.mirror
      };
    }

    target.children = newChildren;
    target.useAutomaticThresholds = data.useAutomaticThresholds;

    EditorUtility.SetDirty(target);
  }

  private static BlendTree CreateNestedBlendTreeCopy(
      BlendTreeCopyData data,
      string targetAssetPath
  )
  {
    BlendTree nestedCopy = new BlendTree
    {
      // Keep same nested Blend Tree name.
      // Do not rename to "_Copy".
      name = data.sourceName
    };

    AssetDatabase.AddObjectToAsset(nestedCopy, targetAssetPath);
    Undo.RegisterCreatedObjectUndo(nestedCopy, "Create Nested BlendTree Copy");

    ApplyBlendTreeData(nestedCopy, data, targetAssetPath);

    EditorUtility.SetDirty(nestedCopy);

    return nestedCopy;
  }

  // ------------------------------------------------------------
  // Selection
  // ------------------------------------------------------------

  private static BlendTree GetSelectedBlendTree()
  {
    if (Selection.activeObject is BlendTree activeBlendTree)
    {
      return activeBlendTree;
    }

    foreach (UnityEngine.Object selectedObject in Selection.objects)
    {
      if (selectedObject is BlendTree blendTree)
      {
        return blendTree;
      }
    }

    return null;
  }

  // ------------------------------------------------------------
  // Shortcuts
  // ------------------------------------------------------------

  [Shortcut("Blend Tree/Copy Options", KeyCode.C, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
  private static void CopyOptionsShortcut()
  {
    BlendTree source = GetSelectedBlendTree();

    if (source == null)
    {
      Debug.LogError("Select a BlendTree first.");
      return;
    }

    copiedData = CaptureBlendTree(source);
    Debug.Log($"Copied full BlendTree setup from '{source.name}'.");
  }

  [Shortcut("Blend Tree/Paste Options", KeyCode.V, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
  private static void PasteOptionsShortcut()
  {
    BlendTree target = GetSelectedBlendTree();

    if (target == null)
    {
      Debug.LogError("Select a BlendTree first.");
      return;
    }

    if (copiedData == null)
    {
      Debug.LogError("No copied BlendTree setup found.");
      return;
    }

    PasteBlendTree(target, copiedData);
  }
}