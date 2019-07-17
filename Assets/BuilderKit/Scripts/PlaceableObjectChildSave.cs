using System;
using System.Collections.Generic;
using UnityEngine;

/*
This class is needed to avoid cycles when serializing/deserializing with the built in json utility.
 */
[Serializable]
public class PlaceableObjectChildSave {
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;

    public PlaceableObjectChildSave(GameObject placeable) {
        this.prefabName = placeable.name;
        this.position = placeable.transform.position;
        this.rotation = placeable.transform.rotation;
    }

    public GameObject Load() {
        GameObject instance = ObjectLoader.GetObject(prefabName);
        if(instance!=null) {
            PlaceableObject placeable = instance.GetComponent<PlaceableObject>();
            placeable.Init();
            instance.transform.rotation = rotation;
            placeable.Place(position,position);
        } else {
            Debug.LogWarning("No prefab could be loaded for "+prefabName);
        }

        return instance;
    }
}