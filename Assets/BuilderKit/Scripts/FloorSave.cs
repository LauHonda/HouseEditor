using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FloorSave {
    public int floorNumber;

    public List<PlaceableObjectSave> objectSaves;

    public FloorSave(int floorNumber, GameObject buildingFloor) {
        this.floorNumber = floorNumber;
        objectSaves = new List<PlaceableObjectSave>();

        for(int i=0;i<buildingFloor.transform.childCount;i++) {
            objectSaves.Add(new PlaceableObjectSave(buildingFloor.transform.GetChild(i).gameObject));
        }

    }

    public void Load() {
        Transform floor = GameObject.Find("BuildingFloor"+floorNumber).transform;
        BuildingFloorManager.activeFloor = floorNumber;

        foreach(PlaceableObjectSave objectSave in objectSaves) {
            GameObject placeable = objectSave.Load();
            if(placeable==null) {
                Debug.LogWarning("Failed to load saved object with prefab name " + objectSave.prefabName);
            }
        }
    }

}