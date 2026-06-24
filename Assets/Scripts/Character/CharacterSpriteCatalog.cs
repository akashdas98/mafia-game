using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterSpriteCatalog",
    menuName = "Character Builder/Sprite Catalog"
)]
public class CharacterSpriteCatalog : ScriptableObject
{
  [SerializeField] private List<CharacterCatalogEntry> entries = new();

  public IReadOnlyList<CharacterCatalogEntry> Entries => entries;

  public CharacterCatalogEntry Resolve(string key)
  {
    key = Normalize(key);

    return entries.FirstOrDefault(e => e.key == key);
  }

  public void SetEntries(List<CharacterCatalogEntry> newEntries)
  {
    entries = newEntries
        .Where(e => !string.IsNullOrWhiteSpace(e.key))
        .GroupBy(e => e.key)
        .Select(g => g.Last())
        .OrderBy(e => e.partGroup.ToString())
        .ThenBy(e => e.gender)
        .ThenBy(e => e.bodyType)
        .ThenBy(e => e.skinColor)
        .ThenBy(e => e.faceType)
        .ThenBy(e => e.variant)
        .ToList();
  }

  public List<string> GetGenders()
  {
    return entries
        .Select(e => e.gender)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct()
        .OrderBy(v => v)
        .ToList();
  }

  public List<string> GetBodyTypes(string gender)
  {
    gender = Normalize(gender);

    return entries
        .Where(e =>
            e.partGroup == CharacterPartGroup.Body &&
            e.gender == gender
        )
        .Select(e => e.bodyType)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct()
        .OrderBy(v => v)
        .ToList();
  }

  public List<string> GetSkinColors(string gender, string bodyType)
  {
    gender = Normalize(gender);
    bodyType = Normalize(bodyType);

    return entries
        .Where(e =>
            e.partGroup == CharacterPartGroup.Body &&
            e.gender == gender &&
            e.bodyType == bodyType
        )
        .Select(e => e.skinColor)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct()
        .OrderBy(v => v)
        .ToList();
  }

  public List<string> GetFaceTypes(string gender, string skinColor)
  {
    gender = Normalize(gender);
    skinColor = Normalize(skinColor);

    return entries
        .Where(e =>
            e.partGroup == CharacterPartGroup.Face &&
            e.gender == gender &&
            e.skinColor == skinColor
        )
        .Select(e => e.faceType)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct()
        .OrderBy(v => v)
        .ToList();
  }

  public List<string> GetHairStyles(string gender, string faceType)
  {
    gender = Normalize(gender);
    faceType = Normalize(faceType);

    return entries
        .Where(e =>
            e.partGroup == CharacterPartGroup.Hair &&
            e.gender == gender &&
            e.faceType == faceType
        )
        .Select(e => e.variant)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct()
        .OrderBy(v => v)
        .ToList();
  }

  public List<string> GetUpperClothingVariants(
      string gender,
      string bodyType
  )
  {
    return GetBodyTypeDependentVariants(
        CharacterPartGroup.UpperClothing,
        gender,
        bodyType
    );
  }

  public List<string> GetLowerClothingVariants(
      string gender,
      string bodyType
  )
  {
    return GetBodyTypeDependentVariants(
        CharacterPartGroup.LowerClothing,
        gender,
        bodyType
    );
  }

  public List<string> GetShoeVariants(string gender)
  {
    gender = Normalize(gender);

    return entries
        .Where(e =>
            e.partGroup == CharacterPartGroup.Shoes &&
            e.gender == gender
        )
        .Select(e => e.variant)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct()
        .OrderBy(v => v)
        .ToList();
  }

  public List<string> GetWeaponVariants(string gender)
  {
    gender = Normalize(gender);

    return entries
        .Where(e =>
            e.partGroup == CharacterPartGroup.Weapon &&
            e.gender == gender
        )
        .Select(e => e.variant)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct()
        .OrderBy(v => v)
        .ToList();
  }

  private List<string> GetBodyTypeDependentVariants(
      CharacterPartGroup partGroup,
      string gender,
      string bodyType
  )
  {
    gender = Normalize(gender);
    bodyType = Normalize(bodyType);

    return entries
        .Where(e =>
            e.partGroup == partGroup &&
            e.gender == gender &&
            e.bodyType == bodyType
        )
        .Select(e => e.variant)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct()
        .OrderBy(v => v)
        .ToList();
  }

  public static string BuildBodyKey(
      string gender,
      string bodyType,
      string skinColor
  )
  {
    return $"body_{Normalize(gender)}_{Normalize(bodyType)}_{Normalize(skinColor)}";
  }

  public static string BuildFaceKey(
      string gender,
      string skinColor,
      string faceType
  )
  {
    return $"face_{Normalize(gender)}_{Normalize(skinColor)}_{Normalize(faceType)}";
  }

  public static string BuildHairKey(
      string gender,
      string faceType,
      string hairStyle
  )
  {
    return $"hair_{Normalize(gender)}_{Normalize(faceType)}_{Normalize(hairStyle)}";
  }

  public static string BuildUpperClothingKey(
      string gender,
      string bodyType,
      string variant
  )
  {
    return $"upperclothing_{Normalize(gender)}_{Normalize(bodyType)}_{Normalize(variant)}";
  }

  public static string BuildLowerClothingKey(
      string gender,
      string bodyType,
      string variant
  )
  {
    return $"lowerclothing_{Normalize(gender)}_{Normalize(bodyType)}_{Normalize(variant)}";
  }

  public static string BuildShoesKey(string gender, string variant)
  {
    return $"shoes_{Normalize(gender)}_{Normalize(variant)}";
  }

  public static string BuildWeaponKey(string gender, string variant)
  {
    return $"weapon_{Normalize(gender)}_{Normalize(variant)}";
  }

  public static string Normalize(string value)
  {
    return CharacterPartGroupUtility.Normalize(value);
  }
}

[Serializable]
public class CharacterCatalogEntry
{
  public string key;

  public CharacterPartGroup partGroup;

  public string gender;
  public string bodyType;

  public string skinColor;
  public string faceType;

  public string variant;

  public bool useStaticSprite;

  public RuntimeAnimatorController overrideController;
  public Sprite staticSprite;
}