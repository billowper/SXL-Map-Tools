using System;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// One-click export of the active scene to a unique asset bundle, with automatic version numbering
/// Asset bundle is exporter to the project root folder and copied into the Skater XL maps folder in MyDocuments
/// </summary>
public static class ExportMapTool
{
    private const string ASSET_BUNDLES_BUILD_PATH = "AssetBundles";

    [MenuItem("SXL/Quick Map Export")]
    public static void ExportMap()
    {
        ExportMap(null, EditorPrefs.GetBool("SXL_UseVersionNumbering", true));
    }

    public static Action<Scene> OnPreExport;

    public static void ExportMap(string override_asset_bundle_name, bool use_version_numbering, bool run_game_after_export = false)
    {
        IEnumerator routine()
        {
            var scene = SceneManager.GetActiveScene();

            var start_time = DateTime.Now;

            EditorSceneManager.SaveScene(scene);

            yield return ProcessScene(scene);

            var bundle_name = scene.name;

            if (use_version_numbering)
            {
                var version = EditorPrefs.GetInt($"{scene.name}_version", 1);

                version++;

                EditorPrefs.SetInt($"{scene.name}_version", version);

                bundle_name = $"{scene.name} v{version}";
            }

            if (string.IsNullOrEmpty(override_asset_bundle_name) == false)
            {
                bundle_name = override_asset_bundle_name;
            }

            var build = new AssetBundleBuild
            {
                assetBundleName = bundle_name,
                assetNames = new[] {scene.path}
            };

            if (!Directory.Exists(ASSET_BUNDLES_BUILD_PATH))
                Directory.CreateDirectory(ASSET_BUNDLES_BUILD_PATH);

            BuildPipeline.BuildAssetBundles(ASSET_BUNDLES_BUILD_PATH, new []{ build }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);

            var time_taken = start_time - DateTime.Now;

            Debug.Log($"BuildAssetBundles took {time_taken:mm\\:ss}");

            var map_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SkaterXL/Maps");
            var bundle_path = Path.Combine(Application.dataPath.Replace("/Assets", "/AssetBundles"), build.assetBundleName);
            var dest_path = Path.Combine(map_dir, build.assetBundleName);
      
            Debug.Log($"Copying {bundle_path} to {dest_path}");

            File.Copy(bundle_path, dest_path, overwrite: true);
            File.Delete(bundle_path);

            EditorSceneManager.OpenScene(scene.path);

	        if (run_game_after_export)
	        {
		        Application.OpenURL("steam://run/962730");
	        }
        }

        EditorCoroutineUtility.StartCoroutineOwnerless(routine());
    }

    [MenuItem("SXL/Test Export Process")]
    public static void TestExportSceneProcess()
    {
        if (EditorUtility.DisplayDialog("Are you sure?", "Running this test will delete all GrindSurface and GrindSpline scripts in your scene, and move splines and colliders into an importer-friendly configuration. It is strictly for testing purposes, so do NOT save your scene after running it!", "Yes, I'm sure!", "No"))
        {
            var scene = SceneManager.GetActiveScene();

            if (EditorSceneManager.SaveScene(scene))
            {
                EditorCoroutineUtility.StartCoroutineOwnerless(ProcessScene(scene));
            }
        }
    }

	private static IEnumerator ProcessScene(Scene scene)
    {
        var grind_splines = Object.FindObjectsOfType<GrindSpline>();
        var grind_surfaces = Object.FindObjectsOfType<GrindSurface>();

        var grinds_root = scene.GetRootGameObjects().FirstOrDefault(o => o.name == "Grinds") ?? new GameObject("Grinds");
        
        yield return null;

        for (var i = 0; i < grind_splines.Length; i++)
        {
            var spline = grind_splines[i];
           
            EditorUtility.DisplayProgressBar("Processing Scene for Export", $"{i}/{grind_splines.Length} Splines Processed", (float) i / grind_splines.Length);

            var prefab_root = PrefabUtility.GetOutermostPrefabInstanceRoot(spline);
            if (prefab_root != null)
            {
                PrefabUtility.UnpackPrefabInstance(prefab_root, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            }

            spline.transform.SetParent(grinds_root.transform);

            yield return null;

            // remove points container transform, re-parent points to the spline root

            if (spline.PointsContainer != spline.transform)
            {
                var points = spline.PointsContainer.GetComponentsInChildren<Transform>().Where(t => t != spline.PointsContainer);
                foreach (var p in points)
                {
                    p.SetParent(spline.transform);
                }

                Object.DestroyImmediate(spline.PointsContainer.gameObject);

                yield return null;
            }

            // move colliders out to scene root

            if (spline.ColliderContainer == null || spline.ColliderContainer == spline.transform)
            {
                foreach (var c in spline.GeneratedColliders)
                {
                    c.transform.SetParent(null);
                }
            }
            else
            {
                spline.ColliderContainer.SetParent(null);
            }

            yield return null;
        }

        yield return null;

        // strip components

        foreach (var gs in grind_splines)
        {
            Object.DestroyImmediate(gs);
        }

        foreach (var gs in grind_surfaces)
        {
            Object.DestroyImmediate(gs);
        }

        var missing_scripts = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>().Where(c => c == null)).ToArray();

        if (missing_scripts.Length > 0)
        {
            Debug.Log($"Found {missing_scripts.Length} missing scripts which will be removed.");

            foreach (var s in missing_scripts)
            {
                Object.DestroyImmediate(s);
            }
        }

        EditorUtility.ClearProgressBar();
    }
}
