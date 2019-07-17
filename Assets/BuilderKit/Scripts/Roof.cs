using UnityEngine;
using System.Collections.Generic;

/*
 * Roofs behave in a particular way, you select entire areas and the 
 * roof tile expands to fit it, adjusting it's height accordingly
 * To allow more variety, multiple roof tiles can share the same
 * space too
 */ 
public class Roof : PlaceableObject {
  //This tells the roof tile how high it can scale on the y axis
  [Range(0,10)]
  public float maxElevation = 4;

  void Start() {
    gameObject.layer = LayerMask.NameToLayer("Roof");
    Init();
    positionDeltas = new Vector3(-0.5f,0,-0.5f);
    supportsMultiPlacement = true;
  }

  public override bool IsValidLocation(Vector3 start, Vector3 end) {
    //A roof can be placed anywhere for now.
    return true;
  }

  public override void PickUp() {
    return;
  }

  public override List<GameObject> Place(Vector3 start, Vector3 end, bool modifier = false) {
    List<GameObject> placedRoofs = new List<GameObject>();
    if (IsValidLocation(start,end)) {
      Vector3 middle = (end + start) / 2;
      float xSize = Mathf.Abs(end.x - start.x)+1;
      float zSize = Mathf.Abs(end.z - start.z)+1;
      float ySize = Mathf.Min(zSize, xSize) * 0.3f;
      if (ySize > maxElevation) {
        ySize = maxElevation;
      } else {
        if (ySize < 1) {
          ySize = 1;
        }
      }
        
      middle.y += ySize * 0.2f;

      GameObject newRoof = GameObject.Instantiate(this.gameObject);
      newRoof.transform.position = middle;

      if (transform.rotation.eulerAngles.y == 0 || transform.rotation.eulerAngles.y == 180) {
        newRoof.transform.localScale = new Vector3(xSize, ySize, zSize);
      } else {
        newRoof.transform.localScale = new Vector3(zSize, ySize, xSize);
      }
      placedRoofs.Add(newRoof);
    }

    return placedRoofs;
  }

  public override void Select() {
    BuilderController.instance.ToggleVisibility(true, true);
  }
}
