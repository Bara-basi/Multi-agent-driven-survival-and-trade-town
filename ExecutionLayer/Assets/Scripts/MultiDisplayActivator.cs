using UnityEngine;

public class MultiDisplayActivator : MonoBehaviour
{
    [SerializeField] [Range(0, 7)] private int extraDisplaysToActivate = 4;
    [SerializeField] [Range(0, 7)] private int ensureDisplayIndex = 4; // ensure Display5 is available

    private void Start()
    {
        int requestedMax = Mathf.Max(extraDisplaysToActivate, ensureDisplayIndex);
        int maxDisplayIndex = Mathf.Min(Display.displays.Length - 1, requestedMax);
        for (int i = 1; i <= maxDisplayIndex; i++)
        {
            Display.displays[i].Activate();
        }

        RouteAgentDisplayObjectsAwayFromShopDisplay();
    }

    private static void RouteAgentDisplayObjectsAwayFromShopDisplay()
    {
        var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            var camera = cameras[i];
            if (camera == null || camera.targetDisplay != 0 || IsShopDisplayCamera(camera))
            {
                continue;
            }

            int targetDisplay = ResolveAgentDisplay(camera.transform);
            if (targetDisplay > 0)
            {
                camera.targetDisplay = targetDisplay;
            }
        }

        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            var canvas = canvases[i];
            if (canvas == null || canvas.targetDisplay != 0 || IsShopDisplayCanvas(canvas))
            {
                continue;
            }

            int targetDisplay = ResolveAgentDisplay(canvas.worldCamera != null ? canvas.worldCamera.transform : canvas.transform);
            if (targetDisplay > 0)
            {
                canvas.targetDisplay = targetDisplay;
            }
        }
    }

    private static bool IsShopDisplayCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return false;
        }

        string name = canvas.name;
        if (!string.IsNullOrEmpty(name)
            && (name.Contains("ShopAssistant")
                || name.Contains("GameEnd")
                || name.Contains("RoundStart")
                || name.Contains("RoundEnd")))
        {
            return true;
        }

        return IsShopDisplayCamera(canvas.worldCamera);
    }

    private static bool IsShopDisplayCamera(Camera camera)
    {
        return camera != null && camera.enabled && camera.targetDisplay == 0;
    }

    private static int ResolveAgentDisplay(Transform source)
    {
        var agentRoot = FindAgentRoot(source);
        if (agentRoot == null)
        {
            return -1;
        }

        var cameras = agentRoot.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            var camera = cameras[i];
            if (camera != null && camera.enabled && camera.targetDisplay > 0)
            {
                return camera.targetDisplay;
            }
        }

        return -1;
    }

    private static Transform FindAgentRoot(Transform source)
    {
        for (var current = source; current != null; current = current.parent)
        {
            if (current.name.StartsWith("Agent_"))
            {
                return current;
            }
        }

        return null;
    }
}
