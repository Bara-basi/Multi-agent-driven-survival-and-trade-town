using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed partial class ShopAssistantDisplayUI
{
    private TextMeshProUGUI CreateTMPText(string name, Transform parent, string content, float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = align;
        txt.color = textColor;
        txt.raycastTarget = false;
        if (uiFont != null)
        {
            txt.font = uiFont;
        }
        return txt;
    }

    private static float CreateTMPTextPreferredHeight(string content, float fontSize, FontStyles style, float width)
    {
        return EstimateTMPTextLineCount(content, fontSize, style, width) * fontSize * 1.36f;
    }

    private static int EstimateTMPTextLineCount(string content, float fontSize, FontStyles style, float width)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 1;
        }

        float charWidth = fontSize * (style.HasFlag(FontStyles.Bold) ? 0.98f : 0.90f);
        int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(width / charWidth));
        int lineCount = 1;
        int currentLineChars = 0;

        foreach (char ch in content)
        {
            if (ch == '\n')
            {
                lineCount++;
                currentLineChars = 0;
                continue;
            }

            currentLineChars++;
            if (currentLineChars > charsPerLine)
            {
                lineCount++;
                currentLineChars = 1;
            }
        }

        return lineCount;
    }


    private static void ApplyInventoryTextWeight(TextMeshProUGUI text)
    {
        ApplyTextFaceDilate(text, 0.10f);
    }

    private static void ApplyTextOutline(TextMeshProUGUI text, Color outlineColor, float outlineWidth, float faceDilate = 0f)
    {
        if (text == null || text.fontMaterial == null)
        {
            return;
        }

        var material = new Material(text.fontMaterial)
        {
            name = $"{text.fontMaterial.name}_Outline"
        };

        if (material.HasProperty(ShaderUtilities.ID_FaceDilate))
        {
            material.SetFloat(ShaderUtilities.ID_FaceDilate, faceDilate);
        }

        if (material.HasProperty(ShaderUtilities.ID_OutlineColor))
        {
            material.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
        }

        if (material.HasProperty(ShaderUtilities.ID_OutlineWidth))
        {
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
        }

        text.fontMaterial = material;
    }

    private static void ApplyUiOutline(TextMeshProUGUI text, Color outlineColor, Vector2 distance)
    {
        if (text == null)
        {
            return;
        }

        var outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = outlineColor;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = false;
    }

    private static void ApplyTextFaceDilate(TextMeshProUGUI text, float dilate)
    {
        if (text == null || text.fontMaterial == null)
        {
            return;
        }

        var material = new Material(text.fontMaterial)
        {
            name = $"{text.fontMaterial.name}_Thick"
        };

        if (material.HasProperty(ShaderUtilities.ID_FaceDilate))
        {
            material.SetFloat(ShaderUtilities.ID_FaceDilate, dilate);
        }

        text.fontMaterial = material;
    }

    private static void SetAnchoredRect(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private RectTransform CreateStatusRow(string name, Transform parent, float height)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        var rt = (RectTransform)row.transform;
        rt.sizeDelta = new Vector2(0f, height);

        var element = row.GetComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;

        row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.28f);
        var line = row.GetComponent<Outline>();
        line.effectColor = new Color(0.55f, 0.39f, 0.12f, 1f);
        line.effectDistance = new Vector2(statusRowLineThickness, -statusRowLineThickness);
        return rt;
    }

    private void StretchText(TextMeshProUGUI text, float horizontalPadding = 10f)
    {
        if (text == null) return;
        var rt = (RectTransform)text.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(horizontalPadding, 0f);
        rt.offsetMax = new Vector2(-horizontalPadding, 0f);
    }

    private void AddFrameBorder(RectTransform parent, float inset, Color borderColor, float thickness)
    {
        var border = new GameObject("FrameBorder", typeof(RectTransform));
        border.transform.SetParent(parent, false);
        border.transform.SetAsFirstSibling();

        var rt = (RectTransform)border.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);

        CreateBorderLine("Top", rt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -thickness), new Vector2(0f, 0f), borderColor);
        CreateBorderLine("Bottom", rt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, thickness), borderColor);
        CreateBorderLine("Left", rt, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(thickness, 0f), borderColor);
        CreateBorderLine("Right", rt, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-thickness, 0f), new Vector2(0f, 0f), borderColor);
    }

    private void CreateBorderLine(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color color)
    {
        var line = new GameObject(name, typeof(RectTransform), typeof(Image));
        line.transform.SetParent(parent, false);

        var rt = (RectTransform)line.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        var image = line.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }


    private Sprite ResolveUiDecorationSprite(string resourcePath, string assetPath, string spriteName)
    {
        if (!string.IsNullOrWhiteSpace(resourcePath))
        {
            var primaryName = string.IsNullOrWhiteSpace(spriteName) ? string.Empty : spriteName.Trim();
            if (!string.IsNullOrEmpty(primaryName))
            {
                var sprite = ResolveSpriteFromResources(resourcePath.Trim(), primaryName, primaryName);
                if (sprite != null)
                {
                    return sprite;
                }
            }
            else
            {
                var sprites = Resources.LoadAll<Sprite>(resourcePath.Trim());
                if (sprites != null && sprites.Length > 0)
                {
                    return sprites[0];
                }
            }
        }

#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(assetPath))
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath.Trim());
            foreach (var asset in assets)
            {
                var s = asset as Sprite;
                if (s == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(spriteName))
                {
                    return s;
                }

                var trimmed = spriteName.Trim();
                if (string.Equals(s.name, trimmed, StringComparison.Ordinal))
                {
                    return s;
                }
            }
        }
#endif

        Debug.LogWarning($"[ShopAssistantUI] UI sprite not found: {assetPath} ({spriteName})");
        return null;
    }

    [Serializable]
    private sealed class MarketInformationPayload
    {
        public MarketItem[] items;
        public MarketPlayer player;
    }

    [Serializable]
    private sealed class MarketPlayer
    {
        public float currentMoney;
        public float todayIncome;
    }

    [Serializable]
    private sealed class AgentInformationPayload
    {
        public AgentInformation[] agents;
    }

    [Serializable]
    private sealed class AgentInformation
    {
        public string actorId;
        public string agentCode;
        public string agentName;
        public string name;
        public float hungerValue;
        public float fatigueValue;
        public float waterValue;
        public float money;
    }

    [Serializable]
    private sealed class MessageInformationPayload
    {
        public MessageInformation[] messages;
    }

    [Serializable]
    private sealed class MessageInformation
    {
        public string source;
        public string message;
    }

    [Serializable]
    private sealed class MarketItem
    {
        public string name;
        public float purchasePrice;
        public float basePrice;
        public float referenceBasePrice;
        public float yesterdayPrice;
        public float quantity;
        public float defaultQuantity;
        public bool priceLocked;
    }

    [Serializable]
    private sealed class ShopStockUpdatePayload
    {
        public int currentMoney;
        public int todayIncome;
        public ShopStockUpdateItem[] items;
    }

    [Serializable]
    private sealed class ShopStockUpdateItem
    {
        public string name;
        public int currentStock;
        public int purchaseQuantity;
        public int todayPrice;
        public int costPrice;
    }

    [Serializable]
    private struct ProductImageMapping
    {
        public string productName;
        [Tooltip("Resources relative path without extension, e.g. UI/Item/base_goods")]
        public string imagePath;
        [Tooltip("Sub-sprite name in atlas, e.g. 面包")]
        public string spriteName;
        [Tooltip("Direct sprite assignment; if set, it overrides imagePath/spriteName")]
        public Sprite sprite;
    }

}

