using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpriteBuilder))]
public class SpriteBuilderEditor : Editor
{
  public override void OnInspectorGUI()
  {
    SpriteBuilder builder = (SpriteBuilder)target;

    DrawDefaultInspector();

    EditorGUILayout.Space(12);

    CharacterSpriteCatalog catalog = builder.Catalog;
    CharacterBuildConfig config = builder.Config;

    if (catalog == null || config == null)
    {
      EditorGUILayout.HelpBox(
          "Assign a CharacterSpriteCatalog to enable dependency-aware dropdowns.",
          MessageType.Info
      );
      return;
    }

    EditorGUILayout.LabelField(
        "Dependency-Aware Character Selection",
        EditorStyles.boldLabel
    );

    Undo.RecordObject(builder, "Change Sprite Builder Selection");

    config.gender = DrawDropdown(
        "Gender",
        config.gender,
        catalog.GetGenders()
    );

    config.bodyType = DrawDropdown(
        "Body Type",
        config.bodyType,
        catalog.GetBodyTypes(config.gender)
    );

    config.skinColor = DrawDropdown(
        "Skin Color",
        config.skinColor,
        catalog.GetSkinColors(config.gender, config.bodyType)
    );

    config.faceType = DrawDropdown(
        "Face Type",
        config.faceType,
        catalog.GetFaceTypes(config.gender, config.skinColor)
    );

    config.hairStyle = DrawDropdown(
        "Hair Style",
        config.hairStyle,
        catalog.GetHairStyles(config.gender, config.faceType)
    );

    config.upperClothingVariant = DrawDropdown(
        "Upper Clothing",
        config.upperClothingVariant,
        catalog.GetUpperClothingVariants(config.gender, config.bodyType)
    );

    config.lowerClothingVariant = DrawDropdown(
        "Lower Clothing",
        config.lowerClothingVariant,
        catalog.GetLowerClothingVariants(config.gender, config.bodyType)
    );

    config.shoesVariant = DrawDropdown(
        "Shoes",
        config.shoesVariant,
        catalog.GetShoeVariants(config.gender)
    );

    config.weaponVariant = DrawDropdownAllowEmpty(
        "Weapon",
        config.weaponVariant,
        catalog.GetWeaponVariants(config.gender)
    );

    EditorGUILayout.Space(8);

    if (GUILayout.Button("Auto Select Missing / Invalid Options"))
    {
      builder.AutoSelectMissingOrInvalidOptions();
      builder.Apply();
      EditorUtility.SetDirty(builder);
    }

    if (GUILayout.Button("Apply Sprite Build"))
    {
      builder.Apply();
      EditorUtility.SetDirty(builder);
    }

    if (GUI.changed)
    {
      builder.AutoSelectMissingOrInvalidOptions();
      builder.Apply();
      EditorUtility.SetDirty(builder);
    }
  }

  private static string DrawDropdown(
      string label,
      string current,
      List<string> options
  )
  {
    if (options == null || options.Count == 0)
    {
      EditorGUILayout.LabelField(label, "No valid options");
      return "";
    }

    current = CharacterSpriteCatalog.Normalize(current);

    int index = options.IndexOf(current);

    if (index < 0)
      index = 0;

    int newIndex = EditorGUILayout.Popup(
        label,
        index,
        options.ToArray()
    );

    return options[newIndex];
  }

  private static string DrawDropdownAllowEmpty(
      string label,
      string current,
      List<string> options
  )
  {
    List<string> fullOptions = new List<string> { "" };

    if (options != null)
      fullOptions.AddRange(options);

    int index = fullOptions.IndexOf(
        CharacterSpriteCatalog.Normalize(current)
    );

    if (index < 0)
      index = 0;

    int newIndex = EditorGUILayout.Popup(
        label,
        index,
        fullOptions.ToArray()
    );

    return fullOptions[newIndex];
  }
}
