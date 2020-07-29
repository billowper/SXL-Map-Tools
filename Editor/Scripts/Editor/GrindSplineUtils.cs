using UnityEditor;
using UnityEngine;

public static class GrindSplineUtils
{
    [MenuItem("GameObject/Create Other/Grind Spline")]   
    private static void CreateGrindSpline()
    {
        CreateObjectWithComponent<GrindSpline>("GrindSpline");
    }

    [MenuItem("GameObject/Create Other/Grind Surface")]   
    private static void CreateGrindSurface()
    {
        CreateObjectWithComponent<GrindSurface>("GrindSurface");
    }

    [MenuItem("SXL/Add GrindSurface")]   
    private static void AddGrindSurface()
    {
        foreach (var o in Selection.gameObjects)
        {
            var surface = o.GetComponent<GrindSurface>();
            if (surface == null)
                surface = o.AddComponent<GrindSurface>();
            else
                surface.DestroySplines();

            GrindSplineGenerator.Generate(surface, new ColliderGenerationSettings()
            {
                IsEdge = EditorPrefs.GetBool("gsDefault_IsEdge"),
                AutoDetectEdgeAlignment = EditorPrefs.GetBool("gsDefault_AutoDetectEdgeAlignment"),
                ColliderType = (ColliderGenerationSettings.ColliderTypes) EditorPrefs.GetInt("gsDefault_ColliderType")
            });
        }
    }

    [MenuItem("SXL/Add GrindSurface %g", true)]   
    private static bool AddGrindSurface_Validator()
    {
        return Selection.gameObjects.Length > 0;
    }

    private static void CreateObjectWithComponent<T>(string name)
    {
        var go = new GameObject(name, typeof(T));

        var ray = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f));
        if (Physics.Raycast(ray, out var hit))
        {
            go.transform.position = hit.point + Vector3.up * 2.5f;
        }

        Selection.activeGameObject = go;
    }

    public static void AddPoint(GrindSpline spline)
    {
        var p = spline.PointsContainer;
        var n = p.childCount;
        var go = new GameObject($"Point ({n + 1})");

        var pos = n > 0 ? p.GetChild(p.childCount - 1).localPosition : Vector3.zero;
        
        go.transform.SetParent(p);
        go.transform.localPosition = pos + (p.childCount > 1 ? p.InverseTransformVector(Vector3.forward) : Vector3.zero);

        Undo.RegisterCreatedObjectUndo(go, "GrindSpline.AddPoint");
    }

    public static GameObject AddPoint(GrindSpline spline, Vector3 position)
    {
        var p = spline.PointsContainer;
        var n = p.childCount;
        var go = new GameObject($"Point ({n + 1})");

        go.transform.position = position;
        go.transform.SetParent(p);

        Undo.RegisterCreatedObjectUndo(go, "GrindSpline.AddPoint");

        return go;
    }

    public static bool SnapToSurfaceAtCursor(out Vector3 position, Transform parent = null)
    {
        var mouse_pos = Event.current != null ? Event.current.mousePosition : Vector2.zero;
        mouse_pos.y = SceneView.lastActiveSceneView.camera.pixelHeight - mouse_pos.y;
        var ray = SceneView.lastActiveSceneView.camera.ScreenPointToRay(mouse_pos);

        if (Physics.Raycast(ray, out var hit))
        {
            position = hit.point;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    public static bool SnapToVertexAtCursor(out Vector3 position, Transform parent = null)
    {
        var mouse_pos = Event.current != null ? Event.current.mousePosition : Vector2.zero;
        mouse_pos.y = SceneView.lastActiveSceneView.camera.pixelHeight - mouse_pos.y;
        var ray = SceneView.lastActiveSceneView.camera.ScreenPointToRay(mouse_pos);

        if (Physics.Raycast(ray, out var hit))
        {
            var mesh = hit.transform?.GetComponent<MeshFilter>() ?? hit.transform?.GetComponentInParent<MeshFilter>();
            if (mesh != null)
            {
                if (parent != null)
                {
                    if (mesh.transform.IsChildOf(parent) || mesh.transform == parent)
                    {
                        position = GetNearestVertex(mesh, hit.point);
                        return true;
                    }
                }
                else
                {
                    position = GetNearestVertex(mesh, hit.point);
                    return true;
                }
            }
        }

        position = Vector3.zero;
        return false;
    }
    
    private static Vector3 GetNearestVertex(MeshFilter mesh, Vector3 reference_point)
    {
        var best = Vector3.zero;
        var best_distance = Mathf.Infinity;

        foreach (var p in mesh.sharedMesh.vertices)
        {
            var w = mesh.transform.TransformPoint(p);
            var d = Vector3.Distance(w, reference_point);

            if (d < best_distance)
            {
                best = w;
                best_distance = d;
            }
        }

        return best;
    }
}