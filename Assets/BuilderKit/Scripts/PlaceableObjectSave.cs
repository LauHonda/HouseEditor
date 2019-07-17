using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlaceableObjectSave {
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
    public string[] materials;
    public List<PlaceableObjectChildSave> children;

    public PlaceableObjectSave(GameObject placeable) {
        this.prefabName = placeable.name;
        this.position = placeable.transform.position;
        this.rotation = placeable.transform.rotation;

        MeshRenderer rend = placeable.GetComponent<MeshRenderer>();
        if(rend!=null) {
            if(rend.materials.Length>0) {
                materials = new string[rend.materials.Length];
                for(int i=0;i<rend.materials.Length;i++) {
                    materials[i] = rend.materials[i].name.Replace(" (Instance)","");;
                }
            }
        }

        if(placeable.transform.childCount>0) {
            children = new List<PlaceableObjectChildSave>();
            for(int i=0;i<placeable.transform.childCount;i++) {
                children.Add(new PlaceableObjectChildSave(placeable.transform.GetChild(i).gameObject));
            }
        }
    }

    public GameObject Load() {
        GameObject instance = ObjectLoader.GetObject(prefabName);
        
        if(instance!=null) {
            PlaceableObject placeable = instance.GetComponent<PlaceableObject>();
            placeable.Init();
            instance.transform.rotation = rotation;
            List<GameObject> placedObjects = placeable.Place(position,position);
            if(materials!=null && materials.Length>0) {
                Material[] instancedMaterials = new Material[materials.Length];
                for(int i=0;i<materials.Length;i++) {
                    Material loadedMaterial = ObjectLoader.GetMaterial(materials[i]);
                    if(loadedMaterial!=null) {
                        instancedMaterials[i] = loadedMaterial;
                    }else {
                        Debug.LogWarning("Couldn't find material: "+materials[i]);
                    }
                }
                foreach(GameObject placed in placedObjects) {
                    MeshRenderer rend = placed.GetComponent<MeshRenderer>();
                    rend.materials = instancedMaterials;
                }
            }
            
            if(children!=null && children.Count>0) {
                foreach(PlaceableObjectChildSave child in children) {
                    child.Load();
                }
            }
        } else {
            Debug.LogWarning("No prefab could be loaded for "+prefabName);
        }

        return instance;
    }
}