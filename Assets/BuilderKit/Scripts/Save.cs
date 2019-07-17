using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Save {
    public string name;
    public DateTime date;
    public List<FloorSave> floors;

    public Save(string name) {
        this.name = name;
        date = new DateTime();

        floors = new List<FloorSave>();

        int floorIndex = 0;
        GameObject buildingFloor = GameObject.Find("BuildingFloor"+floorIndex);

        while(buildingFloor!=null) {
            FloorSave floorSave = new FloorSave(floorIndex,buildingFloor);
            floors.Add(floorSave);
            floorIndex++;
            buildingFloor = GameObject.Find("BuildingFloor"+floorIndex);
        }
    }

    public void Load() {
        BuilderController.instance.Reset(floors.Count);
        foreach(FloorSave floor in floors) {
            floor.Load();
        }
        BuildingFloorManager.activeFloor = 0;
    }

}