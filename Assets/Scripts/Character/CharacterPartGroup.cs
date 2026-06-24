using System;

public enum CharacterPartGroup
{
  Body,
  Face,
  Hair,
  UpperClothing,
  LowerClothing,
  Shoes,
  Weapon
}

public static class CharacterPartGroupUtility
{
  public static bool TryParse(string token, out CharacterPartGroup group)
  {
    switch (Normalize(token))
    {
      case "body":
        group = CharacterPartGroup.Body;
        return true;

      case "face":
        group = CharacterPartGroup.Face;
        return true;

      case "hair":
        group = CharacterPartGroup.Hair;
        return true;

      case "upperclothing":
        group = CharacterPartGroup.UpperClothing;
        return true;

      case "lowerclothing":
        group = CharacterPartGroup.LowerClothing;
        return true;

      case "shoes":
        group = CharacterPartGroup.Shoes;
        return true;

      case "weapon":
        group = CharacterPartGroup.Weapon;
        return true;

      default:
        group = default;
        return false;
    }
  }

  public static string ToToken(CharacterPartGroup group)
  {
    switch (group)
    {
      case CharacterPartGroup.Body:
        return "body";

      case CharacterPartGroup.Face:
        return "face";

      case CharacterPartGroup.Hair:
        return "hair";

      case CharacterPartGroup.UpperClothing:
        return "upperclothing";

      case CharacterPartGroup.LowerClothing:
        return "lowerclothing";

      case CharacterPartGroup.Shoes:
        return "shoes";

      case CharacterPartGroup.Weapon:
        return "weapon";

      default:
        throw new ArgumentOutOfRangeException(nameof(group), group, null);
    }
  }

  public static string Normalize(string value)
  {
    return string.IsNullOrWhiteSpace(value)
        ? ""
        : value.Trim().ToLowerInvariant();
  }
}
