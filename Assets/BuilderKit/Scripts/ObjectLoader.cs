using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Builds a dictionary containing the loaded prefabs for building and furniture assets.
 */
public class ObjectLoader {
  //We may not want everything inside the Prefabs folder to be loaded, so we can control it with this array
  static string[] categories = { "Floors", "Walls and Stairs", "Doors and Windows", "Furniture", "Bathroom",
    "Kitchen", "Bedroom", "Electronics", "Wall Paint", "Roofs" };

  static Dictionary<string,GameObject[]> objectsByCategory;
  static Dictionary<string,GameObject> objects;

  static Dictionary<string,Material> materials;

  private static void LoadObjects() {
    objectsByCategory = new Dictionary<string,GameObject[]>();
    objects = new Dictionary<string,GameObject>();

    foreach (string category in categories) {
      UnityEngine.Object[] loadedObjects = Resources.LoadAll("Prefabs/" + category, typeof(GameObject));

      if (loadedObjects != null && loadedObjects.Length > 0) {
        GameObject[] loadedGameObjects = new GameObject[loadedObjects.Length];

        //We cast the items from UnityEngine.Object to GameObject and put them in a new array
        for (int i = 0; i < loadedObjects.Length; i++) {
          GameObject loaded = (GameObject)loadedObjects[i];
          loadedGameObjects[i] = loaded;
          objects.Add(loaded.name,loaded);
        }

        objectsByCategory.Add(category, loadedGameObjects);
      } else {
        Debug.LogWarning("Loading " + category + ". No items found");
      }

    }
  }

  public static Dictionary<string,GameObject[]> GetObjectsByCategory() {
    if (objectsByCategory == null) {
      LoadObjects();
    }

    return objectsByCategory;
  }

  public static GameObject GetObject(string name) {
    if(objects==null) {
      LoadObjects();
    }

    GameObject obj = null;
    if(!objects.TryGetValue(name,out obj)) {
      Debug.LogWarning("Object prefab not found for name "+name);
    }

    return obj;
  }

  public static Material GetMaterial(string name) {
    if(materials==null) {
      materials = new Dictionary<string,Material>();
      UnityEngine.Object[] mats = Resources.LoadAll("Materials");
      foreach(UnityEngine.Object mat in mats) {
        Material material = (Material)mat;
        materials.Add(material.name,material);
      }
    }

    Material searchedMat = null;
    materials.TryGetValue(name,out searchedMat);

    return searchedMat;
  }
	
}
