using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Runtime failure overlay for the shop assistant game-end screen.
/// </summary>
public sealed class GameEndFailureUI : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] [Range(0, 7)] private int targetDisplay = 0;
    [SerializeField] private string displayCanvasNameHint = "Display1";
    [SerializeField] private int baseResolutionX = 1920;
    [SerializeField] private int baseResolutionY = 1080;
    [SerializeField] private bool showOnStart;
    [SerializeField] private Key debugShowKey = Key.M;

    [Header("Sprites")]
    [SerializeField] private string failureTitleResourcePath = "Art/UI/UI/ShopAssistantUI/游戏结束";
    [SerializeField] private string failureTitleAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/游戏结束.png";
    [SerializeField] private string failureTitleSpriteName = "游戏结束";
    [SerializeField] private string panelResourcePath = "Art/UI/UI/ShopAssistantUI/失败背景板";
    [SerializeField] private string panelAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/失败背景板.png";
    [SerializeField] private string panelTopSpriteName = "背景框上片";
    [SerializeField] private string panelMiddleSpriteName = "背景框中片";
    [SerializeField] private string panelBottomSpriteName = "背景框下片";
    [SerializeField] private string statRowResourcePath = "Art/UI/UI/ShopAssistantUI/回合结束条目框";
    [SerializeField] private string statRowAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/回合结束条目框.png";
    [SerializeField] private string statRowLeftSpriteName = "回合结束条目框左侧";
    [SerializeField] private string statRowRightSpriteName = "回合结束条目框右侧";
    [SerializeField] private string statPluginResourcePath = "Art/UI/UI/ShopAssistantUI/回合结束插件";
    [SerializeField] private string statPluginAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/回合结束插件.png";
    [SerializeField] private string statLikeSpriteName = "点赞";
    [SerializeField] private string statBoxSpriteName = "货箱";
    [SerializeField] private string itemResourcePath = "UI/Item/base_goods";
    [SerializeField] private string itemAssetPath = "Assets/Resources/UI/Item/base_goods.png";
    [SerializeField] private string stampResourcePath = "Art/UI/UI/ShopAssistantUI/评级盖章";
    [SerializeField] private string stampAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/评级盖章.png";
    [SerializeField] private string stampBestSpriteName = "人上人";
    [SerializeField] private string stampGreatSpriteName = "NPC";
    [SerializeField] private string stampNormalSpriteName = "拉完了";
    [SerializeField] private string settlementButtonResourcePath = "Art/UI/UI/ShopAssistantUI/结算按钮";
    [SerializeField] private string settlementButtonAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/结算按钮.png";
    [SerializeField] private string restartButtonSpriteName = "重新开始";
    [SerializeField] private string menuButtonSpriteName = "回到菜单";

    [Header("Layout")]
    [SerializeField] private float panelWidth = 1000f;
    [SerializeField] private float panelTopHeight = 62f;
    [SerializeField] private float panelMiddleHeight = 396f;
    [SerializeField] private float panelBottomHeight = 72f;
    [SerializeField] private float panelOverlapPixels = 3f;
    [SerializeField] private float panelAnchoredY = -58f;
    [SerializeField] private float titleWidth = 695f;
    [SerializeField] private float titleHeight = 464f;
    [SerializeField] private float titleAnchoredX = 0f;
    [SerializeField] private float titleAnchoredY = 168f;
    [SerializeField] private float drawerOpenSeconds = 0.56f;

    [Header("Stat Rows")]
    [SerializeField] private int statRowCount = 5;
    [SerializeField] private float statRowHorizontalInset = 35f;
    [SerializeField] private float statRowHeight = 78f;
    [SerializeField] private float statRowSpacing = 0f;
    [SerializeField] private float statRowsTopInset = 15f;
    [SerializeField] private float statRowRightLeftCrop = 96f;
    [SerializeField] private float statRowIntroSeconds = 0.18f;
    [SerializeField] private float statRowIntroStaggerSeconds = 0.065f;
    [SerializeField] private float statTextIntroSeconds = 0.12f;
    [SerializeField] private float statValueCountSeconds = 0.48f;
    [SerializeField] private float statValuePulseSeconds = 0.20f;
    [SerializeField] private Color statRowFailureFilterColor = new Color(0.20f, 0.16f, 0.12f, 0.28f);
    [SerializeField] private bool showLikeMarks = false;
    [SerializeField] private float statLikeIntroSeconds = 0.42f;
    [SerializeField] private float statLikeSize = 58f;
    [SerializeField] private float statItemIconSize = 60f;
    [SerializeField] private float statGoodsItemIconScale = 1.18f;
    [SerializeField] private float stampSize = 392f;
    [SerializeField] private float stampIntroSeconds = 1.05f;

    [Header("Buttons")]
    [SerializeField] private Vector2 settlementButtonSize = new Vector2(330f, 106f);
    [SerializeField] private float settlementButtonGap = 120f;
    [SerializeField] private float settlementButtonTopGap = 24f;

    private static GameEndFailureUI instance;
    private static readonly string[] StatLabels =
    {
        "死因:",
        "最畅销物品:",
        "最多购入物品:",
        "总出售商品:",
        "总收入:"
    };
    private static readonly string[] ProductSpriteNames =
    {
        "瓶装水",
        "面包",
        "烤肉",
        "银戒指",
        "黄金"
    };

    private TMP_FontAsset uiFont;
    private GameObject overlayRoot;
    private CanvasGroup overlayCanvasGroup;
    private RectTransform panelRoot;
    private RectTransform middleClipRect;
    private RectTransform middleImageRect;
    private RectTransform bottomRect;
    private RectTransform statsRowsRoot;
    private RectTransform[] statRowRects;
    private CanvasGroup[] statRowCanvasGroups;
    private CanvasGroup[] statTextCanvasGroups;
    private TextMeshProUGUI[] statLabelTexts;
    private TextMeshProUGUI[] statValueTexts;
    private RectTransform[] statValueRects;
    private RectTransform[] statItemIconRects;
    private CanvasGroup[] statItemIconCanvasGroups;
    private Image[] statItemIconImages;
    private RectTransform[] statLikeRects;
    private CanvasGroup[] statLikeCanvasGroups;
    private Image[] statLikeImages;
    private RectTransform stampRect;
    private CanvasGroup stampCanvasGroup;
    private Image stampImage;
    private RectTransform restartButtonRect;
    private RectTransform menuButtonRect;
    private Sprite[] productSprites;
    private Sprite statBoxSprite;
    private FailureStatRowData[] statRowsData;
    private Coroutine showRoutine;
    private string failureReason = "林墨墨的体力值归零";
    private string gameEndInfoJson;
    private static int lastDebugInputFrame = -1;
    private const string TestGameEndInfoJson = "{\"result\":\"failure\",\"isVictory\":false,\"failureReason\":\"林墨墨的饥饿值归零\",\"stats\":{\"roundCount\":18,\"bestSellingItem\":{\"itemId\":\"item:meat\",\"shortItemId\":\"meat\",\"name\":\"烤肉\",\"quantity\":87},\"mostPurchasedItem\":{\"itemId\":\"item:gold\",\"shortItemId\":\"gold\",\"name\":\"黄金\",\"quantity\":356},\"totalSoldQuantity\":512,\"totalIncome\":12800}}";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<GameEndFailureUI>() != null)
        {
            return;
        }

        var root = new GameObject("UI_GameEndFailure");
        root.AddComponent<GameEndFailureUI>();
    }

    public static void ShowFailure()
    {
        ShowFailure(null);
    }

    public static void ShowFailure(string failureReason)
    {
        ShowFailure(failureReason, null);
    }

    public static void ShowFailure(string failureReason, string gameEndInfoJson)
    {
        var ui = instance != null ? instance : FindObjectOfType<GameEndFailureUI>();
        if (ui == null)
        {
            var root = new GameObject("UI_GameEndFailure");
            ui = root.AddComponent<GameEndFailureUI>();
        }

        ui.failureReason = string.IsNullOrWhiteSpace(failureReason) ? "经营失败" : failureReason.Trim();
        ui.gameEndInfoJson = string.IsNullOrWhiteSpace(gameEndInfoJson) ? TestGameEndInfoJson : gameEndInfoJson;
        ui.Show();
    }

    public void Show()
    {
        EnsureBuilt();
        if (overlayRoot == null)
        {
            return;
        }

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }

        showRoutine = StartCoroutine(PlayShowRoutine());
    }

    public void Hide()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
        }

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    private void Awake()
    {
        instance = this;
        uiFont = ResolveUiFont();
        EnsureBuilt();
        if (showOnStart)
        {
            Show();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        HandleDebugInput();
    }

    public static void HandleDebugInput()
    {
        if (lastDebugInputFrame == Time.frameCount)
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[Key.M].wasPressedThisFrame)
        {
            lastDebugInputFrame = Time.frameCount;
            ShowFailure(null, TestGameEndInfoJson);
        }
    }

    private void EnsureBuilt()
    {
        if (overlayRoot != null)
        {
            return;
        }

        var hostCanvas = ResolveOrCreateHostCanvas();
        BuildOverlay(hostCanvas.transform);
        Hide();
    }

    private void BuildOverlay(Transform parent)
    {
        overlayRoot = new GameObject("GameEndFailureOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlayRoot.transform.SetParent(parent, false);
        overlayRoot.transform.SetAsLastSibling();

        var overlayRt = (RectTransform)overlayRoot.transform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        overlayCanvasGroup = overlayRoot.GetComponent<CanvasGroup>();
        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;

        var overlayImage = overlayRoot.GetComponent<Image>();
        overlayImage.color = new Color(0.015f, 0.017f, 0.02f, 0.62f);
        overlayImage.raycastTarget = false;

        BuildPanel(overlayRoot.transform);
        BuildSettlementButtons(overlayRoot.transform);
        BuildFailureTitle(overlayRoot.transform);
    }

    private void BuildPanel(Transform parent)
    {
        panelRoot = CreateRect("FailurePanelRoot", parent);
        panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.sizeDelta = new Vector2(panelWidth, panelTopHeight + panelMiddleHeight + panelBottomHeight);
        panelRoot.anchoredPosition = new Vector2(0f, panelAnchoredY);

        middleClipRect = CreateRect("PanelMiddleClip", panelRoot);
        middleClipRect.anchorMin = new Vector2(0.5f, 1f);
        middleClipRect.anchorMax = new Vector2(0.5f, 1f);
        middleClipRect.pivot = new Vector2(0.5f, 1f);
        middleClipRect.sizeDelta = new Vector2(panelWidth, 0f);
        middleClipRect.anchoredPosition = new Vector2(0f, -panelTopHeight + panelOverlapPixels);
        middleClipRect.gameObject.AddComponent<RectMask2D>();

        var middleImage = CreateImage("PanelMiddleImage", middleClipRect, ResolveSprite(panelResourcePath, panelAssetPath, panelMiddleSpriteName));
        middleImageRect = middleImage.rectTransform;
        middleImageRect.anchorMin = new Vector2(0.5f, 1f);
        middleImageRect.anchorMax = new Vector2(0.5f, 1f);
        middleImageRect.pivot = new Vector2(0.5f, 1f);
        middleImageRect.sizeDelta = new Vector2(panelWidth, panelMiddleHeight);
        middleImageRect.anchoredPosition = Vector2.zero;

        var top = CreateImage("PanelTop", panelRoot, ResolveSprite(panelResourcePath, panelAssetPath, panelTopSpriteName));
        var topRect = top.rectTransform;
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.sizeDelta = new Vector2(panelWidth, panelTopHeight);
        topRect.anchoredPosition = Vector2.zero;

        var bottom = CreateImage("PanelBottom", panelRoot, ResolveSprite(panelResourcePath, panelAssetPath, panelBottomSpriteName));
        bottomRect = bottom.rectTransform;
        bottomRect.anchorMin = new Vector2(0.5f, 1f);
        bottomRect.anchorMax = new Vector2(0.5f, 1f);
        bottomRect.pivot = new Vector2(0.5f, 1f);
        bottomRect.sizeDelta = new Vector2(panelWidth, panelBottomHeight);
        bottomRect.anchoredPosition = new Vector2(0f, -panelTopHeight + panelOverlapPixels);

        BuildStatRows(panelRoot);
    }

    private void BuildStatRows(Transform parent)
    {
        statsRowsRoot = CreateRect("FailureStatRows", parent);
        statsRowsRoot.anchorMin = new Vector2(0.5f, 1f);
        statsRowsRoot.anchorMax = new Vector2(0.5f, 1f);
        statsRowsRoot.pivot = new Vector2(0.5f, 1f);
        statsRowsRoot.sizeDelta = new Vector2(StatRowWidth(), panelMiddleHeight + panelBottomHeight);
        statsRowsRoot.anchoredPosition = new Vector2(0f, -panelTopHeight - statRowsTopInset);

        int rowCount = Mathf.Max(0, statRowCount);
        statRowRects = new RectTransform[rowCount];
        statRowCanvasGroups = new CanvasGroup[rowCount];
        statTextCanvasGroups = new CanvasGroup[rowCount];
        statLabelTexts = new TextMeshProUGUI[rowCount];
        statValueTexts = new TextMeshProUGUI[rowCount];
        statValueRects = new RectTransform[rowCount];
        statItemIconRects = new RectTransform[rowCount];
        statItemIconCanvasGroups = new CanvasGroup[rowCount];
        statItemIconImages = new Image[rowCount];
        statLikeRects = new RectTransform[rowCount];
        statLikeCanvasGroups = new CanvasGroup[rowCount];
        statLikeImages = new Image[rowCount];

        var leftSprite = ResolveSprite(statRowResourcePath, statRowAssetPath, statRowLeftSpriteName);
        var rightSprite = ResolveSprite(statRowResourcePath, statRowAssetPath, statRowRightSpriteName);
        var likeSprite = showLikeMarks ? ResolveSprite(statPluginResourcePath, statPluginAssetPath, statLikeSpriteName) : null;
        productSprites = ResolveProductSprites();
        statBoxSprite = ResolveSprite(statPluginResourcePath, statPluginAssetPath, statBoxSpriteName);

        for (int i = 0; i < rowCount; i++)
        {
            var row = CreateRect($"FailureStatRow_{i + 1}", statsRowsRoot);
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(StatRowWidth(), statRowHeight);
            row.anchoredPosition = GetStatRowRestPosition(i);

            var group = row.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            BuildStatRowBackground(row, leftSprite, rightSprite);
            BuildStatRowForeground(i, row, likeSprite);
            BuildStatRowFailureFilter(row);

            row.gameObject.SetActive(false);
            statRowRects[i] = row;
            statRowCanvasGroups[i] = group;
        }

        BuildStamp(parent);
    }

    private void BuildStatRowBackground(RectTransform row, Sprite leftSprite, Sprite rightSprite)
    {
        float leftWidth = statRowHeight;
        if (leftSprite != null && leftSprite.rect.height > 0f)
        {
            leftWidth = statRowHeight * leftSprite.rect.width / leftSprite.rect.height;
        }

        var leftImage = CreateImage("RowLeft", row, leftSprite);
        var leftRect = leftImage.rectTransform;
        leftRect.anchorMin = new Vector2(0f, 0.5f);
        leftRect.anchorMax = new Vector2(0f, 0.5f);
        leftRect.pivot = new Vector2(0f, 0.5f);
        leftRect.sizeDelta = new Vector2(leftWidth, statRowHeight);
        leftRect.anchoredPosition = Vector2.zero;

        float rightWidth = Mathf.Max(1f, StatRowWidth() - leftWidth);
        var rightClip = CreateRect("RowRightClip", row);
        rightClip.anchorMin = new Vector2(0f, 0.5f);
        rightClip.anchorMax = new Vector2(0f, 0.5f);
        rightClip.pivot = new Vector2(0f, 0.5f);
        rightClip.sizeDelta = new Vector2(rightWidth, statRowHeight);
        rightClip.anchoredPosition = new Vector2(leftWidth, 0f);
        rightClip.gameObject.AddComponent<RectMask2D>();

        var rightImage = CreateImage("RowRight", rightClip, rightSprite);
        var rightRect = rightImage.rectTransform;
        rightRect.anchorMin = new Vector2(0f, 0.5f);
        rightRect.anchorMax = new Vector2(0f, 0.5f);
        rightRect.pivot = new Vector2(0f, 0.5f);
        float leftCrop = Mathf.Max(0f, statRowRightLeftCrop);
        rightRect.sizeDelta = new Vector2(rightWidth + leftCrop, statRowHeight);
        rightRect.anchoredPosition = new Vector2(-leftCrop, 0f);
    }

    private void BuildStatRowFailureFilter(RectTransform row)
    {
        var filter = CreateImage("FailureRowFilter", row, null);
        filter.color = statRowFailureFilterColor;
        filter.raycastTarget = false;

        var filterRect = filter.rectTransform;
        filterRect.anchorMin = Vector2.zero;
        filterRect.anchorMax = Vector2.one;
        filterRect.offsetMin = Vector2.zero;
        filterRect.offsetMax = Vector2.zero;
        filterRect.SetAsLastSibling();
    }

    private void BuildStatRowForeground(int index, RectTransform row, Sprite likeSprite)
    {
        var textGroupRect = CreateRect("RowTextGroup", row);
        textGroupRect.anchorMin = Vector2.zero;
        textGroupRect.anchorMax = Vector2.one;
        textGroupRect.offsetMin = Vector2.zero;
        textGroupRect.offsetMax = Vector2.zero;

        var textGroup = textGroupRect.gameObject.AddComponent<CanvasGroup>();
        textGroup.alpha = 0f;
        textGroup.interactable = false;
        textGroup.blocksRaycasts = false;

        float rowWidth = StatRowWidth();
        float leftLogoSpace = statRowHeight * 0.78f;
        float labelLeft = leftLogoSpace + 20f;
        float labelRight = rowWidth * 0.48f;

        var label = CreateTMPText("RowLabel", textGroupRect, LabelForIndex(index), 34f, FontStyles.Bold, TextAlignmentOptions.Left);
        label.rectTransform.anchorMin = new Vector2(0f, 0f);
        label.rectTransform.anchorMax = new Vector2(0f, 1f);
        label.rectTransform.pivot = new Vector2(0f, 0.5f);
        label.rectTransform.offsetMin = new Vector2(labelLeft, 0f);
        label.rectTransform.offsetMax = new Vector2(labelRight, 0f);

        bool productRow = IsProductValueRow(index);
        bool reasonRow = index == 0;
        float likeSlotWidth = statLikeSize + 34f;
        var value = CreateTMPText("RowValue", textGroupRect, "0", 34f, FontStyles.Bold, TextAlignmentOptions.Left);
        if (reasonRow)
        {
            value.alignment = TextAlignmentOptions.Right;
        }
        value.enableAutoSizing = true;
        value.fontSizeMin = reasonRow ? 18f : 24f;
        value.fontSizeMax = reasonRow ? 31f : 36f;
        value.color = new Color(0.08f, 0.50f, 0.16f, 1f);
        value.rectTransform.anchorMin = new Vector2(0f, 0f);
        value.rectTransform.anchorMax = new Vector2(0f, 1f);
        value.rectTransform.pivot = new Vector2(0f, 0.5f);
        // 商品行
        value.rectTransform.offsetMin = new Vector2(reasonRow ? rowWidth * 0.42f : rowWidth * 0.78f, 0f);
        value.rectTransform.offsetMax = new Vector2(reasonRow ? rowWidth - statItemIconSize * 2f : rowWidth - likeSlotWidth, 0f);

        var itemIcon = CreateImage("RowItemIcon", textGroupRect, null);
        var itemIconRect = itemIcon.rectTransform;
        itemIconRect.anchorMin = new Vector2(0f, 0.5f);
        itemIconRect.anchorMax = new Vector2(0f, 0.5f);
        itemIconRect.pivot = new Vector2(0.5f, 0.5f);
        float itemIconSize = StatItemIconSizeForIndex(index);
        itemIconRect.sizeDelta = new Vector2(itemIconSize, itemIconSize);
        // 商品行数字
        itemIconRect.anchoredPosition = new Vector2(rowWidth * 0.74f, 0f);
        itemIcon.preserveAspect = true;

        var itemIconGroup = itemIconRect.gameObject.AddComponent<CanvasGroup>();
        itemIconGroup.alpha = productRow ? 1f : 0f;
        itemIconGroup.interactable = false;
        itemIconGroup.blocksRaycasts = false;
        itemIconRect.gameObject.SetActive(productRow);

        var likeImage = CreateImage("RowLike", row, likeSprite);
        var likeRect = likeImage.rectTransform;
        likeRect.anchorMin = new Vector2(1f, 0.5f);
        likeRect.anchorMax = new Vector2(1f, 0.5f);
        likeRect.pivot = new Vector2(0.5f, 0.5f);
        likeRect.sizeDelta = new Vector2(statLikeSize, statLikeSize);
        likeRect.anchoredPosition = new Vector2(-44f, 0f);
        likeImage.preserveAspect = true;

        var likeGroup = likeRect.gameObject.AddComponent<CanvasGroup>();
        likeGroup.alpha = 0f;
        likeGroup.interactable = false;
        likeGroup.blocksRaycasts = false;
        likeRect.gameObject.SetActive(false);

        statTextCanvasGroups[index] = textGroup;
        statLabelTexts[index] = label;
        statValueTexts[index] = value;
        statValueRects[index] = value.rectTransform;
        statItemIconRects[index] = itemIconRect;
        statItemIconCanvasGroups[index] = itemIconGroup;
        statItemIconImages[index] = itemIcon;
        statLikeRects[index] = likeRect;
        statLikeCanvasGroups[index] = likeGroup;
        statLikeImages[index] = likeImage;
    }

    private void BuildStamp(Transform parent)
    {
        var stamp = CreateImage("FailureGradeStamp", parent, null);
        stampRect = stamp.rectTransform;
        stampRect.anchorMin = new Vector2(0.5f, 1f);
        stampRect.anchorMax = new Vector2(0.5f, 1f);
        stampRect.pivot = new Vector2(0.5f, 0.5f);
        stampRect.sizeDelta = new Vector2(stampSize, stampSize);
        stampRect.anchoredPosition = new Vector2(0f, -panelTopHeight - panelMiddleHeight * 0.55f);
        stamp.preserveAspect = true;
        stamp.raycastTarget = false;

        stampCanvasGroup = stampRect.gameObject.AddComponent<CanvasGroup>();
        stampCanvasGroup.alpha = 0f;
        stampCanvasGroup.interactable = false;
        stampCanvasGroup.blocksRaycasts = false;
        stampRect.gameObject.SetActive(false);
        stampImage = stamp;
    }

    private void BuildFailureTitle(Transform parent)
    {
        var title = CreateImage("FailureTitleHat", parent, ResolveSprite(failureTitleResourcePath, failureTitleAssetPath, failureTitleSpriteName));
        title.preserveAspect = true;
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(titleWidth, titleHeight);
        titleRt.anchoredPosition = new Vector2(titleAnchoredX, titleAnchoredY);
        title.transform.SetAsLastSibling();
    }

    private void BuildSettlementButtons(Transform parent)
    {
        var restartSprite = ResolveSprite(settlementButtonResourcePath, settlementButtonAssetPath, restartButtonSpriteName);
        var menuSprite = ResolveSprite(settlementButtonResourcePath, settlementButtonAssetPath, menuButtonSpriteName);

        float panelTotalHeight = panelTopHeight + panelMiddleHeight + panelBottomHeight;
        float buttonY = panelAnchoredY - panelTotalHeight * 0.5f - settlementButtonTopGap - settlementButtonSize.y * 0.5f;
        float halfGap = settlementButtonGap * 0.5f;
        float halfButtonWidth = settlementButtonSize.x * 0.5f;

        restartButtonRect = CreateSettlementButton(
            "FailureRestartButton",
            parent,
            restartSprite,
            new Vector2(-halfGap - halfButtonWidth, buttonY));
        BindButtonClick(restartButtonRect, RestartGame);

        menuButtonRect = CreateSettlementButton(
            "FailureMenuButton",
            parent,
            menuSprite,
            new Vector2(halfGap + halfButtonWidth, buttonY));
    }

    private RectTransform CreateSettlementButton(string name, Transform parent, Sprite sprite, Vector2 anchoredPosition)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = settlementButtonSize;
        rt.anchoredPosition = anchoredPosition;

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = SettlementButtonColors();
        button.onClick.AddListener(Hide);
        return rt;
    }

    private static void BindButtonClick(RectTransform rect, UnityEngine.Events.UnityAction action)
    {
        if (rect == null || action == null)
        {
            return;
        }

        var button = rect.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void RestartGame()
    {
        Hide();
        WsAgentClient.RequestGameResetAfterSceneReload();
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    private IEnumerator PlayShowRoutine()
    {
        overlayRoot.SetActive(true);
        overlayRoot.transform.SetAsLastSibling();
        overlayCanvasGroup.alpha = 1f;
        overlayCanvasGroup.interactable = true;
        overlayCanvasGroup.blocksRaycasts = true;

        SetDrawerProgress(0f);
        PrepareStatRows();
        ResetStatRows();

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, drawerOpenSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetDrawerProgress(Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        SetDrawerProgress(1f);
        yield return PlayStatRowsIntro();
        yield return PlayGradeStampIntro();
        showRoutine = null;
    }

    private void ResetStatRows()
    {
        if (statRowRects == null || statRowCanvasGroups == null)
        {
            return;
        }

        for (int i = 0; i < statRowRects.Length; i++)
        {
            var row = statRowRects[i];
            var group = i < statRowCanvasGroups.Length ? statRowCanvasGroups[i] : null;
            if (row == null)
            {
                continue;
            }

            row.gameObject.SetActive(false);
            row.anchoredPosition = GetStatRowRestPosition(i) + new Vector2(0f, 18f);
            row.localScale = Vector3.one * 0.96f;
            if (group != null)
            {
                group.alpha = 0f;
            }

            if (statTextCanvasGroups != null && i < statTextCanvasGroups.Length && statTextCanvasGroups[i] != null)
            {
                statTextCanvasGroups[i].alpha = 0f;
            }

            if (statValueTexts != null && i < statValueTexts.Length && statValueTexts[i] != null)
            {
                statValueTexts[i].text = "0";
            }

            if (statValueRects != null && i < statValueRects.Length && statValueRects[i] != null)
            {
                statValueRects[i].localScale = Vector3.one;
            }

            if (statItemIconRects != null && i < statItemIconRects.Length && statItemIconRects[i] != null)
            {
                bool productRow = IsProductValueRow(i);
                statItemIconRects[i].gameObject.SetActive(productRow);
                statItemIconRects[i].localScale = Vector3.one;
                statItemIconRects[i].localRotation = Quaternion.identity;
            }

            if (statItemIconCanvasGroups != null && i < statItemIconCanvasGroups.Length && statItemIconCanvasGroups[i] != null)
            {
                statItemIconCanvasGroups[i].alpha = IsProductValueRow(i) ? 1f : 0f;
            }

            if (statLikeRects != null && i < statLikeRects.Length && statLikeRects[i] != null)
            {
                statLikeRects[i].gameObject.SetActive(false);
                statLikeRects[i].localScale = Vector3.zero;
                statLikeRects[i].localRotation = Quaternion.identity;
            }

            if (statLikeCanvasGroups != null && i < statLikeCanvasGroups.Length && statLikeCanvasGroups[i] != null)
            {
                statLikeCanvasGroups[i].alpha = 0f;
            }
        }

        if (stampRect != null)
        {
            stampRect.gameObject.SetActive(false);
            stampRect.localScale = Vector3.zero;
            stampRect.localRotation = Quaternion.identity;
        }

        if (stampCanvasGroup != null)
        {
            stampCanvasGroup.alpha = 0f;
        }
    }

    private IEnumerator PlayStatRowsIntro()
    {
        if (statRowRects == null || statRowCanvasGroups == null)
        {
            yield break;
        }

        for (int i = 0; i < statRowRects.Length; i++)
        {
            yield return PlaySingleStatRowSequence(i);
            if (statRowIntroStaggerSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(statRowIntroStaggerSeconds);
            }
        }
    }

    private IEnumerator PlaySingleStatRowSequence(int index)
    {
        yield return PlaySingleStatRowIntro(index);
        yield return PlayStatTextIntro(index);
        yield return PlayStatValueIntro(index);
        yield return PlayStatValuePulse(index);

        if (showLikeMarks && statRowsData != null && index < statRowsData.Length && statRowsData[index].liked)
        {
            yield return PlayStatLikeIntro(index);
        }
    }

    private IEnumerator PlaySingleStatRowIntro(int index)
    {
        if (index < 0 || statRowRects == null || index >= statRowRects.Length)
        {
            yield break;
        }

        var row = statRowRects[index];
        var group = statRowCanvasGroups != null && index < statRowCanvasGroups.Length ? statRowCanvasGroups[index] : null;
        if (row == null)
        {
            yield break;
        }

        Vector2 restPos = GetStatRowRestPosition(index);
        Vector2 startPos = restPos + new Vector2(0f, 18f);
        row.gameObject.SetActive(true);
        row.anchoredPosition = startPos;
        row.localScale = Vector3.one * 0.96f;
        if (group != null)
        {
            group.alpha = 0f;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, statRowIntroSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBack(t);
            row.anchoredPosition = Vector2.LerpUnclamped(startPos, restPos, eased);
            row.localScale = Vector3.one * Mathf.LerpUnclamped(0.96f, 1f, eased);
            if (group != null)
            {
                group.alpha = Mathf.SmoothStep(0f, 1f, t);
            }
            yield return null;
        }

        row.anchoredPosition = restPos;
        row.localScale = Vector3.one;
        if (group != null)
        {
            group.alpha = 1f;
        }
    }

    private IEnumerator PlayStatTextIntro(int index)
    {
        var group = statTextCanvasGroups != null && index < statTextCanvasGroups.Length ? statTextCanvasGroups[index] : null;
        if (group == null)
        {
            yield break;
        }

        group.alpha = 0f;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, statTextIntroSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }

        group.alpha = 1f;
    }

    private IEnumerator PlayStatValueIntro(int index)
    {
        if (statRowsData == null || index >= statRowsData.Length || statValueTexts == null || index >= statValueTexts.Length)
        {
            yield break;
        }

        var valueText = statValueTexts[index];
        if (valueText == null)
        {
            yield break;
        }

        var data = statRowsData[index];
        var itemImage = statItemIconImages != null && index < statItemIconImages.Length ? statItemIconImages[index] : null;
        var itemRect = statItemIconRects != null && index < statItemIconRects.Length ? statItemIconRects[index] : null;
        if (data.isTextValue)
        {
            valueText.text = data.displayValue;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, statValueCountSeconds * 0.35f));
            yield break;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, statValueCountSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            if (data.hasProductIcon && itemImage != null && (data.fixedProductIcon || (productSprites != null && productSprites.Length > 0)))
            {
                if (data.fixedProductIcon)
                {
                    itemImage.sprite = data.productSprite;
                }
                else
                {
                    int cycle = Mathf.FloorToInt(t * productSprites.Length * 3.2f);
                    itemImage.sprite = t >= 0.92f ? data.productSprite : productSprites[cycle % productSprites.Length];
                }
                itemImage.color = Color.white;
                if (itemRect != null)
                {
                    float flicker = data.fixedProductIcon ? 0f : Mathf.Sin(t * Mathf.PI * 16f) * Mathf.Lerp(1f, 0f, t);
                    itemRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.08f, Mathf.Abs(flicker));
                    itemRect.localRotation = Quaternion.Euler(0f, 0f, flicker * 5f);
                }
            }

            int shown = Mathf.RoundToInt(Mathf.Lerp(0f, data.numericValue, eased));
            valueText.text = shown.ToString();

            yield return null;
        }

        valueText.text = data.displayValue;
        if (data.hasProductIcon && itemImage != null)
        {
            itemImage.sprite = data.productSprite;
            itemImage.color = Color.white;
        }

        if (itemRect != null)
        {
            itemRect.localScale = Vector3.one;
            itemRect.localRotation = Quaternion.identity;
        }
    }

    private IEnumerator PlayStatValuePulse(int index)
    {
        var valueRect = statValueRects != null && index < statValueRects.Length ? statValueRects[index] : null;
        if (valueRect == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, statValuePulseSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            valueRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.18f, pulse);
            yield return null;
        }

        valueRect.localScale = Vector3.one;
    }

    private IEnumerator PlayStatLikeIntro(int index)
    {
        var likeRect = statLikeRects != null && index < statLikeRects.Length ? statLikeRects[index] : null;
        var likeGroup = statLikeCanvasGroups != null && index < statLikeCanvasGroups.Length ? statLikeCanvasGroups[index] : null;
        if (likeRect == null)
        {
            yield break;
        }

        likeRect.gameObject.SetActive(true);
        likeRect.localScale = Vector3.zero;
        likeRect.localRotation = Quaternion.identity;
        if (likeGroup != null)
        {
            likeGroup.alpha = 0f;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, statLikeIntroSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pop = EaseOutBack(Mathf.InverseLerp(0f, 0.42f, t));
            float shake = Mathf.Sin(t * Mathf.PI * 7f) * Mathf.Lerp(14f, 0f, t);
            likeRect.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, pop);
            likeRect.localRotation = Quaternion.Euler(0f, 0f, shake);
            if (likeGroup != null)
            {
                likeGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.26f, t));
            }
            yield return null;
        }

        likeRect.localScale = Vector3.one;
        likeRect.localRotation = Quaternion.identity;
        if (likeGroup != null)
        {
            likeGroup.alpha = 1f;
        }
    }

    private IEnumerator PlayGradeStampIntro()
    {
        if (stampRect == null || stampImage == null)
        {
            yield break;
        }

        int likeCount = CountLikedRows();
        string spriteName = likeCount >= 4 ? stampBestSpriteName : likeCount == 3 ? stampGreatSpriteName : stampNormalSpriteName;
        stampImage.sprite = ResolveSprite(stampResourcePath, stampAssetPath, spriteName);
        stampImage.color = stampImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);

        float power = likeCount >= 4 ? 0.92f : likeCount == 3 ? 0.82f : 0.74f;
        Vector2 restPos = new Vector2(0f, -panelTopHeight - panelMiddleHeight * 0.55f);
        Vector2 startPos = restPos + new Vector2(0f, 28f);
        stampRect.gameObject.SetActive(true);
        stampRect.anchoredPosition = startPos;
        stampRect.localScale = Vector3.one * 2.75f * power;
        stampRect.localRotation = Quaternion.Euler(0f, 0f, -8f);
        if (stampCanvasGroup != null)
        {
            stampCanvasGroup.alpha = 0f;
        }

        float elapsed = 0f;
        float windupSeconds = Mathf.Max(0.01f, stampIntroSeconds * 0.30f);
        while (elapsed < windupSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / windupSeconds);
            stampRect.localScale = Vector3.one * Mathf.Lerp(2.75f * power, 2.92f * power, Mathf.SmoothStep(0f, 1f, t));
            stampRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-8f, -10f, t));
            stampRect.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(0f, 8f), t);
            if (stampCanvasGroup != null)
            {
                stampCanvasGroup.alpha = Mathf.SmoothStep(0f, 0.92f, t);
            }
            yield return null;
        }

        elapsed = 0f;
        float slamSeconds = Mathf.Max(0.01f, stampIntroSeconds * 0.34f);
        while (elapsed < slamSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slamSeconds);
            float eased = t * t * (3f - 2f * t);
            stampRect.localScale = Vector3.one * Mathf.LerpUnclamped(2.92f * power, 1f, eased);
            stampRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(-10f, -3f, eased));
            stampRect.anchoredPosition = Vector2.LerpUnclamped(startPos + new Vector2(0f, 8f), restPos, eased);
            if (stampCanvasGroup != null)
            {
                stampCanvasGroup.alpha = 1f;
            }
            yield return null;
        }

        stampRect.localScale = Vector3.one;
        stampRect.localRotation = Quaternion.Euler(0f, 0f, -3f);
        stampRect.anchoredPosition = restPos;
        yield return PlayStampImpact(power);

        stampRect.localScale = Vector3.one;
        stampRect.localRotation = Quaternion.Euler(0f, 0f, -3f);
        stampRect.anchoredPosition = restPos;
        if (stampCanvasGroup != null)
        {
            stampCanvasGroup.alpha = 1f;
        }
    }

    private IEnumerator PlayStampImpact(float power)
    {
        if (panelRoot == null)
        {
            yield break;
        }

        Vector2 panelRestPos = new Vector2(0f, panelAnchoredY);
        Vector2 stampRestPos = stampRect != null ? stampRect.anchoredPosition : Vector2.zero;
        float elapsed = 0f;
        const float impactSeconds = 0.15f;
        while (elapsed < impactSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / impactSeconds);
            float decay = 1f - t;
            float hit = Mathf.Sin(t * Mathf.PI * 6f) * decay;
            panelRoot.anchoredPosition = panelRestPos + new Vector2(hit * 5f * power, Mathf.Abs(hit) * -3f * power);
            if (stampRect != null)
            {
                stampRect.anchoredPosition = stampRestPos + new Vector2(hit * 1.4f * power, Mathf.Abs(hit) * -0.8f * power);
            }
            yield return null;
        }

        panelRoot.anchoredPosition = panelRestPos;
        if (stampRect != null)
        {
            stampRect.anchoredPosition = stampRestPos;
        }
    }

    private int CountLikedRows()
    {
        if (statRowsData == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 1; i < statRowsData.Length; i++)
        {
            if (statRowsData[i].liked)
            {
                count++;
            }
        }
        return count;
    }

    private Vector2 GetStatRowRestPosition(int index)
    {
        return new Vector2(0f, -index * (statRowHeight + statRowSpacing));
    }

    private float StatRowWidth()
    {
        return Mathf.Max(1f, panelWidth - statRowHorizontalInset * 2f);
    }

    private static bool IsProductValueRow(int index)
    {
        return index == 1 || index == 2 || index == 3;
    }

    private float StatItemIconSizeForIndex(int index)
    {
        return index == 1 || index == 2 ? statItemIconSize * statGoodsItemIconScale : statItemIconSize;
    }

    private Sprite[] ResolveProductSprites()
    {
        var sprites = new Sprite[ProductSpriteNames.Length];
        for (int i = 0; i < ProductSpriteNames.Length; i++)
        {
            sprites[i] = ResolveSprite(itemResourcePath, itemAssetPath, ProductSpriteNames[i]);
        }
        return sprites;
    }

    private Sprite ResolveProductSprite(int index)
    {
        if (productSprites == null || productSprites.Length == 0)
        {
            return null;
        }

        return productSprites[Mathf.Abs(index) % productSprites.Length];
    }

    private void PrepareStatRows()
    {
        int count = Mathf.Max(0, statRowCount);
        statRowsData = new FailureStatRowData[count];
        var info = ParseGameEndInfo(string.IsNullOrWhiteSpace(gameEndInfoJson) ? TestGameEndInfoJson : gameEndInfoJson);
        var fallback = ParseGameEndInfo(TestGameEndInfoJson);
        var stats = info != null && info.stats != null ? info.stats : fallback.stats;
        string reason = info != null && !string.IsNullOrWhiteSpace(info.failureReason)
            ? info.failureReason
            : failureReason;

        for (int i = 0; i < count; i++)
        {
            if (i == 0)
            {
                statRowsData[i] = new FailureStatRowData
                {
                    displayValue = string.IsNullOrWhiteSpace(reason) ? "经营失败" : reason,
                    numericValue = 0,
                    hasProductIcon = false,
                    productSprite = null,
                    fixedProductIcon = false,
                    isTextValue = true,
                    liked = false
                };
                continue;
            }

            if (IsProductValueRow(i))
            {
                GameEndItemStat item = null;
                int productValue;
                Sprite productSprite;
                bool fixedIcon = false;
                if (i == 1)
                {
                    item = stats.bestSellingItem;
                    productValue = Mathf.Max(0, item != null ? item.quantity : 0);
                    productSprite = ResolveStatItemSprite(item);
                }
                else if (i == 2)
                {
                    item = stats.mostPurchasedItem;
                    productValue = Mathf.Max(0, item != null ? item.quantity : 0);
                    productSprite = ResolveStatItemSprite(item);
                }
                else
                {
                    productValue = Mathf.Max(0, stats.totalSoldQuantity);
                    productSprite = statBoxSprite;
                    fixedIcon = true;
                }
                statRowsData[i] = new FailureStatRowData
                {
                    displayValue = productValue.ToString(),
                    numericValue = productValue,
                    hasProductIcon = true,
                    productSprite = productSprite,
                    fixedProductIcon = fixedIcon,
                    isTextValue = false,
                    liked = productValue > 0
                };
                continue;
            }

            int numericValue = Mathf.Max(0, stats.totalIncome);
            statRowsData[i] = new FailureStatRowData
            {
                displayValue = numericValue.ToString(),
                numericValue = numericValue,
                hasProductIcon = false,
                productSprite = null,
                fixedProductIcon = false,
                isTextValue = false,
                liked = numericValue > 0
            };
        }
    }

    private Sprite ResolveStatItemSprite(GameEndItemStat item)
    {
        if (item != null)
        {
            var sprite = ResolveSprite(itemResourcePath, itemAssetPath, item.name);
            if (sprite != null)
            {
                return sprite;
            }
        }
        return ResolveProductSprite(0);
    }

    private static GameEndInfoPayload ParseGameEndInfo(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }
        try
        {
            return JsonUtility.FromJson<GameEndInfoPayload>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void SetDrawerProgress(float progress)
    {
        float visibleMiddleHeight = Mathf.Lerp(0f, panelMiddleHeight, Mathf.Clamp01(progress));
        float clipHeight = visibleMiddleHeight + panelOverlapPixels;
        if (middleClipRect != null)
        {
            middleClipRect.sizeDelta = new Vector2(panelWidth, clipHeight);
        }

        if (middleImageRect != null)
        {
            middleImageRect.sizeDelta = new Vector2(panelWidth, panelMiddleHeight);
            middleImageRect.anchoredPosition = Vector2.zero;
        }

        if (bottomRect != null)
        {
            bottomRect.anchoredPosition = new Vector2(0f, -panelTopHeight - visibleMiddleHeight + panelOverlapPixels);
        }
    }

    private Canvas ResolveOrCreateHostCanvas()
    {
        var allCanvases = FindObjectsOfType<Canvas>(true);

        foreach (var existing in allCanvases)
        {
            if (existing == null)
            {
                continue;
            }

            if (IsDisplayNamedCanvas(existing.gameObject))
            {
                existing.targetDisplay = targetDisplay;
                EnsureCanvasInputComponents(existing);
                return existing;
            }
        }

        var canvasGo = new GameObject("GameEndFailureCanvas_Display1", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = targetDisplay;
        canvas.sortingOrder = 360;
        canvas.pixelPerfect = true;
        EnsureCanvasInputComponents(canvas);
        return canvas;
    }

    private bool IsDisplayNamedCanvas(GameObject go)
    {
        if (go == null)
        {
            return false;
        }

        if (NameMatchesDisplay(go.name))
        {
            return true;
        }

        var parent = go.transform.parent;
        return parent != null && NameMatchesDisplay(parent.name);
    }

    private bool NameMatchesDisplay(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        string lowered = objectName.ToLowerInvariant();
        return lowered.Contains(displayCanvasNameHint.ToLowerInvariant()) || lowered.Contains("display 1");
    }

    private void EnsureCanvasInputComponents(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(baseResolutionX, baseResolutionY);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = false;
        return image;
    }

    private static Sprite ResolveSprite(string resourcePath, string assetPath, string spriteName)
    {
        if (!string.IsNullOrWhiteSpace(resourcePath))
        {
            var sprites = Resources.LoadAll<Sprite>(resourcePath.Trim());
            var sprite = FindSpriteByName(sprites, spriteName);
            if (sprite != null)
            {
                return sprite;
            }
        }

#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(assetPath))
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath.Trim());
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite && NameMatches(sprite.name, spriteName))
                {
                    return sprite;
                }
            }
        }
#endif

        Debug.LogWarning($"[GameEndFailureUI] UI sprite not found: {assetPath} ({spriteName})");
        return null;
    }

    private static Sprite FindSpriteByName(Sprite[] sprites, string spriteName)
    {
        if (sprites == null || sprites.Length == 0)
        {
            return null;
        }

        foreach (var sprite in sprites)
        {
            if (sprite != null && NameMatches(sprite.name, spriteName))
            {
                return sprite;
            }
        }

        return string.IsNullOrWhiteSpace(spriteName) || sprites.Length == 1 ? sprites[0] : null;
    }

    private static bool NameMatches(string actualName, string requestedName)
    {
        if (string.IsNullOrWhiteSpace(actualName) || string.IsNullOrWhiteSpace(requestedName))
        {
            return false;
        }

        var trimmed = requestedName.Trim();
        return string.Equals(actualName, trimmed, StringComparison.Ordinal)
            || string.Equals(actualName, $"{trimmed}_0", StringComparison.Ordinal)
            || actualName.StartsWith($"{trimmed}_", StringComparison.Ordinal);
    }

    private static ColorBlock SettlementButtonColors()
    {
        var colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.05f;
        return colors;
    }

    private TextMeshProUGUI CreateTMPText(string name, Transform parent, string content, float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = align;
        text.color = new Color(0.05f, 0.045f, 0.04f, 1f);
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        if (uiFont != null)
        {
            text.font = uiFont;
        }

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.70f);
        outline.effectDistance = new Vector2(1.4f, -1.4f);
        outline.useGraphicAlpha = false;
        return text;
    }

    private static string LabelForIndex(int index)
    {
        return index >= 0 && index < StatLabels.Length ? StatLabels[index] : "统计条目";
    }

    private static TMP_FontAsset ResolveUiFont()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SIMHEI SDF");
        return font != null ? font : TMP_Settings.defaultFontAsset;
    }

    private struct FailureStatRowData
    {
        public string displayValue;
        public int numericValue;
        public bool hasProductIcon;
        public Sprite productSprite;
        public bool fixedProductIcon;
        public bool isTextValue;
        public bool liked;
    }

    [Serializable]
    private sealed class GameEndInfoPayload
    {
        public string result;
        public bool isVictory;
        public string failureReason;
        public GameEndStats stats;
    }

    [Serializable]
    private sealed class GameEndStats
    {
        public int roundCount;
        public GameEndItemStat bestSellingItem;
        public GameEndItemStat mostPurchasedItem;
        public int totalSoldQuantity;
        public int totalIncome;
    }

    [Serializable]
    private sealed class GameEndItemStat
    {
        public string itemId;
        public string shortItemId;
        public string name;
        public int quantity;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = Mathf.Clamp01(t) - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }
}
