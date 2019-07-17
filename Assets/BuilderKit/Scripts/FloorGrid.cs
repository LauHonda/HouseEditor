using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorGrid : MonoBehaviour {
  bool isEnabled = false;
  bool isEnabling = false;
  bool isDisabling = false;

  float opacity = 0;
  Vector3 gridTargetPosition;
  float gridFadingSpeed;
  float gridMovingSpeed;

  Material gridMaterial;

  void Start() {
    gridMaterial = this.transform.GetComponent<Renderer>().material;
    gridMaterial.SetFloat("_Opacity", opacity);
    gridMaterial.SetFloat("_MapWidth", BuilderKitConfig.MAP_WIDTH);
    gridMaterial.SetFloat("_MapHeight", BuilderKitConfig.MAP_HEIGHT);

    gridFadingSpeed = BuilderKitConfig.GRID_FADING_SPEED;
    gridMovingSpeed = BuilderKitConfig.GRID_MOVING_SPEED;

    gridTargetPosition = new Vector3(0, BuilderKitConfig.GRID_FLOAT_HEIGHT, 0);
    transform.position = gridTargetPosition;
  }

  //Places the grid on the given floor
  public void MoveToFloor(int floor, bool animate = true) {
    float yPosition = floor * BuilderKitConfig.FLOOR_HEIGHT + BuilderKitConfig.GRID_FLOAT_HEIGHT;
    gridTargetPosition = new Vector3(0, yPosition, 0);
    if (!animate) {
      transform.position = gridTargetPosition;
    }
  }
	
  public void Enable() {
    if (!isEnabled && !isEnabling) {
      isEnabling = true;
    }
  }

  public void Disable() {
    if (isEnabled && !isDisabling) {
      isDisabling = true;
    }
  }

  void Update() {
    if (transform.position.y != gridTargetPosition.y) {
      transform.position = Vector3.Lerp(transform.position, gridTargetPosition, gridMovingSpeed * Time.deltaTime);
      if (Mathf.Abs(transform.position.y - gridTargetPosition.y) < 0.02) {
        transform.position = gridTargetPosition;
      }
    }

    if (isEnabling) {
      if (opacity < 1) {
        opacity += gridFadingSpeed * Time.deltaTime;
        gridMaterial.SetFloat("_Opacity", opacity);
      } else {
        opacity = 1;
        isEnabling = false;
        isEnabled = true;
      }
    } else {
      if (isDisabling) {
        if (opacity > 0) {
          opacity -= gridFadingSpeed * Time.deltaTime;
          gridMaterial.SetFloat("_Opacity", opacity);
        } else {
          opacity = 0;
          isDisabling = false;
          isEnabled = false;
        }
      }
    }
    
  }
}
