using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ScaleReferenceTool
{
    private static GameObject scaleRef;
    private static GameObject scaleRefInstance;

    [MenuItem("SXL/Place Player Scale Reference at Cursor #g")]
    public static void PlaceScaleReference()
    {
        var mouse_pos = Event.current != null ? Event.current.mousePosition : Vector2.zero;
        mouse_pos.y = SceneView.lastActiveSceneView.camera.pixelHeight - mouse_pos.y;
        var ray = SceneView.lastActiveSceneView.camera.ScreenPointToRay(mouse_pos);

        if (scaleRef == null)
        {
            scaleRef = Resources.Load<GameObject>("Player Scale Reference");
        }

        if (scaleRefInstance == null)
        {
            scaleRefInstance = Object.Instantiate(scaleRef);
            scaleRefInstance.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            scaleRefInstance.SetActive(false);
        }

        if (Physics.Raycast(ray, out var hit))
        {
            scaleRefInstance.transform.position = hit.point;
            scaleRefInstance.SetActive(true);
        }
    }
}
