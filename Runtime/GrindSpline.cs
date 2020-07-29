using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GrindSpline : MonoBehaviour
{
    public enum SurfaceTypes
    {
        Concrete,
        Metal,
    }

    public SurfaceTypes SurfaceType;
    public bool IsRound;
    public ColliderGenerationSettings ColliderGenerationSettings = new ColliderGenerationSettings();
    public Transform PointsContainer;
    public Transform ColliderContainer;
    public List<Collider> GeneratedColliders = new List<Collider>();

    private bool flipEdgeOffset;

    public bool DrawingActive { get; set; }
    public static bool AutoUpdateColliders { get; set; }

#if UNITY_EDITOR

    private Color gizmoColor = Color.green;
    private Color gizmoColorSelected = Color.cyan;

    private void OnValidate()
    {
        var proper_name = $"GrindSpline_Grind_{SurfaceType}{(IsRound ? "_Round" : "")}";

        if (gameObject.name.Contains(proper_name) == false || IsRound == false && gameObject.name.Contains("_Round"))
        {
            gameObject.name = proper_name;
        }

        if (PointsContainer == null)
        {
            PointsContainer = transform;
        }

        if (IsRound && ColliderGenerationSettings.ColliderType == ColliderGenerationSettings.ColliderTypes.Box)
        {
            ColliderGenerationSettings.ColliderType = ColliderGenerationSettings.ColliderTypes.Capsule;
        }
    }

    private bool AnyChildSelected()
    {
        foreach (var o in Selection.gameObjects)
        {
            if (o.transform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (PointsContainer != null && 
            Selection.activeGameObject != null && 
            Selection.activeGameObject != null && 
            Selection.activeGameObject.transform.IsChildOf(PointsContainer))
        {
            if (AutoUpdateColliders && Selection.activeGameObject.transform.hasChanged)
            {
                GenerateColliders();
                Selection.activeGameObject.transform.hasChanged = false;
            }
        }

        var selected = DrawingActive || Selection.gameObjects.Contains(gameObject) || AnyChildSelected();

        var col = selected ? gizmoColorSelected : gizmoColor;

        col.a = selected ? 1f : 0.5f;

        Gizmos.color = col;
        Handles.color = col;

        if (PointsContainer == null) 
            return;

        for (int i = 0; i < PointsContainer.childCount; i++)
        {
            var child = PointsContainer.GetChild(i);

            Handles.CircleHandleCap(0, child.position, Quaternion.LookRotation(SceneView.currentDrawingSceneView.camera.transform.forward), 0.02f, EventType.Repaint);

            if (i + 1 < PointsContainer.childCount)
            {
                if (selected)
                {
                    Handles.DrawAAPolyLine(5f, child.position, PointsContainer.GetChild(i + 1).position);
                }
                else
                {
                    Gizmos.DrawLine(child.position, PointsContainer.GetChild(i + 1).position);
                }
            }
        }
    }
    
    public void GenerateColliders(ColliderGenerationSettings settings = null)
    {
        if (settings == null)
            settings = ColliderGenerationSettings;

        foreach (var c in GeneratedColliders.ToArray())
        {
            if (c != null) 
                DestroyImmediate(c.gameObject);
        }
        
        GeneratedColliders.Clear();

        if (PointsContainer.childCount < 2)
            return;

        for (int i = 0; i < PointsContainer.childCount - 1; i++)
        {
            var a = PointsContainer.GetChild(i).position;
            var b = PointsContainer.GetChild(i + 1).position;
            var col = CreateColliderBetweenPoints(settings, a, b);

            GeneratedColliders.Add(col);
        }
    }
    
    private Collider CreateColliderBetweenPoints(ColliderGenerationSettings settings, Vector3 pointA, Vector3 pointB)
    {
        var go = new GameObject("Grind Cols")
        {
            layer = LayerMask.NameToLayer("Grindable")
        };

        go.transform.position = Vector3.Lerp(pointA, pointB, .5f);
        go.transform.LookAt(pointA);
        go.transform.SetParent(ColliderContainer != null ? ColliderContainer : transform);
        go.tag = $"Grind_{SurfaceType}";

        var length = Vector3.Distance(pointA, pointB);

        if (settings.ColliderType == ColliderGenerationSettings.ColliderTypes.Capsule)
        {
            var cap = go.AddComponent<CapsuleCollider>();

            cap.direction = 2;
            cap.radius = settings.Radius;
            cap.height = length + 2f * settings.Radius;

            go.transform.localPosition += go.transform.InverseTransformVector(new Vector3(0, -settings.Radius, 0));

        }
        else
        {
            var box = go.AddComponent<BoxCollider>();

            box.size = new Vector3(settings.Width, settings.Depth, length);

            if (settings.IsEdge)
            {
                var inset_direction = GetInsetDirection(settings, go.transform);
                var inset_distance = settings.Width / 2f;

                go.transform.localPosition += inset_direction * inset_distance;
                go.transform.localPosition += go.transform.InverseTransformVector(new Vector3(0, -(settings.Depth / 2f), 0));
            }
        }
        
        return go.GetComponent<Collider>();
    }

    private Vector3 GetInsetDirection(ColliderGenerationSettings settings, Transform collider_transform)
    {
        var root = collider_transform.parent;

        if (root.parent != null && root.parent.GetComponent<GrindSurface>() != null)
            root = root.parent;
        
        if (settings.AutoDetectEdgeAlignment)
        {
            var ray = new Ray(collider_transform.position + collider_transform.right * settings.Width + root.up, -root.up);

            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, settings.LayerMask) == false || hit.transform.IsChildOf(transform.parent))
            {
                return collider_transform.right;
            }

            return -collider_transform.right;
        }
            
        return settings.FlipEdge ? -collider_transform.right : collider_transform.right;
    }

#endif

}
