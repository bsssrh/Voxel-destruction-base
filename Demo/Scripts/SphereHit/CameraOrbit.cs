using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VoxelDestructionPro.Demo
{
    public class CameraOrbit : MonoBehaviour
    {
        public Transform target;        // The object to orbit around
        public float rotationSpeed = 5f; // Speed of camera rotation
        public float distance = 5f;     // Distance from the target
        public float minVerticalAngle = -80f; // Minimum vertical angle
        public float maxVerticalAngle = 80f;  // Maximum vertical angle
    
        private float currentRotationX = 0f;
        private float currentRotationY = 0f;

        public SphereHit controller;
    
        void Start()
        {
            // Initialize the camera position
            Vector3 dir = new Vector3(0, 0, -distance);
            Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
            transform.position = target.position + rotation * dir;
            transform.LookAt(target.position);
        }
    
        void LateUpdate()
        {
            if (target == null)
            {
                Debug.LogWarning("Target not set for CameraOrbit script!");
                return;
            }
    
            if (controller.inItemMode)
                return;
            
            // Allow manual rotation with mouse input
            if (Input.GetMouseButton(0))
            {
                float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;
    
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    currentRotationY += mouseX;
                    currentRotationX -= mouseY;   
                }
            }
    
            // Clamp the vertical rotation
            currentRotationX = Mathf.Clamp(currentRotationX, minVerticalAngle, maxVerticalAngle);
    
            // Calculate the new position
            Vector3 dir = new Vector3(0, 0, -distance);
            Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
            Vector3 newPosition = target.position + rotation * dir;
    
            // Update the camera's position and rotation
            transform.position = newPosition;
            transform.LookAt(target.position);
        }
    }
}