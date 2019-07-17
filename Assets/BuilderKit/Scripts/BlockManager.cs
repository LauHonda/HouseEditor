using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/*
 * Manages the registering/removing objects from blocks
 */
public class BlockManager {
  BlockManager instance;

  static Dictionary<string,Block> blocks;

  static float maxHeight;

  static FloorTile blockerTile; //Used to allow multi floor objects to block tile building on some spaces

  public BlockManager() {
    if (instance == null) {
      blocks = new Dictionary<string,Block>();
      maxHeight = BuilderKitConfig.FLOOR_HEIGHT * BuilderKitConfig.NUMBER_OF_FLOORS;

      GameObject blockerTileObject = new GameObject("BlockerTile");
      blockerTileObject.transform.position = new Vector3(0, -100f, 0);
      blockerTile = blockerTileObject.AddComponent<FloorTile>();

      instance = this;
    }
  }

  public static void Reset() {
    blocks.Clear();
  }

  //Determines if the position is within the map size specified in BuilderKitConfig
  public static bool IsBlockPositionValid(Vector3 position) {
    //We use the width and height -1 just to be on the safe side and prevent building on the edges, remove if necessary
    return Mathf.Abs(position.x) <= BuilderKitConfig.MAP_WIDTH / 2 - 1
    && Mathf.Abs(position.z) <= BuilderKitConfig.MAP_HEIGHT / 2 - 1
    && position.y >= 0 && position.y <= maxHeight;
  }

  public static bool IsAreaValid(Vector3 start, Vector3 end) {
    //Because maps are always rectangular or square we only need to check two corners
    return IsBlockPositionValid(start) && IsBlockPositionValid(end);
  }

  //This is used for regular objects, not walls or floor tiles
  public static bool IsAreaEmpty(Vector3 start, Vector3 end) {
    bool isEmpty = true;
    if (start == end) {
      Block block = GetBlock(end);
      isEmpty = block == null || block.blockObject == null;
    } else {
      int width = (int)Mathf.Abs(end.x - start.x);
      int height = (int)Mathf.Abs(end.z - start.z);

      float i = 0;
      int widthDirection = start.x <= end.x ? 1 : -1;
      int heightDirection = start.z <= end.z ? 1 : -1;
      Vector3 current = start;
      Block currentBlock = null;
      while (i <= width) {
        float j = 0;
        current = start;
        current.x = start.x + i;
        while (j <= height) {
          current.z = current.z + j;
          currentBlock = GetBlock(current);
          if (currentBlock != null && (currentBlock.wall != null || currentBlock.blockObject != null)) {
            isEmpty = false;
            break;
          }
          j += heightDirection * 0.5f;
        }
        if (!isEmpty) {
          break;
        }
        i += widthDirection * 0.5f;
      }
    }
    return isEmpty;
  }

  public static Block GetBlock(Vector3 position, bool addIfNotPresent = false) {
    Block block = null;

    if (!blocks.TryGetValue(position.ToString(), out block) && addIfNotPresent) {
      block = new Block();
      blocks.Add(position.ToString(), block);
    }

    return block;
  }

  public static Block GetWallBlock(PlaceableObject placeableObject, Vector3 position, bool addIfNotPresent = false) {
    return GetBlock(GetWallPosition(placeableObject, position), addIfNotPresent);
  }

  public static Block PutFloorTile(FloorTile tile, Vector3 position) {
    Block block = GetBlock(position, true);

    //If there's a tile already placed there we just remove it before placing the new one
    //We don't touch blocker tiles though.
    if(block.floorTile != blockerTile) {
      if (block.floorTile != null) {
        GameObject.Destroy(block.floorTile.gameObject);
      }

      block.floorTile = tile;
    }

    return block;
  }

  //This is used for walls and wall objects
  private static Vector3 GetWallPosition(PlaceableObject placeableObject, Vector3 originalPosition) {
    Vector3 adjustedPosition = originalPosition;
    switch ((int)placeableObject.transform.rotation.eulerAngles.y) {
      case 0:
        adjustedPosition.x += 0.5f;
        break;
      case 180:
        adjustedPosition.x -= 0.5f;
        break;
      case 90:
        adjustedPosition.z -= 0.5f;
        break;
      case 270:
      case -90:
        adjustedPosition.z += 0.5f;
        break;
    }
    
    return adjustedPosition;
  }

  public static Block PutWall(Wall wall, Vector3 position) {
    Block block = GetWallBlock(wall, position, true);
    block.wall = wall;
    return block;
  }

  //Wall objects are made children of their respective walls
  public static Block PutWallObject(WallObject wallObject, Vector3 position) {
    Block block = GetWallBlock(wallObject, position);
    if (block.wall != null) {
      wallObject.transform.SetParent(block.wall.transform, true);
    }
    return block;
  }

  public static Block[] PutObject(PlaceableObject item, Vector3 position) {
    Vector3 adjustedBase = new Vector3(Mathf.Ceil(position.x), position.y, Mathf.Ceil(position.z));
    Block[] blocks = new Block[item.blockOffsets.Count];
    int i = 0;

    int floorBlocksLimit = item.floorCount > 1 ? item.blockOffsets.Count / item.floorCount : item.blockOffsets.Count;

    foreach (Vector3 offset in item.blockOffsets) {
      Block block = GetBlock(adjustedBase + offset, true);
      block.blockObject = item;

      if (i >= floorBlocksLimit) {
        block.floorTile = blockerTile;
      }

      blocks[i++] = block;
    }

    return blocks;
  }

  /*
   * Checks if a block has a tile assigned
   */
  public static bool HasTile(Vector3 position) {
    Block block = GetBlock(position);
    return block != null && block.floorTile != null && block.floorTile != blockerTile;
  }

  /*
   * Check if a block has a contiguous block with a tile
   */
  public static bool IsNextToTile(Vector3 position) {
    bool isNextToTile = false;

    Vector3[] positions = { 
      new Vector3(position.x - 1, position.y, position.z),
      new Vector3(position.x + 1, position.y, position.z),
      new Vector3(position.x, position.y, position.z - 1),
      new Vector3(position.x, position.y, position.z + 1)
    };

    for (int i = 0; i < positions.Length; i++) {
      if (HasTile(positions[i])) {
        isNextToTile = true;
        break;
      }  
    }

    return isNextToTile;
  }

  /*
   * Checks if a block has a wall next to it
   */
  public static bool IsNextToWall(Vector3 position) {
    bool isNextToWall = false;

    Vector3[] positions = { 
      new Vector3(position.x - 0.5f, position.y, position.z),
      new Vector3(position.x + 0.5f, position.y, position.z),
      new Vector3(position.x, position.y, position.z - 0.5f),
      new Vector3(position.x, position.y, position.z + 0.5f)
    };

    for (int i = 0; i < positions.Length; i++) {
      Block block = GetBlock(positions[i]);
      if (block != null && block.wall != null) {
        isNextToWall = true;
        break;
      }  
    }
    return isNextToWall;
  }

  /*
   * We can usually replace any tile, except blocker tiles, which 
   * are invisible and used to prevent tiling in a specific place
   */ 
  public static bool IsBlockerTileSet(Vector3 position) {
    Block block = GetBlock(position);
    return (block != null && block.floorTile == blockerTile);
  }

}
