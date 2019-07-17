/*
 * Container struct for config parameters
 */
public struct BuilderKitConfig {
  public static int NUMBER_OF_FLOORS = 4;
  public static int MAP_WIDTH = 64;
  public static int MAP_HEIGHT = 64;
  public static float FLOOR_HEIGHT = 2.403474f; //This is the height of the tallest wall
  public static float GRID_FLOAT_HEIGHT = 0.025f; //The distance from the floor the grid floats at
  public static float GRID_FADING_SPEED = 0.65f;
  public static float GRID_MOVING_SPEED = 2.6f;
  public static string SAVE_EXTENSION = ".sav";
  public static string SAVE_PREFIX = "save";
  public static int SAVE_ITEM_HEIGHT = 35; //The size each of the save buttons should be on the dialog
}


