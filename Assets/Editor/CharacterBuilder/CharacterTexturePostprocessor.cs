using UnityEditor;
using UnityEngine;

public class CharacterTexturePostprocessor : AssetPostprocessor
{
  private void OnPreprocessTexture()
  {
    if (!CharacterTextureImportUtility.ShouldHandle(assetPath))
      return;

    if (!CharacterFilenameParser.TryParse(
            assetPath,
            out ParsedCharacterPng parsed,
            out string error
        ))
    {
      Debug.LogError(
          $"Invalid character PNG filename: {assetPath}. {error}"
      );
      return;
    }

    TextureImporter importer = (TextureImporter)assetImporter;
    CharacterTextureImportUtility.ConfigureImporter(importer, parsed);
  }
}