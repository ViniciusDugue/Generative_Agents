using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockScript : MonoBehaviour
{
    public GameObject Agent;
    public enum BlockState { PickedUp, Dropped }
    public BlockState CurrentState;

    void Update()
    {
        if (CurrentState == BlockState.PickedUp)
        {
            HoldBlockAboveAgent();
        }
    }

    public void PickUpBlock()
    {
        CurrentState = BlockState.PickedUp;
        HoldBlockAboveAgent();
    }

    public void DropBlock()
    {
        CurrentState = BlockState.Dropped;
        DropBlockInFrontOfAgent();
    }

    void HoldBlockAboveAgent()
    {
        if (Agent != null)
        {
            transform.position = Agent.transform.position + new Vector3(0, 1, 0);
        }
    }

    void DropBlockInFrontOfAgent()
    {
        if (Agent != null)
        {
            transform.position = Agent.transform.position + Agent.transform.forward + new Vector3(0, 0.5f, 0);
        }
    }
}
