using UnityEngine;
using System.Collections.Generic;

public class WallObject : PlaceableObject {
  public Mesh wallMesh;
  public Mesh originalWallMesh;

  void Start() {
    Init();
    LoadWallMesh();
  }

  public void LoadWallMesh() {
    if(wallMesh==null) {
      //If there's a wall as a child of the object, we take the mesh data
      //and then hide it. We will use this mesh to alter the walls we are
      //putting the object on.
      for (int i = 0; i < transform.childCount; i++) {
        if (transform.GetChild(i).name.Contains("Wall")) {
          wallMesh = transform.GetChild(i).GetComponent<MeshFilter>().mesh;
          transform.GetChild(i).gameObject.SetActive(false);
          break;
        }
      }
    }
  }

  public override bool IsValidLocation(Vector3 start, Vector3 end) {
    Block block = BlockManager.GetWallBlock(this, end);
    return block != null && block.wall != null && block.wall.gameObject.transform.childCount == 0;
  }

  private GameObject PutWallObject(Vector3 position) {
    GameObject placedObject = GameObject.Instantiate(transform.gameObject);
    WallObject wallObject = placedObject.GetComponent<WallObject>();
    placedObject.transform.position = position;
    BuildingFloorManager.AssignToCurrentFloor(placedObject);
    Block block = BlockManager.PutWallObject(wallObject,position);
    wallObject.LoadWallMesh(); //We need this because when loading from a save file Start is not called
    if (wallObject.wallMesh != null) {
      wallObject.originalWallMesh = block.wall.GetComponent<MeshFilter>().mesh;
      block.wall.GetComponent<MeshFilter>().mesh = wallObject.wallMesh;
    }
    return placedObject;
  }

  public override void PickUp() {
    Block block = BlockManager.GetWallBlock(this, this.transform.position);
    block.wall.transform.DetachChildren();

    if (originalWallMesh != null) {
      block.wall.GetComponent<MeshFilter>().mesh = originalWallMesh;
    }

    BuildingFloorManager.AssignToTempFloor(transform.gameObject);
    return;
  }

  public override List<GameObject> Place(Vector3 start, Vector3 end, bool modifier = false) {
    List<GameObject> ret = new List<GameObject>();
    if (IsValidLocation(start, end)) {
      //Wall objects must be placed one item at a time, so we just use the last position
      ret.Add(PutWallObject(end));
    }
    return ret;
  }

  public override void Select() {
    BuilderController.instance.ToggleVisibility(false, true);
  }

}
