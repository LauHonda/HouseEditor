using UnityEngine;


/* This class is just a container for all the elements that can be placed in a single block at a time
 * It's purpose is to simplify the lookup when adding/removing items and to avoid unnecessary raycastings */
public class Block {
  public Wall wall;
  public FloorTile floorTile;
  public PlaceableObject blockObject;
}
