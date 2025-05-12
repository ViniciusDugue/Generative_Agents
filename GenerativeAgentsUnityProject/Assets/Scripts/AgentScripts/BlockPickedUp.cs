using UnityEngine;

public class BlockPickedUp : MonoBehaviour
{
    private void OnDisable()
    {
        string blockName = gameObject.name;
        foreach (var bm in BehaviorManager.AllManagers)
            bm.RemoveBlock(blockName);
        Debug.Log("Block Removed from all Agent Dicts");
    }
}