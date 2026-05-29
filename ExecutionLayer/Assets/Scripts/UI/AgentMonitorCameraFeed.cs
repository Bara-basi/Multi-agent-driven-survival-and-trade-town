using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class AgentMonitorCameraFeed : MonoBehaviour
{
    private const int AgentCount = 4;

    private readonly Camera[] agentCameras = new Camera[AgentCount];
    private readonly RenderTexture[] renderTextures = new RenderTexture[AgentCount];
    private readonly Dictionary<Camera, RenderTexture> originalTargets = new();

    private RawImage targetImage;
    private int textureWidth = 640;
    private int textureHeight = 488;
    private int selectedAgentIndex;
    private float retryTimer;

    public void Bind(RawImage image, int width, int height)
    {
        targetImage = image;
        textureWidth = Mathf.Max(64, width);
        textureHeight = Mathf.Max(64, height);
        RefreshBindings(force: true);
        SetSelectedAgent(selectedAgentIndex);
    }

    public void SetSelectedAgent(int index)
    {
        selectedAgentIndex = Mathf.Clamp(index, 0, AgentCount - 1);
        if (targetImage == null)
        {
            return;
        }

        var texture = renderTextures[selectedAgentIndex];
        targetImage.texture = texture;
        targetImage.enabled = texture != null;
    }

    private void LateUpdate()
    {
        if (AllCamerasBound())
        {
            return;
        }

        retryTimer += Time.unscaledDeltaTime;
        if (retryTimer < 1f)
        {
            return;
        }

        retryTimer = 0f;
        RefreshBindings(force: false);
        SetSelectedAgent(selectedAgentIndex);
    }

    private void RefreshBindings(bool force)
    {
        for (int i = 0; i < AgentCount; i++)
        {
            if (!force && agentCameras[i] != null)
            {
                continue;
            }

            var camera = ResolveAgentCamera(i);
            if (camera == null)
            {
                continue;
            }

            agentCameras[i] = camera;
            if (!originalTargets.ContainsKey(camera))
            {
                originalTargets[camera] = camera.targetTexture;
            }

            var rt = EnsureRenderTexture(i);
            camera.targetTexture = rt;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.enabled = true;
        }
    }

    private RenderTexture EnsureRenderTexture(int index)
    {
        var rt = renderTextures[index];
        if (rt != null && rt.width == textureWidth && rt.height == textureHeight)
        {
            return rt;
        }

        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
        }

        rt = new RenderTexture(textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32)
        {
            name = $"AgentMonitor_RT_{index + 1}",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
        };
        rt.Create();
        renderTextures[index] = rt;
        return rt;
    }

    private static Camera ResolveAgentCamera(int agentIndex)
    {
        string agentName = $"Agent_{agentIndex + 1:00}";
        var root = FindNamedTransform(agentName);
        if (root != null)
        {
            var camera = BestCamera(root.GetComponentsInChildren<Camera>(true));
            if (camera != null)
            {
                return camera;
            }
        }

        int displayIndex = agentIndex + 1;
        var allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Camera fallback = null;
        for (int i = 0; i < allCameras.Length; i++)
        {
            var camera = allCameras[i];
            if (camera == null || camera.targetDisplay != displayIndex)
            {
                continue;
            }

            if (camera.enabled)
            {
                return camera;
            }

            fallback ??= camera;
        }

        return fallback;
    }

    private static Transform FindNamedTransform(string objectName)
    {
        var allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            var t = allTransforms[i];
            if (t != null && string.Equals(t.name, objectName, StringComparison.Ordinal))
            {
                return t;
            }
        }

        return null;
    }

    private static Camera BestCamera(Camera[] cameras)
    {
        Camera fallback = null;
        for (int i = 0; i < cameras.Length; i++)
        {
            var camera = cameras[i];
            if (camera == null)
            {
                continue;
            }

            fallback ??= camera;
            if (camera.enabled && camera.targetDisplay > 0)
            {
                return camera;
            }
        }

        return fallback;
    }

    private bool AllCamerasBound()
    {
        for (int i = 0; i < AgentCount; i++)
        {
            if (agentCameras[i] == null || renderTextures[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void OnDestroy()
    {
        foreach (var pair in originalTargets)
        {
            if (pair.Key != null)
            {
                pair.Key.targetTexture = pair.Value;
            }
        }

        for (int i = 0; i < renderTextures.Length; i++)
        {
            if (renderTextures[i] == null)
            {
                continue;
            }

            renderTextures[i].Release();
            Destroy(renderTextures[i]);
            renderTextures[i] = null;
        }
    }
}
