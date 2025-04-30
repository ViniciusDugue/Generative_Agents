// using UnityEngine;

// public class BlockPickedUp : MonoBehaviour
// {
//     private void OnDisable()
//     {
//         // Notify all agents that this block was picked up
//         GameObject[] agents = GameObject.FindGameObjectsWithTag("agent");

//         foreach (GameObject agent in agents)
//         {
//             BehaviorManager bm = agent.GetComponent<BehaviorManager>();
//             if (bm != null)
//             {
//                 if (bm.blockPositions.ContainsKey(gameObject.name))
//                 {
//                     bm.blockPositions.Remove(gameObject.name);
//                     Debug.Log($"[BlockPickedUp] Removed {gameObject.name} from Agent {bm.agentID}'s block dictionary.");
//                 }
//             }
//         }
//     }
// }