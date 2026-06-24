#pragma warning disable 0618

using System.IO;
using UnityEditor;
using UnityEngine;

public static class CharacterTextureImportUtility
{
  public static bool ShouldHandle(string assetPath)
  {
    if (!assetPath.StartsWith(CharacterBuilderConstants.InputRoot))
      return false;

    string fileName = Path.GetFileNameWithoutExtension(assetPath)
        .ToLowerInvariant();

    string[] tokens = fileName.Split('_');

    if (tokens.Length == 0)
      return false;

    string lastToken = tokens[tokens.Length - 1];

    if (lastToken == "single")
      return true;

    if (lastToken == "sheet")
      return true;

    return tokens.Length >= 2 &&
           tokens[tokens.Length - 2] == "sheet";
  }

  public static void ConfigureImporter(
      TextureImporter importer,
      ParsedCharacterPng parsed
  )
  {
    importer.textureType = TextureImporterType.Sprite;
    importer.filterMode = FilterMode.Point;
    importer.mipmapEnabled = false;
    importer.textureCompression = TextureImporterCompression.Uncompressed;
    importer.alphaIsTransparency = true;
    importer.spritePixelsPerUnit = CharacterBuilderConstants.PixelsPerUnit;
    importer.npotScale = TextureImporterNPOTScale.None;

    if (parsed.exportType == CharacterExportType.Single)
    {
      importer.spriteImportMode = SpriteImportMode.Single;
      importer.spritePivot = CharacterBuilderConstants.Pivot;
      return;
    }

    importer.spriteImportMode = SpriteImportMode.Multiple;

    if (!TryGetImageSize(parsed.assetPath, out int width, out int height))
    {
      Debug.LogWarning($"Could not read image size: {parsed.assetPath}");
      return;
    }

    if (
        width % CharacterBuilderConstants.CellSize != 0 ||
        height % CharacterBuilderConstants.CellSize != 0
    )
    {
      Debug.LogError(
          $"Sheet dimensions must be divisible by {CharacterBuilderConstants.CellSize}: {parsed.assetPath}"
      );
      return;
    }

    importer.spritesheet = BuildSpriteMetaData(
        parsed.fileNameWithoutExtension,
        width,
        height
    );
  }

  private static SpriteMetaData[] BuildSpriteMetaData(
      string baseName,
      int width,
      int height
  )
  {
    int cell = CharacterBuilderConstants.CellSize;
    int columns = width / cell;
    int rows = height / cell;

    SpriteMetaData[] metadata = new SpriteMetaData[columns * rows];

    int index = 0;

    for (int visualRow = 0; visualRow < rows; visualRow++)
    {
      int unityRow = rows - 1 - visualRow;

      for (int col = 0; col < columns; col++)
      {
        int frameIndex = visualRow * columns + col;

        metadata[index] = new SpriteMetaData
        {
          name = $"{baseName}_{frameIndex:000}",
          rect = new Rect(
                col * cell,
                unityRow * cell,
                cell,
                cell
            ),
          alignment = (int)SpriteAlignment.Custom,
          pivot = CharacterBuilderConstants.Pivot
        };

        index++;
      }
    }

    return metadata;
  }

  private static bool TryGetImageSize(
      string assetPath,
      out int width,
      out int height
  )
  {
    width = 0;
    height = 0;

    string fullPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        assetPath
    );

    if (!File.Exists(fullPath))
      return false;

    byte[] bytes = File.ReadAllBytes(fullPath);
    Texture2D texture = new Texture2D(2, 2);

    bool loaded = ImageConversion.LoadImage(texture, bytes);

    if (!loaded)
    {
      Object.DestroyImmediate(texture);
      return false;
    }

    width = texture.width;
    height = texture.height;

    Object.DestroyImmediate(texture);
    return true;
  }
}