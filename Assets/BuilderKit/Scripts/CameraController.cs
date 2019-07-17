using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
  float MAX_CAMERA_X = 17f;
  float MIN_CAMERA_X = -17f;
  float MAX_CAMERA_Z = 20f;
  float MIN_CAMERA_Z = -20f;

  [Range(1,10)]
  public float movementSpeed = 5f;

  [Range(1,10)]
  public float rotationSpeed = 5f;

  Camera mainCamera;
  GameObject cameraPivot;

  Vector3 zMovement;
  Vector3 xMovement;

  bool isRotating;
  Quaternion targetRotation;


  void Start() {
    mainCamera = Camera.main;
    cameraPivot = GameObject.Find("CameraPivot");
    mainCamera.transform.LookAt(cameraPivot.transform);

    zMovement = new Vector3(0, 0, movementSpeed);
    xMovement = new Vector3(movementSpeed, 0, 0);
  }


  //This makes it easy to move around with WASD when the camera is rotated
  //by changing the directions of the different axes depending on the angle
  private void UpdateMovementVectors() {
    switch ((int)cameraPivot.transform.rotation.eulerAngles.y) {
      case 0:
        zMovement.z = movementSpeed;
        zMovement.x = 0;
        xMovement.x = movementSpeed;
        xMovement.z = 0;
        break;
      case 90:
        zMovement.z = 0;
        zMovement.x = movementSpeed;
        xMovement.x = 0;
        xMovement.z = -movementSpeed;
        break;
      case 180:
        zMovement.z = -movementSpeed;
        zMovement.x = 0;
        xMovement.x = -movementSpeed;
        xMovement.z = 0;
        break;
      case 270:
      case -90:
        zMovement.z = 0;
        zMovement.x = -movementSpeed;
        xMovement.x = 0;
        xMovement.z = movementSpeed;
        break;
    }
  }

  void Update() {
    //Zoom handling
    mainCamera.fieldOfView += Input.GetAxis("Mouse ScrollWheel");
    if (mainCamera.fieldOfView < 20) {
      mainCamera.fieldOfView = 20;
    } else {
      if (mainCamera.fieldOfView > 100) {
        mainCamera.fieldOfView = 100;
      }
    }
      
    //Rotates smoothly on the y axis
    if (isRotating) {
      cameraPivot.transform.rotation = Quaternion.Lerp(cameraPivot.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
      float delta = Mathf.Abs(cameraPivot.transform.rotation.eulerAngles.y - targetRotation.eulerAngles.y);
      if (delta < 1f) {
        cameraPivot.transform.rotation = targetRotation;
        UpdateMovementVectors();
        isRotating = false;
      }
    }

    if (!isRotating) {
      //These handle the rotation keypresses
      if (Input.GetKeyUp(KeyCode.Q)) {
        isRotating = true;
        targetRotation = Quaternion.Euler(new Vector3(0,cameraPivot.transform.rotation.eulerAngles.y + 90,0));
      }

      if (Input.GetKeyUp(KeyCode.E)) {
        isRotating = true;
        targetRotation = Quaternion.Euler(new Vector3(0,cameraPivot.transform.rotation.eulerAngles.y - 90,0));
      }

      //These handle the arrow key movements
      Vector3 cameraPosition = cameraPivot.transform.localPosition;
      if (Input.GetKey(KeyCode.A)) {
        cameraPosition -= xMovement * Time.deltaTime;
      }
      if (Input.GetKey(KeyCode.D)) {
        cameraPosition += xMovement * Time.deltaTime;
      }
      if (Input.GetKey(KeyCode.W)) {
        cameraPosition += zMovement * Time.deltaTime;
      }
      if (Input.GetKey(KeyCode.S)) {
        cameraPosition -= zMovement * Time.deltaTime;
      }

      //We have to make sure we are not off bounds, independently of the rotation
      if (cameraPosition.x < MIN_CAMERA_X) {
        cameraPosition.x = MIN_CAMERA_X;
      }
      if (cameraPosition.z < MIN_CAMERA_Z) {
        cameraPosition.z = MIN_CAMERA_Z;
      }
      if (cameraPosition.z > MAX_CAMERA_Z) {
        cameraPosition.z = MAX_CAMERA_Z;
      }
      if (cameraPosition.x > MAX_CAMERA_X) {
        cameraPosition.x = MAX_CAMERA_X;
      }

      cameraPivot.transform.localPosition = cameraPosition;
    }

  }
}
