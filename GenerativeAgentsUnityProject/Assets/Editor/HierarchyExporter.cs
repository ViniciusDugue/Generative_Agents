using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq; // Ensure LINQ is included

public class HierarchyExporter : EditorWindow
{
    [MenuItem("Tools/Export Hierarchy")]
    static void ExportHierarchy()
    {
        StringBuilder hierarchy = new StringBuilder();
        TraverseHierarchy(null, "", hierarchy);
        string path = EditorUtility.SaveFilePanel("Save Hierarchy", "", "HierarchyList.txt", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, hierarchy.ToString());
            Debug.Log("Hierarchy exported to: " + path);
        }
    }

    static void TraverseHierarchy(Transform parent, string indent, StringBuilder hierarchy)
    {
        Transform[] children = parent == null ? GetRootGameObjects() : parent.Cast<Transform>().ToArray();
        foreach (Transform child in children)
        {
            hierarchy.AppendLine(indent + child.name);
            TraverseHierarchy(child, indent + "  ", hierarchy);
        }
    }

    static Transform[] GetRootGameObjects()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(go => go.transform).ToArray();
    }
}
