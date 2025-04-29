<<<<<<< HEAD
=======
using System.Collections;
using System.Collections.Generic;

>>>>>>> main
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform cam; // Use private to prevent unassigned warnings in the Inspector

    void Start()
    {
        Camera mainCam = Camera.main; // Get the main camera
        if (mainCam != null)
        {
            cam = mainCam.transform;
        }
        else
        {
            Debug.LogError("Billboard: No Main Camera found in the scene!");
        }
    }

    void LateUpdate()
    {
        if (cam != null)
        {
            transform.LookAt(transform.position + cam.forward);
        }
    }
}
