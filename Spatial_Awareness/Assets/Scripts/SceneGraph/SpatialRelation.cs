using UnityEngine;

namespace SpatialAwareness.SceneGraph
{
    public enum SpatialConfig
    {
        Near,
        Far,
        LeftOf,
        RightOf,
        Behind,
        InFrontOf,
        OnTop,
        Inside,
        None
    }

    [System.Serializable]
    public class Relation
    {
        public SceneNode targetNode;
        public SpatialConfig type;
    }
}
