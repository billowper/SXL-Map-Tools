using UnityEditor;
using UnityEngine;

public static class SplineDrawingShared
{
    public static void OnSceneGUI_SplineDrawingCommon(Editor editor, GrindSpline grindSpline, string lmbLabel, ref bool vertexSnap, ref Vector3 pointPosition)
    {
        HandleUtility.AddDefaultControl(editor.GetHashCode());

        Handles.BeginGUI();
        {
            var r = new Rect(10, SceneView.currentDrawingSceneView.camera.pixelHeight - 30 * 3 + 10, 400, 30 * 4);

            GUILayout.BeginArea(r);
            GUILayout.BeginVertical(new GUIStyle("box"));
                
            var label = $"{lmbLabel}\n" +
                        $"V : Toggle Vertex Snap\n" +
                        $"Space : Confirm\n" +
                        $"Escape : Cancel";

            GUILayout.Label($"<color=white>{label}</color>", new GUIStyle("label") {richText = true, fontSize = 14, fontStyle = FontStyle.Bold});
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        Handles.EndGUI();

        if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.V)
        {
            vertexSnap = !vertexSnap;
        }

        if (vertexSnap)
        {
            if (GrindSplineUtils.SnapToVertexAtCursor(out var pos))
            {
                pointPosition = pos;
            }
        }
        else
        {
            if (GrindSplineUtils.SnapToSurfaceAtCursor(out var pos))
            {
                pointPosition = pos;
            }
        }

        HandleUtility.Repaint();

        Handles.color = Color.cyan;

        if (grindSpline != null && grindSpline.PointsContainer.childCount > 0)
        {
            Handles.DrawAAPolyLine(3f, grindSpline.PointsContainer.GetChild(grindSpline.PointsContainer.childCount - 1).position, pointPosition);
        }

        Handles.CircleHandleCap(0, pointPosition, Quaternion.LookRotation(SceneView.currentDrawingSceneView.camera.transform.forward), 0.02f, EventType.Repaint);
    }
}