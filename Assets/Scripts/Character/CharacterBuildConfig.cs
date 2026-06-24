using System;
using UnityEngine;

[Serializable]
public class CharacterBuildConfig
{
  [Header("Independent")]
  public string gender = "";
  public string bodyType = "";
  public string skinColor = "";

  [Header("Dependent")]
  public string faceType = "";
  public string hairStyle = "";

  [Header("Clothing / Accessories")]
  public string upperClothingVariant = "";
  public string lowerClothingVariant = "";
  public string shoesVariant = "";
  public string weaponVariant = "";

  public string BuildKey(CharacterPartGroup partGroup)
  {
    string genderValue = Normalize(gender);
    string bodyTypeValue = Normalize(bodyType);
    string skinColorValue = Normalize(skinColor);
    string faceTypeValue = Normalize(faceType);

    switch (partGroup)
    {
      case CharacterPartGroup.Body:
        return CharacterSpriteCatalog.BuildBodyKey(
            genderValue,
            bodyTypeValue,
            skinColorValue
        );

      case CharacterPartGroup.Face:
        return CharacterSpriteCatalog.BuildFaceKey(
            genderValue,
            skinColorValue,
            faceTypeValue
        );

      case CharacterPartGroup.Hair:
        return CharacterSpriteCatalog.BuildHairKey(
            genderValue,
            faceTypeValue,
            Normalize(hairStyle)
        );

      case CharacterPartGroup.UpperClothing:
        return CharacterSpriteCatalog.BuildUpperClothingKey(
            genderValue,
            bodyTypeValue,
            Normalize(upperClothingVariant)
        );

      case CharacterPartGroup.LowerClothing:
        return CharacterSpriteCatalog.BuildLowerClothingKey(
            genderValue,
            bodyTypeValue,
            Normalize(lowerClothingVariant)
        );

      case CharacterPartGroup.Shoes:
        return CharacterSpriteCatalog.BuildShoesKey(
            genderValue,
            Normalize(shoesVariant)
        );

      case CharacterPartGroup.Weapon:
        return CharacterSpriteCatalog.BuildWeaponKey(
            genderValue,
            Normalize(weaponVariant)
        );

      default:
        return "";
    }
  }

  private static string Normalize(string value)
  {
    return CharacterPartGroupUtility.Normalize(value);
  }
}