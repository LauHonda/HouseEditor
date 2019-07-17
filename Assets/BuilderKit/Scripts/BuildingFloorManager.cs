using System;
using UnityEngine;

public class BuildingFloorManager {
  public static BuildingFloorManager instance;

  public static int activeFloor = 0;
  static GameObject[] buildingFloors;
  static GameObject tempFloor; //This is used when moving existing objects

  public BuildingFloorManager() {
    AddFloors(BuilderKitConfig.NUMBER_OF_FLOORS);
    tempFloor = new GameObject("TempFloor");
    tempFloor.transform.position = new Vector3(0, -1 * BuilderKitConfig.FLOOR_HEIGHT, 0);

    instance = this;
  }

  public void Reset(int numberOfFloors) {
    for (int i = 0; i < buildingFloors.Length; i++) {
      GameObject buildingFloor = GameObject.Find("BuildingFloor"+i);
      if(buildingFloor!=null) {
        buildingFloor.name += "_old"; //workaround to prevent unity from grabbing old objects when using Find
        GameObject.Destroy(buildingFloor);
      }
    }
    
    AddFloors(numberOfFloors);
    activeFloor = 0;
  }

  void AddFloors(int numberOfFloors) {
    buildingFloors = new GameObject[numberOfFloors];

    for (int i = 0; i < numberOfFloors; i++) {
      GameObject buildingFloor = new GameObject("BuildingFloor"+i);
      //This position setting is just to make it easier to inspect when developing, but not really necessary
      buildingFloor.transform.position = new Vector3(0, i * BuilderKitConfig.FLOOR_HEIGHT, 0);
      buildingFloors[i] = buildingFloor;
    }
  }

  public static void GoDown() {
    buildingFloors[activeFloor].SetActive(false);
    activeFloor--;
  }

  public static void GoUp() {
    activeFloor++;
    buildingFloors[activeFloor].SetActive(true);
  }

  public static void AssignToCurrentFloor(GameObject placeable) {
    placeable.transform.SetParent(buildingFloors[activeFloor].transform, true);
  }

  public static void AssignToTempFloor(GameObject placeable) {
    placeable.transform.SetParent(tempFloor.transform, true);
  }
}


