using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CombineWallMesh : MonoBehaviour
{
    [ContextMenu("Combine Meshes")] // Right-click on the component in Unity to manually trigger this function
    void CombineWallMeshes()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false); // Hide individual cubes
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);

        // Create a new GameObject for the combined mesh
        GameObject combinedObject = new GameObject("CombinedWalls");
        combinedObject.transform.position = transform.position;

        MeshFilter filter = combinedObject.AddComponent<MeshFilter>();
        filter.mesh = combinedMesh;
        combinedObject.AddComponent<MeshRenderer>().sharedMaterial = meshFilters[0].GetComponent<MeshRenderer>().sharedMaterial;

        // // SAVE the mesh so it persists
        // AssetDatabase.CreateAsset(combinedMesh, "Assets/CombinedWallsMesh.asset");
        // AssetDatabase.SaveAssets();

        Debug.Log("Walls Combined and Saved!");
    }
}
