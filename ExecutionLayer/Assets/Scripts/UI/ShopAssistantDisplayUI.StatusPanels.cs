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
    private void BuildTopLeftStatusPanel(Transform parent)
    {
        var panelGo = new GameObject("TopLeft_StatusPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(parent, false);
        var panel = (RectTransform)panelGo.transform;
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);
        panel.sizeDelta = new Vector2(360f, 198f);
        panel.anchoredPosition = new Vector2(30f, -28f);

        var panelImage = panelGo.GetComponent<Image>();
        var panelSprite = ResolveUiDecorationSprite(statusPanelResourcePath, statusPanelAssetPath, statusPanelSpriteName);
        if (panelSprite != null)
        {
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Simple;
            panelImage.preserveAspect = true;
            panelImage.color = Color.white;
        }
        else
        {
            panelImage.color = paperColor;
            Debug.LogWarning("[ShopAssistantUI] Status panel sprite missing, fallback color panel is used.");
        }
        panelImage.raycastTarget = false;

        var rowRound = CreateStatusTextArea("RowRound", panel, 0.635f, 0.855f);
        var rowMoney = CreateStatusTextArea("RowMoney", panel, 0.385f, 0.575f);
        var rowState = CreateStatusTextArea("RowState", panel, 0.125f, 0.315f);

        var roundPrefix = CreateStatusText("RoundPrefix", rowRound, "第", 27f, FontStyles.Bold, TextAlignmentOptions.Right, StatusStaticTextColor);
        SetAnchoredRect(roundPrefix.rectTransform, 0.01f, 0f, 0.12f, 1f);
        roundText = CreateStatusText("RoundValue", rowRound, "1", 27f, FontStyles.Bold, TextAlignmentOptions.Center, StatusDynamicTextColor);
        SetAnchoredRect(roundText.rectTransform, 0.12f, 0f, 0.30f, 1f);
        var roundSuffix = CreateStatusText("RoundSuffix", rowRound, "回合", 27f, FontStyles.Bold, TextAlignmentOptions.Left, StatusStaticTextColor);
        SetAnchoredRect(roundSuffix.rectTransform, 0.30f, 0f, 0.53f, 1f);

        var moneyPrefix = CreateStatusText("MoneyPrefix", rowMoney, "当前金钱：", 22f, FontStyles.Bold, TextAlignmentOptions.Left, StatusStaticTextColor);
        SetAnchoredRect(moneyPrefix.rectTransform, 0f, 0f, 0.44f, 1f);
        moneyText = CreateStatusText("MoneyValue", rowMoney, "1000", 23f, FontStyles.Bold, TextAlignmentOptions.Left, StatusDynamicTextColor);
        SetAnchoredRect(moneyText.rectTransform, 0.44f, 0f, 1f, 1f);

        var statePrefix = CreateStatusText("StatePrefix", rowState, "游戏状态：", 22f, FontStyles.Bold, TextAlignmentOptions.Left, StatusStaticTextColor);
        SetAnchoredRect(statePrefix.rectTransform, 0f, 0f, 0.44f, 1f);
        stateText = CreateStatusText("StateValue", rowState, "回合进行中", 23f, FontStyles.Bold, TextAlignmentOptions.Left, StatusDynamicTextColor);
        SetAnchoredRect(stateText.rectTransform, 0.44f, 0f, 1f, 1f);
    }

    private void BuildBottomLeftMessageFeed(Transform parent)
    {
        messageBubbleSprite = ResolveUiDecorationSprite(messageBubbleResourcePath, messageBubbleAssetPath, messageBubbleSpriteName);

        var rootGo = new GameObject("BottomLeft_MessageFeed", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        rootGo.transform.SetParent(parent, false);
        var root = (RectTransform)rootGo.transform;
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0f, 0f);
        root.pivot = new Vector2(0f, 0f);
        root.sizeDelta = new Vector2(512f, 246f);
        root.anchoredPosition = new Vector2(30f, 30f);

        var rootImage = rootGo.GetComponent<Image>();
        rootImage.color = new Color(1f, 1f, 1f, 0.01f);
        rootImage.raycastTarget = true;
        rootGo.GetComponent<Mask>().showMaskGraphic = false;

        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(root, false);
        messageFeedContent = (RectTransform)contentGo.transform;
        messageFeedContent.anchorMin = new Vector2(0f, 0f);
        messageFeedContent.anchorMax = new Vector2(1f, 0f);
        messageFeedContent.pivot = new Vector2(0.5f, 0f);
        messageFeedContent.offsetMin = Vector2.zero;
        messageFeedContent.offsetMax = Vector2.zero;
        messageFeedContent.anchoredPosition = Vector2.zero;

        var layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerLeft;
        layout.spacing = 3f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        messageFeedScrollRect = rootGo.GetComponent<ScrollRect>();
        messageFeedScrollRect.viewport = root;
        messageFeedScrollRect.content = messageFeedContent;
        messageFeedScrollRect.horizontal = false;
        messageFeedScrollRect.vertical = true;
        messageFeedScrollRect.movementType = ScrollRect.MovementType.Clamped;
        messageFeedScrollRect.inertia = true;
        messageFeedScrollRect.scrollSensitivity = 30f;
    }

    private void AddDebugBroadcastMessages()
    {
        AddBroadcastMessage("系统", "回合开始");
        AddBroadcastMessage("随机事件", "雨天：今日出门额外消耗5点体力");
        AddBroadcastMessage("商店", "今日面包已售罄");
        AddBroadcastMessage("广播", "江凡在河边钓到了15斤的大鱼，售价1500！");
        AddBroadcastMessage("林墨墨", "去商店看看好了");
    }

    private void AddBroadcastMessage(string source, string message)
    {
        if (messageFeedContent == null)
        {
            return;
        }

        const float messageBodyWidth = 340f;
        const float messageBodyFontSize = 24f;
        const float defaultRowHeight = 64f;

        var rowGo = new GameObject($"Message_{++messageFeedCount}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        rowGo.transform.SetParent(messageFeedContent, false);
        var row = (RectTransform)rowGo.transform;
        row.sizeDelta = new Vector2(0f, defaultRowHeight);

        var layoutElement = rowGo.GetComponent<LayoutElement>();
        layoutElement.minHeight = defaultRowHeight;
        layoutElement.preferredHeight = defaultRowHeight;
        layoutElement.flexibleHeight = 0f;

        var rowImage = rowGo.GetComponent<Image>();
        rowImage.sprite = messageBubbleSprite;
        rowImage.type = Image.Type.Simple;
        rowImage.preserveAspect = false;
        rowImage.color = messageBubbleSprite != null ? new Color(1f, 1f, 1f, 0.66f) : new Color(0.10f, 0.10f, 0.11f, 0.52f);
        rowImage.raycastTarget = false;

        BuildBroadcastSourceBadge(row, source);
        var body = BuildBroadcastBodyText(row, message);
        float bodyHeight = Mathf.Ceil(body.GetPreferredValues(message, messageBodyWidth, 0f).y);
        bool singleLine = bodyHeight <= messageBodyFontSize * 1.55f;
        float rowHeight = Mathf.Max(defaultRowHeight, bodyHeight + 28f);
        row.sizeDelta = new Vector2(0f, rowHeight);
        layoutElement.minHeight = rowHeight;
        layoutElement.preferredHeight = rowHeight;
        ConfigureBroadcastBodyTextRect(body.rectTransform, singleLine);

        LayoutRebuilder.ForceRebuildLayoutImmediate(messageFeedContent);
        Canvas.ForceUpdateCanvases();
        if (messageFeedScrollRect != null)
        {
            messageFeedScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void ApplyBroadcastMessages(string infoJson)
    {
        if (string.IsNullOrWhiteSpace(infoJson))
        {
            return;
        }

        try
        {
            var payload = JsonUtility.FromJson<MessageInformationPayload>(infoJson);
            if (payload == null || payload.messages == null || payload.messages.Length == 0)
            {
                return;
            }

            foreach (var row in payload.messages)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.message))
                {
                    continue;
                }

                string source = string.IsNullOrWhiteSpace(row.source) ? "系统" : row.source.Trim();
                AddBroadcastMessage(source, row.message.Trim());
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ShopAssistantUI] Failed to parse broadcast messages json: {e.Message}");
        }
    }

    private void BuildBroadcastSourceBadge(RectTransform parent, string source)
    {
        var frameSprite = ResolveUiDecorationSprite(
            messageSourceFrameResourcePath,
            messageSourceFrameAssetPath,
            ResolveBroadcastSourceFrameSpriteName(source));

        var badgeGo = new GameObject("SourceBadge", typeof(RectTransform), typeof(Image));
        badgeGo.transform.SetParent(parent, false);
        var badge = (RectTransform)badgeGo.transform;
        badge.anchorMin = new Vector2(0f, 1f);
        badge.anchorMax = new Vector2(0f, 1f);
        badge.pivot = new Vector2(0f, 1f);
        badge.sizeDelta = new Vector2(108f, 40f);
        badge.anchoredPosition = new Vector2(10f, -13f);

        var badgeImage = badgeGo.GetComponent<Image>();
        badgeImage.sprite = frameSprite;
        badgeImage.type = Image.Type.Simple;
        badgeImage.preserveAspect = false;
        badgeImage.color = frameSprite != null ? Color.white : ResolveBroadcastSourceFallbackColor(source);
        badgeImage.raycastTarget = false;

        CreateOutlinedBadgeText(badge, source, ResolveBroadcastSourceTextColor(source));
    }

    private TextMeshProUGUI BuildBroadcastBodyText(RectTransform parent, string message)
    {
        var body = CreateTMPText("Body", parent, message, 24f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        body.color = new Color(0.93f, 0.91f, 0.86f, 1f);
        body.enableAutoSizing = false;
        body.enableWordWrapping = true;
        body.overflowMode = TextOverflowModes.Overflow;
        ApplyTextFaceDilate(body, 0.08f);
        ConfigureBroadcastBodyTextRect(body.rectTransform, false);
        return body;
    }

    private static void ConfigureBroadcastBodyTextRect(RectTransform bodyRt, bool singleLine)
    {
        bodyRt.anchorMin = new Vector2(0f, singleLine ? 0.5f : 0f);
        bodyRt.anchorMax = new Vector2(1f, singleLine ? 0.5f : 1f);
        bodyRt.pivot = new Vector2(0f, singleLine ? 0.5f : 1f);
        if (singleLine)
        {
            bodyRt.sizeDelta = new Vector2(-152f, 36f);
            bodyRt.anchoredPosition = new Vector2(132f, -4f);
        }
        else
        {
            bodyRt.offsetMin = new Vector2(132f, 4f);
            bodyRt.offsetMax = new Vector2(-20f, -11f);
        }
    }

    private void CreateOutlinedBadgeText(RectTransform badge, string source, Color textColor)
    {
        Vector2[] offsets =
        {
            new Vector2(-1.4f, 0f),
            new Vector2(1.4f, 0f),
            new Vector2(0f, -1.4f),
            new Vector2(0f, 1.4f),
            new Vector2(-1f, -1f),
            new Vector2(-1f, 1f),
            new Vector2(1f, -1f),
            new Vector2(1f, 1f)
        };

        foreach (var offset in offsets)
        {
            var outline = CreateTMPText("LabelOutline", badge, source, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
            outline.color = Color.white;
            outline.enableWordWrapping = false;
            outline.overflowMode = TextOverflowModes.Ellipsis;
            ApplyTextFaceDilate(outline, -0.05f);
            StretchText(outline, 6f);
            outline.rectTransform.anchoredPosition += offset;
        }

        var label = CreateTMPText("Label", badge, source, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
        label.color = textColor;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        ApplyTextFaceDilate(label, -0.05f);
        StretchText(label, 6f);
    }

    private static string ResolveBroadcastSourceFrameSpriteName(string source)
    {
        if (string.Equals(source, "广播", StringComparison.Ordinal))
        {
            return "黄框";
        }

        if (string.Equals(source, "商店", StringComparison.Ordinal))
        {
            return "绿框";
        }

        if (string.Equals(source, "随机事件", StringComparison.Ordinal))
        {
            return "紫框";
        }

        if (string.Equals(source, "系统", StringComparison.Ordinal))
        {
            return "红框";
        }

        return "蓝框";
    }

    private static Color ResolveBroadcastSourceFallbackColor(string source)
    {
        string spriteName = ResolveBroadcastSourceFrameSpriteName(source);
        return spriteName switch
        {
            "黄框" => new Color(0.93f, 0.72f, 0.15f, 1f),
            "绿框" => new Color(0.22f, 0.67f, 0.25f, 1f),
            "紫框" => new Color(0.48f, 0.30f, 0.72f, 1f),
            "红框" => new Color(0.72f, 0.18f, 0.18f, 1f),
            _ => new Color(0.24f, 0.52f, 0.84f, 1f)
        };
    }

    private static Color ResolveBroadcastSourceTextColor(string source)
    {
        string spriteName = ResolveBroadcastSourceFrameSpriteName(source);
        return spriteName switch
        {
            "黄框" => new Color(0.70f, 0.43f, 0.03f, 1f),
            "绿框" => new Color(0.12f, 0.55f, 0.16f, 1f),
            "紫框" => new Color(0.48f, 0.25f, 0.72f, 1f),
            "红框" => new Color(0.72f, 0.16f, 0.14f, 1f),
            _ => new Color(0.16f, 0.42f, 0.76f, 1f)
        };
    }

    private void BuildTopRightPlayerPanel(Transform parent)
    {
        playerAvatarFrameImages.Clear();
        selectedPlayerFrameIndex = 0;
        InitializeAgentModels();

        var rootGo = new GameObject("TopRight_PlayerPanel", typeof(RectTransform));
        rootGo.transform.SetParent(parent, false);
        var root = (RectTransform)rootGo.transform;
        root.anchorMin = new Vector2(1f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(1f, 1f);
        root.sizeDelta = new Vector2(340f, 316f);
        root.anchoredPosition = new Vector2(-30f, -28f);

        BuildPlayerAvatarFrameRow(root);
        BuildPlayerInfoBackground(root);
    }

    private void BuildPlayerAvatarFrameRow(RectTransform parent)
    {
        var selectedSprite = ResolveUiDecorationSprite(playerUiResourcePath, playerUiAssetPath, playerAvatarSelectedSpriteName);
        var normalSprite = ResolveUiDecorationSprite(playerUiResourcePath, playerUiAssetPath, playerAvatarNormalSpriteName);

        var rowGo = new GameObject("AvatarFrameRow", typeof(RectTransform));
        rowGo.transform.SetParent(parent, false);
        var row = (RectTransform)rowGo.transform;
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.sizeDelta = new Vector2(0f, 56f);
        row.anchoredPosition = Vector2.zero;

        const int frameCount = 4;
        const float slotWidth = 82f;
        const float slotHeight = 56f;
        const float slotGap = 2f;
        float startX = -((frameCount - 1) * (slotWidth + slotGap)) * 0.5f;

        for (int i = 0; i < frameCount; i++)
        {
            int frameIndex = i;
            var slotGo = new GameObject($"AvatarFrame_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            slotGo.transform.SetParent(row, false);
            var slot = (RectTransform)slotGo.transform;
            slot.anchorMin = new Vector2(0.5f, 0.5f);
            slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.sizeDelta = new Vector2(slotWidth, slotHeight);
            slot.anchoredPosition = new Vector2(startX + i * (slotWidth + slotGap), 0f);

            var frameImage = slotGo.GetComponent<Image>();
            frameImage.sprite = i == selectedPlayerFrameIndex ? selectedSprite : normalSprite;
            frameImage.type = Image.Type.Simple;
            frameImage.preserveAspect = true;
            frameImage.color = frameImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            playerAvatarFrameImages.Add(frameImage);

            CreatePlayerAvatarImage(slot, frameIndex);

            var button = slotGo.GetComponent<Button>();
            button.targetGraphic = frameImage;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.6f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() => SelectPlayerFrame(frameIndex));
        }
    }

    private void BuildPlayerInfoBackground(RectTransform parent)
    {
        var panelSprite = ResolveUiDecorationSprite(playerUiResourcePath, playerUiAssetPath, playerPanelBackgroundSpriteName);
        var panelGo = new GameObject("PlayerInfoBackground", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(parent, false);
        var panel = (RectTransform)panelGo.transform;
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.sizeDelta = new Vector2(0f, 255f);
        panel.anchoredPosition = new Vector2(0f, -58f);

        var panelImage = panelGo.GetComponent<Image>();
        panelImage.sprite = panelSprite;
        panelImage.type = Image.Type.Simple;
        panelImage.preserveAspect = false;
        panelImage.color = panelSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        panelImage.raycastTarget = false;

        BuildPlayerInfoHeader(panel);
        BuildPlayerAttributeRows(panel);
        BuildPlayerMoneyRow(panel);
        RefreshPlayerInfoHeader();
        RefreshPlayerAttributeRows();
        RefreshPlayerMoneyRow();
    }

    private void BuildPlayerInfoHeader(RectTransform parent)
    {
        var avatarBgGo = new GameObject("HeaderAvatarBackground", typeof(RectTransform), typeof(Image));
        avatarBgGo.transform.SetParent(parent, false);
        var avatarBgRt = (RectTransform)avatarBgGo.transform;
        avatarBgRt.anchorMin = new Vector2(0f, 1f);
        avatarBgRt.anchorMax = new Vector2(0f, 1f);
        avatarBgRt.pivot = new Vector2(0.5f, 0.5f);
        avatarBgRt.sizeDelta = new Vector2(46f, 41f);
        avatarBgRt.anchoredPosition = new Vector2(46f, -40f);

        playerInfoAvatarBackgroundImage = avatarBgGo.GetComponent<Image>();
        playerInfoAvatarBackgroundImage.type = Image.Type.Simple;
        playerInfoAvatarBackgroundImage.preserveAspect = true;
        playerInfoAvatarBackgroundImage.raycastTarget = false;

        var avatarGo = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
        avatarGo.transform.SetParent(avatarBgRt, false);
        var avatarRt = (RectTransform)avatarGo.transform;
        avatarRt.anchorMin = new Vector2(0.20f, 0.12f);
        avatarRt.anchorMax = new Vector2(0.80f, 0.88f);
        avatarRt.offsetMin = Vector2.zero;
        avatarRt.offsetMax = Vector2.zero;

        playerInfoAvatarImage = avatarGo.GetComponent<Image>();
        playerInfoAvatarImage.type = Image.Type.Simple;
        playerInfoAvatarImage.preserveAspect = true;
        playerInfoAvatarImage.raycastTarget = false;

        playerInfoNameText = CreateTMPText("PlayerName", parent, string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        playerInfoNameText.color = new Color(0.17f, 0.11f, 0.06f, 1f);
        playerInfoNameText.enableWordWrapping = false;
        playerInfoNameText.overflowMode = TextOverflowModes.Overflow;
        ApplyTextFaceDilate(playerInfoNameText, 0.18f);
        var nameRt = (RectTransform)playerInfoNameText.transform;
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0f, 0.5f);
        nameRt.offsetMin = Vector2.zero;
        nameRt.offsetMax = Vector2.zero;
        nameRt.sizeDelta = new Vector2(-94f, 42f);
        nameRt.anchoredPosition = new Vector2(90f, -43f);
    }

    private void BuildPlayerAttributeRows(RectTransform parent)
    {
        for (int i = 0; i < playerAttributeFillImages.Length; i++)
        {
            BuildPlayerAttributeRow(parent, i);
        }
    }

    private void BuildPlayerAttributeRow(RectTransform parent, int attributeIndex)
    {
        var rowGo = new GameObject($"AttributeRow_{attributeIndex + 1}", typeof(RectTransform));
        rowGo.transform.SetParent(parent, false);
        var row = (RectTransform)rowGo.transform;
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0f, 0.5f);
        row.sizeDelta = new Vector2(0f, 42f);
        row.anchoredPosition = new Vector2(0f, -87f - attributeIndex * 45f);

        var iconSprite = ResolveUiDecorationSprite(
            playerUiResourcePath,
            playerUiAssetPath,
            playerAttributeIconSpriteNames[attributeIndex]);
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(row, false);
        var iconRt = (RectTransform)iconGo.transform;
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(34f, 34f);
        iconRt.anchoredPosition = new Vector2(48f, 0f);

        var iconImage = iconGo.GetComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true;
        iconImage.color = iconSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        iconImage.raycastTarget = false;

        var frameSprite = ResolveUiDecorationSprite(playerUiPatchResourcePath, playerUiPatchAssetPath, playerHpFrameSpriteName);
        var barSprite = ResolveUiDecorationSprite(
            playerUiPatchResourcePath,
            playerUiPatchAssetPath,
            playerAttributeBarSpriteNames[attributeIndex]);

        var barRootGo = new GameObject("BarRoot", typeof(RectTransform));
        barRootGo.transform.SetParent(row, false);
        var barRoot = (RectTransform)barRootGo.transform;
        barRoot.anchorMin = new Vector2(0f, 0.5f);
        barRoot.anchorMax = new Vector2(0f, 0.5f);
        barRoot.pivot = new Vector2(0f, 0.5f);
        barRoot.sizeDelta = new Vector2(178f, 26f);
        barRoot.anchoredPosition = new Vector2(84f, 0f);

        var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frameGo.transform.SetParent(barRoot, false);
        var frameRt = (RectTransform)frameGo.transform;
        frameRt.anchorMin = Vector2.zero;
        frameRt.anchorMax = Vector2.one;
        frameRt.offsetMin = Vector2.zero;
        frameRt.offsetMax = Vector2.zero;

        var frameImage = frameGo.GetComponent<Image>();
        frameImage.sprite = frameSprite;
        frameImage.type = Image.Type.Simple;
        frameImage.preserveAspect = false;
        frameImage.color = frameSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        frameImage.raycastTarget = false;

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(barRoot, false);
        var fillRt = (RectTransform)fillGo.transform;
        fillRt.anchorMin = new Vector2(0.025f, 0.13f);
        fillRt.anchorMax = new Vector2(0.975f, 0.87f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        var fillImage = fillGo.GetComponent<Image>();
        fillImage.sprite = barSprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.preserveAspect = false;
        fillImage.color = barSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        fillImage.raycastTarget = false;
        playerAttributeFillImages[attributeIndex] = fillImage;

        var valueText = CreateTMPText("Value", row, "0/100", 18f, FontStyles.Bold, TextAlignmentOptions.Left);
        valueText.color = new Color(0.16f, 0.09f, 0.04f, 1f);
        valueText.enableWordWrapping = false;
        valueText.overflowMode = TextOverflowModes.Overflow;
        ApplyTextFaceDilate(valueText, 0.14f);
        var valueRt = (RectTransform)valueText.transform;
        valueRt.anchorMin = new Vector2(0f, 0.5f);
        valueRt.anchorMax = new Vector2(0f, 0.5f);
        valueRt.pivot = new Vector2(0f, 0.5f);
        valueRt.sizeDelta = new Vector2(72f, 30f);
        valueRt.anchoredPosition = new Vector2(268f, 0f);
        playerAttributeValueTexts[attributeIndex] = valueText;
    }

    private void BuildPlayerMoneyRow(RectTransform parent)
    {
        var rowGo = new GameObject("MoneyRow", typeof(RectTransform));
        rowGo.transform.SetParent(parent, false);
        var row = (RectTransform)rowGo.transform;
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0f, 0.5f);
        row.sizeDelta = new Vector2(0f, 42f);
        row.anchoredPosition = new Vector2(0f, -87f - 3f * 45f);

        var iconSprite = ResolveUiDecorationSprite(playerUiResourcePath, playerUiAssetPath, playerMoneyIconSpriteName);
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(row, false);
        var iconRt = (RectTransform)iconGo.transform;
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(34f, 34f);
        iconRt.anchoredPosition = new Vector2(48f, 0f);

        var iconImage = iconGo.GetComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true;
        iconImage.color = iconSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        iconImage.raycastTarget = false;

        playerMoneyValueText = CreateTMPText("Value", row, "0", 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        playerMoneyValueText.color = new Color(0.12f, 0.58f, 0.18f, 1f);
        playerMoneyValueText.enableWordWrapping = false;
        playerMoneyValueText.overflowMode = TextOverflowModes.Overflow;
        ApplyTextFaceDilate(playerMoneyValueText, 0.18f);
        var valueRt = (RectTransform)playerMoneyValueText.transform;
        valueRt.anchorMin = new Vector2(0f, 0.5f);
        valueRt.anchorMax = new Vector2(0f, 0.5f);
        valueRt.pivot = new Vector2(0.5f, 0.5f);
        valueRt.sizeDelta = new Vector2(178f, 34f);
        valueRt.anchoredPosition = new Vector2(173f, 0f);
    }

    private void CreatePlayerAvatarImage(RectTransform parent, int playerIndex)
    {
        var avatarSprite = ResolvePlayerAvatarSprite(playerIndex);
        var avatarGo = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
        avatarGo.transform.SetParent(parent, false);
        avatarGo.transform.SetAsFirstSibling();

        var avatarRt = (RectTransform)avatarGo.transform;
        avatarRt.anchorMin = new Vector2(0.18f, 0.10f);
        avatarRt.anchorMax = new Vector2(0.82f, 0.92f);
        avatarRt.offsetMin = Vector2.zero;
        avatarRt.offsetMax = Vector2.zero;

        var avatarImage = avatarGo.GetComponent<Image>();
        avatarImage.sprite = avatarSprite;
        avatarImage.type = Image.Type.Simple;
        avatarImage.preserveAspect = true;
        avatarImage.color = avatarSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        avatarImage.raycastTarget = false;
    }

    private Sprite ResolvePlayerAvatarSprite(int playerIndex)
    {
        if (playerAvatarSpriteNames == null || playerIndex < 0 || playerIndex >= playerAvatarSpriteNames.Length)
        {
            return null;
        }

        string resourcePath = playerAvatarResourcePaths != null && playerIndex < playerAvatarResourcePaths.Length
            ? playerAvatarResourcePaths[playerIndex]
            : string.Empty;
        string assetPath = playerAvatarAssetPaths != null && playerIndex < playerAvatarAssetPaths.Length
            ? playerAvatarAssetPaths[playerIndex]
            : string.Empty;

        return ResolveUiDecorationSprite(resourcePath, assetPath, playerAvatarSpriteNames[playerIndex]);
    }

    private Sprite ResolvePlayerAvatarBackgroundSprite(int playerIndex)
    {
        if (playerAvatarBackgroundSpriteNames == null || playerIndex < 0 || playerIndex >= playerAvatarBackgroundSpriteNames.Length)
        {
            return null;
        }

        return ResolveUiDecorationSprite(
            playerAvatarBackgroundResourcePath,
            playerAvatarBackgroundAssetPath,
            playerAvatarBackgroundSpriteNames[playerIndex]);
    }

    private string ResolvePlayerDisplayName(int playerIndex)
    {
        if (playerDisplayNames == null || playerIndex < 0 || playerIndex >= playerDisplayNames.Length)
        {
            return string.Empty;
        }

        return playerDisplayNames[playerIndex];
    }

    private void InitializeAgentModels()
    {
        agentModels.Clear();
        agentModelByName.Clear();
        agentModelByCode.Clear();

        if (playerDisplayNames == null)
        {
            return;
        }

        for (int i = 0; i < playerDisplayNames.Length; i++)
        {
            string agentName = ExtractAgentName(ResolvePlayerDisplayName(i));
            var model = new AgentModel($"Agent-{i + 1}", agentName, 80, 80, 80, 1000);
            agentModels.Add(model);
            RegisterAgentModel(model);
        }
    }

    private void RegisterAgentModel(AgentModel model)
    {
        if (model == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(model.AgentName))
        {
            agentModelByName[model.AgentName] = model;
        }

        if (!string.IsNullOrEmpty(model.AgentCode))
        {
            agentModelByCode[model.AgentCode] = model;
        }
    }

    private AgentModel ResolveSelectedAgentModel()
    {
        string displayName = ResolvePlayerDisplayName(selectedPlayerFrameIndex);
        string agentName = ExtractAgentName(displayName);
        if (!string.IsNullOrEmpty(agentName) && agentModelByName.TryGetValue(agentName, out var namedModel))
        {
            return namedModel;
        }

        string agentCode = $"Agent-{selectedPlayerFrameIndex + 1}";
        if (agentModelByCode.TryGetValue(agentCode, out var codedModel))
        {
            return codedModel;
        }

        return selectedPlayerFrameIndex >= 0 && selectedPlayerFrameIndex < agentModels.Count
            ? agentModels[selectedPlayerFrameIndex]
            : null;
    }

    private static string ExtractAgentName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return string.Empty;
        }

        string trimmed = displayName.Trim();
        int roleStart = trimmed.IndexOf('（');
        if (roleStart < 0)
        {
            roleStart = trimmed.IndexOf('(');
        }

        return roleStart > 0 ? trimmed.Substring(0, roleStart).Trim() : trimmed;
    }

    private void RefreshPlayerInfoHeader()
    {
        if (playerInfoAvatarBackgroundImage != null)
        {
            var bgSprite = ResolvePlayerAvatarBackgroundSprite(selectedPlayerFrameIndex);
            playerInfoAvatarBackgroundImage.sprite = bgSprite;
            playerInfoAvatarBackgroundImage.color = bgSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (playerInfoAvatarImage != null)
        {
            var avatarSprite = ResolvePlayerAvatarSprite(selectedPlayerFrameIndex);
            playerInfoAvatarImage.sprite = avatarSprite;
            playerInfoAvatarImage.color = avatarSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (playerInfoNameText != null)
        {
            playerInfoNameText.text = ResolvePlayerDisplayName(selectedPlayerFrameIndex);
        }
    }

    private void RefreshPlayerAttributeRows()
    {
        AgentModel model = ResolveSelectedAgentModel();
        for (int i = 0; i < playerAttributeFillImages.Length; i++)
        {
            int value = model == null ? 0 : i switch
            {
                0 => model.HungerValue,
                1 => model.FatigueValue,
                2 => model.WaterValue,
                _ => 0
            };

            if (playerAttributeFillImages[i] != null)
            {
                playerAttributeFillImages[i].fillAmount = value / 100f;
            }

            if (playerAttributeValueTexts[i] != null)
            {
                playerAttributeValueTexts[i].text = $"{value}/100";
            }
        }
    }

    private void RefreshPlayerMoneyRow()
    {
        if (playerMoneyValueText == null)
        {
            return;
        }

        AgentModel model = ResolveSelectedAgentModel();
        playerMoneyValueText.text = (model == null ? 0 : model.Money).ToString();
    }

    private void SelectPlayerFrame(int index)
    {
        if (index < 0 || index >= playerAvatarFrameImages.Count)
        {
            return;
        }

        selectedPlayerFrameIndex = index;
        var selectedSprite = ResolveUiDecorationSprite(playerUiResourcePath, playerUiAssetPath, playerAvatarSelectedSpriteName);
        var normalSprite = ResolveUiDecorationSprite(playerUiResourcePath, playerUiAssetPath, playerAvatarNormalSpriteName);

        for (int i = 0; i < playerAvatarFrameImages.Count; i++)
        {
            var frameImage = playerAvatarFrameImages[i];
            if (frameImage == null)
            {
                continue;
            }

            frameImage.sprite = i == selectedPlayerFrameIndex ? selectedSprite : normalSprite;
            frameImage.color = frameImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        RefreshPlayerInfoHeader();
        RefreshPlayerAttributeRows();
        RefreshPlayerMoneyRow();
    }

    private void BuildOpenInventoryButton(Transform parent)
    {
        openInventoryButton = CreateOpenInventorySpriteButton(parent);
        openInventoryButton.onClick.AddListener(() => SetInventoryVisible(true));
    }

    private Button CreateOpenInventorySpriteButton(Transform parent)
    {
        var buttonRoot = new GameObject("Btn_OpenInventory", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonRoot.transform.SetParent(parent, false);

        var rt = (RectTransform)buttonRoot.transform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(220f, 72f);
        rt.anchoredPosition = new Vector2(-36f, 30f);

        var buttonImage = buttonRoot.GetComponent<Image>();
        var buttonSprite = ResolveUiDecorationSprite(inventoryOpenButtonResourcePath, inventoryOpenButtonAssetPath, inventoryOpenButtonSpriteName);
        if (buttonSprite != null)
        {
            buttonImage.sprite = buttonSprite;
            buttonImage.type = Image.Type.Simple;
            buttonImage.preserveAspect = true;
            buttonImage.color = Color.white;
        }
        else
        {
            buttonImage.color = woodColor;
            Debug.LogWarning("[ShopAssistantUI] Inventory open button sprite missing, fallback color button is used.");
        }

        var button = buttonRoot.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }


    private void ApplyAgentInformation(string infoJson)
    {
        if (string.IsNullOrWhiteSpace(infoJson))
        {
            return;
        }

        try
        {
            if (agentModels.Count == 0)
            {
                InitializeAgentModels();
            }

            var payload = JsonUtility.FromJson<AgentInformationPayload>(infoJson);
            if (payload == null || payload.agents == null || payload.agents.Length == 0)
            {
                return;
            }

            foreach (var agent in payload.agents)
            {
                if (agent == null)
                {
                    continue;
                }

                string agentCode = string.IsNullOrWhiteSpace(agent.agentCode) ? string.Empty : agent.agentCode.Trim();
                string agentName = ExtractAgentName(string.IsNullOrWhiteSpace(agent.agentName) ? agent.name : agent.agentName);
                AgentModel model = null;

                if (!string.IsNullOrEmpty(agentCode))
                {
                    agentModelByCode.TryGetValue(agentCode, out model);
                }

                if (model == null && !string.IsNullOrEmpty(agentName))
                {
                    agentModelByName.TryGetValue(agentName, out model);
                }

                if (model == null)
                {
                    model = new AgentModel(agentCode, agentName);
                    agentModels.Add(model);
                }
                else
                {
                    if (!string.IsNullOrEmpty(agentCode))
                    {
                        model.AgentCode = agentCode;
                    }

                    if (string.IsNullOrEmpty(model.AgentName) && !string.IsNullOrEmpty(agentName))
                    {
                        model.AgentName = agentName;
                    }
                }

                model.UpdateRuntimeValues(
                    RoundToPercent(agent.hungerValue),
                    RoundToPercent(agent.fatigueValue),
                    RoundToPercent(agent.waterValue),
                    RoundToInt(agent.money));
                RegisterAgentModel(model);
            }

            RefreshPlayerInfoHeader();
            RefreshPlayerAttributeRows();
            RefreshPlayerMoneyRow();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ShopAssistantUI] Failed to parse agent info json: {e.Message}");
        }
    }

    private static int RoundToInt(float value)
    {
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    private static int RoundToPercent(float value)
    {
        return Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
    }


    private void RefreshTopLeftStatus(int round, int money, string state)
    {
        int clampedRound = Mathf.Clamp(round, 0, 999);
        string stateValue = string.IsNullOrWhiteSpace(state) ? "回合进行中" : state.Trim();
        currentRoundValue = clampedRound;
        currentGameStateValue = stateValue;
        playerModel.CurrentMoney = money;

        if (roundText != null) roundText.text = clampedRound.ToString();
        RefreshMoneyDisplays();
        if (stateText != null)
        {
            stateText.text = stateValue;
            stateText.color = stateValue.IndexOf("结算", StringComparison.Ordinal) >= 0
                ? StatusSettlementTextColor
                : StatusDynamicTextColor;
        }
    }

    private RectTransform CreateStatusTextArea(string name, RectTransform parent, float yMin, float yMax)
    {
        var row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var rt = (RectTransform)row.transform;
        rt.anchorMin = new Vector2(0.265f, yMin);
        rt.anchorMax = new Vector2(0.955f, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private TextMeshProUGUI CreateStatusText(string name, RectTransform parent, string content, float fontSize, FontStyles style, TextAlignmentOptions align, Color color)
    {
        var text = CreateTMPText(name, parent, content, fontSize, style, align);
        text.color = color;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = Vector4.zero;
        ApplyTextFaceDilate(text, 0.10f);
        return text;
    }

}

