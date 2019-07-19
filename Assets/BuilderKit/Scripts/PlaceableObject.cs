using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlaceableObject : MonoBehaviour {
  public string objectName;

  [HideInInspector]
  public MeshRenderer mesh;

  [HideInInspector]
  public Vector3 positionDeltas;

  [HideInInspector]
  public List<Vector3> blockOffsets;

  //This is
  [HideInInspector]
  public List<Vector3> multiLevelBlockOffsets;

  //Some objects (like stairs) can take more than one floor
  [HideInInspector]
  public int floorCount;

  //Indicated if the object can be placed in a line/area by holding the mouse button down
  public bool supportsMultiPlacement = false;

  public float yPosition = 0.02f;


    [SerializeField]
    [Tooltip("When snapping is enabled, this value defines a position offset that is added to the center of the object when dragging. Note that this value is added on top of the snapOffset defined in the MobilePickingController. When a top-down camera is used, these 2 values are applied to the X/Z position.")]
    private Vector2 localSnapOffset = Vector2.zero;
 

    public Vector2 LocalSnapOffset { get { return (localSnapOffset); } }


    void Start() {
    Init();
  }

  //This method sets a couple of initial values that shouldn't be overriden if a child implements it's own Start() method
  public void Init() {
    mesh = GetComponent<MeshRenderer>();

    if (mesh != null) {
      CalculateDeltas();
      CalculateFloorCount();
      CalculateBlockOffsets();
    }
    
    this.gameObject.name = this.gameObject.name.Replace("(Clone)","");
  }

  private void CalculateFloorCount() {
    floorCount = Mathf.RoundToInt(mesh.bounds.size.y / BuilderKitConfig.FLOOR_HEIGHT);
    if (BuilderKitConfig.FLOOR_HEIGHT / floorCount < mesh.bounds.size.y) {
      floorCount++;
    }
  }

  /*
   * We use the deltas to adjust the placing position on objects that are bigger than 1 block
   */
  private void CalculateDeltas() {
    int x = Mathf.RoundToInt(mesh.bounds.size.x);
    int z = Mathf.RoundToInt(mesh.bounds.size.z);

    float xDelta = x > 0 && x % 2 == 0 ? -0.5f : 0f;
    float zDelta = z > 0 && z % 2 == 0 ? -0.5f : 0f;

    positionDeltas = new Vector3(xDelta, 0, zDelta);
  }

  public void CalculateBlockOffsets() {
    float tolerance = 0.15f;
    int x = Mathf.CeilToInt(mesh.bounds.size.x - tolerance);
    int z = Mathf.CeilToInt(mesh.bounds.size.z - tolerance);
    
    blockOffsets = new List<Vector3>();

    if (x <= 1 && z <= 1) {
      blockOffsets.Add(new Vector3(0, 0, 0));
    } else {
      float i = 0;
      int xMiddle = Mathf.CeilToInt(x / 2);
      int zMiddle = Mathf.CeilToInt(z / 2);

      float xLimit = x - 0.5f;
      float zLimit = z - 0.5f;
      
      while (i < xLimit) {
        float j = 0;
        while (j < zLimit) {
          if (i == Mathf.Round(i) || j == Mathf.Round(j)) {
            Vector3 offset = new Vector3(i - xMiddle, 0, j - zMiddle);
            blockOffsets.Add(offset);
          }
          j += 0.5f;
        }
        i += 0.5f;
      }

      if (floorCount > 1) {
        List<Vector3> additionalOffsets = new List<Vector3>();

        foreach (Vector3 blockOffset in blockOffsets) {
          for (int n = 1; n < floorCount; n++) {
            additionalOffsets.Add(new Vector3(blockOffset.x,n*BuilderKitConfig.FLOOR_HEIGHT,blockOffset.z));
          }
        }

        blockOffsets.AddRange(additionalOffsets);
      }
    }
  }

    
  /* 
   * Implemented in child classes
   * Ensures the object can be placed in the current location
   */
  public abstract bool IsValidLocation(Vector3 start, Vector3 end);

  /*
   * Implemented in child classes
   * The placing of items changes according to their types
   * The buildingFloor argument makes the placed object the child of the buildingFloor passed,
   * if null, the object will always be visible.
   * The modifier flag is used for things like room painting by holding down shift
   */
  public abstract List<GameObject> Place(Vector3 start, Vector3 end, bool modifier = false);

  /*
   * Implemented in child classes
   * Used when moving an object to a different location
   */
  public abstract void PickUp();

  /*
   * Called when selecting an item. Used to hook
   * things like changing the wall visibility depending on the selected item
   */
  public abstract void Select();

  /*
   * Adds 90 degrees to whatever the current rotation is, this results on 0 ; 90 ; 180 ; (270 or -90)
   * due to the way Unity handles rotations
   */
  public virtual void Rotate() {
    Vector3 rotation = transform.rotation.eulerAngles;
    rotation.y = ((int)rotation.y + 90) % 360;
    transform.rotation = Quaternion.Euler(rotation);
    CalculateBlockOffsets();
  }


}
