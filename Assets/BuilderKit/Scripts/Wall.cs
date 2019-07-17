using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 * Used for tall walls, fences, and anything that should behave like a wall (e.g. a thin bush fence)
 */
public class Wall : PlaceableObject {
  public bool allowObjectPlacement = true;
  public bool allowPainting = true;

  [HideInInspector]
  public bool isFullHeight;

  void Start() {
    gameObject.layer = LayerMask.NameToLayer("Walls");
    Init();
    isFullHeight = transform.GetComponent<Renderer>().bounds.size.y == BuilderKitConfig.FLOOR_HEIGHT;
    supportsMultiPlacement = true;
  }

  bool IsBlockTiled(Vector3 position) {
    Block nextBlock = BlockManager.GetBlock(position);
    return nextBlock != null && nextBlock.floorTile != null;
  }

  /* If we are not at the ground floor we need to check for walls 
   * right below or floor tiles next to the position. This helps us sort that out
   */
  bool IsFloating(Vector3 position) {
    bool isFloating = false;
    if (BuildingFloorManager.activeFloor != 0) {
      float belowYPosition = (BuildingFloorManager.activeFloor - 1) * BuilderKitConfig.FLOOR_HEIGHT;
      Block belowWall = BlockManager.GetWallBlock(this, new Vector3(position.x, belowYPosition, position.z));
      if (belowWall == null) {
        //If there's no wall below, we beed to check if there's floor tiles next to the position
        //We can reduce the number of checks by determining the Y rotation of the wall.
        if (this.transform.rotation.eulerAngles.y != 0) {
          isFloating = !IsBlockTiled(new Vector3(position.x, position.y, position.z - 1)) &&
            !IsBlockTiled(new Vector3(position.x, position.y, position.z + 1));
        } else {
          isFloating = !IsBlockTiled(new Vector3(position.x - 1, position.y, position.z)) &&
            !IsBlockTiled(new Vector3(position.x + 1, position.y, position.z));
        }
      } else {
        //If there's a wall right below, then we can safely build here
        isFloating = false;
      }
    }
    return isFloating;
  }

  public override bool IsValidLocation(Vector3 start, Vector3 end) {
    bool isValid = true;

    if (start == end) {
      Block block = BlockManager.GetWallBlock(this, end);
      isValid = BlockManager.IsBlockPositionValid(start) &&
      (block == null || (block.wall == null && block.blockObject == null)) &&
      !IsFloating(end);
    } else {
      //If it's not a straight line then we can't place this
      if (start.x != end.x && start.z != end.z) {
        isValid = false;
      } else {
        if (start.x == end.x) {
          int direction = start.z < end.z ? 1 : -1;
          int i = (int)(start.z - direction);
          while (i != end.z) {
            i += direction;
            Vector3 testPosition = new Vector3(start.x, start.y, i);
            Block block = BlockManager.GetWallBlock(this, testPosition);
            if (block != null && (block.wall != null || block.blockObject != null) || IsFloating(testPosition)) {
              isValid = false;
              break;
            }
          }
        } else {
          int direction = start.x < end.x ? 1 : -1;
          int i = (int)(start.x - direction);
          while (i != end.x) {
            i += direction;
            Vector3 testPosition = new Vector3(i, start.y, start.z);
            Block block = BlockManager.GetWallBlock(this, testPosition);
            if (block != null && (block.wall != null || block.blockObject != null) || IsFloating(testPosition)) {
              isValid = false;
              break;
            }
          }
        }

        isValid = isValid && BlockManager.IsAreaValid(start, end);
      }
    }

    return isValid;
  }

  private GameObject PutWall(Vector3 position) {
    GameObject placedObject = GameObject.Instantiate(transform.gameObject);
    placedObject.transform.position = position;
    BuildingFloorManager.AssignToCurrentFloor(placedObject);
    BlockManager.PutWall(placedObject.GetComponent<Wall>(), position);

    return placedObject;
  }

  public override void PickUp() {
    Block block = BlockManager.GetWallBlock(this, transform.position);
    if(block!=null) {
      block.wall = null;
    }
    
    BuildingFloorManager.AssignToTempFloor(transform.gameObject);
  }

  public override List<GameObject> Place(Vector3 start, Vector3 end, bool modifier = false) {
    List<GameObject> placedWalls = new List<GameObject>();
    if (IsValidLocation(start, end)) {
      //If start and end are the same, then we are putting a wall in a single location
      if (start == end) {
        placedWalls.Add(PutWall(end));
      } else {
        //We need to make sure walls are being built in straight lines
        //So either x or z have to be equal on both coordinates
        if (start.z == end.z) {
          if (start.x < end.x) {
            end.x++;
          } else {
            end.x--;
          }

          Vector3 current = start;
          int increment = start.x < end.x ? 1 : -1;
          while (current.x != end.x) {
            placedWalls.Add(PutWall(current));
            current.x += increment;
          }
        } else {
          if (start.x == end.x) {
            if (start.z < end.z) {
              end.z++;
            } else {
              end.z--;
            }

            Vector3 current = start;
            int increment = start.z < end.z ? 1 : -1;
            while (current.z != end.z) {
              placedWalls.Add(PutWall(current));
              current.z += increment;
            }
          }
        }
      }
    }
    return placedWalls;
  }

  public override void Select() {
    BuilderController.instance.ToggleVisibility(false, true);
  }

  //Wall objects only have two different rotations 0 and 90 degrees.
  public override void Rotate() {
    Vector3 rotation = transform.rotation.eulerAngles;
    rotation.y = (rotation.y == 0f) ? 90f : 0f;
    transform.rotation = Quaternion.Euler(rotation);
    CalculateBlockOffsets();
  }

}