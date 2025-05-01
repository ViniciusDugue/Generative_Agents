using System;
using UnityEngine;

namespace AgentDataStructures
{
    [Serializable]
    public class BlockPositionEntry
    {
        public string   blockName;
        public Vector2  position;
    }

    [Serializable]
    public class BlockMappingEntry
    {
        public string     blockName;
        public GameObject blockObject;
    }
}
