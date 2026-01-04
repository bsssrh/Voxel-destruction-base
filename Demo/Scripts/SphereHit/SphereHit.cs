using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VoxelDestructionPro.VoxDataProviders;
using VoxelDestructionPro.VoxelObjects;
using Random = UnityEngine.Random;

namespace VoxelDestructionPro.Demo
{
    public class SphereHit : MonoBehaviour
    {
        public DynamicVoxelObj mainVoxObject;
        public Camera cam;
        
        public Item[] items;
        
        [HideInInspector]
        public bool inItemMode;
        private int selectedItem;
        
        private void Start()
        {
            inItemMode = false;
        }
        
        public void SelectItem(int index)
        {
            inItemMode = true;

            selectedItem = index;
        }

        private void Update()
        {
            if (inItemMode)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;
                
                if (Input.GetKeyDown(KeyCode.Escape))
                    inItemMode = false;

                if (Input.GetMouseButtonDown(0))
                {
                    Item item = items[selectedItem];

                    GameObject newItem = Instantiate(item.item);
                    Ray r = cam.ScreenPointToRay(Input.mousePosition);
                    
                    if (!item.placeToHit)
                    {
                        if (newItem.TryGetComponent(out Rigidbody rb))
                        {
                            rb.position = transform.position;
                            rb.rotation = Random.rotation;
                            
                            rb.AddForce(r.direction * item.throwSpeed, ForceMode.VelocityChange);
                            if (item.randomTorque)
                                rb.AddTorque(Random.onUnitSphere * 5, ForceMode.VelocityChange);
                            else
                                newItem.transform.rotation = Quaternion.LookRotation(r.direction);
                        }
                    }
                    else
                    {
                        if (!Physics.Raycast(r, out RaycastHit hit, 999))
                            return;

                        newItem.transform.position = hit.point;
                        newItem.transform.rotation = Quaternion.LookRotation(hit.normal);
                        newItem.transform.parent = hit.transform;
                    }
                }
            }
        }
    }

    [Serializable]
    public class Item
    {
        public GameObject item;
        public bool placeToHit;
        public bool randomTorque;
        public float throwSpeed;
    }
}