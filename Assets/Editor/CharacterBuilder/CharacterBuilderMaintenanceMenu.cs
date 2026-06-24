using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CharacterBuilderMaintenanceMenu
{
    [MenuItem("Tools/Character Builder/Maintenance/Prepare For Re-export")]
    public static void PrepareForReexport()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Prepare Character Assets For Re-export",
            "This will delete character PNGs and disposable generated outputs.\n\n" +
            "It will NOT delete:\n" +
            "- LayerAnimatorTemplate.controller seed\n" +
            "- per-layer template controllers\n" +
            "- per-layer slot_*.anim clips\n" +
            "- CharacterSpriteCatalog.asset\n\n" +
            "After this, export fresh PNGs into Assets/Graphics/Character, then run Reimport And Rebuild.",
            "Prepare",
            "Cancel"
        );

        if (!confirmed)
            return;

        DeleteCharacterSourcePngs();
        CleanGeneratedOutputsOnly();
        ClearCatalogButKeepAsset();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Character Builder prepare-for-reexport complete.\n" +
            "Deleted source PNGs and disposable generated outputs.\n" +
            "Preserved layer template controllers, slot clips, and catalog asset."
        );
    }

    [MenuItem("Tools/Character Builder/Maintenance/Reimport And Rebuild")]
    public static void ReimportAndRebuild()
    {
        AssetDatabase.ImportAsset(
            CharacterBuilderConstants.InputRoot,
            ImportAssetOptions.ImportRecursive |
            ImportAssetOptions.ForceUpdate
        );

        CharacterBuilderMenu.CreateOrUpdateSlotClips();
        CharacterBuilderMenu.RebuildCharacterAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Character Builder reimport-and-rebuild complete.");
    }

    [MenuItem("Tools/Character Builder/Maintenance/Rebuild Layer Templates From Seed")]
    public static void RebuildLayerTemplatesFromSeed()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Rebuild Layer Templates From Seed",
            "This will delete and recreate every per-layer template controller from LayerAnimatorTemplate.controller.\n\n" +
            "It will NOT delete:\n" +
            "- LayerAnimatorTemplate.controller seed\n" +
            "- per-layer slot_*.anim clips\n" +
            "- generated sprite animation clips\n" +
            "- character source PNGs\n\n" +
            "After recreating the layer templates, it will rebuild generated character assets so override controllers use the refreshed templates.",
            "Rebuild",
            "Cancel"
        );

        if (!confirmed)
            return;

        int rebuiltCount = CharacterBuilderMenu.RebuildLayerTemplatesFromSeed();
        CharacterBuilderMenu.RebuildCharacterAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Character Builder rebuild-layer-templates-from-seed complete.\n" +
            $"Rebuilt layer templates: {rebuiltCount}\n" +
            "Generated character assets were rebuilt after template recreation."
        );
    }

    [MenuItem("Tools/Character Builder/Maintenance/Clean Generated Outputs Only")]
    public static void CleanGeneratedOutputsOnlyMenu()
    {
        CleanGeneratedOutputsOnly();
        ClearCatalogButKeepAsset();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Character Builder generated outputs cleaned.\n" +
            "Preserved source PNGs, layer template controllers, slot clips, and catalog asset."
        );
    }

    private static void CleanGeneratedOutputsOnly()
    {
        DeleteFolderContents(CharacterBuilderConstants.GeneratedClipRoot);
        DeleteFolderContents(CharacterBuilderConstants.OverrideControllerRoot);

        EnsureFolder("Assets", "Animations");
        EnsureFolder("Assets/Animations", "Character");
        EnsureFolder(CharacterBuilderConstants.AnimationRoot, "GeneratedClips");
        EnsureFolder(CharacterBuilderConstants.AnimationRoot, "OverrideControllers");
    }

    private static void DeleteCharacterSourcePngs()
    {
        if (!AssetDatabase.IsValidFolder(CharacterBuilderConstants.InputRoot))
            return;

        string[] guids = AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { CharacterBuilderConstants.InputRoot }
        );

        int deleted = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (!CharacterTextureImportUtility.ShouldHandle(assetPath))
                continue;

            if (AssetDatabase.DeleteAsset(assetPath))
                deleted++;
        }

        Debug.Log($"Deleted character source PNGs: {deleted}");
    }

    private static void ClearCatalogButKeepAsset()
    {
        CharacterSpriteCatalog catalog =
            AssetDatabase.LoadAssetAtPath<CharacterSpriteCatalog>(
                CharacterBuilderConstants.CatalogPath
            );

        if (catalog == null)
            return;

        catalog.SetEntries(new List<CharacterCatalogEntry>());
        EditorUtility.SetDirty(catalog);
    }

    private static void DeleteFolderContents(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });

        int deleted = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (assetPath == folderPath)
                continue;

            if (AssetDatabase.DeleteAsset(assetPath))
                deleted++;
        }

        Debug.Log($"Deleted assets inside {folderPath}: {deleted}");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";

        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
