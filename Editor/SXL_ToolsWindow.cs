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

    private int settings_MaxVertices;
    private float settings_CollisionTestOffset;
    private float settings_CollisionTestRadius;
    private float settings_MaxHorizontalAngle;
    private float settings_MaxSlope;
    private float settings_MinVertexDistance;
    private bool settings_SkipExternalCollisionChecks;
    private bool settings_AutoUpdateColliders;
    private bool settings_AutoGenerateNewSurfaces;

    private void OnEnable()
    {
        containerStyle = new GUIStyle() {padding = new RectOffset(10, 10, 10, 10)};

        UseVersionNumbering = EditorPrefs.GetBool("SXL_UseVersionNumbering", true);

	    settings_MaxVertices = EditorPrefs.GetInt(nameof(settings_MaxVertices), GrindSplineGenerator.MaxVertices);
        settings_CollisionTestOffset = EditorPrefs.GetFloat(nameof(settings_CollisionTestOffset), GrindSplineGenerator.CollisionTestOffset);
        settings_CollisionTestRadius = EditorPrefs.GetFloat(nameof(settings_CollisionTestRadius), GrindSplineGenerator.CollisionTestRadius);
        settings_MaxHorizontalAngle = EditorPrefs.GetFloat(nameof(settings_MaxHorizontalAngle), GrindSplineGenerator.MaxHorizontalAngle);
        settings_MaxSlope = EditorPrefs.GetFloat(nameof(settings_MaxSlope), GrindSplineGenerator.MaxSlope);
        settings_MinVertexDistance = EditorPrefs.GetFloat(nameof(settings_MinVertexDistance), GrindSplineGenerator.MinVertexDistance);
        settings_SkipExternalCollisionChecks = EditorPrefs.GetBool(nameof(settings_SkipExternalCollisionChecks), GrindSplineGenerator.SkipExternalCollisionChecks);
        settings_AutoUpdateColliders = EditorPrefs.GetBool(nameof(settings_AutoUpdateColliders), GrindSpline.AutoUpdateColliders);
	    settings_AutoGenerateNewSurfaces = EditorPrefs.GetBool(nameof(settings_AutoGenerateNewSurfaces), GrindSurface.AutoGenerateNewSuraces);
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

					    settings_AutoGenerateNewSurfaces = EditorGUILayout.Toggle(new GUIContent("AutoGenerateNewSurfaces", "If true, GrindSurfaces created via the SXL menu (Ctrl + G) will automatically run spline generation"), settings_AutoGenerateNewSurfaces);
					    settings_AutoUpdateColliders = EditorGUILayout.Toggle(new GUIContent("AutoUpdateColliders", "If true, colliders will regenerate as you adjust splines"), settings_AutoUpdateColliders);

					    if (EditorGUI.EndChangeCheck())
					    {
						    GrindSpline.AutoUpdateColliders = settings_AutoUpdateColliders;
						    GrindSurface.AutoGenerateNewSuraces = settings_AutoGenerateNewSurfaces;
						    EditorPrefs.SetBool(nameof(settings_AutoUpdateColliders), settings_AutoUpdateColliders);
						    EditorPrefs.SetBool(nameof(settings_AutoGenerateNewSurfaces), settings_AutoGenerateNewSurfaces);
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
					    EditorGUILayout.HelpBox("These settings are used to test the suitability of a vertex for grind point placement", MessageType.Info);

					    settings_MaxVertices = EditorGUILayout.IntField(new GUIContent("MaxVertices", "Maxiumum number of vertices that we will collect & test for spline point creation"), settings_MaxVertices);
					    settings_MaxHorizontalAngle = EditorGUILayout.FloatField(new GUIContent("MaxHorizontalAngle", "The maximum horizontal angle between two generated spline points"), settings_MaxHorizontalAngle);
					    settings_MaxSlope = EditorGUILayout.FloatField(new GUIContent("MaxSlope", "The maximum vertical angle between two generated spline points"), settings_MaxSlope);
					    settings_MinVertexDistance = EditorGUILayout.FloatField(new GUIContent("MinVertexDistance", "Minimum distance in units that a vertex must be from any other vertex in a generated spline"), settings_MinVertexDistance);
					    settings_SkipExternalCollisionChecks = EditorGUILayout.Toggle(new GUIContent("SkipExternalCollisionChecks", "When attempting to generate spline points, we test each vertex position to ensure it has some open space around it. If this toggle is enabled, those tests will only run against colliders that are children of the same GrindSurface we are generating splines for, otherwise the tests will look at ALL colliders."), settings_SkipExternalCollisionChecks);
						settings_CollisionTestOffset = EditorGUILayout.FloatField(new GUIContent("CollisionTestOffset", "Distance from the collision check point that we will perform the collision check (to avoid collision check hitting underlying ledge geometry, etc)"), settings_CollisionTestOffset);
					    settings_CollisionTestRadius = EditorGUILayout.FloatField(new GUIContent("CollisionTestRadius", "Size of the collision box check"), settings_CollisionTestRadius);

					    if (EditorGUI.EndChangeCheck())
					    {
						    EditorPrefs.SetInt(nameof(settings_MaxVertices), settings_MaxVertices);
						    EditorPrefs.SetFloat(nameof(settings_MaxHorizontalAngle), settings_MaxHorizontalAngle);
						    EditorPrefs.SetFloat(nameof(settings_MaxSlope), settings_MaxSlope);
						    EditorPrefs.SetFloat(nameof(settings_MinVertexDistance), settings_MinVertexDistance);
						    EditorPrefs.SetBool(nameof(settings_SkipExternalCollisionChecks), settings_SkipExternalCollisionChecks);
						    EditorPrefs.SetFloat(nameof(settings_CollisionTestOffset), settings_CollisionTestOffset);
						    EditorPrefs.SetFloat(nameof(settings_CollisionTestRadius), settings_CollisionTestRadius);
							
						    GrindSplineGenerator.MaxVertices = settings_MaxVertices;
						    GrindSplineGenerator.MaxHorizontalAngle = settings_MaxHorizontalAngle;
						    GrindSplineGenerator.MaxSlope = settings_MaxSlope;
						    GrindSplineGenerator.MinVertexDistance = settings_MinVertexDistance;
						    GrindSplineGenerator.SkipExternalCollisionChecks = settings_SkipExternalCollisionChecks;
						    GrindSplineGenerator.CollisionTestOffset = settings_CollisionTestOffset;
						    GrindSplineGenerator.CollisionTestRadius = settings_CollisionTestRadius;
					    }
				    }
			    }
		    }
	    }
    }
}
