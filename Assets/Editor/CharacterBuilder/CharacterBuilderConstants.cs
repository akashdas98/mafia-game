using UnityEngine;

public static class CharacterBuilderConstants
{
    public const string InputRoot = "Assets/Graphics/Character";

    public const string AnimationRoot = "Assets/Animations/Character";
    public const string BaseRoot = AnimationRoot + "/Base";
    public const string SlotRoot = BaseRoot + "/Slots";
    public const string LayerTemplateRoot = BaseRoot + "/Templates";
    public const string GeneratedClipRoot = AnimationRoot + "/GeneratedClips";
    public const string OverrideControllerRoot = AnimationRoot + "/OverrideControllers";
    public const string CatalogRoot = AnimationRoot + "/Catalog";

    public const string SharedLayerTemplateControllerPath =
        BaseRoot + "/LayerAnimatorTemplate.controller";

    public const string CatalogPath =
        CatalogRoot + "/CharacterSpriteCatalog.asset";

    public const int CellSize = 128;
    public const int PixelsPerUnit = 32;
    public const float DefaultSampleRate = 12f;

    public static readonly Vector2 Pivot = new(0.5f, 0.5f);

    public static string GetLayerSlotRoot(CharacterPartGroup partGroup)
    {
        return $"{SlotRoot}/{CharacterPartGroupUtility.ToToken(partGroup)}";
    }

    public static string GetLayerTemplateControllerPath(CharacterPartGroup partGroup)
    {
        return $"{LayerTemplateRoot}/{CharacterPartGroupUtility.ToToken(partGroup)}Layer.controller";
    }

    public static string BuildLayerSlotName(CharacterPartGroup partGroup, string expandedContext)
    {
        return $"slot_{CharacterPartGroupUtility.ToToken(partGroup)}_{expandedContext}";
    }
}
