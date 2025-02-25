using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapToggle : MonoBehaviour
{
    public GameObject minimapUI;  // Assign your minimap GameObject here

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            bool isActive = minimapUI.activeSelf;
            minimapUI.SetActive(!isActive);
        }
    }
}
