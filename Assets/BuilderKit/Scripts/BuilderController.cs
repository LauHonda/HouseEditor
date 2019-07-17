using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuilderController : MonoBehaviour
{
    public static BuilderController instance;
    public Material validSelectionAreaMaterial;
    public Material invalidSelectionAreaMaterial;

    PlaceableObject selectedObject;

    public bool placingEnabled = true;

    public bool modalOpen = false;



    bool isMovingExistingObject = false;
    GameObject selectionArea;
    Renderer selectionAreaRenderer;

    Vector3 previousBlockPosition;
    Vector3 blockPosition;
    Vector3 lastMousePosition;

    Vector3 buttonDownStartPosition;

    int floorLayerMask;
    int roofLayerMask;
    int wallsLayerMask;
    int defaultLayerMask;

    FloorGrid grid;
    GameObject floor;

    void Start()
    {
        new BlockManager();
        new BuildingFloorManager();
        InitializeLayers();
        InitializeFloor();
        InitializeSelectionArea();
        InitializeGrid();

        defaultLayerMask = Camera.main.cullingMask;

        placingEnabled = false;

        instance = this;
    }

    public void Reset(int numberOfFloors)
    {
        floor.transform.position = new Vector3(0, 0, 0);
        grid.transform.position = new Vector3(0, 0, 0);
        BuildingFloorManager.instance.Reset(numberOfFloors);
        BlockManager.Reset();
    }

    void InitializeLayers()
    {
        floorLayerMask = LayerMask.NameToLayer("Floor");
        roofLayerMask = LayerMask.NameToLayer("Roof");
        wallsLayerMask = LayerMask.NameToLayer("Walls");

        //If the layer was not defined (or overriden), we need to manually create it
        if (floorLayerMask == -1 || roofLayerMask == -1 || wallsLayerMask == -1)
        {
            Debug.LogError("Error: A layer is not defined. please make sure Floor, Roof, and Walls are layers");
        }
        else
        {
            floorLayerMask = 1 << floorLayerMask;
            roofLayerMask = 1 << roofLayerMask;
            wallsLayerMask = 1 << wallsLayerMask;
        }
    }

    /*
     * Initializes de floor collider object
     * Floor is a game object that contains a collider and we use it 
     * to check for collisions when placing objects.
     * It moves up and down depending on which floor is active.
     */
    void InitializeFloor()
    {
        floor = GameObject.Find("Floor");
        if (floor == null)
        {
            floor = GameObject.Instantiate(Resources.Load("Prefabs/Misc/Floor") as GameObject);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, 0, 0);
        }
    }

    void InitializeSelectionArea()
    {
        selectionArea = GameObject.Instantiate(Resources.Load("Prefabs/Misc/SelectionArea") as GameObject);
        selectionArea.name = "SelectionArea";
        selectionAreaRenderer = selectionArea.GetComponent<Renderer>();
        selectionArea.SetActive(false);
    }

    void InitializeGrid()
    {
        GameObject gridObject = GameObject.Find("Grid");
        if (grid == null)
        {
            gridObject = GameObject.Instantiate(Resources.Load("Prefabs/Misc/Grid") as GameObject);
            gridObject.name = "Grid";
            gridObject.transform.position = new Vector3(0, 0, 0);
        }

        grid = gridObject.GetComponent<FloorGrid>();
        if (grid == null)
        {
            Debug.LogError("Error: FloorGrid component missing from the Grid game object, please add it");
        }
    }

    public void ToggleVisibility(bool isRoofVisible, bool areWallsVisible)
    {
        int mask = defaultLayerMask;

        if (!isRoofVisible)
        {
            mask = mask & ~roofLayerMask;
        }

        if (!areWallsVisible)
        {
            mask = mask & ~wallsLayerMask;
        }

        Camera.main.cullingMask = mask;
    }

    //Go up one floor, does nothing if on top floor. NUMBER_OF_FLOORS in BuilderKitConfig determines this.
    public void GoUp()
    {
        if (BuildingFloorManager.activeFloor < BuilderKitConfig.NUMBER_OF_FLOORS - 1)
        {
            BuildingFloorManager.GoUp();
            grid.MoveToFloor(BuildingFloorManager.activeFloor);
            floor.transform.position = new Vector3(0, BuildingFloorManager.activeFloor * BuilderKitConfig.FLOOR_HEIGHT, 0);
        }
    }

    //Go down one floor, does nothing if at ground level
    public void GoDown()
    {
        if (BuildingFloorManager.activeFloor > 0)
        {
            BuildingFloorManager.GoDown();
            grid.MoveToFloor(BuildingFloorManager.activeFloor);
            floor.transform.position = new Vector3(0, BuildingFloorManager.activeFloor * BuilderKitConfig.FLOOR_HEIGHT, 0);
        }
    }

    public void EnablePlacing()
    {
        placingEnabled = true;

        if (selectedObject != null)
        {
            selectedObject.gameObject.SetActive(true);
            Cursor.visible = false;
        }
    }

    void UpdateBlockPosition(bool forceUpdate = false)
    {
        //We only need to update the block position if the mouse moved
        if (placingEnabled && (Input.mousePosition != lastMousePosition || forceUpdate))
        {
            lastMousePosition = Input.mousePosition;
            previousBlockPosition = blockPosition;
            Ray ray = Camera.main.ScreenPointToRay(lastMousePosition);
            RaycastHit hit;
            //Using the floor layer helps us remove any wonkiness that may be caused by
            //the ray hitting objects and messing up the position
            if (Physics.Raycast(ray, out hit, 100f, floorLayerMask))
            {
                if (selectedObject.transform.rotation.eulerAngles.y == 0 || selectedObject.transform.rotation.eulerAngles.y == 180)
                {
                    blockPosition.x = Mathf.Floor(hit.point.x) + selectedObject.positionDeltas.x;
                    blockPosition.z = Mathf.Floor(hit.point.z) + selectedObject.positionDeltas.z;
                }
                else
                {
                    blockPosition.x = Mathf.Floor(hit.point.x) + selectedObject.positionDeltas.z;
                    blockPosition.z = Mathf.Floor(hit.point.z) + selectedObject.positionDeltas.x;
                }
                blockPosition.y = floor.transform.position.y;
            }
        }
    }

    public void SelectObject(GameObject selection)
    {
        if (!isMovingExistingObject)
        {
            if (selectedObject != null)
            {
                GameObject.Destroy(selectedObject);
            }

            GameObject selected = GameObject.Instantiate(selection);
            selectedObject = selected.GetComponent<PlaceableObject>();
            selectedObject.Select();
            SoundController.instance.PlayClip(SoundController.instance.itemSelected);
            grid.Enable();

            if (!placingEnabled)
            {
                selected.SetActive(false);
            }
        }
    }

    void GrabObject()
    {
        if (modalOpen)
        {
            return;
        }
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            GameObject selected = hit.transform.gameObject;
            selectedObject = selected.GetComponent<PlaceableObject>();

            //If we couldn't get the component it means it's not a placeable object
            //so we unset everything
            if (selectedObject == null)
            {
                selectedObject = null;
                Debug.Log("Nothing to grab");
            }
            else
            {
                Debug.Log("Grabbing " + selectedObject.name);
                selectedObject.PickUp();
                selectedObject.Select();
                SoundController.instance.PlayClip(SoundController.instance.itemSelected);
                Cursor.visible = false;
                isMovingExistingObject = true;
                grid.Enable();
            }
        }
    }

    public void HideSelectedObject()
    {
        if (selectedObject != null)
        {
            selectedObject.gameObject.SetActive(false);
            selectionArea.SetActive(false);
        }
    }

    public void DeselectObject()
    {
        if (selectedObject != null)
        {
            GameObject.Destroy(selectedObject.gameObject);
            selectedObject = null;
        }
        Cursor.visible = true;
        isMovingExistingObject = false;
        selectionArea.SetActive(false);
        grid.Disable();
    }

    void UpdateSelectionArea(bool forceUpdate = false)
    {
        if (selectedObject != null && selectedObject.mesh != null)
        {
            if (previousBlockPosition != blockPosition || forceUpdate)
            {

                //Making the selection area size slightly larger than the object
                float objectSizeX = selectedObject.mesh.bounds.size.x + 0.2f;
                float objectSizeZ = selectedObject.mesh.bounds.size.z + 0.2f;

                if (!selectionArea.activeSelf)
                {
                    selectionArea.SetActive(true);
                }

                bool isValidArea = false;
                if (Input.GetMouseButton(0) && selectedObject.supportsMultiPlacement)
                {
                    Vector3 newPosition = (buttonDownStartPosition + blockPosition) / 2;
                    //We make the selection float slightly above the floor tiles to avoid visual artifacts
                    newPosition.y = floor.transform.position.y + 0.04f;
                    selectionArea.transform.position = newPosition;
                    selectionArea.transform.localScale = new Vector3(
                      Mathf.Abs(buttonDownStartPosition.x - selectedObject.mesh.bounds.center.x) + selectedObject.transform.localScale.x,
                      1,
                      Mathf.Abs(buttonDownStartPosition.z - selectedObject.mesh.bounds.center.z) + selectedObject.transform.localScale.z
                    );
                    isValidArea = selectedObject.IsValidLocation(buttonDownStartPosition, blockPosition);
                }
                else
                {
                    Vector3 newPosition = selectedObject.mesh.bounds.center;
                    newPosition.y = floor.transform.position.y + 0.04f;
                    selectionArea.transform.position = newPosition;
                    selectionArea.transform.localScale = new Vector3(objectSizeX, 1, objectSizeZ);
                    isValidArea = selectedObject.IsValidLocation(blockPosition, blockPosition);
                }

                selectionAreaRenderer.material = isValidArea ? validSelectionAreaMaterial : invalidSelectionAreaMaterial;
            }
        }
    }

    void Update()
    {
        if (modalOpen)
        {
            return;
        }
        if (Input.GetKeyUp(KeyCode.U))
        {
            GoUp();
        }

        if (Input.GetKeyUp(KeyCode.J))
        {
            GoDown();
        }

        if (placingEnabled)
        {
            if (selectedObject != null)
            {
                UpdateBlockPosition();
                selectedObject.transform.position = blockPosition;
                if (Input.GetMouseButtonDown(0))
                {
                    buttonDownStartPosition = blockPosition;
                }

                if (Input.GetMouseButtonUp(0))
                {
                    if (selectedObject.IsValidLocation(buttonDownStartPosition, blockPosition))
                    {
                        SoundController.instance.PlayClip(SoundController.instance.itemPlaced);
                        selectedObject.Place(buttonDownStartPosition, blockPosition);

                        if (isMovingExistingObject)
                        {
                            isMovingExistingObject = false;
                            DeselectObject();
                        }
                    }
                    else
                    {
                        SoundController.instance.PlayClip(SoundController.instance.invalidLocation);
                    }
                    UpdateSelectionArea(true);
                }
                else
                {
                    UpdateSelectionArea();
                }

                if (Input.GetMouseButtonUp(1))
                {
                    selectedObject.Rotate();
                    UpdateBlockPosition(true); //When rotating we may have to adjust positions a bit
                    selectedObject.transform.position = blockPosition;
                    UpdateSelectionArea(true);
                }

                if (Input.GetKeyUp(KeyCode.Escape))
                {
                    DeselectObject();
                }
            }
            else
            {
                //If we have no object selected but there's a click, we may want to grab something.
                if (Input.GetMouseButtonUp(0))
                {
                    GrabObject();
                }
            }
        }

    }
}
