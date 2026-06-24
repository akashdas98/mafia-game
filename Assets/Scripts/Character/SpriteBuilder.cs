using UnityEngine;

[ExecuteAlways]
public class SpriteBuilder : MonoBehaviour
{
  [Header("Catalog")]
  [SerializeField] private CharacterSpriteCatalog catalog;

  [Header("Sorting")]
  [SerializeField] private CharacterLayerSortingProfile sortingProfile;

  [Header("Current Character")]
  [SerializeField] private CharacterBuildConfig config = new();

  [Header("Layers")]
  [SerializeField] private CharacterSpriteLayer bodyLayer;
  [SerializeField] private CharacterSpriteLayer faceLayer;
  [SerializeField] private CharacterSpriteLayer hairLayer;
  [SerializeField] private CharacterSpriteLayer upperClothingLayer;
  [SerializeField] private CharacterSpriteLayer lowerClothingLayer;
  [SerializeField] private CharacterSpriteLayer shoesLayer;
  [SerializeField] private CharacterSpriteLayer weaponLayer;

  public CharacterSpriteCatalog Catalog => catalog;
  public CharacterBuildConfig Config => config;

  private void OnValidate()
  {
    if (!Application.isPlaying)
      Apply();
  }

  [ContextMenu("Apply Sprite Build")]
  public void Apply()
  {
    if (catalog == null || config == null)
      return;

    ApplyLayer(bodyLayer, CharacterPartGroup.Body);
    ApplyLayer(lowerClothingLayer, CharacterPartGroup.LowerClothing);
    ApplyLayer(shoesLayer, CharacterPartGroup.Shoes);
    ApplyLayer(upperClothingLayer, CharacterPartGroup.UpperClothing);
    ApplyLayer(faceLayer, CharacterPartGroup.Face);
    ApplyLayer(hairLayer, CharacterPartGroup.Hair);
    ApplyLayer(weaponLayer, CharacterPartGroup.Weapon);
  }

  public void AutoSelectMissingOrInvalidOptions()
  {
    if (catalog == null || config == null)
      return;

    config.gender = PickValid(config.gender, catalog.GetGenders());

    config.bodyType = PickValid(
        config.bodyType,
        catalog.GetBodyTypes(config.gender)
    );

    config.skinColor = PickValid(
        config.skinColor,
        catalog.GetSkinColors(config.gender, config.bodyType)
    );

    config.faceType = PickValid(
        config.faceType,
        catalog.GetFaceTypes(config.gender, config.skinColor)
    );

    config.hairStyle = PickValid(
        config.hairStyle,
        catalog.GetHairStyles(config.gender, config.faceType)
    );

    config.upperClothingVariant = PickValid(
        config.upperClothingVariant,
        catalog.GetUpperClothingVariants(config.gender, config.bodyType)
    );

    config.lowerClothingVariant = PickValid(
        config.lowerClothingVariant,
        catalog.GetLowerClothingVariants(config.gender, config.bodyType)
    );

    config.shoesVariant = PickValid(
        config.shoesVariant,
        catalog.GetShoeVariants(config.gender)
    );

    config.weaponVariant = PickValidAllowEmpty(
        config.weaponVariant,
        catalog.GetWeaponVariants(config.gender)
    );
  }

  public void SetGender(string value)
  {
    config.gender = CharacterSpriteCatalog.Normalize(value);
    AutoSelectMissingOrInvalidOptions();
    Apply();
  }

  public void SetBodyType(string value)
  {
    config.bodyType = CharacterSpriteCatalog.Normalize(value);
    AutoSelectMissingOrInvalidOptions();
    Apply();
  }

  public void SetSkinColor(string value)
  {
    config.skinColor = CharacterSpriteCatalog.Normalize(value);
    AutoSelectMissingOrInvalidOptions();
    Apply();
  }

  public void SetFaceType(string value)
  {
    config.faceType = CharacterSpriteCatalog.Normalize(value);
    AutoSelectMissingOrInvalidOptions();
    Apply();
  }

  public void SetHairStyle(string value)
  {
    config.hairStyle = CharacterSpriteCatalog.Normalize(value);
    AutoSelectMissingOrInvalidOptions();
    Apply();
  }

  public void SetUpperClothing(string value)
  {
    config.upperClothingVariant = CharacterSpriteCatalog.Normalize(value);
    AutoSelectMissingOrInvalidOptions();
    Apply();
  }

  public void SetLowerClothing(string value)
  {
    config.lowerClothingVariant = CharacterSpriteCatalog.Normalize(value);
    AutoSelectMissingOrInvalidOptions();
    Apply();
  }

  public void SetShoes(string value)
  {
    config.shoesVariant = CharacterSpriteCatalog.Normalize(value);
    AutoSelectMissingOrInvalidOptions();
    Apply();
  }

  public void SetWeapon(string value)
  {
    config.weaponVariant = CharacterSpriteCatalog.Normalize(value);
    AutoSelectMissingOrInvalidOptions();
    Apply();
  }

  private void ApplyLayer(
      CharacterSpriteLayer layer,
      CharacterPartGroup partGroup
  )
  {
    if (layer == null)
      return;

    ApplySorting(layer, partGroup);

    string key = config.BuildKey(partGroup);

    if (string.IsNullOrWhiteSpace(key))
    {
      layer.Clear();
      return;
    }

    CharacterCatalogEntry entry = catalog.Resolve(key);

    if (entry == null)
    {
      if (partGroup != CharacterPartGroup.Weapon)
      {
        Debug.LogWarning(
            $"Missing catalog entry for {partGroup}: {key}",
            this
        );
      }

      layer.Clear();
      return;
    }

    if (entry.useStaticSprite)
    {
      layer.ApplyStatic(entry.staticSprite);
    }
    else
    {
      layer.ApplyAnimated(entry.overrideController);
    }
  }

  private void ApplySorting(
      CharacterSpriteLayer layer,
      CharacterPartGroup partGroup
  )
  {
    if (sortingProfile == null)
      return;

    layer.SetSorting(
        sortingProfile.SortingLayerName,
        sortingProfile.GetSortingOrder(partGroup)
    );
  }

  private static string PickValid(
      string current,
      System.Collections.Generic.List<string> options
  )
  {
    if (options == null || options.Count == 0)
      return "";

    current = CharacterSpriteCatalog.Normalize(current);

    return options.Contains(current)
        ? current
        : options[0];
  }

  private static string PickValidAllowEmpty(
      string current,
      System.Collections.Generic.List<string> options
  )
  {
    if (options == null || options.Count == 0)
      return "";

    current = CharacterSpriteCatalog.Normalize(current);

    if (string.IsNullOrWhiteSpace(current))
      return "";

    return options.Contains(current)
        ? current
        : "";
  }
}
