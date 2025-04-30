using System;
using UnityEngine;

namespace AgentDataStructures
{
    [Serializable]
    public class BlockPositionEntry
    {
        public string blockName;
        public Vector3 position;
    }

    [Serializable]
    public class BlockMappingEntry
    {
        public string blockName;
        public GameObject blockObject;
    }
}
