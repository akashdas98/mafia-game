using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public enum CharacterExportType
{
  Single,
  Sheet
}

public sealed class ParsedCharacterPng
{
  public string assetPath;
  public string fileNameWithoutExtension;

  public CharacterExportType exportType;
  public float sampleRate;

  public string gender;
  public string sourceBodyType;

  public List<string> contextTokens = new();

  public CharacterPartGroup partGroup;

  public List<string> variantTokens = new();

  public string Context => string.Join("_", contextTokens);
  public string RawVariant => string.Join("_", variantTokens);

  public bool HasContext => contextTokens.Count > 0;

  public CharacterPartIdentity Identity =>
      CharacterPartIdentity.FromParsed(this);
}

public sealed class CharacterPartIdentity
{
  public string key;

  public CharacterPartGroup partGroup;

  public string gender;
  public string bodyType;

  public string skinColor;
  public string faceType;

  public string variant;

  public static CharacterPartIdentity FromParsed(ParsedCharacterPng parsed)
  {
    string gender = Normalize(parsed.gender);
    string sourceBodyType = Normalize(parsed.sourceBodyType);

    List<string> variantTokens = parsed.variantTokens
        .Select(Normalize)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .ToList();

    string rawVariant = string.Join("_", variantTokens);

    var identity = new CharacterPartIdentity
    {
      partGroup = parsed.partGroup,
      gender = gender,
      bodyType = "",
      skinColor = "",
      faceType = "",
      variant = rawVariant
    };

    switch (parsed.partGroup)
    {
      case CharacterPartGroup.Body:
        {
          identity.bodyType = sourceBodyType;
          identity.skinColor = rawVariant;
          identity.variant = rawVariant;

          identity.key = CharacterSpriteCatalog.BuildBodyKey(
              gender,
              sourceBodyType,
              identity.skinColor
          );

          return identity;
        }

      case CharacterPartGroup.Face:
        {
          if (variantTokens.Count < 2)
          {
            throw new InvalidOperationException(
                $"Face variant must be faceType_skinColor. Got: {rawVariant}"
            );
          }

          identity.skinColor = variantTokens.Last();

          identity.faceType = string.Join(
              "_",
              variantTokens.Take(variantTokens.Count - 1)
          );

          identity.variant = identity.faceType;

          identity.key = CharacterSpriteCatalog.BuildFaceKey(
              gender,
              identity.skinColor,
              identity.faceType
          );

          return identity;
        }

      case CharacterPartGroup.Hair:
        {
          if (variantTokens.Count < 2)
          {
            throw new InvalidOperationException(
                $"Hair variant must be hairStyle_faceType. Got: {rawVariant}"
            );
          }

          identity.faceType = variantTokens.Last();

          identity.variant = string.Join(
              "_",
              variantTokens.Take(variantTokens.Count - 1)
          );

          identity.key = CharacterSpriteCatalog.BuildHairKey(
              gender,
              identity.faceType,
              identity.variant
          );

          return identity;
        }

      case CharacterPartGroup.UpperClothing:
        {
          identity.bodyType = sourceBodyType;

          identity.key = CharacterSpriteCatalog.BuildUpperClothingKey(
              gender,
              sourceBodyType,
              rawVariant
          );

          return identity;
        }

      case CharacterPartGroup.LowerClothing:
        {
          identity.bodyType = sourceBodyType;

          identity.key = CharacterSpriteCatalog.BuildLowerClothingKey(
              gender,
              sourceBodyType,
              rawVariant
          );

          return identity;
        }

      case CharacterPartGroup.Shoes:
        {
          identity.key = CharacterSpriteCatalog.BuildShoesKey(
              gender,
              rawVariant
          );

          return identity;
        }

      case CharacterPartGroup.Weapon:
        {
          identity.key = CharacterSpriteCatalog.BuildWeaponKey(
              gender,
              rawVariant
          );

          return identity;
        }

      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  private static string Normalize(string value)
  {
    return CharacterSpriteCatalog.Normalize(value);
  }
}

public sealed class ExpandedAnimationContext
{
  public string context;
  public bool reverseFrames;
}

public static class CharacterFilenameParser
{

  public static bool TryParse(
      string assetPath,
      out ParsedCharacterPng parsed,
      out string error
  )
  {
    parsed = null;
    error = "";

    string fileName = Path.GetFileNameWithoutExtension(assetPath)
        .ToLowerInvariant();

    string[] tokens = fileName.Split('_');

    if (tokens.Length < 5)
    {
      error = "Not enough tokens.";
      return false;
    }

    CharacterExportType exportType;
    float? explicitSampleRate = null;
    int contentEndExclusive;

    string lastToken = tokens[tokens.Length - 1];

    if (lastToken == "single")
    {
      exportType = CharacterExportType.Single;
      contentEndExclusive = tokens.Length - 1;
    }
    else if (lastToken == "sheet")
    {
      exportType = CharacterExportType.Sheet;
      contentEndExclusive = tokens.Length - 1;
    }
    else if (
        tokens.Length >= 2 &&
        tokens[tokens.Length - 2] == "sheet"
    )
    {
      exportType = CharacterExportType.Sheet;
      contentEndExclusive = tokens.Length - 2;

      if (!float.TryParse(
              lastToken,
              System.Globalization.NumberStyles.Float,
              System.Globalization.CultureInfo.InvariantCulture,
              out float parsedSampleRate
          ))
      {
        error = "Invalid sheet sample rate. Use _sheet or _sheet_{sampleRate}. Example: _sheet_12";
        return false;
      }

      if (parsedSampleRate <= 0f)
      {
        error = "Sheet sample rate must be greater than 0.";
        return false;
      }

      explicitSampleRate = parsedSampleRate;
    }
    else
    {
      error = "Filename must end with _single, _sheet, or _sheet_{sampleRate}.";
      return false;
    }

    float sampleRate = explicitSampleRate
        ?? CharacterBuilderConstants.DefaultSampleRate;

    if (contentEndExclusive < 4)
    {
      error = "Not enough content tokens before export suffix.";
      return false;
    }

    string gender = tokens[0];
    string bodyType = tokens[1];

    int partGroupIndex = -1;
    CharacterPartGroup partGroup = default;

    for (int i = 2; i < contentEndExclusive; i++)
    {
      if (CharacterPartGroupUtility.TryParse(tokens[i], out partGroup))
      {
        partGroupIndex = i;
        break;
      }
    }

    if (partGroupIndex < 0)
    {
      error = "Could not find known part group token.";
      return false;
    }

    if (partGroupIndex == contentEndExclusive - 1)
    {
      error = "Part group exists, but variant is missing.";
      return false;
    }

    parsed = new ParsedCharacterPng
    {
      assetPath = assetPath,
      fileNameWithoutExtension = fileName,
      exportType = exportType,
      sampleRate = sampleRate,
      gender = gender,
      sourceBodyType = bodyType,
      partGroup = partGroup,
      contextTokens = tokens
            .Skip(2)
            .Take(partGroupIndex - 2)
            .ToList(),
      variantTokens = tokens
            .Skip(partGroupIndex + 1)
            .Take(contentEndExclusive - partGroupIndex - 1)
            .ToList()
    };

    return true;
  }
  public static List<ExpandedAnimationContext> ExpandDirectionPairs(
      IReadOnlyList<string> contextTokens
  )
  {
    int pairIndex = -1;
    string pairToken = "";

    for (int i = 0; i < contextTokens.Count; i++)
    {
      if (contextTokens[i].Contains("+"))
      {
        pairIndex = i;
        pairToken = contextTokens[i];
        break;
      }
    }

    if (pairIndex < 0)
    {
      return new List<ExpandedAnimationContext>
            {
                new()
                {
                    context = string.Join("_", contextTokens),
                    reverseFrames = false
                }
            };
    }

    string[] directions = pairToken.Split('+');

    if (directions.Length != 2)
    {
      throw new InvalidOperationException(
          $"Invalid direction-pair token: {pairToken}"
      );
    }

    List<string> forward = contextTokens.ToList();
    forward[pairIndex] = directions[0];

    List<string> reverse = contextTokens.ToList();
    reverse[pairIndex] = directions[1];

    return new List<ExpandedAnimationContext>
        {
            new()
            {
                context = string.Join("_", forward),
                reverseFrames = false
            },
            new()
            {
                context = string.Join("_", reverse),
                reverseFrames = true
            }
        };
  }

  public static string BuildGeneratedClipName(
      CharacterPartIdentity identity,
      string expandedContext
  )
  {
    return $"{identity.key}_{expandedContext}";
  }

  public static string BuildSlotName(string expandedContext)
  {
    return $"slot_{expandedContext}";
  }
}