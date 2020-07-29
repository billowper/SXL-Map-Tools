using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class SXL_ToolsWindow : EditorWindow
{
    [MenuItem("SXL/Tools Window")]
    private static void Init()
    {
        var window = GetWindow<SXL_ToolsWindow>();
        window.titleContent = new GUIContent("SXL Tools");
        window.Show();
    }

    private Vector2 scroll;
    private GUIStyle containerStyle;
    private bool UseVersionNumbering;
    private bool StripComponents;
    private string OverrideAssetBundleName;

    [SerializeField] private bool showSettings;
    [SerializeField] private bool runGameAfterExport;

    private bool gsDefault_IsEdge;
    private bool gsDefault_AutoDetectEdgeAlignment;
    private ColliderGenerationSettings.ColliderTypes gsDefault_ColliderType = ColliderGenerationSettings.ColliderTypes.Box;

    private float settings_PointTestOffset;
    private float settings_PointTestRadius;
    private float settings_MaxHorizontalAngle;
    private float settings_MaxSlope;
    private float settings_MinVertexDistance;
    private bool settings_SkipExternalCollisionChecks;
    private bool settings_AutoUpdateColliders;

    private void OnEnable()
    {
        containerStyle = new GUIStyle() {padding = new RectOffset(10, 10, 10, 10)};

        UseVersionNumbering = EditorPrefs.GetBool("SXL_UseVersionNumbering", true);

        settings_PointTestOffset = EditorPrefs.GetFloat(nameof(settings_PointTestOffset), GrindSplineGenerator.PointTestOffset);
        settings_PointTestRadius = EditorPrefs.GetFloat(nameof(settings_PointTestRadius), GrindSplineGenerator.PointTestRadius);
        settings_MaxHorizontalAngle = EditorPrefs.GetFloat(nameof(settings_MaxHorizontalAngle), GrindSplineGenerator.MaxHorizontalAngle);
        settings_MaxSlope = EditorPrefs.GetFloat(nameof(settings_MaxSlope), GrindSplineGenerator.MaxSlope);
        settings_MinVertexDistance = EditorPrefs.GetFloat(nameof(settings_MinVertexDistance), GrindSplineGenerator.MinVertexDistance);
        settings_SkipExternalCollisionChecks = EditorPrefs.GetBool(nameof(settings_SkipExternalCollisionChecks), GrindSplineGenerator.SkipExternalCollisionChecks);
        settings_AutoUpdateColliders = EditorPrefs.GetBool(nameof(settings_AutoUpdateColliders), GrindSpline.AutoUpdateColliders);
    }
    
    private void OnGUI()
    {
	    var scene = SceneManager.GetActiveScene();

	    using (var sv = new EditorGUILayout.ScrollViewScope(scroll))
	    {
		    scroll = sv.scrollPosition;

		    using (new EditorGUILayout.VerticalScope(containerStyle, GUILayout.Width(position.width)))
		    {
			    using (new EditorGUILayout.VerticalScope(new GUIStyle("box")))
			    {
				    EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

				    EditorGUI.BeginChangeCheck();

				    UseVersionNumbering = EditorGUILayout.Toggle(new GUIContent("Use Version Numbering", "If true, the exported AssetBundle will be appended with an incremental version number, e.g. Example Map v4"), UseVersionNumbering);
				    OverrideAssetBundleName = EditorGUILayout.TextField(new GUIContent("Override Name", "Optionally override the AssetBundle name (by default we just use the scene name)"), OverrideAssetBundleName);

				    if (EditorGUI.EndChangeCheck())
				    {
					    EditorPrefs.SetBool("SXL_UseVersionNumbering", UseVersionNumbering);
				    }

				    if (EditorPrefs.HasKey($"{scene.name}_version"))
				    {
					    EditorGUILayout.LabelField($"Version Number", EditorPrefs.GetInt($"{scene.name}_version", 1).ToString());
				    }

				    runGameAfterExport = EditorGUILayout.Toggle(new GUIContent("Run Game After Export"), runGameAfterExport);

				    if (GUILayout.Button("Export Map"))
				    {
					    ExportMapTool.ExportMap(OverrideAssetBundleName, UseVersionNumbering, runGameAfterExport);
				    }

				    using (new EditorGUILayout.HorizontalScope())
				    {
					    if (GUILayout.Button("Open Maps Folder"))
					    {
						    var map_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SkaterXL/Maps/");

						    EditorUtility.RevealInFinder(map_dir);
					    }

					    if (GUILayout.Button("Run Skater XL"))
					    {
						    Application.OpenURL("steam://run/962730");
					    }
				    }

				    using (new EditorGUILayout.HorizontalScope())
				    {
					    if (GUILayout.Button("Delete Previous Versions"))
					    {
						    if (EditorUtility.DisplayDialog("Are you sure?", $"This will delete all previously exported maps containing the name '{scene.name}'", "Yes", "Cancel"))
						    {
							    var map_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SkaterXL", "Maps");
							    var paths = Directory.GetFiles(map_dir).Where(p => Path.GetFileName(p).StartsWith(scene.name)).ToArray();

							    foreach (var p in paths)
							    {
								    Debug.Log($"Deleting '{p}'");
								    File.Delete(p);
							    }
						    }
					    }

					    if (GUILayout.Button("Reset Version Number"))
					    {
						    if (EditorPrefs.HasKey($"{scene.name}_version"))
						    {
							    EditorPrefs.DeleteKey($"{scene.name}_version");
						    }
					    }
				    }
			    }

			    using (new EditorGUILayout.VerticalScope(new GUIStyle("box")))
			    {
				    showSettings = EditorGUILayout.Foldout(showSettings, "Settings", new GUIStyle("foldout") {fontStyle = FontStyle.Bold});

				    if (showSettings)
				    {
					    EditorGUI.BeginChangeCheck();

					    EditorGUILayout.LabelField("Grind Spline Generation", EditorStyles.boldLabel);

					    EditorGUI.BeginChangeCheck();

					    settings_AutoUpdateColliders = EditorGUILayout.Toggle("AutoUpdateColliders", settings_AutoUpdateColliders);

					    if (EditorGUI.EndChangeCheck())
					    {
						    GrindSpline.AutoUpdateColliders = settings_AutoUpdateColliders;
						    EditorPrefs.SetBool(nameof(settings_AutoUpdateColliders), settings_AutoUpdateColliders);
					    }

					    GrindSplineGenerator.DrawDebug = EditorGUILayout.Toggle("Draw Generation Debug", GrindSplineGenerator.DrawDebug);

					    EditorGUI.BeginChangeCheck();

					    EditorGUILayout.LabelField("Default Surface Settings", EditorStyles.boldLabel);
					    gsDefault_IsEdge = EditorGUILayout.Toggle("Is Edge", gsDefault_IsEdge);
					    gsDefault_AutoDetectEdgeAlignment = EditorGUILayout.Toggle("Auto Edge Alignment", gsDefault_AutoDetectEdgeAlignment);
					    gsDefault_ColliderType = (ColliderGenerationSettings.ColliderTypes) EditorGUILayout.EnumPopup("Collider Type", gsDefault_ColliderType);

					    if (EditorGUI.EndChangeCheck())
					    {
						    EditorPrefs.SetBool(nameof(gsDefault_IsEdge), gsDefault_IsEdge);
						    EditorPrefs.SetBool(nameof(gsDefault_AutoDetectEdgeAlignment), gsDefault_AutoDetectEdgeAlignment);
						    EditorPrefs.SetInt(nameof(gsDefault_ColliderType), (int) gsDefault_ColliderType);
					    }

					    EditorGUI.BeginChangeCheck();

					    EditorGUILayout.Separator();
					    EditorGUILayout.LabelField("Grindable Vertex Settings", EditorStyles.boldLabel);

					    settings_PointTestOffset = EditorGUILayout.FloatField("PointTestOffset", settings_PointTestOffset);
					    settings_PointTestRadius = EditorGUILayout.FloatField("PointTestRadius", settings_PointTestRadius);
					    settings_MaxHorizontalAngle = EditorGUILayout.FloatField("MaxHorizontalAngle", settings_MaxHorizontalAngle);
					    settings_MaxSlope = EditorGUILayout.FloatField("MaxSlope", settings_MaxSlope);
					    settings_MinVertexDistance = EditorGUILayout.FloatField("MinVertexDistance", settings_MinVertexDistance);
					    settings_SkipExternalCollisionChecks = EditorGUILayout.Toggle("SkipExternalCollisionChecks", settings_SkipExternalCollisionChecks);

					    if (EditorGUI.EndChangeCheck())
					    {
						    EditorPrefs.SetFloat(nameof(settings_PointTestOffset), settings_PointTestOffset);
						    EditorPrefs.SetFloat(nameof(settings_PointTestRadius), settings_PointTestRadius);
						    EditorPrefs.SetFloat(nameof(settings_MaxHorizontalAngle), settings_MaxHorizontalAngle);
						    EditorPrefs.SetFloat(nameof(settings_MaxSlope), settings_MaxSlope);

						    GrindSplineGenerator.PointTestOffset = settings_PointTestOffset;
						    GrindSplineGenerator.PointTestRadius = settings_PointTestRadius;
						    GrindSplineGenerator.MaxHorizontalAngle = settings_MaxHorizontalAngle;
						    GrindSplineGenerator.MaxSlope = settings_MaxSlope;
					    }
				    }
			    }
		    }
	    }
    }
}
