using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Used for floor tiles.
 */
public class FloorTile : PlaceableObject
{

    void Start()
    {
        Init();
        supportsMultiPlacement = true;
    }

    /*
     * TODO set this up as a coroutine to improve performance
     * and allow tiling on massive areas
     */
    bool IsFloatingArea(Vector3 start, Vector3 end)
    {
        Vector3 current = start;
        end = AdjustEndValueForStepping(current, end);

        bool isFloating = true;

        if (start.x == end.x)
        { //Line of tiles across the z axis
            while (current.z != end.z)
            {
                if (!IsFloating(current))
                {
                    isFloating = false;
                    break;
                }
                current = StepBlock(current, end, true);
            }
        }
        else
        {
            if (start.z == end.z)
            { //Line of tiles across the x axis
                while (current.x != end.x)
                {
                    if (!IsFloating(current))
                    {
                        isFloating = false;
                        break;
                    }
                    current = StepBlock(current, end, false);
                }
            }
            else
            { //Rectangular set of tiles
                while (current.x != end.x)
                {
                    while (current.z != end.z)
                    {
                        if (!IsFloating(current))
                        {
                            isFloating = false;
                            break;
                        }
                        current = StepBlock(current, end, true);
                    }
                    current.z = start.z;
                    current = StepBlock(current, end, false);
                }
            }
        }

        return isFloating;
    }

    bool IsFloating(Vector3 position)
    {
        Vector3 positionBelow = new Vector3(position.x, position.y - BuilderKitConfig.FLOOR_HEIGHT, position.z);
        return !BlockManager.IsNextToTile(position) && !BlockManager.IsNextToWall(positionBelow);
    }

    /* 
     * Floating tiles are not allowed, so we check for adjacent walls
     * on the level below or tiles next to the positon to make sure we can place a tile.
     * We only need to check the borders, anything in the middle 
     * will be connected if the borders are connected.
     */
    bool IsFloating(Vector3 start, Vector3 end)
    {
        bool isFloating = false;
        if (start.y > 0)
        {
            if (start == end)
            {
                isFloating = IsFloating(end);
            }
            else
            {
                isFloating = IsFloatingArea(start, end);
            }
        }
        return isFloating;
    }

    public override bool IsValidLocation(Vector3 start, Vector3 end)
    {
        return BlockManager.IsAreaValid(start, end)
          && !IsFloating(start, end)
          && (start != end || !BlockManager.IsBlockerTileSet(end));
    }

    private GameObject PutTile(Vector3 position)
    {
        if (!BlockManager.IsBlockerTileSet(position))
        {
            GameObject placedObject = GameObject.Instantiate(transform.gameObject);
            placedObject.transform.position = position;
            BuildingFloorManager.AssignToCurrentFloor(placedObject);
            BlockManager.PutFloorTile(placedObject.GetComponent<FloorTile>(), position);

            return placedObject;
        }

        return null;
    }

    public override void PickUp()
    {
        BlockManager.GetBlock(transform.position).floorTile = null;
        BuildingFloorManager.AssignToTempFloor(transform.gameObject);
    }

    /* 
     * We use this to step over the different coordinates because it contemplates selections
     * in all directions (e.g. startX = 1; endX = -4 should decrement instead of increment)
     */
    private Vector3 StepBlock(Vector3 current, Vector3 end, bool zAxis)
    {
        if (zAxis)
        {
            if (current.z < end.z)
            {
                current.z++;
            }
            else
            {
                current.z--;
            }
        }
        else
        {
            if (current.x < end.x)
            {
                current.x++;
            }
            else
            {
                current.x--;
            }
        }
        return current;
    }

    Vector3 AdjustEndValueForStepping(Vector3 current, Vector3 end)
    {
        if (current.z < end.z)
        {
            end.z++;
        }
        else
        {
            if (current.z > end.z)
            {
                end.z--;
            }
        }
        if (current.x < end.x)
        {
            end.x++;
        }
        else
        {
            if (current.x > end.x)
            {
                end.x--;
            }
        }

        return end;
    }

    public override List<GameObject> Place(Vector3 start, Vector3 end, bool modifier = false)
    {
        List<GameObject> placedTiles = new List<GameObject>();
        //Single tile placement
        if (start == end)
        {
            placedTiles.Add(PutTile(end));
        }
        else
        {
            //If the start and end are different we have 3 possible scenarios
            //A line across the X axis, a line across the Z axis, or a rectangular selection
            Vector3 current = start;

            end = AdjustEndValueForStepping(current, end);

            if (start.x == end.x)
            { //Line of tiles across the z axis
                while (current.z != end.z)
                {
                    PutTile(current);
                    current = StepBlock(current, end, true);
                }
            }
            else
            {
                if (start.z == end.z)
                { //Line of tiles across the x axis
                    while (current.x != end.x)
                    {
                        placedTiles.Add(PutTile(current));
                        current = StepBlock(current, end, false);
                    }
                }
                else
                { //Rectangular set of tiles
                    while (current.x != end.x)
                    {
                        while (current.z != end.z)
                        {
                            placedTiles.Add(PutTile(current));
                            current = StepBlock(current, end, true);
                        }
                        current.z = start.z;
                        current = StepBlock(current, end, false);
                    }
                }
            }
        }
        return placedTiles;
    }

    public override void Select() { }

}