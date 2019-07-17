using UnityEngine;
using System.Collections.Generic;


/*
 * This covers most items. Chairs, tables, lamps, trees, bushes, etc.
 */
public class CommonObject : PlaceableObject {

  public override bool IsValidLocation(Vector3 start, Vector3 end) {
    Vector3 adjustedBase = new Vector3(Mathf.Ceil(end.x), end.y, Mathf.Ceil(end.z));
    
    foreach (Vector3 offset in blockOffsets) {  
      Vector3 position = adjustedBase + offset;
      Block block = BlockManager.GetBlock(position);
      
      bool isWallOffset = Mathf.Approximately(Mathf.Abs(offset.x),0.5f) || Mathf.Approximately(Mathf.Abs(offset.z),0.5f);
      bool noBase = !isWallOffset && (BuildingFloorManager.activeFloor != 0 && !BlockManager.HasTile(position));
      if (!BlockManager.IsBlockPositionValid(position)
          || (block != null && (block.blockObject != null || block.wall != null))
          || noBase) {
        return false;
      }
    }
    return true;
  }

  private GameObject PutObject(Vector3 position) {
    GameObject placedObject = GameObject.Instantiate(transform.gameObject);
    placedObject.transform.position = position;
    BuildingFloorManager.AssignToCurrentFloor(placedObject);
    BlockManager.PutObject(placedObject.GetComponent<CommonObject>(), position);
    return placedObject;
  }

  public override void PickUp() {
    Vector3 adjustedBase = new Vector3(Mathf.Ceil(transform.position.x), 0, Mathf.Ceil(transform.position.z));
    int floorBlocksLimit = floorCount > 1 ? blockOffsets.Count / floorCount : blockOffsets.Count;

    for(int i = 0; i<blockOffsets.Count; i++) {
      Block block = BlockManager.GetBlock(blockOffsets[i] + adjustedBase);
      if (block != null) {
        block.blockObject = null;
        if (i >= floorBlocksLimit) {
          block.floorTile = null;
        }
      }
    }
    CalculateBlockOffsets();
    BuildingFloorManager.AssignToTempFloor(transform.gameObject);
  }

  public override List<GameObject> Place(Vector3 start, Vector3 end, bool modifier = false) {
    List<GameObject> placed = new List<GameObject>();
    if (IsValidLocation(start, end)) {
      //Objects must be placed one item at a time, so we just use the last position
      placed.Add(PutObject(end));
    }

    return placed;
  }

  public override void Select() {
  }

}
