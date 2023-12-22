using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadUnloadScene : MonoBehaviour
{
  public string[] scenesToLoad, scenesToUnload;
  private GameObject player;

  void Awake()
  {
    player = GameObject.FindWithTag("Player");
  }

  private void OnTriggerEnter2D(Collider2D collision)
  {
    Debug.Log($"{collision.gameObject}, {player}");
    if (collision.gameObject == player)
    {
      LoadScenes();
      UnloadScenes();
    }
  }

  private void LoadScenes()
  {
    for (int i = 0; i < scenesToLoad.Length; i++)
    {
      string sceneName = scenesToLoad[i];
      bool isSceneLoaded = SceneManager.GetSceneByName(sceneName).isLoaded;

      Debug.Log($"{sceneName}, {isSceneLoaded}");

      if (!isSceneLoaded)
      {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
      }
    }
  }

  private void UnloadScenes()
  {
    for (int i = 0; i < scenesToUnload.Length; i++)
    {
      string sceneName = scenesToUnload[i];
      bool isSceneLoaded = SceneManager.GetSceneByName(sceneName).isLoaded;

      if (isSceneLoaded)
      {
        SceneManager.UnloadSceneAsync(sceneName);
      }
    }
  }
}
