using System;
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
    private void BuildInventoryOverlay(Transform parent)
    {
        inventoryOverlayRoot = new GameObject("InventoryOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        inventoryOverlayRoot.transform.SetParent(parent, false);

        var overlayRt = (RectTransform)inventoryOverlayRoot.transform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        var overlayBg = inventoryOverlayRoot.GetComponent<Image>();
        overlayBg.color = new Color(0.17f, 0.12f, 0.04f, 0.45f);

        // Match the background sprite aspect (1360x998) to avoid content overflowing visible paper area.
        var window = CreatePanel("InventoryWindow", inventoryOverlayRoot.transform, paperColor, woodColor, new Vector2(1505, 1055), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        ApplyInventoryWindowBackground(window);

        CreateInventoryTitleSprite(window);

        var closeButton = CreateInventoryCloseSpriteButton(window);
        closeButton.onClick.AddListener(() => SetInventoryVisible(false));

        CreateInventoryRightPanelSprite(window);

        var scrollRoot = new GameObject("GoodsScroll", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollRoot.transform.SetParent(window, false);
        var scrollRt = (RectTransform)scrollRoot.transform;
        scrollRt.anchorMin = new Vector2(0.04f, 0.16f);
        scrollRt.anchorMax = new Vector2(0.78f, 0.86f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        var scrollImage = scrollRoot.GetComponent<Image>();
        scrollImage.color = new Color(1f, 1f, 1f, 0.22f);
        scrollRoot.GetComponent<Mask>().showMaskGraphic = false;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollRoot.transform, false);
        var viewportRt = (RectTransform)viewport.transform;
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(8f, 8f);
        viewportRt.offsetMax = new Vector2(-8f, -8f);
        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRt = (RectTransform)content.transform;
        inventoryContentRoot = contentRt;
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 1200f);

        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(210f, 366f);
        grid.spacing = new Vector2(14f, 14f);
        grid.padding = new RectOffset(14, 14, 14, 14);
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        var autoColumns = content.AddComponent<InventoryGridAutoColumns>();
        autoColumns.Bind(grid, viewportRt, 4, 4);

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = scrollRoot.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        RebuildProductCells();

        stockInventoryButton = CreateInventoryStockSpriteButton(window);
        stockInventoryButton.onClick.AddListener(SubmitStockPlan);
        RefreshStockControlsInteractable();

        SetInventoryVisible(false);
    }

    private RectTransform CreatePanel(
        string name,
        Transform parent,
        Color fill,
        Color border,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Shadow));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var image = go.GetComponent<Image>();
        image.color = fill;

        var outline = go.GetComponent<Outline>();
        outline.effectColor = border;
        outline.effectDistance = new Vector2(panelOutlineThickness, -panelOutlineThickness);

        var shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.2f);
        shadow.effectDistance = new Vector2(panelOutlineThickness + 2f, -(panelOutlineThickness + 2f));

        return rt;
    }

    private GameObject CreateButtonLikePanel(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPos,
        string label,
        float fontSize)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Shadow), typeof(Button));
        root.transform.SetParent(parent, false);
        var rt = (RectTransform)root.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        root.GetComponent<Image>().color = woodColor;

        var outline = root.GetComponent<Outline>();
        outline.effectColor = new Color(0.48f, 0.34f, 0.08f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);

        var shadow = root.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.2f);
        shadow.effectDistance = new Vector2(5f, -5f);

        var fadedEdge = new GameObject("FadedEdge", typeof(RectTransform), typeof(Image));
        fadedEdge.transform.SetParent(root.transform, false);
        var edgeRt = (RectTransform)fadedEdge.transform;
        edgeRt.anchorMin = Vector2.zero;
        edgeRt.anchorMax = Vector2.one;
        edgeRt.offsetMin = new Vector2(6f, 6f);
        edgeRt.offsetMax = new Vector2(-6f, -6f);
        fadedEdge.GetComponent<Image>().color = woodEdgeFade;

        var center = new GameObject("Center", typeof(RectTransform), typeof(Image));
        center.transform.SetParent(root.transform, false);
        var centerRt = (RectTransform)center.transform;
        centerRt.anchorMin = Vector2.zero;
        centerRt.anchorMax = Vector2.one;
        centerRt.offsetMin = new Vector2(14f, 12f);
        centerRt.offsetMax = new Vector2(-14f, -12f);
        center.GetComponent<Image>().color = woodColor;

        var txt = CreateTMPText("Label", root.transform, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        var textRt = (RectTransform)txt.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var button = root.GetComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        return root;
    }

    private void RebuildProductCells()
    {
        if (inventoryContentRoot == null)
        {
            return;
        }

        inventorySteppers.Clear();
        for (int i = inventoryContentRoot.childCount - 1; i >= 0; i--)
        {
            var child = inventoryContentRoot.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        if (marketProducts.Count == 0)
        {
            marketProducts.AddRange(BuildMockProducts());
        }

        for (int i = 0; i < marketProducts.Count; i++)
        {
            CreateProductCell(inventoryContentRoot, marketProducts[i], i);
        }
    }

    private List<ShopProductModel> ParseMarketInformation(string infoJson)
    {
        try
        {
            var payload = JsonUtility.FromJson<MarketInformationPayload>(infoJson);
            if (payload == null || payload.items == null || payload.items.Length == 0)
            {
                return new List<ShopProductModel>();
            }

            var result = new List<ShopProductModel>(payload.items.Length);
            foreach (var item in payload.items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.name))
                {
                    continue;
                }

                int currentStock = RoundToInt(item.quantity);
                int defaultStock = RoundToInt(item.defaultQuantity);
                if (defaultStock <= 0)
                {
                    defaultStock = currentStock;
                }
                int defaultPurchaseQuantity = Mathf.Max(defaultStock - currentStock, 0);

                result.Add(new ShopProductModel(
                    item.name.Trim(),
                    RoundToInt(item.purchasePrice),
                    defaultPurchaseQuantity,
                    RoundToInt(item.basePrice),
                    currentStock,
                    item.priceLocked,
                    defaultStock,
                    RoundToInt(item.yesterdayPrice),
                    RoundToInt(item.referenceBasePrice)));
            }

            return result;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ShopAssistantUI] Failed to parse market info json: {e.Message}");
            return new List<ShopProductModel>();
        }
    }

    private void ApplyPlayerInformation(string infoJson)
    {
        try
        {
            var payload = JsonUtility.FromJson<MarketInformationPayload>(infoJson);
            if (payload == null || payload.player == null)
            {
                return;
            }

            playerModel.CurrentMoney = Mathf.RoundToInt(payload.player.currentMoney);
            playerModel.TodayIncome = Mathf.RoundToInt(payload.player.todayIncome);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ShopAssistantUI] Failed to parse player info json: {e.Message}");
        }
    }


    private void CreateProductCell(Transform content, ShopProductModel data, int cellIndex)
    {
        data.TodayPrice = Mathf.Min(data.TodayPrice, MaxProductTodayPrice(data));

        var cell = CreatePanel(
            $"Cell_{data.ProductName}",
            content,
            new Color(1f, 1f, 1f, 0.45f),
            woodColor,
            new Vector2(210f, 350f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero
        );
        var cellSprite = ResolveUiDecorationSprite(inventoryProductCellBgResourcePath, inventoryProductCellBgAssetPath, inventoryProductCellBgSpriteName);
        var cellImage = cell.GetComponent<Image>();
        if (cellSprite != null)
        {
            cellImage.sprite = cellSprite;
            cellImage.type = Image.Type.Simple;
            cellImage.preserveAspect = false;
            cellImage.color = Color.white;
        }

        var cellOutline = cell.GetComponent<Outline>();
        if (cellOutline != null)
        {
            cellOutline.enabled = false;
        }

        var cellShadow = cell.GetComponent<Shadow>();
        if (cellShadow != null)
        {
            cellShadow.enabled = false;
        }

        var iconBg = new GameObject("IconBG", typeof(RectTransform), typeof(Image));
        iconBg.transform.SetParent(cell, false);
        var iconRt = (RectTransform)iconBg.transform;
        iconRt.anchorMin = new Vector2(0.08f, 1f);
        iconRt.anchorMax = new Vector2(0.92f, 1f);
        iconRt.pivot = new Vector2(0.5f, 1f);
        iconRt.sizeDelta = new Vector2(0f, 76f);
        iconRt.anchoredPosition = new Vector2(0f, -14f);
        iconBg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        var iconImageObj = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
        iconImageObj.transform.SetParent(iconBg.transform, false);
        var iconImageRt = (RectTransform)iconImageObj.transform;
        iconImageRt.anchorMin = new Vector2(0f, 0f);
        iconImageRt.anchorMax = new Vector2(1f, 1f);
        iconImageRt.offsetMin = new Vector2(4f, 4f);
        iconImageRt.offsetMax = new Vector2(-4f, -4f);
        var iconImage = iconImageObj.GetComponent<Image>();
        iconImage.preserveAspect = true;

        var sprite = TryResolveProductSprite(data.ProductName);
        if (sprite != null)
        {
            iconImage.sprite = sprite;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.color = new Color(1f, 1f, 1f, 0f);
            var iconText = CreateTMPText("IconText", iconBg.transform, "图片占位", 18, FontStyles.Italic, TextAlignmentOptions.Center);
            StretchText(iconText, 0f);
        }

        var nameBannerObj = new GameObject("NameBanner", typeof(RectTransform), typeof(Image));
        nameBannerObj.transform.SetParent(cell, false);
        var nameBannerRt = (RectTransform)nameBannerObj.transform;
        nameBannerRt.anchorMin = new Vector2(0.07f, 1f);
        nameBannerRt.anchorMax = new Vector2(0.93f, 1f);
        nameBannerRt.pivot = new Vector2(0.5f, 1f);
        nameBannerRt.sizeDelta = new Vector2(0f, 34f);
        nameBannerRt.anchoredPosition = new Vector2(0f, -95f);
        var nameBannerImage = nameBannerObj.GetComponent<Image>();
        var nameBannerSprite = ResolveProductNameBannerSprite(cellIndex);
        nameBannerImage.sprite = nameBannerSprite;
        nameBannerImage.type = Image.Type.Simple;
        nameBannerImage.preserveAspect = false;
        nameBannerImage.color = nameBannerSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        nameBannerImage.raycastTarget = false;

        var nameText = CreateTMPText("Name", nameBannerObj.transform, data.ProductName, 26, FontStyles.Bold, TextAlignmentOptions.Center);
        nameText.color = Color.white;
        var nameRt = (RectTransform)nameText.transform;
        nameRt.anchorMin = Vector2.zero;
        nameRt.anchorMax = Vector2.one;
        nameRt.offsetMin = Vector2.zero;
        nameRt.offsetMax = Vector2.zero;

        var buyPrice = CreateTMPText("BuyPrice", cell, $"进货价：{data.CostPrice}", 22, FontStyles.Bold, TextAlignmentOptions.Left);
        ApplyInventoryTextWeight(buyPrice);
        var buyRt = (RectTransform)buyPrice.transform;
        buyRt.anchorMin = new Vector2(0.08f, 1f);
        buyRt.anchorMax = new Vector2(0.92f, 1f);
        buyRt.pivot = new Vector2(0.5f, 1f);
        buyRt.sizeDelta = new Vector2(0f, 26f);
        buyRt.anchoredPosition = new Vector2(0f, -136f);

        var yesterdayPrice = CreateTMPText("YesterdayPrice", cell, $"昨日售价：{data.YesterdayPrice}", 22, FontStyles.Bold, TextAlignmentOptions.Left);
        ApplyInventoryTextWeight(yesterdayPrice);
        var yesterdayRt = (RectTransform)yesterdayPrice.transform;
        yesterdayRt.anchorMin = new Vector2(0.08f, 1f);
        yesterdayRt.anchorMax = new Vector2(0.92f, 1f);
        yesterdayRt.pivot = new Vector2(0.5f, 1f);
        yesterdayRt.sizeDelta = new Vector2(0f, 24f);
        yesterdayRt.anchoredPosition = new Vector2(0f, -166f);

        CreateProductCellDivider(cell, -194f);
        var currentStock = CreateTMPText("CurrentStock", cell, $"当前储量：{data.CurrentStock}", 22, FontStyles.Bold, TextAlignmentOptions.Left);
        ApplyInventoryTextWeight(currentStock);
        var stockRt = (RectTransform)currentStock.transform;
        stockRt.anchorMin = new Vector2(0.08f, 1f);
        stockRt.anchorMax = new Vector2(0.92f, 1f);
        stockRt.pivot = new Vector2(0.5f, 1f);
        stockRt.sizeDelta = new Vector2(0f, 24f);
        stockRt.anchoredPosition = new Vector2(0f, -210f);

        CreateProductCellDivider(cell, -238f);

        CreateStepperRow(
            cell,
            "进货\n数量",
            0,
            -246f,
            data.PurchaseQuantity,
            value =>
            {
                data.PurchaseQuantity = value;
                RefreshMoneyDisplays();
                RefreshStockControlsInteractable();
            },
            () => CanAffordAdditionalPurchase(data));

        CreateStepperRow(
            cell,
            "出售\n价格",
            1,
            -292f,
            data.TodayPrice,
            value => data.TodayPrice = value,
            null,
            MaxProductTodayPrice(data),
            !data.PriceLocked,
            value => PriceColorForValue(value, data.BasePrice));
    }

    private void CreateStepperRow(
        RectTransform parent,
        string label,
        int rowId,
        float topOffset,
        int initialValue,
        Action<int> onValueChanged = null,
        Func<bool> canIncrease = null,
        int maxValue = int.MaxValue,
        bool canEditThisStepper = true,
        Func<int, Color> valueColorSelector = null)
    {
        var row = new GameObject($"StepperRow_{rowId}", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var rowRt = (RectTransform)row.transform;
        rowRt.anchorMin = new Vector2(0.06f, 1f);
        rowRt.anchorMax = new Vector2(0.94f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.sizeDelta = new Vector2(0f, 38f);
        rowRt.anchoredPosition = new Vector2(0f, topOffset);

        var lbl = CreateTMPText("Label", row.transform, $"{label}:", 20, FontStyles.Bold, TextAlignmentOptions.Left);
        ApplyInventoryTextWeight(lbl);
        var lblRt = (RectTransform)lbl.transform;
        lblRt.anchorMin = new Vector2(0f, 0f);
        lblRt.anchorMax = new Vector2(0.42f, 1f);
        lblRt.offsetMin = Vector2.zero;
        lblRt.offsetMax = Vector2.zero;

        var minus = CreateMiniButton(row.transform, "-", new Vector2(0.43f, 0.5f));
        var plus = CreateMiniButton(row.transform, "+", new Vector2(plusButtonAnchorX, 0.5f));

        var valueText = CreateTMPText("Value", row.transform, initialValue.ToString(), 22, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyInventoryTextWeight(valueText);
        var valueRt = (RectTransform)valueText.transform;
        valueRt.anchorMin = new Vector2(0.54f, 0f);
        valueRt.anchorMax = new Vector2(0.82f, 1f);
        valueRt.offsetMin = Vector2.zero;
        valueRt.offsetMax = Vector2.zero;
        valueText.enableAutoSizing = true;
        valueText.fontSizeMin = 16f;
        valueText.fontSizeMax = 22f;
        valueText.enableWordWrapping = false;
        valueText.overflowMode = TextOverflowModes.Overflow;

        var stepper = row.AddComponent<ShopUiStepper>();
        stepper.Bind(minus, plus, valueText, initialValue, onValueChanged, canIncrease, maxValue, canEditThisStepper, valueColorSelector);
        stepper.SetInteractable(canEditStockPlan);
        inventorySteppers.Add(stepper);
    }

    private static Color PriceColorForValue(int price, int basePrice)
    {
        if (basePrice <= 0)
        {
            return StatusDynamicTextColor;
        }

        float ratio = (float)price / basePrice;
        if (ratio > 1.5f || ratio < 0.5f)
        {
            return PriceDangerTextColor;
        }
        if (ratio > 1.3f || ratio < 0.7f)
        {
            return PriceWarningTextColor;
        }
        return StatusDynamicTextColor;
    }

    private Button CreateMiniButton(Transform parent, string sign, Vector2 anchor)
    {
        var btn = new GameObject($"Btn_{sign}", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(parent, false);

        var rt = (RectTransform)btn.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(30f, 30f);

        var img = btn.GetComponent<Image>();
        var spriteName = sign == "+" ? inventoryStepperPlusSpriteName : inventoryStepperMinusSpriteName;
        var btnSprite = ResolveUiDecorationSprite(inventoryStepperButtonResourcePath, inventoryStepperButtonAssetPath, spriteName);
        if (btnSprite != null)
        {
            img.sprite = btnSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
        }
        else
        {
            img.color = woodEdgeFade;
        }

        if (btnSprite == null)
        {
            var text = CreateTMPText("Sign", btn.transform, sign, 24, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchText(text, 0f);
        }

        var button = btn.GetComponent<Button>();
        button.targetGraphic = img;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private Sprite ResolveProductNameBannerSprite(int index)
    {
        if (ProductNameBannerSpriteCycle.Length == 0)
        {
            return null;
        }

        int spriteIndex = Mathf.Abs(index) % ProductNameBannerSpriteCycle.Length;
        return ResolveUiDecorationSprite(inventoryNameBannerResourcePath, inventoryNameBannerAssetPath, ProductNameBannerSpriteCycle[spriteIndex]);
    }

    private void CreateProductCellDivider(RectTransform parent, float topOffset)
    {
        var divider = new GameObject("CellDivider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(parent, false);
        var rt = (RectTransform)divider.transform;
        rt.anchorMin = new Vector2(0.14f, 1f);
        rt.anchorMax = new Vector2(0.86f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, topOffset);

        var image = divider.GetComponent<Image>();
        image.color = new Color(0.42f, 0.30f, 0.15f, 0.22f);
        image.raycastTarget = false;
    }


    private List<ShopProductModel> BuildMockProducts()
    {
        return new List<ShopProductModel>
        {
            new("瓶装水", 4, 0, 5, 40, false, 40, 5, 5),
            new("面包", 5, 0, 7, 60, false, 60, 7, 7),
            new("烤肉", 13, 0, 15, 30, false, 30, 15, 15),
            new("银戒指", 150, 0, 200, 10, false, 10, 200, 200),
            new("黄金", 950, 0, 1000, 10, false, 10, 1000, 1000),
        };
    }

    private void SetInventoryVisible(bool visible)
    {
        if (inventoryOverlayRoot != null)
        {
            inventoryOverlayRoot.SetActive(visible);
        }

        if (openInventoryButton != null)
        {
            openInventoryButton.gameObject.SetActive(!visible);
        }
    }

    public void OpenInventory()
    {
        SetInventoryVisible(true);
        RefreshStockControlsInteractable();
    }

    private void SubmitStockPlan()
    {
        if (!canEditStockPlan)
        {
            return;
        }

        int plannedCost = CalculatePlannedPurchaseCost();
        if (plannedCost > playerModel.CurrentMoney)
        {
            RefreshMoneyDisplays();
            RefreshStockControlsInteractable();
            Debug.LogWarning("[ShopAssistantUI] Stock plan rejected: not enough money.");
            return;
        }

        foreach (var product in marketProducts)
        {
            product.CurrentStock += product.PurchaseQuantity;
        }
        playerModel.CurrentMoney -= plannedCost;

        canEditStockPlan = false;
        RebuildProductCells();
        RefreshTopLeftStatus(currentRoundValue, playerModel.CurrentMoney, currentGameStateValue);
        RefreshStockControlsInteractable();

        string payload = BuildStockPlanUpdateJson();
        WsAgentClient.SubmitShopStockUpdateJson(payload);
        SetInventoryVisible(false);
        Debug.Log($"[ShopAssistantUI] Stock plan submitted: {payload}");
    }

    private void RefreshStockControlsInteractable()
    {
        bool canSubmit = canEditStockPlan && CalculatePreviewMoney() >= 0;
        if (stockInventoryButton != null)
        {
            stockInventoryButton.interactable = canSubmit;
            if (stockInventoryButton.targetGraphic != null)
            {
                stockInventoryButton.targetGraphic.color = canEditStockPlan ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        }

        foreach (var stepper in inventorySteppers)
        {
            if (stepper != null)
            {
                stepper.SetInteractable(canEditStockPlan);
            }
        }
    }

    private int CalculatePreviewMoney()
    {
        return playerModel.CurrentMoney - CalculatePlannedPurchaseCost();
    }

    private int CalculatePlannedPurchaseCost()
    {
        int total = 0;
        foreach (var product in marketProducts)
        {
            total += Mathf.Max(0, product.CostPrice) * Mathf.Max(0, product.PurchaseQuantity);
        }
        return total;
    }

    private bool CanAffordAdditionalPurchase(ShopProductModel product)
    {
        if (product == null)
        {
            return false;
        }

        int extraCost = Mathf.Max(0, product.CostPrice);
        return extraCost <= 0 || CalculatePreviewMoney() >= extraCost;
    }

    private static int MaxProductTodayPrice(ShopProductModel product)
    {
        if (product == null)
        {
            return 0;
        }

        return Mathf.Max(0, product.CostPrice * 2);
    }

    private void RefreshMoneyDisplays()
    {
        if (moneyText != null)
        {
            moneyText.text = playerModel.CurrentMoney.ToString();
        }
        if (rightPanelMoneyText != null)
        {
            int displayMoney = canEditStockPlan ? CalculatePreviewMoney() : playerModel.CurrentMoney;
            rightPanelMoneyText.text = $"资金：{displayMoney}";
        }
    }

    private string BuildStockPlanUpdateJson()
    {
        var payload = new ShopStockUpdatePayload
        {
            currentMoney = playerModel.CurrentMoney,
            todayIncome = playerModel.TodayIncome,
            items = new ShopStockUpdateItem[marketProducts.Count]
        };

        for (int i = 0; i < marketProducts.Count; i++)
        {
            var product = marketProducts[i];
            payload.items[i] = new ShopStockUpdateItem
            {
                name = product.ProductName,
                currentStock = product.CurrentStock,
                purchaseQuantity = product.PurchaseQuantity,
                todayPrice = product.TodayPrice,
                costPrice = product.CostPrice
            };
        }

        return JsonUtility.ToJson(payload);
    }


    private void ApplyInventoryWindowBackground(RectTransform window)
    {
        if (window == null)
        {
            return;
        }

        var windowImage = window.GetComponent<Image>();
        if (windowImage == null)
        {
            return;
        }

        var bgSprite = ResolveInventoryBackgroundSprite();
        if (bgSprite == null)
        {
            windowImage.color = paperColor;
            return;
        }

        windowImage.sprite = bgSprite;
        windowImage.type = Image.Type.Simple;
        windowImage.preserveAspect = true;
        windowImage.color = Color.white;

        var outline = window.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        var shadow = window.GetComponent<Shadow>();
        if (shadow != null)
        {
            shadow.enabled = false;
        }
    }

    private Sprite ResolveInventoryBackgroundSprite()
    {
        return ResolveUiDecorationSprite(inventoryBackgroundResourcePath, inventoryBackgroundAssetPath, inventoryBackgroundSpriteName);
    }

    private void CreateInventoryTitleSprite(RectTransform window)
    {
        var titleSprite = ResolveUiDecorationSprite(inventoryTitleResourcePath, inventoryTitleAssetPath, inventoryTitleSpriteName);
        if (titleSprite == null)
        {
            Debug.LogWarning("[ShopAssistantUI] Inventory title sprite missing, title image will be hidden.");
        }

        var titleObj = new GameObject("TitleSprite", typeof(RectTransform), typeof(Image));
        titleObj.transform.SetParent(window, false);

        var titleRt = (RectTransform)titleObj.transform;
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(520f, 88f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);

        var titleImage = titleObj.GetComponent<Image>();
        titleImage.sprite = titleSprite;
        titleImage.type = Image.Type.Simple;
        titleImage.preserveAspect = true;
        titleImage.color = titleSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        titleImage.raycastTarget = false;
    }

    private Button CreateInventoryStockSpriteButton(RectTransform window)
    {
        var buttonRoot = new GameObject("Btn_StockIn", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonRoot.transform.SetParent(window, false);

        var rt = (RectTransform)buttonRoot.transform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(392f, 96f);
        rt.anchoredPosition = new Vector2(0f, 20f);

        var buttonImage = buttonRoot.GetComponent<Image>();
        var buttonSprite = ResolveUiDecorationSprite(inventoryStockButtonResourcePath, inventoryStockButtonAssetPath, inventoryStockButtonSpriteName);
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
            Debug.LogWarning("[ShopAssistantUI] Inventory stock button sprite missing, fallback color button is used.");
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

    private Button CreateInventoryCloseSpriteButton(RectTransform window)
    {
        var buttonRoot = new GameObject("Btn_CloseInventory", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonRoot.transform.SetParent(window, false);

        var rt = (RectTransform)buttonRoot.transform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(72f, 72f);
        rt.anchoredPosition = new Vector2(-48f, -44f);

        var buttonImage = buttonRoot.GetComponent<Image>();
        var buttonSprite = ResolveUiDecorationSprite(inventoryCloseButtonResourcePath, inventoryCloseButtonAssetPath, inventoryCloseButtonSpriteName);
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
            Debug.LogWarning("[ShopAssistantUI] Inventory close button sprite missing, fallback color button is used.");
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

    private void CreateInventoryRightPanelSprite(RectTransform window)
    {
        var panelSprite = ResolveUiDecorationSprite(inventoryRightPanelResourcePath, inventoryRightPanelAssetPath, inventoryRightPanelSpriteName);
        if (panelSprite == null)
        {
            Debug.LogWarning("[ShopAssistantUI] Inventory right panel sprite missing, right panel image will be hidden.");
        }

        var panelObj = new GameObject("RightInfoPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(window, false);

        var panelRt = (RectTransform)panelObj.transform;
        panelRt.anchorMin = new Vector2(0.745f, 0.11f);
        panelRt.anchorMax = new Vector2(0.955f, 0.865f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        var panelImage = panelObj.GetComponent<Image>();
        panelImage.sprite = panelSprite;
        panelImage.type = Image.Type.Simple;
        // Keep height controlled by anchors; preserveAspect shrinks visible height unexpectedly here.
        panelImage.preserveAspect = false;
        panelImage.color = panelSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        panelImage.raycastTarget = false;

        BuildRightInfoPanelContent(panelRt);
    }

    private void BuildRightInfoPanelContent(RectTransform panelRt)
    {
        if (panelRt == null)
        {
            return;
        }

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(panelRt, false);
        var contentRt = (RectTransform)content.transform;
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = new Vector2(12f, 18f);
        contentRt.offsetMax = new Vector2(-16f, -18f);

        var logoSprite = ResolveUiDecorationSprite(inventoryShopLogoResourcePath, inventoryShopLogoAssetPath, inventoryShopLogoSpriteName);
        var logoObj = new GameObject("Logo", typeof(RectTransform), typeof(Image));
        logoObj.transform.SetParent(contentRt, false);
        var logoRt = (RectTransform)logoObj.transform;
        logoRt.anchorMin = new Vector2(0.10f, 0.73f);
        logoRt.anchorMax = new Vector2(0.90f, 0.96f);
        logoRt.offsetMin = Vector2.zero;
        logoRt.offsetMax = Vector2.zero;
        var logoImage = logoObj.GetComponent<Image>();
        logoImage.sprite = logoSprite;
        logoImage.type = Image.Type.Simple;
        logoImage.preserveAspect = true;
        logoImage.color = logoSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        logoImage.raycastTarget = false;

        var header = CreateTMPText("InfoHeader", contentRt, "-◆商店信息◆-", 26, FontStyles.Bold, TextAlignmentOptions.Center);
        var headerRt = (RectTransform)header.transform;
        headerRt.anchorMin = new Vector2(0.02f, 0.63f);
        headerRt.anchorMax = new Vector2(0.98f, 0.70f);
        headerRt.offsetMin = Vector2.zero;
        headerRt.offsetMax = Vector2.zero;
        header.enableWordWrapping = false;

        var ownerText = CreateTMPText("OwnerText", contentRt, "店主：Barabasi", 23, FontStyles.Bold, TextAlignmentOptions.Left);
        ApplyInventoryTextWeight(ownerText);
        var ownerRt = (RectTransform)ownerText.transform;
        ownerRt.anchorMin = new Vector2(0.08f, 0.57f);
        ownerRt.anchorMax = new Vector2(0.94f, 0.62f);
        ownerRt.offsetMin = Vector2.zero;
        ownerRt.offsetMax = Vector2.zero;
        ownerText.enableWordWrapping = false;

        CreateRightPanelDivider(contentRt, 0.545f);

        var moneyRow = new GameObject("MoneyRow", typeof(RectTransform));
        moneyRow.transform.SetParent(contentRt, false);
        var moneyRt = (RectTransform)moneyRow.transform;
        moneyRt.anchorMin = new Vector2(0.08f, 0.485f);
        moneyRt.anchorMax = new Vector2(0.94f, 0.535f);
        moneyRt.offsetMin = Vector2.zero;
        moneyRt.offsetMax = Vector2.zero;

        rightPanelMoneyText = CreateTMPText("MoneyText", moneyRt, $"资金：{playerModel.CurrentMoney}", 23, FontStyles.Bold, TextAlignmentOptions.Left);
        ApplyInventoryTextWeight(rightPanelMoneyText);
        var moneyTextRt = (RectTransform)rightPanelMoneyText.transform;
        moneyTextRt.anchorMin = new Vector2(0f, 0f);
        moneyTextRt.anchorMax = new Vector2(0.80f, 1f);
        moneyTextRt.offsetMin = Vector2.zero;
        moneyTextRt.offsetMax = Vector2.zero;
        rightPanelMoneyText.enableWordWrapping = false;

        var coinSprite = ResolveUiDecorationSprite(inventoryCoinFeatherResourcePath, inventoryCoinFeatherAssetPath, inventoryCoinSpriteName);
        var coinObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        coinObj.transform.SetParent(moneyRt, false);
        var coinRt = (RectTransform)coinObj.transform;
        coinRt.anchorMin = new Vector2(0.84f, 0.15f);
        coinRt.anchorMax = new Vector2(0.99f, 0.85f);
        coinRt.offsetMin = Vector2.zero;
        coinRt.offsetMax = Vector2.zero;
        var coinImage = coinObj.GetComponent<Image>();
        coinImage.sprite = coinSprite;
        coinImage.type = Image.Type.Simple;
        coinImage.preserveAspect = true;
        coinImage.color = coinSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        coinImage.raycastTarget = false;

        CreateRightPanelDivider(contentRt, 0.465f);

        var openingLabel = CreateTMPText("OpeningLabel", contentRt, "营业时间：", 23, FontStyles.Bold, TextAlignmentOptions.Left);
        ApplyInventoryTextWeight(openingLabel);
        var openingLabelRt = (RectTransform)openingLabel.transform;
        openingLabelRt.anchorMin = new Vector2(0.08f, 0.41f);
        openingLabelRt.anchorMax = new Vector2(0.94f, 0.46f);
        openingLabelRt.offsetMin = Vector2.zero;
        openingLabelRt.offsetMax = Vector2.zero;
        openingLabel.enableWordWrapping = false;

        var openingTime = CreateTMPText("OpeningTime", contentRt, "08:00 - 22:00", 23, FontStyles.Bold, TextAlignmentOptions.Left);
        ApplyInventoryTextWeight(openingTime);
        var openingTimeRt = (RectTransform)openingTime.transform;
        openingTimeRt.anchorMin = new Vector2(0.08f, 0.36f);
        openingTimeRt.anchorMax = new Vector2(0.94f, 0.41f);
        openingTimeRt.offsetMin = Vector2.zero;
        openingTimeRt.offsetMax = Vector2.zero;
        openingTime.enableWordWrapping = false;

        CreateRightPanelDivider(contentRt, 0.34f);

        var hintSprite = ResolveUiDecorationSprite(inventoryHintPanelResourcePath, inventoryHintPanelAssetPath, inventoryHintPanelSpriteName);
        var hintObj = new GameObject("HintPanel", typeof(RectTransform), typeof(Image));
        hintObj.transform.SetParent(contentRt, false);
        var hintRt = (RectTransform)hintObj.transform;
        hintRt.anchorMin = new Vector2(0.06f, 0.02f);
        hintRt.anchorMax = new Vector2(0.94f, 0.02f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.sizeDelta = new Vector2(0f, 10f);
        var hintAspect = hintObj.AddComponent<AspectRatioFitter>();
        hintAspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
        hintAspect.aspectRatio = 947f / 847f; // Match sprite rect ratio from meta.
        var hintImage = hintObj.GetComponent<Image>();
        hintImage.sprite = hintSprite;
        hintImage.type = Image.Type.Simple;
        hintImage.preserveAspect = true;
        hintImage.color = hintSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        hintImage.raycastTarget = false;

    }

    private void CreateRightPanelDivider(RectTransform parent, float yAnchor)
    {
        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(parent, false);
        var dividerRt = (RectTransform)divider.transform;
        dividerRt.anchorMin = new Vector2(0.14f, yAnchor);
        dividerRt.anchorMax = new Vector2(0.86f, yAnchor);
        dividerRt.sizeDelta = new Vector2(0f, 1f);
        dividerRt.anchoredPosition = Vector2.zero;

        var dividerImage = divider.GetComponent<Image>();
        dividerImage.color = new Color(0.42f, 0.30f, 0.15f, 0.22f);
        dividerImage.raycastTarget = false;
    }

}

