using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class GrindSplineGenerator
{
    private static List<Vector3> vertices = new List<Vector3>();
    private static readonly List<int> vertexScores = new List<int>();
    private static readonly List<Vector3> endPoints = new List<Vector3>();
    private static readonly List<Vector3> searchList = new List<Vector3>();
    private static readonly List<Vector3> blockedPoints = new List<Vector3>();
    private static readonly List<Vector3> activeSplinePoints = new List<Vector3>();
    private static readonly List<Vector3> searchBuffer = new List<Vector3>();

    public static float PointTestOffset = 0.1f;
    public static float PointTestRadius = 0.05f;
    public static float MaxHorizontalAngle = 15f;
    public static float MaxSlope = 60f;
    public static float MinVertexDistance = 0.05f;
    public static bool SkipExternalCollisionChecks = true;

    public static bool DrawDebug;

    private static ColliderGenerationSettings settings;

    public static void Generate(GrindSurface surface, ColliderGenerationSettings collider_generation_settings = null)
    {
        IEnumerator routine()
        {
            var original_rotation = surface.transform.rotation;

            surface.transform.rotation = Quaternion.identity;

            yield return null;

            settings = collider_generation_settings;

            // build a list of vertexes from child objects that are valid potential grindable surfaces
            // do a set offset sphere checks to see if we have open space in any cardinal directions

            vertices.Clear();
            vertexScores.Clear();
            endPoints.Clear();
            blockedPoints.Clear();
            activeSplinePoints.Clear();

            foreach (var m in surface.GetComponentsInChildren<MeshFilter>())
            {
                for (int i = 0; i < m.sharedMesh.vertexCount; i++)
                {
                    if (IsValidPotentialVertex(surface.transform, m, i, out var w, out var score))
                    {
                        if (vertices.Contains(w) == false)
                        {
                            vertices.Add(w);
                            vertexScores.Add(score);
                        }
                    }
                }
            }

            vertices = vertices.OrderByDescending(v => vertexScores[vertices.IndexOf(v)]).ToList();

            Debug.Log($"Found {vertices.Count} potential valid vertices in child meshes.");

            // start a grind spline by picking a valid vertex
            // find the nearest valid vert and add that, repeat until there are no valid verts left

            var start = vertices[0];
            var active_spline = CreateSpline(surface, start, collider_generation_settings);
            var current_point = start;
            var current_index = 0;

            vertices.RemoveAt(0);

            endPoints.Add(start);
        
            AddSplinePoint(active_spline, start);
        
            while (active_spline != null)
            {
                RefreshSearchList();

                // if ran out of verts to use, we're done

                if (searchList.Count == 0 || (searchList.Count == 1 && searchList[0] == current_point))
                {
                    break;
                }

                // find nearest vert for our next spline point

                var previous_point = current_index > 0 ? active_spline.PointsContainer.GetChild(current_index - 1) : null;

                if (TryGetNextValidPoint(surface, out var next_point, current_point, previous_point?.position))
                {
                    if (vertices.Contains(next_point))
                        vertices.Remove(next_point);

                    AddSplinePoint(active_spline, next_point);

                    current_point = next_point;
                    current_index++;
                }

                // if we failed to find a valid next point, but still have verts left, lets create a new spline

                else
                {
                    var last = active_spline.PointsContainer.GetChild(active_spline.PointsContainer.childCount - 1).position;

                    if (endPoints.Contains(last) == false)
                        endPoints.Add(last);
                    else
                        blockedPoints.Add(last);

                    activeSplinePoints.Clear();

                    if (CanFindValidStartPoint(surface, out var pt))
                    {
                        current_point = pt;

                        if (vertices.Contains(current_point))
                            vertices.Remove(current_point);

                        if (endPoints.Contains(current_point) == false)
                            endPoints.Add(current_point);
                        else
                            blockedPoints.Add(current_point);

                        current_index = 0;

                        active_spline = CreateSpline(surface, current_point, collider_generation_settings);

                        AddSplinePoint(active_spline, current_point);
                    }
                    else
                    {
                        active_spline = null;
                    }
                }
            }
        
            // remove any invalid splines lingering around for whatever reason

            var buffer = surface.Splines.ToArray();

            foreach (var s in buffer)
            {
                if (s.PointsContainer.childCount <= 1)
                {
                    surface.Splines.Remove(s);

                    Object.DestroyImmediate(s.gameObject);
                }
            }

            foreach (var spline in surface.Splines)
            {
                spline.GenerateColliders(settings);
            }
            
            yield return null;

            surface.transform.rotation = original_rotation;
        }
        
        EditorCoroutineUtility.StartCoroutineOwnerless(routine());
    }

    private static bool TryGetNextValidPoint(GrindSurface surface, out Vector3 best, Vector3 reference_point, Vector3? previous_point)
    {
        searchBuffer.Clear();

        foreach (var other in searchList)
        {
            if (other == reference_point)
                continue;

            // if any spline contains both p and other, then p is not valid

            var both_points_in_spline = false;

            foreach (var spline in surface.Splines)
            {
                if (spline.ContainsPosition(reference_point) && spline.ContainsPosition(other))
                {
                    both_points_in_spline = true;
                    break;
                }
            }

            if (both_points_in_spline)
                continue;

            if (CheckVerticalAngle(reference_point, other) == false)
                continue;

            if (previous_point.HasValue && CheckHorizontalAngle(previous_point.Value, reference_point, other) == false)
                continue;

            if (CheckMidPoint(reference_point, other) == false)
                continue;

            searchBuffer.Add(other);
        }

        if (searchBuffer.Contains(reference_point))
            searchBuffer.Remove(reference_point);

        best = Vector3.zero;

        if (searchBuffer.Count == 0)
        {
            return false;
        }

        var best_distance = Mathf.Infinity;

        for (var i = 0; i < searchBuffer.Count; i++)
        {
            var p = searchBuffer[i];
            var d = Vector3.Distance(p, reference_point);

            if (d < best_distance)
            {
                best = p;
                best_distance = d;
            }
        }

        return true;
    }

    private static bool CanFindValidStartPoint(GrindSurface surface, out Vector3 pt)
    {
        // points are valid if
        // - in vertices list (thus un-used by any existing splines)
        // - or in end points
        // AND
        // - not blocked (e.g. used by 2 existing splines)
        // - not in active spline
        
        // additionally, a point is only valid if we can construct a new, unique spline using other valid point
        // so we filter each potential point from the search list
        // checking that there are no existing splines which contain that point and any other point in the searach list

        RefreshSearchList();

        var filter = new List<Vector3>();

        if (searchList.Count > 1)
        {
            foreach (var p in searchList)
            {
                foreach (var other in searchList)
                {
                    if (other == p)
                        continue;

                    // if any spline contains both p and other, then p is not valid

                    var both_points_in_spline = false;

                    foreach (var spline in surface.Splines)
                    {
                        if (spline.ContainsPosition(p) && spline.ContainsPosition(other))
                        {
                            both_points_in_spline = true;
                            break;
                        }
                    }

                    if (both_points_in_spline)
                        continue;

                    if (filter.Contains(p) == false)
                    {
                        filter.Add(p);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        if (filter.Count > 0)
        {
            pt = filter[0];
            return true;
        }

        pt = default;
        return false;
    }

    private static bool ContainsPosition(this GrindSpline spline, Vector3 world_point)
    {
        var points = spline.PointsContainer.GetComponentsInChildren<Transform>();
        return points.Any(p => p.position == world_point);
    }

    private static void RefreshSearchList()
    {
        searchList.Clear();
        searchList.AddRange(vertices.Where(CheckPointDistance));
        searchList.AddRange(endPoints.Where(CheckPointDistance));

        foreach (var b in blockedPoints)
            searchList.Remove(b);
        
        foreach (var p in activeSplinePoints)
            searchList.Remove(p);
    }

    private static bool CheckPointDistance(Vector3 point)
    {
        foreach (var p in blockedPoints)
        {
            var d = Vector3.Distance(point, p);
            if (d <= MinVertexDistance)
            {
                return false;
            }
        }

        return true;
    }

    private static bool CheckMidPoint(Vector3 current, Vector3 next)
    {
        var m = Vector3.Lerp(current, next, 0.5f);

        if (Physics.CheckBox(m, Vector3.one * PointTestRadius))
        {
            return true;
        }

        return false;
    }
    
    private static bool CheckHorizontalAngle(Vector3 previous, Vector3 current, Vector3 next)
    {
        next.y = 0;
        current.y = 0;
        previous.y = 0;

        var flat_dir = next - current;
        var prev_dir = current - previous;
        var h_angle = Vector3.Angle(flat_dir, prev_dir);

        return h_angle <= MaxHorizontalAngle;
    }

    private static bool CheckVerticalAngle(Vector3 current, Vector3 next)
    {
        var dir = next - current;

        next.y = 0;
        current.y = 0;

        var flat_dir = next - current;
        var v_angle = Vector3.Angle(dir, flat_dir);

        return v_angle <= MaxSlope;
    }

    private static void AddSplinePoint(GrindSpline spline, Vector3 world_position)
    {
        var p = spline.PointsContainer;
        var n = p.childCount;
        var go = new GameObject($"Point ({n + 1})");

        go.transform.position = world_position;
        go.transform.SetParent(p);

        activeSplinePoints.Add(world_position);
    }

    private static GrindSpline CreateSpline(GrindSurface surface, Vector3 world_position, ColliderGenerationSettings collider_generation_settings = null)
    {
        var gs = new GameObject("GrindSpline", typeof(GrindSpline)).GetComponent<GrindSpline>();

        if (collider_generation_settings != null)
        {
            gs.ColliderGenerationSettings.ColliderType = collider_generation_settings.ColliderType;
            gs.ColliderGenerationSettings.Radius = collider_generation_settings.Radius;
            gs.ColliderGenerationSettings.Width = collider_generation_settings.Width;
            gs.ColliderGenerationSettings.Depth = collider_generation_settings.Depth;
            gs.ColliderGenerationSettings.IsEdge = collider_generation_settings.IsEdge;
            gs.ColliderGenerationSettings.AutoDetectEdgeAlignment = collider_generation_settings.AutoDetectEdgeAlignment;
            gs.ColliderGenerationSettings.FlipEdge = collider_generation_settings.FlipEdge;
        }

        gs.SurfaceType = surface.SurfaceType;
        gs.IsRound = surface.IsRound;

        gs.transform.position = world_position;

        gs.PointsContainer = new GameObject("Points").transform;
        gs.PointsContainer.SetParent(gs.transform);
        gs.PointsContainer.localPosition = Vector3.zero;

        gs.transform.SetParent(surface.transform);

        surface.Splines.Add(gs);
        
        return gs;
    }

    private static bool IsValidPotentialVertex(Transform root, MeshFilter mesh_filter, int vertex_index, out Vector3 world_pos, out int score)
    {
        var mesh = mesh_filter.sharedMesh;

        var v = mesh_filter.transform.TransformPoint(mesh.vertices[vertex_index]);

        world_pos = v;
        score = 0;

        bool test_dir(Vector3 dir)
        {
            var t = v + (dir * PointTestOffset);

            if (SkipExternalCollisionChecks)
            {
                var cols = Physics.OverlapBox(t, Vector3.one * PointTestRadius);
                foreach (var c in cols)
                {
                    if (c.transform.IsChildOf(root))
                    {
                        if (DrawDebug)
                            Debug.DrawLine(v, t, Color.red, 0.2f);

                        return false;
                    }
                }
            }
            else
            {
                if (Physics.CheckBox(t, Vector3.one * PointTestRadius))
                {
                    if (DrawDebug)
                        Debug.DrawLine(v, t, Color.red, 0.2f);

                    return false;
                }
            }

            if (DrawDebug)
                Debug.DrawLine(v, t, Color.green, 0.2f);

            return true;
        }

        if (test_dir(Vector3.up))
        {
            var passed = false;

            for (int i = 0; i < 8; i++)
            {
                var t = i / 8f;

                var r = Quaternion.AngleAxis(t * 360f, Vector3.up) * Vector3.forward;

                if (test_dir(r) && passed == false)
                {
                    passed = true;
                    score++;
                }
            }

            return passed;
        }

        return false;
    }
}