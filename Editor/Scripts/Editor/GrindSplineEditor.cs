using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(GrindSpline))]
public class GrindSplineEditor : Editor
{
    [SerializeField] private bool showPoints;

    public override void OnInspectorGUI()
    {
        serializedObject.UpdateIfRequiredOrScript();

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginVertical(new GUIStyle("box"));
        {
            EditorGUILayout.LabelField("Grind Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SurfaceType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IsRound"));

            EditorGUILayout.HelpBox("These are used by the map importer to determine what kind of grind this is", MessageType.Info, true);
        }
        EditorGUILayout.EndVertical();

        if (targets.Length == 1)
        {
            EditorGUILayout.BeginVertical(new GUIStyle("box"));
            {
                EditorGUILayout.LabelField("Spline Tools", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Add Point"))
                {
                    GrindSplineUtils.AddPoint(grindSpline);
                }

                drawPoints = GUILayout.Toggle(drawPoints, "Draw Points", new GUIStyle("button"));

                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Rename Points"))
                {
                    foreach (Transform x in grindSpline.PointsContainer)
                    {
                        x.gameObject.name = $"Point ({x.GetSiblingIndex() + 1})";
                    }
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("PointsContainer"));

                EditorGUI.indentLevel++;

                if (grindSpline.PointsContainer != null)
                {
                    showPoints = EditorGUILayout.Foldout(showPoints, $"Points ({grindSpline.PointsContainer.childCount})");
                    if (showPoints)
                    {
                        foreach (Transform child in grindSpline.PointsContainer)
                        {
                            EditorGUILayout.ObjectField(child, typeof(Transform), true);
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginVertical(new GUIStyle("box"));
        {
            EditorGUILayout.LabelField("Colliders ", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ColliderContainer"));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ColliderGenerationSettings"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("GeneratedColliders"), true);
            EditorGUI.indentLevel--;

            if (GUILayout.Button("Generate Colliders"))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Are you sure? This cannot be undone", "Yes", "No!"))
                {
                    foreach (var o in targets)
                    {
                        var t = (GrindSpline) o;

                        t.GenerateColliders();
                    }

                    serializedObject.UpdateIfRequiredOrScript();
                }
            }
        }
        EditorGUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    private bool drawPoints;
    private Vector3 pointPosition;
    private bool vertexSnap;

    private GrindSpline grindSpline => ((GrindSpline) target);

    private void OnSceneGUI()
    {
        grindSpline.DrawingActive = drawPoints;

        if (drawPoints)
        {
            Tools.current = Tool.None;

            SplineDrawingShared.OnSceneGUI_SplineDrawingCommon(
                editor: this, 
                grindSpline: grindSpline, 
                lmbLabel: "Shift Click : Add Point", 
                vertexSnap: ref vertexSnap, 
                pointPosition: ref pointPosition);

            if (Event.current == null)
                return;

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && Event.current.modifiers.HasFlag(EventModifiers.Shift))
            {
                GrindSplineUtils.AddPoint(grindSpline, pointPosition);
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
            {
                drawPoints = false;
                Repaint();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                drawPoints = false;
                Repaint();
            }
        }
    }
}

