using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CharacterBuilderMenu
{
  private sealed class GeneratedAnimationSet
  {
    public CharacterPartIdentity identity;
    public readonly Dictionary<string, AnimationClip> clipsByContext = new();
  }

  [MenuItem("Tools/Character Builder/Create / Update Slot Clips")]
  public static void CreateOrUpdateSlotClips()
  {
    EnsureFolders();

    int createdOrUpdated = 0;
    int invalidCount = 0;
    HashSet<CharacterPartGroup> touchedPartGroups = new();

    foreach (ParsedCharacterPng parsed in EnumerateValidParsedPngs(ref invalidCount))
    {
      if (!parsed.HasContext)
        continue;

      CharacterPartIdentity identity;

      try
      {
        identity = parsed.Identity;
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"{parsed.assetPath}: {ex.Message}");
        invalidCount++;
        continue;
      }

      List<ExpandedAnimationContext> expandedContexts;

      try
      {
        expandedContexts =
            CharacterFilenameParser.ExpandDirectionPairs(parsed.contextTokens);
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"{parsed.assetPath}: {ex.Message}");
        invalidCount++;
        continue;
      }

      foreach (ExpandedAnimationContext expanded in expandedContexts)
      {
        EnsureSlotClipExists(identity.partGroup, expanded.context);
        createdOrUpdated++;
      }

      touchedPartGroups.Add(identity.partGroup);
    }

    foreach (CharacterPartGroup partGroup in touchedPartGroups)
    {
      EnsureLayerTemplateController(partGroup);
    }

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    Debug.Log(
        "Character Builder slot creation complete.\n" +
        $"Created/updated slot clips: {createdOrUpdated}\n" +
        $"Invalid PNGs: {invalidCount}\n" +
        $"Slot folder: {CharacterBuilderConstants.SlotRoot}"
    );
  }

  [MenuItem("Tools/Character Builder/Rebuild Character Assets")]
  public static void RebuildCharacterAssets()
  {
    EnsureFolders();

    Dictionary<string, GeneratedAnimationSet> animationSets = new();
    List<CharacterCatalogEntry> staticEntries = new();

    int parsedCount = 0;
    int invalidCount = 0;
    int generatedClipCount = 0;

    foreach (ParsedCharacterPng parsed in EnumerateValidParsedPngs(ref invalidCount))
    {
      parsedCount++;

      CharacterPartIdentity identity;

      try
      {
        identity = parsed.Identity;
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"{parsed.assetPath}: {ex.Message}");
        invalidCount++;
        continue;
      }

      ForceCorrectImportSettings(parsed.assetPath, parsed);

      if (!parsed.HasContext && parsed.exportType == CharacterExportType.Single)
      {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(parsed.assetPath);

        if (sprite == null)
        {
          Debug.LogError($"Could not load static sprite: {parsed.assetPath}");
          invalidCount++;
          continue;
        }

        staticEntries.Add(CreateStaticCatalogEntry(identity, sprite));
        continue;
      }

      Sprite[] sourceFrames = LoadSpritesForParsedAsset(parsed);

      if (sourceFrames.Length == 0)
      {
        Debug.LogError($"No sprites loaded for: {parsed.assetPath}");
        invalidCount++;
        continue;
      }

      List<ExpandedAnimationContext> expandedContexts;

      try
      {
        expandedContexts =
            CharacterFilenameParser.ExpandDirectionPairs(parsed.contextTokens);
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"{parsed.assetPath}: {ex.Message}");
        invalidCount++;
        continue;
      }

      foreach (ExpandedAnimationContext expanded in expandedContexts)
      {
        Sprite[] frames = expanded.reverseFrames
            ? sourceFrames.Reverse().ToArray()
            : sourceFrames;

        string clipName = CharacterFilenameParser.BuildGeneratedClipName(
            identity,
            expanded.context
        );

        string clipPath =
            $"{CharacterBuilderConstants.GeneratedClipRoot}/{clipName}.anim";

        bool loop = ShouldLoop(expanded.context);

        AnimationClip clip =
            CharacterAnimationClipGenerator.CreateOrUpdateSpriteClip(
                clipPath,
                frames,
                parsed.sampleRate,
                loop
            );

        generatedClipCount++;

        EnsureSlotClipExists(identity.partGroup, expanded.context);

        GeneratedAnimationSet set =
            GetOrCreateAnimationSet(animationSets, identity);

        set.clipsByContext[expanded.context] = clip;
      }
    }

    int overrideCount = GenerateOverrideControllers(
      animationSets,
      out int missingTemplateSlotCount
    );

    BuildCatalog(animationSets, staticEntries);

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    Debug.Log(
        "Character Builder rebuild complete.\n" +
        $"Parsed PNGs: {parsedCount}\n" +
        $"Invalid PNGs: {invalidCount}\n" +
        $"Generated/updated clips: {generatedClipCount}\n" +
        $"Generated/updated override controllers: {overrideCount}\n" +
        $"Generated clips with no matching layer-template slot: {missingTemplateSlotCount}\n" +
        $"Catalog: {CharacterBuilderConstants.CatalogPath}"
    );
  }

  public static int RebuildLayerTemplatesFromSeed()
  {
    EnsureFolders();
    EnsureSharedLayerTemplateController();

    int rebuiltCount = 0;

    foreach (CharacterPartGroup partGroup in EnumeratePartGroups())
    {
      string templatePath =
        CharacterBuilderConstants.GetLayerTemplateControllerPath(partGroup);

      RuntimeAnimatorController existingController =
        AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(templatePath);

      if (existingController != null)
        AssetDatabase.DeleteAsset(templatePath);

      RuntimeAnimatorController rebuiltController =
        EnsureLayerTemplateController(partGroup);

      if (rebuiltController != null)
        rebuiltCount++;
    }

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    Debug.Log(
      "Character Builder layer templates rebuilt from seed.\n" +
      $"Seed: {CharacterBuilderConstants.SharedLayerTemplateControllerPath}\n" +
      $"Rebuilt layer templates: {rebuiltCount}"
    );

    return rebuiltCount;
  }

  private static List<ParsedCharacterPng> EnumerateValidParsedPngs(
      ref int invalidCount
  )
  {
    var parsedFiles = new List<ParsedCharacterPng>();

    string[] guids = AssetDatabase.FindAssets(
        "t:Texture2D",
        new[] { CharacterBuilderConstants.InputRoot }
    );

    foreach (string guid in guids)
    {
      string assetPath = AssetDatabase.GUIDToAssetPath(guid);

      if (!CharacterTextureImportUtility.ShouldHandle(assetPath))
        continue;

      if (!CharacterFilenameParser.TryParse(
              assetPath,
              out ParsedCharacterPng parsed,
              out string error
          ))
      {
        Debug.LogError($"Invalid character PNG: {assetPath}. {error}");
        invalidCount++;
        continue;
      }

      parsedFiles.Add(parsed);
    }

    return parsedFiles;
  }

  private static void EnsureFolders()
  {
    EnsureFolder("Assets", "Animations");
    EnsureFolder("Assets/Animations", "Character");
    EnsureFolder(CharacterBuilderConstants.AnimationRoot, "Base");
    EnsureFolder(CharacterBuilderConstants.BaseRoot, "Slots");
    EnsureFolder(CharacterBuilderConstants.BaseRoot, "Templates");
    EnsureFolder(CharacterBuilderConstants.AnimationRoot, "GeneratedClips");
    EnsureFolder(CharacterBuilderConstants.AnimationRoot, "OverrideControllers");
    EnsureFolder(CharacterBuilderConstants.AnimationRoot, "Catalog");
  }

  private static void EnsureFolder(string parent, string child)
  {
    string path = $"{parent}/{child}";

    if (!AssetDatabase.IsValidFolder(path))
      AssetDatabase.CreateFolder(parent, child);
  }

  private static RuntimeAnimatorController EnsureSharedLayerTemplateController()
  {
    RuntimeAnimatorController controller =
        AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            CharacterBuilderConstants.SharedLayerTemplateControllerPath
        );

    if (controller != null)
      return controller;

    AnimatorController animatorController =
        AnimatorController.CreateAnimatorControllerAtPath(
            CharacterBuilderConstants.SharedLayerTemplateControllerPath
        );

    AssetDatabase.SaveAssets();

    Debug.Log(
        $"Created shared layer animator template controller: {CharacterBuilderConstants.SharedLayerTemplateControllerPath}"
    );

    return animatorController;
  }

  private static RuntimeAnimatorController EnsureLayerTemplateController(
    CharacterPartGroup partGroup
  )
  {
    string templatePath =
      CharacterBuilderConstants.GetLayerTemplateControllerPath(partGroup);

    RuntimeAnimatorController controller =
        AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(templatePath);

    if (controller != null)
      return controller;

    EnsureFolder(CharacterBuilderConstants.BaseRoot, "Templates");

    RuntimeAnimatorController sharedTemplate =
      EnsureSharedLayerTemplateController();

    if (sharedTemplate != null &&
        AssetDatabase.CopyAsset(
          CharacterBuilderConstants.SharedLayerTemplateControllerPath,
          templatePath
        ))
    {
      controller =
        AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(templatePath);
    }

    if (controller == null)
    {
      controller = AnimatorController.CreateAnimatorControllerAtPath(
        templatePath
      );
    }

    if (controller is AnimatorController animatorController)
    {
      animatorController.name =
        $"{CharacterPartGroupUtility.ToToken(partGroup)}Layer";
      RetargetTemplateSlots(animatorController, partGroup);
    }

    AssetDatabase.SaveAssets();

    Debug.Log(
        $"Created {partGroup} layer animator template controller: {templatePath}"
    );

    return controller;
  }

  private static void ForceCorrectImportSettings(
      string assetPath,
      ParsedCharacterPng parsed
  )
  {
    TextureImporter importer =
        AssetImporter.GetAtPath(assetPath) as TextureImporter;

    if (importer == null)
      return;

    CharacterTextureImportUtility.ConfigureImporter(importer, parsed);
    importer.SaveAndReimport();
  }

  private static Sprite[] LoadSpritesForParsedAsset(ParsedCharacterPng parsed)
  {
    if (parsed.exportType == CharacterExportType.Single)
    {
      Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
          parsed.assetPath
      );

      return sprite == null
          ? new Sprite[0]
          : new[] { sprite };
    }

    return AssetDatabase
        .LoadAllAssetsAtPath(parsed.assetPath)
        .OfType<Sprite>()
        .OrderBy(sprite => sprite.name)
        .ToArray();
  }

  private static bool ShouldLoop(string context)
  {
    string[] tokens = context.Split('_');

    return tokens.Contains("idle") ||
           tokens.Contains("walk") ||
           tokens.Contains("aim");
  }

  private static void EnsureSlotClipExists(
    CharacterPartGroup partGroup,
    string expandedContext
  )
  {
    UpdateSlotClipPreviewMetadata(
      partGroup,
      expandedContext,
      1f,
      CharacterBuilderConstants.DefaultSampleRate,
      ShouldLoop(expandedContext)
    );
  }

  private static void UpdateSlotClipPreviewMetadata(
    CharacterPartGroup partGroup,
    string expandedContext,
    float duration,
    float sampleRate,
    bool loop
  )
  {
    EnsureLayerSlotFolder(partGroup);

    string slotName =
      CharacterBuilderConstants.BuildLayerSlotName(partGroup, expandedContext);
    string slotPath =
      $"{CharacterBuilderConstants.GetLayerSlotRoot(partGroup)}/{slotName}.anim";

    CharacterAnimationClipGenerator.CreateOrUpdateSlotClip(
      slotPath,
      slotName,
      duration,
      sampleRate,
      loop
    );
  }

  private static GeneratedAnimationSet GetOrCreateAnimationSet(
      Dictionary<string, GeneratedAnimationSet> sets,
      CharacterPartIdentity identity
  )
  {
    if (sets.TryGetValue(identity.key, out GeneratedAnimationSet set))
      return set;

    set = new GeneratedAnimationSet
    {
      identity = identity
    };

    sets.Add(identity.key, set);
    return set;
  }

  private static void EnsureLayerSlotFolder(CharacterPartGroup partGroup)
  {
    EnsureFolder(
      CharacterBuilderConstants.SlotRoot,
      CharacterPartGroupUtility.ToToken(partGroup)
    );
  }

  private static void RetargetTemplateSlots(
    AnimatorController controller,
    CharacterPartGroup partGroup
  )
  {
    if (controller == null)
      return;

    foreach (AnimatorControllerLayer layer in controller.layers)
    {
      RetargetStateMachineSlots(layer.stateMachine, partGroup);
    }

    EditorUtility.SetDirty(controller);
  }

  private static void RetargetStateMachineSlots(
    AnimatorStateMachine stateMachine,
    CharacterPartGroup partGroup
  )
  {
    if (stateMachine == null)
      return;

    foreach (ChildAnimatorState childState in stateMachine.states)
    {
      AnimatorState state = childState.state;

      if (state == null)
        continue;

      state.motion = RetargetMotionSlot(state.motion, partGroup);
      EditorUtility.SetDirty(state);
    }

    foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
    {
      RetargetStateMachineSlots(childStateMachine.stateMachine, partGroup);
    }
  }

  private static Motion RetargetMotionSlot(
    Motion motion,
    CharacterPartGroup partGroup
  )
  {
    if (motion is AnimationClip clip)
    {
      if (!TryGetSlotContext(clip.name, out string context))
        return motion;

      AnimationClip layerSlotClip =
        LoadLayerSlotClip(partGroup, context);

      return layerSlotClip != null
        ? layerSlotClip
        : motion;
    }

    if (motion is BlendTree blendTree)
    {
      ChildMotion[] children = blendTree.children;

      for (int i = 0; i < children.Length; i++)
      {
        children[i].motion =
          RetargetMotionSlot(children[i].motion, partGroup);
      }

      blendTree.children = children;
      EditorUtility.SetDirty(blendTree);
    }

    return motion;
  }

  private static AnimationClip LoadLayerSlotClip(
    CharacterPartGroup partGroup,
    string expandedContext
  )
  {
    string slotName =
      CharacterBuilderConstants.BuildLayerSlotName(partGroup, expandedContext);
    string slotPath =
      $"{CharacterBuilderConstants.GetLayerSlotRoot(partGroup)}/{slotName}.anim";

    return AssetDatabase.LoadAssetAtPath<AnimationClip>(slotPath);
  }

  private static bool TryGetSlotContext(string slotClipName, out string context)
  {
    context = "";

    if (!slotClipName.StartsWith("slot_"))
      return false;

    string value = slotClipName.Substring("slot_".Length);

    foreach (CharacterPartGroup partGroup in EnumeratePartGroups())
    {
      string partPrefix =
        $"{CharacterPartGroupUtility.ToToken(partGroup)}_";

      if (value.StartsWith(partPrefix))
      {
        context = value.Substring(partPrefix.Length);
        return !string.IsNullOrWhiteSpace(context);
      }
    }

    context = value;
    return !string.IsNullOrWhiteSpace(context);
  }

  private static IEnumerable<CharacterPartGroup> EnumeratePartGroups()
  {
    return System.Enum
      .GetValues(typeof(CharacterPartGroup))
      .Cast<CharacterPartGroup>();
  }

  private static int GenerateOverrideControllers(
    Dictionary<string, GeneratedAnimationSet> sets,
    out int missingTemplateSlotCount
  )
  {
    int generatedCount = 0;
    missingTemplateSlotCount = 0;

    foreach (GeneratedAnimationSet set in sets.Values)
    {
      RuntimeAnimatorController layerTemplateController =
        EnsureLayerTemplateController(set.identity.partGroup);

      string controllerPath =
          $"{CharacterBuilderConstants.OverrideControllerRoot}/{set.identity.key}.overrideController";

      AnimatorOverrideController overrideController =
          AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(
              controllerPath
          );

      if (overrideController == null)
      {
        overrideController = new AnimatorOverrideController
        {
          runtimeAnimatorController = layerTemplateController
        };

        AssetDatabase.CreateAsset(overrideController, controllerPath);
      }
      else
      {
        overrideController.runtimeAnimatorController = layerTemplateController;
      }

      List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new();
      overrideController.GetOverrides(overrides);

      HashSet<string> usedContexts = new();

      for (int i = 0; i < overrides.Count; i++)
      {
        AnimationClip slotClip = overrides[i].Key;

        if (slotClip == null)
          continue;

        if (!slotClip.name.StartsWith("slot_"))
          continue;

        if (!TryGetSlotContext(slotClip.name, out string context))
          continue;

        if (!set.clipsByContext.TryGetValue(
                context,
                out AnimationClip replacement
            ))
        {
          continue;
        }

        overrides[i] =
            new KeyValuePair<AnimationClip, AnimationClip>(
                slotClip,
                replacement
            );

        usedContexts.Add(context);
      }

      foreach (string generatedContext in set.clipsByContext.Keys)
      {
        if (!usedContexts.Contains(generatedContext))
        {
          missingTemplateSlotCount++;

          Debug.LogWarning(
              $"Generated clip has no matching slot in {set.identity.partGroup} layer template: " +
              $"{set.identity.key} / " +
              CharacterBuilderConstants.BuildLayerSlotName(
                set.identity.partGroup,
                generatedContext
              )
          );
        }
      }

      overrideController.ApplyOverrides(overrides);
      EditorUtility.SetDirty(overrideController);

      generatedCount++;
    }

    return generatedCount;
  }

  private static void BuildCatalog(
      Dictionary<string, GeneratedAnimationSet> animationSets,
      List<CharacterCatalogEntry> staticEntries
  )
  {
    CharacterSpriteCatalog catalog =
        AssetDatabase.LoadAssetAtPath<CharacterSpriteCatalog>(
            CharacterBuilderConstants.CatalogPath
        );

    if (catalog == null)
    {
      catalog = ScriptableObject.CreateInstance<CharacterSpriteCatalog>();

      AssetDatabase.CreateAsset(
          catalog,
          CharacterBuilderConstants.CatalogPath
      );
    }

    List<CharacterCatalogEntry> entries = new();

    foreach (GeneratedAnimationSet set in animationSets.Values)
    {
      string controllerPath =
          $"{CharacterBuilderConstants.OverrideControllerRoot}/{set.identity.key}.overrideController";

      RuntimeAnimatorController controller =
          AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
              controllerPath
          );

      entries.Add(CreateAnimatedCatalogEntry(set.identity, controller));
    }

    entries.AddRange(staticEntries);

    catalog.SetEntries(entries);

    EditorUtility.SetDirty(catalog);
  }

  private static CharacterCatalogEntry CreateAnimatedCatalogEntry(
      CharacterPartIdentity identity,
      RuntimeAnimatorController controller
  )
  {
    return new CharacterCatalogEntry
    {
      key = identity.key,
      partGroup = identity.partGroup,
      gender = identity.gender,
      bodyType = identity.bodyType,
      skinColor = identity.skinColor,
      faceType = identity.faceType,
      variant = identity.variant,
      useStaticSprite = false,
      overrideController = controller
    };
  }

  private static CharacterCatalogEntry CreateStaticCatalogEntry(
      CharacterPartIdentity identity,
      Sprite sprite
  )
  {
    return new CharacterCatalogEntry
    {
      key = identity.key,
      partGroup = identity.partGroup,
      gender = identity.gender,
      bodyType = identity.bodyType,
      skinColor = identity.skinColor,
      faceType = identity.faceType,
      variant = identity.variant,
      useStaticSprite = true,
      staticSprite = sprite
    };
  }
}
