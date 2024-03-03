using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDetails : MonoBehaviour
{
  public string name;
  private Scene scene;
  void Start()
  {
    scene = SceneManager.GetSceneByName(name);
  }

  public GameObject GetTerrain()
  {
    if (scene.isLoaded)
    {
      GameObject[] rootObjects = scene.GetRootGameObjects();
      foreach (GameObject obj in rootObjects)
      {
        if (obj.CompareTag("Terrain"))
        {
          return obj;
        }
      }
    }
    return null;
  }

  public void MoveGameObjectToScene(GameObject gameObject)
  {
    if (scene.isLoaded)
    {
      SceneManager.MoveGameObjectToScene(gameObject, scene);
    }
  }
}