using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BehaviorDatabase", menuName = "AI/BehaviorDatabase")]
public class BehaviorDatabase : ScriptableObject
{
    public List<BehaviorVisual> visuals;
    public BehaviorVisual defaultVisual;
}

[System.Serializable]
public class BehaviorVisual
{
    public string behaviorName;
    public Sprite icon;
    public string displayName;
}
