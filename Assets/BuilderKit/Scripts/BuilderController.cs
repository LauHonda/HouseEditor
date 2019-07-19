using BitBenderGames;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BuilderController : MonoBehaviour
{
    public enum SelectionAction
    {
        Select,
        Deselect,
    }
    public static BuilderController instance;
    public Material validSelectionAreaMaterial;
    public Material invalidSelectionAreaMaterial;

    PlaceableObject selectedObject;

    public bool placingEnabled = true;

    public bool modalOpen = false;

    private TouchInputController touchInputController;
    [SerializeField]
    private MobileTouchCamera mobileTouchCam;

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


    private void Awake()
    {
        mobileTouchCam = FindObjectOfType<MobileTouchCamera>();
        if (mobileTouchCam == null)
        {
            Debug.LogError("No MobileTouchCamera found in scene. This script will not work without this.");
        }
        touchInputController = mobileTouchCam.GetComponent<TouchInputController>();
        if (touchInputController == null)
        {
            Debug.LogError("No TouchInputController found in scene. Make sure this component exists and is attached to the MobileTouchCamera gameObject.");
        }
        instance = this;

        new BlockManager();
        new BuildingFloorManager();
        InitializeLayers();
        InitializeFloor();
        InitializeSelectionArea();
        InitializeGrid();

        defaultLayerMask = Camera.main.cullingMask;

        placingEnabled = false;
    }  

    void Start()
    {
        

        

        touchInputController.OnInputClick += InputControllerOnInputClick;
        touchInputController.OnFingerDown += InputControllerOnFingerDown;
        touchInputController.OnFingerUp += InputControllerOnFingerUp;
        touchInputController.OnDragStart += InputControllerOnDragStart;
        touchInputController.OnDragUpdate += InputControllerOnDragUpdate;
        touchInputController.OnDragStop += InputControllerOnDragStop;
    }

    public void OnDestroy()
    {
        touchInputController.OnInputClick -= InputControllerOnInputClick;
        touchInputController.OnFingerDown -= InputControllerOnFingerDown;
        touchInputController.OnFingerUp -= InputControllerOnFingerUp;
        touchInputController.OnDragStart -= InputControllerOnDragStart;
        touchInputController.OnDragUpdate -= InputControllerOnDragUpdate;
        touchInputController.OnDragStop -= InputControllerOnDragStop;
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


    public GameObject get_selectedObject()
    {
        if (selectedObject != null)
            return selectedObject.gameObject;
        else
            return null;
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
        //if (modalOpen)
        //{
        //    return;
        //}
        //if (Input.GetKeyUp(KeyCode.U))
        //{
        //    GoUp();
        //}

        //if (Input.GetKeyUp(KeyCode.J))
        //{
        //    GoDown();
        //}

        //Debug.Log(selectedObject);

        //if (placingEnabled)
        //{
        //    if (selectedObject != null)
        //    {
        //        UpdateBlockPosition();
        //        Debug.Log(blockPosition);
        //        selectedObject.transform.position = blockPosition;
        //        if (Input.GetMouseButtonDown(0))
        //        {
        //            buttonDownStartPosition = blockPosition;
        //        }

        //        if (Input.GetMouseButtonUp(0))
        //        {
        //            if (selectedObject.IsValidLocation(buttonDownStartPosition, blockPosition))
        //            {
        //                SoundController.instance.PlayClip(SoundController.instance.itemPlaced);
        //                selectedObject.Place(buttonDownStartPosition, blockPosition);

        //                if (isMovingExistingObject)
        //                {
        //                    isMovingExistingObject = false;
        //                    DeselectObject();
        //                }
        //            }
        //            else
        //            {
        //                SoundController.instance.PlayClip(SoundController.instance.invalidLocation);
        //            }
        //            UpdateSelectionArea(true);
        //        }
        //        else
        //        {
        //            UpdateSelectionArea();
        //        }

        //        if (Input.GetMouseButtonUp(1))
        //        {
        //            selectedObject.Rotate();
        //            UpdateBlockPosition(true); //When rotating we may have to adjust positions a bit
        //            selectedObject.transform.position = blockPosition;
        //            UpdateSelectionArea(true);
        //        }

        //        if (Input.GetKeyUp(KeyCode.Escape))
        //        {
        //            //DeselectObject();
        //        }
        //    }
        //    else
        //    {
        //        //If we have no object selected but there's a click, we may want to grab something.
        //        if (Input.GetMouseButtonUp(0))
        //        {
        //            //GrabObject();
        //        }
        //    }
        //}

        if (Input.GetMouseButtonUp(1))
        {
            if (selectedObject != null)
            {
                if (selectedObject.IsValidLocation(buttonDownStartPosition, blockPosition))
                {
                    SoundController.instance.PlayClip(SoundController.instance.itemPlaced);
                    selectedObject.Place(buttonDownStartPosition, blockPosition);
                }

                UpdateSelectionArea(true);
                DeselectObject();
            }
            else
            {
                SoundController.instance.PlayClip(SoundController.instance.invalidLocation);
            }
        }
    }

    public void selectedObjMove()
    {
        if (selectedObject != null)
        {
            UpdateBlockPosition();
            selectedObject.transform.position = blockPosition;
            UpdateSelectionArea();
        }
    }
    private void InputControllerOnInputClick(Vector3 clickPosition, bool isDoubleClick, bool isLongTap)
    {
        //Vector3 intersectionPoint;
        //var newCollider = GetClosestColliderAtScreenPoint(clickPosition, out intersectionPoint);
        //SelectColliderInternal(newCollider, isDoubleClick, isLongTap);
       
    }
    private void InputControllerOnFingerDown(Vector3 fingerDownPos)
    {
        //if (requireLongTapForMove == false || isSelectedViaLongTap == true)
        //{
        //    RequestDragPickable(fingerDownPos);
        //}
        //if (selectedObject != null)
        //{

        //    mobileTouchCam.OnDragSceneObject();
        //    UpdateBlockPosition();
        //    buttonDownStartPosition = blockPosition;
        //    selectedObject.transform.position = blockPosition;
        //    UpdateSelectionArea();

        //}
    }

    private void InputControllerOnFingerUp()
    {
       // EndPickableTransformMove();
    }

    
    private void InputControllerOnDragStart(Vector3 clickPosition, bool isLongTap)
    {             
        if (isLongTap == true && touchInputController.LongTapStartsDrag == true && selectedObject == null)
        {
            Vector3 intersectionPoint;
            Component newCollider = GetClosestColliderAtScreenPoint(clickPosition, out intersectionPoint);
            if (newCollider != null)
            {
                selectedObject = newCollider.GetComponent<PlaceableObject>();
                
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
                    UpdateBlockPosition();
                    buttonDownStartPosition = blockPosition;
                    OnDragUpdate = true;
                }
            }          
        }

        else
        {
            Vector3 intersectionPoint;
            Component newCollider = GetClosestColliderAtScreenPoint(clickPosition, out intersectionPoint);
            if (newCollider != null && selectedObject != null)
            {
                if (selectedObject == newCollider.GetComponent<PlaceableObject>())
                {
                    OnDragUpdate = true;
                }
                else
                    OnDragUpdate = false;
            }
            else
                OnDragUpdate = false;
        }
    }

    private void InputControllerOnDragStop(Vector3 dragStopPos, Vector3 dragFinalMomentum)
    {
        //EndPickableTransformMove();
        if (selectedObject != null)
        {
            if (selectedObject.IsValidLocation(buttonDownStartPosition, blockPosition))
            {
                //SoundController.instance.PlayClip(SoundController.instance.itemPlaced);
                //selectedObject.Place(buttonDownStartPosition, blockPosition);

                if (isMovingExistingObject)
                {
                    isMovingExistingObject = false;
                    //DeselectObject();
                }
            }
            else
            {
               // SoundController.instance.PlayClip(SoundController.instance.invalidLocation);
            }
            //UpdateSelectionArea(true);
            //DeselectObject();
        }       
    }
    bool onDragUpdate = false;
    public bool OnDragUpdate {
        get { return onDragUpdate; }
        set
        {
            onDragUpdate = value;
            if (onDragUpdate)
            {
                Debug.Log(onDragUpdate);
                mobileTouchCam.OnDragSceneObject();
            }
        }
    }
    

    private void InputControllerOnDragUpdate(Vector3 dragPosStart, Vector3 dragPosCurrent, Vector3 correctionOffset)
    {
        if (selectedObject != null && OnDragUpdate)
        {
            UpdateBlockPosition();            
            selectedObject.transform.position = blockPosition;                     
            UpdateSelectionArea();

            //if (Input.GetMouseButtonUp(1))
            //{
            //    selectedObject.Rotate();
            //    UpdateBlockPosition(true); //When rotating we may have to adjust positions a bit
            //    selectedObject.transform.position = blockPosition;
            //    UpdateSelectionArea(true);
            //}

            //if (Input.GetKeyUp(KeyCode.Escape))
            //{
            //    //DeselectObject();
            //}
        }      
    }
    //[SerializeField]
    //[Tooltip("Here you can set up callbacks to be invoked when a pickable transform is moved to a new position.")]
    //private UnityEventWithTransform OnPickableTransformMoved;
    //[SerializeField]
    //[Tooltip("When setting this variable to true, pickables can only be moved by long tapping on them first.")]
    //private bool requireLongTapForMove = false;
    //[SerializeField]
    //[Tooltip("Here you can set up callbacks to be invoked when a pickable transform is selected.")]
    //private UnityEventWithTransform OnPickableTransformSelected;
    //[SerializeField]
    //[Tooltip("When setting this to false, the OnPickableTransformSelect event will only be sent once when clicking on the same pickable repeatedly.")]
    //private bool repeatEventSelectedOnClick = true;
    //[SerializeField]
    //[Tooltip("Here you can set up callbacks to be invoked when a pickable transform is selected through a long tap.")]
    //private UnityEventWithPickableSelected OnPickableTransformSelectedExtended;
    //[SerializeField]
    //[Tooltip("Here you can set up callbacks to be invoked when a pickable transform is deselected.")]
    //private UnityEventWithTransform OnPickableTransformDeselected;
    //[SerializeField]
    //[Tooltip("When this flag is enabled, more than one item can be selected and moved at the same time.")]
    //private bool isMultiSelectionEnabled = false;
    //[SerializeField]
    //[Tooltip("When setting this to false, pickables will not become deselected when the user clicks somewhere on the screen, except when he clicks on another pickable.")]
    //private bool deselectPreviousColliderOnClick = true;
    //[SerializeField]
    //[Tooltip("When set to Straight, picked items will be snapped to a perfectly horizontal and vertical grid in world space. Diagonal snaps the items on a 45 degree grid.")]
    //private SnapAngle snapAngle = SnapAngle.Straight_0_Degrees;
    //[SerializeField]
    //[Tooltip("When set to true, the position of dragged items snaps to discrete units.")]
    //private bool snapToGrid = true;
    //[SerializeField]
    //[Tooltip("When snapping is enabled, this value defines a position offset that is added to the center of the object when dragging. When a top-down camera is used, these 2 values are applied to the X/Z position.")]
    //private Vector2 snapOffset = Vector2.zero;
    //[SerializeField]
    //[Tooltip("Size of the snap units when snapToGrid is enabled.")]
    //private float snapUnitSize = 1;
    //[SerializeField]
    //[Tooltip("Here you can set up callbacks to be invoked when the moving of a pickable transform is ended. The event requires 2 parameters. The first is the start position of the drag. The second is the dragged transform. The start position can be used to reset the transform in case the drag has ended on an invalid position.")]
    //private UnityEventWithPositionAndTransform OnPickableTransformMoveEnded;
    //[SerializeField]
    //[Tooltip("Previous versions of this asset may have fired the OnPickableTransformMoveStarted too early, when it hasn't actually been moved.")]
    //private bool useLegacyTransformMovedEventOrder = false;
    //[SerializeField]
    //[Tooltip("Here you can set up callbacks to be invoked when the moving of a pickable transform is started.")]
    //private UnityEventWithTransform OnPickableTransformMoveStarted;

    //private bool isManualSelectionRequest;
    //private Component SelectedCollider
    //{
    //    get
    //    {
    //        if (SelectedColliders.Count == 0)
    //        {
    //            return null;
    //        }
    //        return SelectedColliders[SelectedColliders.Count - 1];
    //    }
    //}
    //private bool invokeMoveStartedOnDrag = false;
    //private bool invokeMoveEndedOnDrag = false;
    //public List<Component> SelectedColliders { get; private set; }
    //public bool IsMultiSelectionEnabled
    //{
    //    get
    //    {
    //        return isMultiSelectionEnabled;
    //    }
    //    set
    //    {
    //        isMultiSelectionEnabled = value;
    //        if (value == false)
    //        {
    //            DeselectAll();
    //        }
    //    }
    //}
    //private bool _isSelectedViaLongTap = false;
    //private Vector3 selectedObjectTransformPosition = Vector3.zero;
    //private Vector3 currentDragStartPos = Vector3.zero;
    //private Dictionary<Component, Vector3> selectionPositionOffsets = new Dictionary<Component, Vector3>();
    //private Vector3 draggedTransformOffset = Vector3.zero;

    //private Vector3 draggedTransformHeightOffset = Vector3.zero;
    //private const float transformMovedDistanceThreshold = 0.001f;
    //private Vector3 draggedItemCustomOffset = Vector3.zero;
    //private Vector3 itemInitialDragOffsetWorld;
    //public const float snapAngleDiagonal = 45 * Mathf.Deg2Rad;
    //public float SnapUnitSize
    //{
    //    get { return (snapUnitSize); }
    //    set { snapUnitSize = value; }
    //}
    //public Vector2 SnapOffset
    //{
    //    get { return (snapOffset); }
    //    set { snapOffset = value; }
    //}
    //public bool SnapToGrid
    //{
    //    get { return snapToGrid; }
    //    set { snapToGrid = value; }
    //}
    //private bool isSelectedViaLongTap
    //{
    //    get { return _isSelectedViaLongTap; }
    //    set
    //    {
    //        _isSelectedViaLongTap = value;
    //        Debug.LogError(_isSelectedViaLongTap);
    //    }
    //}
    //public SnapAngle SnapAngle
    //{
    //    get { return (snapAngle); }
    //    set { snapAngle = value; }
    //}
    //private void EndPickableTransformMove()
    //{
    //    if (selectedObject.transform != null)
    //    {
    //        if (OnPickableTransformMoveEnded != null)
    //        {
    //            if (invokeMoveEndedOnDrag == true)
    //            {
    //                OnPickableTransformMoveEnded.Invoke(currentDragStartPos, selectedObject.transform);
    //            }
    //        }
    //    }
    //    selectedObject = null;
    //    invokeMoveStartedOnDrag = false;
    //    invokeMoveEndedOnDrag = false;
    //}
    //private float ComputeDistance2d(float x0, float y0, float x1, float y1)
    //{
    //    return (Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0)));
    //}
    //private void InvokePickableMoveStart()
    //{
    //    InvokeTransformActionSafe(OnPickableTransformMoveStarted, selectedObject.transform);
    //    invokeMoveStartedOnDrag = false;
    //    invokeMoveEndedOnDrag = true;
    //}
    //public int DeselectAll()
    //{
    //    SelectedColliders.RemoveAll(item => item == null);
    //    int colliderCount = SelectedColliders.Count;
    //    foreach (Component colliderComponent in SelectedColliders)
    //    {
    //        OnSelectedColliderChanged(SelectionAction.Deselect, colliderComponent.GetComponent<MobileTouchPickable>());
    //    }
    //    SelectedColliders.Clear();
    //    return colliderCount;
    //}
    //private bool Deselect(Component colliderComponent)
    //{
    //    bool wasRemoved = SelectedColliders.Remove(colliderComponent);
    //    if (wasRemoved == true)
    //    {
    //        OnSelectedColliderChanged(SelectionAction.Deselect, colliderComponent.GetComponent<MobileTouchPickable>());
    //    }
    //    return wasRemoved;
    //}
    //private void RequestDragPickable(Vector3 fingerDownPos)
    //{
    //    Vector3 intersectionPoint = Vector3.zero;
    //    Component pickedCollider = GetClosestColliderAtScreenPoint(fingerDownPos, out intersectionPoint);
    //    if (pickedCollider != null && SelectedColliders.Contains(pickedCollider))
    //    {
    //        RequestDragPickable(pickedCollider, fingerDownPos, intersectionPoint);
    //    }
    //}

    //private void RequestDragPickable(Component colliderComponent, Vector2 fingerDownPos, Vector3 intersectionPoint)
    //{

    //    if (requireLongTapForMove == true && isSelectedViaLongTap == false)
    //    {
    //        return;
    //    }

    //    selectedObject = null;
    //    bool isDragStartedOnSelection = colliderComponent != null && SelectedColliders.Contains(colliderComponent);
    //    if (isDragStartedOnSelection == true)
    //    {
    //        PlaceableObject mobileTouchPickable = colliderComponent.GetComponent<PlaceableObject>();
    //        if (mobileTouchPickable != null)
    //        {
    //            mobileTouchCam.OnDragSceneObject(); //Lock camera movement.
    //            selectedObject = mobileTouchPickable;
    //            selectedObjectTransformPosition = selectedObject.transform.position;

    //            invokeMoveStartedOnDrag = true;
    //            currentDragStartPos = selectedObject.transform.position;
    //            selectionPositionOffsets.Clear();
    //            foreach (Component selectionComponent in SelectedColliders)
    //            {
    //                selectionPositionOffsets.Add(selectionComponent, currentDragStartPos - selectionComponent.transform.position);
    //            }

    //            draggedTransformOffset = Vector3.zero;
    //            draggedTransformHeightOffset = Vector3.zero;
    //            draggedItemCustomOffset = Vector3.zero;

    //            //Find offset of item transform relative to ground.
    //            Vector3 groundPosCenter = Vector3.zero;
    //            Ray groundScanRayCenter = new Ray(selectedObject.transform.position, -mobileTouchCam.RefPlane.normal);
    //            bool rayHitSuccess = mobileTouchCam.RaycastGround(groundScanRayCenter, out groundPosCenter);
    //            if (rayHitSuccess == true)
    //            {
    //                draggedTransformHeightOffset = selectedObject.transform.position - groundPosCenter;
    //            }
    //            else
    //            {
    //                groundPosCenter = selectedObject.transform.position;
    //            }

    //            draggedTransformOffset = groundPosCenter - intersectionPoint;
    //            itemInitialDragOffsetWorld = (ComputeDragPosition(fingerDownPos, SnapToGrid) - selectedObject.transform.position);
    //        }
    //    }
    //}
    //private Vector3 ComputeDragPosition(Vector3 dragPosCurrent, bool clampToGrid)
    //{

    //    Vector3 dragPosWorld = Vector3.zero;
    //    Ray dragRay = mobileTouchCam.Cam.ScreenPointToRay(dragPosCurrent);

    //    dragRay.origin += draggedTransformOffset;
    //    bool hitSuccess = mobileTouchCam.RaycastGround(dragRay, out dragPosWorld);
    //    if (hitSuccess == false)
    //    { //This case really should never be met. But in case it is for some unknown reason, return the current item position. That way at least it will remain static and not move somewhere into nirvana.
    //        return selectedObject.transform.position;
    //    }

    //    dragPosWorld += draggedTransformHeightOffset;
    //    dragPosWorld += draggedItemCustomOffset;

    //    if (clampToGrid == true)
    //    {
    //        dragPosWorld = ClampDragPosition(selectedObject, dragPosWorld);
    //    }
    //    return dragPosWorld;
    //}
    //private Vector3 ClampDragPosition(PlaceableObject draggedPickable, Vector3 position)
    //{

    //    if (mobileTouchCam.CameraAxes == CameraPlaneAxes.XY_2D_SIDESCROLL)
    //    {
    //        if (snapAngle == SnapAngle.Diagonal_45_Degrees)
    //        {
    //            RotateVector2(ref position.x, ref position.y, -snapAngleDiagonal);
    //        }
    //        position.x = GetPositionSnapped(position.x, draggedPickable.LocalSnapOffset.x + snapOffset.x);
    //        position.y = GetPositionSnapped(position.y, draggedPickable.LocalSnapOffset.y + snapOffset.y);
    //        if (snapAngle == SnapAngle.Diagonal_45_Degrees)
    //        {
    //            RotateVector2(ref position.x, ref position.y, snapAngleDiagonal);
    //        }
    //    }
    //    else
    //    {
    //        if (snapAngle == SnapAngle.Diagonal_45_Degrees)
    //        {
    //            RotateVector2(ref position.x, ref position.z, -snapAngleDiagonal);
    //        }
    //        position.x = GetPositionSnapped(position.x, draggedPickable.LocalSnapOffset.x + snapOffset.x);
    //        position.z = GetPositionSnapped(position.z, draggedPickable.LocalSnapOffset.y + snapOffset.y);
    //        if (snapAngle == SnapAngle.Diagonal_45_Degrees)
    //        {
    //            RotateVector2(ref position.x, ref position.z, snapAngleDiagonal);
    //        }
    //    }
    //    return (position);
    //}
    //private float GetPositionSnapped(float position, float snapOffset)
    //{
    //    if (snapToGrid == true)
    //    {
    //        return (Mathf.RoundToInt(position / snapUnitSize) * snapUnitSize) + snapOffset;
    //    }
    //    else
    //    {
    //        return (position);
    //    }
    //}

    //private void RotateVector2(ref float x, ref float y, float degrees)
    //{
    //    if (Mathf.Approximately(degrees, 0))
    //    {
    //        return;
    //    }
    //    float newX = x * Mathf.Cos(degrees) - y * Mathf.Sin(degrees);
    //    float newY = x * Mathf.Sin(degrees) + y * Mathf.Cos(degrees);
    //    x = newX;
    //    y = newY;
    //}
    //private void Select(Component colliderComponent, bool isDoubleClick, bool isLongTap)
    //{
    //    MobileTouchPickable mobileTouchPickable = colliderComponent.GetComponent<MobileTouchPickable>();
    //    if (mobileTouchPickable != null)
    //    {
    //        if (SelectedColliders.Contains(colliderComponent) == false)
    //        {
    //            SelectedColliders.Add(colliderComponent);
    //        }
    //    }
    //    OnSelectedColliderChanged(SelectionAction.Select, mobileTouchPickable);
    //    OnSelectedColliderChangedExtended(SelectionAction.Select, mobileTouchPickable, isDoubleClick, isLongTap);
    //}
    //private void OnSelectedColliderChanged(SelectionAction selectionAction, MobileTouchPickable mobileTouchPickable)
    //{
    //    if (mobileTouchPickable != null)
    //    {
    //        if (selectionAction == SelectionAction.Select)
    //        {
    //            InvokeTransformActionSafe(OnPickableTransformSelected, mobileTouchPickable.PickableTransform);
    //        }
    //        else if (selectionAction == SelectionAction.Deselect)
    //        {
    //            InvokeTransformActionSafe(OnPickableTransformDeselected, mobileTouchPickable.PickableTransform);
    //        }
    //    }
    //}
    //private void OnSelectedColliderChangedExtended(SelectionAction selectionAction, MobileTouchPickable mobileTouchPickable, bool isDoubleClick, bool isLongTap)
    //{
    //    if (mobileTouchPickable != null)
    //    {
    //        if (selectionAction == SelectionAction.Select)
    //        {
    //            PickableSelectedData pickableSelectedData = new PickableSelectedData()
    //            {
    //                SelectedTransform = mobileTouchPickable.PickableTransform,
    //                IsDoubleClick = isDoubleClick,
    //                IsLongTap = isLongTap
    //            };
    //            InvokeGenericActionSafe(OnPickableTransformSelectedExtended, pickableSelectedData);
    //        }
    //    }
    //}
    //private void InvokeTransformActionSafe(UnityEventWithTransform eventAction, Transform selectionTransform)
    //{
    //    if (eventAction != null)
    //    {
    //        eventAction.Invoke(selectionTransform);
    //    }
    //}
    //private void InvokeGenericActionSafe<T1, T2>(T1 eventAction, T2 eventArgs) where T1 : UnityEvent<T2>
    //{
    //    if (eventAction != null)
    //    {
    //        eventAction.Invoke(eventArgs);
    //    }
    //}
    //private void SelectColliderInternal(Component colliderComponent, bool isDoubleClick, bool isLongTap)
    //{

    //    if (deselectPreviousColliderOnClick == false)
    //    {
    //        if (colliderComponent == null || colliderComponent.GetComponent<MobileTouchPickable>() == null)
    //        {
    //            return; //Skip selection change in case the user requested to deselect only in case another pickable is clicked.
    //        }
    //    }

    //    if (isManualSelectionRequest == true)
    //    {
    //        return; //Skip selection when the user has already requested a manual selection with the same click.
    //    }

    //    Component previouslySelectedCollider = SelectedCollider;
    //    bool skipSelect = false;

    //    if (isMultiSelectionEnabled == false)
    //    {
    //        if (previouslySelectedCollider != null && previouslySelectedCollider != colliderComponent)
    //        {
    //            Deselect(previouslySelectedCollider);
    //        }
    //    }
    //    else
    //    {
    //        skipSelect = Deselect(colliderComponent);
    //    }

    //    if (skipSelect == false)
    //    {
    //        if (colliderComponent != null)
    //        {
    //            if (colliderComponent != previouslySelectedCollider || repeatEventSelectedOnClick == true)
    //            {
    //                Select(colliderComponent, isDoubleClick, isLongTap);
    //                isSelectedViaLongTap = isLongTap;
    //            }
    //        }
    //    }
    //}
    public Component GetClosestColliderAtScreenPoint(Vector3 screenPoint, out Vector3 intersectionPoint)
    {

        Component hitCollider = null;
        float hitDistance = float.MaxValue;
        Ray camRay = mobileTouchCam.Cam.ScreenPointToRay(screenPoint);
        RaycastHit hitInfo;
        intersectionPoint = Vector3.zero;
        if (Physics.Raycast(camRay, out hitInfo) == true)
        {
            hitDistance = hitInfo.distance;
            hitCollider = hitInfo.collider;
            intersectionPoint = hitInfo.point;
        }
        RaycastHit2D hitInfo2D = Physics2D.Raycast(camRay.origin, camRay.direction);
        if (hitInfo2D == true)
        {
            if (hitInfo2D.distance < hitDistance)
            {
                hitCollider = hitInfo2D.collider;
                intersectionPoint = hitInfo2D.point;
            }
        }
        return (hitCollider);
    }

    //public void LateUpdate()
    //{
    //    if (isManualSelectionRequest == true && TouchWrapper.TouchCount == 0)
    //    {
    //        isManualSelectionRequest = false;
    //    }
    //}
}
