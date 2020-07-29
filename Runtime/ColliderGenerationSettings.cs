using System;
using UnityEngine;

[Serializable]
public class ColliderGenerationSettings
{
    public enum ColliderTypes
    {
        Box,
        Capsule
    }

    public ColliderTypes ColliderType;
    [ShowIf("ColliderType", ColliderTypes.Capsule)] public float Radius = 0.1f;
    [ShowIf("ColliderType", ColliderTypes.Box)] public float Width = 0.1f;
    [ShowIf("ColliderType", ColliderTypes.Box)] public float Depth = 0.05f;

    [Tooltip("If true this collider will be aligned to to the spline points with an offset so its flushed. Should be false for rails and anything else centered on the spline")]
    public bool IsEdge;

    [Tooltip("If true we attempt to auto-detect edge alignment for box colliders")]
    [ShowIf("IsEdge")] public bool AutoDetectEdgeAlignment;

    [ShowIf("AutoDetectEdgeAlignment")] public LayerMask LayerMask = 1;
    
    [Tooltip("If true, we flip alignment of edge colliders on generation.")]
    [ShowIf("IsEdge")] public bool FlipEdge;

}