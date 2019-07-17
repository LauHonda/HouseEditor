using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Wall paint is a bit different from regular objects because it
 * doesn't really put anything on the scene, it just replaces the material
 * of the walls instead.
 */
public class WallPaint : PlaceableObject {
  Material paintMaterial;

  void Start() {
    //We store the material locally to avoid calling GetComponent Multiple times
    //when painting
    Renderer renderer = transform.GetComponent<Renderer>();
    if (renderer != null) {
      paintMaterial = renderer.material;
      paintMaterial.renderQueue = 3000;
    } else {
      Debug.Log("Warning! Wall paint " + name + " is missing a renderer!");
    }
    Init();
    supportsMultiPlacement = true;
  }
  
  public override bool IsValidLocation(Vector3 start, Vector3 end) {
    bool isValid = true;

    if (start == end) {
      isValid = IsValidBlock(BlockManager.GetWallBlock(this, end));
    } else {
      //No area painting allowed, only single blocks and straight lines
      if (start.x != end.x && start.z != end.z) {
        isValid = false;
      } else {
        Vector3 current = start;

        if (start.x == end.x) {
          int increment = start.z < end.z ? 1 : -1;

          while (current.z != end.z) {
            if (!IsValidBlock(BlockManager.GetWallBlock(this, end))) {
              isValid = false; 
              break;
            }
            current.z += increment;
          }
        } else {
          int increment = start.x < end.x ? 1 : -1;

          while (current.x != end.x) {
            if (!IsValidBlock(BlockManager.GetWallBlock(this, end))) {
              isValid = false; 
              break;
            }
            current.x += increment;
          }
        }
      }
    }

    return isValid;
  }

  private bool IsValidBlock(Block block) {
    return (block != null && block.wall != null && block.wall.allowPainting);
  }

  private GameObject Paint(Vector3 position) {
    GameObject paintedItem = null;
    Block block = BlockManager.GetWallBlock(this,position);
    if (block.wall != null) {
      Renderer wallRenderer = block.wall.GetComponent<Renderer>();
      Material[] wallMaterials = wallRenderer.materials;
      if (wallMaterials.Length == 1) {
        wallMaterials[0] = paintMaterial;
      } else {
        //If the angles match then we are trying to paint the inside of a wall, otherwise it's the outside
        if (transform.rotation.eulerAngles.y == block.wall.transform.rotation.eulerAngles.y) {
          wallMaterials[0] = paintMaterial;
        } else {
          wallMaterials[1] = paintMaterial;
        }
      }
      wallRenderer.materials = wallMaterials;
      paintedItem = block.wall.gameObject;
    }

    return paintedItem;
  }

  /* Paint room is a separate method because we need to do it as a 
   * coroutine to prevent lag and hiccups if the room has too many walls
   */
  public IEnumerator PaintRoom(Vector3 wallPosition) {
    yield return null;
  }


  public override List<GameObject> Place(Vector3 start, Vector3 end, bool modifier = false) {
    List<GameObject> paintedItems = new List<GameObject>();
    if (IsValidLocation(start, end)) {
      if (start == end) {
        if (modifier) {
        } else {
          Paint(end);
        }
      } else {
        Vector3 current = start;

        if (start.x == end.x) {
          if (start.z < end.z) {
            end.z++;
          } else {
            end.z--;
          }

          int increment = start.z < end.z ? 1 : -1;

          while (current.z != end.z) {
            Paint(current);
            current.z += increment;
          }
        } else {
          if (start.x < end.x) {
            end.x++;
          } else {
            end.x--;
          }
          int increment = start.x < end.x ? 1 : -1;

          while (current.x != end.x) {
            Paint(current);
            current.x += increment;
          }
        }
      }
    }

    return paintedItems;
  }

  public override void PickUp() {
    //We can't pick up paint because we are not placing an object in the scene
    //It wouldn't make any sense to move paint around anyways
    return;
  }

  public override void Select() {
    BuilderController.instance.ToggleVisibility(false, true);
  }

}