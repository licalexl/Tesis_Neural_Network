using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 2.0f;

    private bool isRotating = false;
    private Vector3 lastMousePosition;
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }

        if (isRotating)
        {
            Vector3 deltaMouse = Input.mousePosition - lastMousePosition;

            float rotationX = deltaMouse.y * sensitivity * -1;
            float rotationY = -deltaMouse.x * sensitivity * -1;

            // rotación a la cámara
            transform.Rotate(Vector3.right, rotationX, Space.Self);
            transform.Rotate(Vector3.up, rotationY, Space.World);

            lastMousePosition = Input.mousePosition;
        }
    }
}
