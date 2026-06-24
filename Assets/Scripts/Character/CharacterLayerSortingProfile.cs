using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterLayerSortingProfile",
    menuName = "Character Builder/Layer Sorting Profile"
)]
public class CharacterLayerSortingProfile : ScriptableObject
{
  [SerializeField] private string sortingLayerName = "Characters";
  [SerializeField] private List<CharacterLayerSortingEntry> entries = new();

  public string SortingLayerName => sortingLayerName;

  public int GetSortingOrder(CharacterPartGroup partGroup)
  {
    foreach (CharacterLayerSortingEntry entry in entries)
    {
      if (entry.partGroup == partGroup)
        return entry.sortingOrder;
    }

    Debug.LogWarning($"No sorting order configured for {partGroup}. Using 0.");
    return 0;
  }
}

[Serializable]
public class CharacterLayerSortingEntry
{
  public CharacterPartGroup partGroup;
  public int sortingOrder;
}