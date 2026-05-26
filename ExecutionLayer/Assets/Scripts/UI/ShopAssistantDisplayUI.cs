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

/// <summary>
/// Runtime UI for ShopAssistant inventory. It accepts market information from backend
/// and renders products dynamically.
/// </summary>
public sealed partial class ShopAssistantDisplayUI : MonoBehaviour
{
    private const int FirstRoundIndex = 1;

    [Header("Display")]
    [SerializeField] [Range(0, 7)] private int targetDisplay = 0; // Display1 (0-based index)
    [SerializeField] private string displayCanvasNameHint = "Display1";
    [SerializeField] private int baseResolutionX = 1920;
    [SerializeField] private int baseResolutionY = 1080;

    [Header("Theme")]
    [SerializeField] private Color paperColor = new(0.97f, 0.94f, 0.84f, 0.96f);
    [SerializeField] private Color woodColor = new(0.83f, 0.64f, 0.13f, 1.0f); // #d3a421
    [SerializeField] private Color woodEdgeFade = new(0.93f, 0.80f, 0.46f, 0.95f);
    [SerializeField] private Color textColor = new(0.23f, 0.17f, 0.08f, 1.0f);

    [Header("Style Tunables")]
    [SerializeField] [Range(2f, 12f)] private float panelOutlineThickness = 6f;
    [SerializeField] [Range(1f, 12f)] private float frameBorderThickness = 5f;
    [SerializeField] [Range(0f, 14f)] private float frameBorderInset = 5f;
    [SerializeField] [Range(1f, 8f)] private float statusRowLineThickness = 2f;
    [SerializeField] [Range(0.75f, 0.95f)] private float plusButtonAnchorX = 0.92f;

    [Header("Mock Data")]
    [SerializeField] private int initialRound = 1;
    [SerializeField] private int initialMoney = 1000;
    [SerializeField] private string initialGameState = "回合进行中";

    [Header("Product Images")]
    [SerializeField] private List<ProductImageMapping> productImageMappings = new();
    [SerializeField] private string productImageMappingCsvResourcePath = "ShopAssistant/ProductImageMappings";

    [Header("Inventory Background")]
    [SerializeField] private string inventoryBackgroundResourcePath = "Art/UI/UI/ShopAssistantUI/背景";
    [SerializeField] private string inventoryBackgroundAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/背景.png";
    [SerializeField] private string inventoryBackgroundSpriteName = "背景";

    [Header("Inventory Decorations")]
    [SerializeField] private string statusPanelResourcePath = "Art/UI/UI/ShopAssistantUI/状态面板";
    [SerializeField] private string statusPanelAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/状态面板.png";
    [SerializeField] private string statusPanelSpriteName = "状态面板";
    [SerializeField] private string inventoryTitleResourcePath = "Art/UI/UI/ShopAssistantUI/商店库存标头";
    [SerializeField] private string inventoryTitleAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/商店库存标头.png";
    [SerializeField] private string inventoryTitleSpriteName = "商店库存标头";
    [SerializeField] private string inventoryOpenButtonResourcePath = "Art/UI/UI/ShopAssistantUI/查看库存";
    [SerializeField] private string inventoryOpenButtonAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/查看库存.png";
    [SerializeField] private string inventoryOpenButtonSpriteName = "查看库存";
    [SerializeField] private string inventoryStockButtonResourcePath = "Art/UI/UI/ShopAssistantUI/进货";
    [SerializeField] private string inventoryStockButtonAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/进货.png";
    [SerializeField] private string inventoryStockButtonSpriteName = "进货按钮";
    [SerializeField] private string inventoryRightPanelResourcePath = "Art/UI/UI/ShopAssistantUI/右侧背景板";
    [SerializeField] private string inventoryRightPanelAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/右侧背景板.png";
    [SerializeField] private string inventoryRightPanelSpriteName = "右侧背景板";
    [SerializeField] private string inventoryShopLogoResourcePath = "Art/UI/UI/ShopAssistantUI/商店图案";
    [SerializeField] private string inventoryShopLogoAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/商店图案.png";
    [SerializeField] private string inventoryShopLogoSpriteName = "商店图案";
    [SerializeField] private string inventoryCoinFeatherResourcePath = "Art/UI/UI/ShopAssistantUI/金币和羽毛";
    [SerializeField] private string inventoryCoinFeatherAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/金币和羽毛.png";
    [SerializeField] private string inventoryCoinSpriteName = "金币";
    [SerializeField] private string inventoryFeatherSpriteName = "羽毛";
    [SerializeField] private string inventoryHintPanelResourcePath = "Art/UI/UI/ShopAssistantUI/右下提示背景板";
    [SerializeField] private string inventoryHintPanelAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/右下提示背景板.png";
    [SerializeField] private string inventoryHintPanelSpriteName = "右下提示背景板";
    [SerializeField] private string inventoryProductCellBgResourcePath = "Art/UI/UI/ShopAssistantUI/商品背景板";
    [SerializeField] private string inventoryProductCellBgAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/商品背景板.png";
    [SerializeField] private string inventoryProductCellBgSpriteName = "商品背景板";
    [SerializeField] private string inventoryNameBannerResourcePath = "Art/UI/UI/ShopAssistantUI/文字背景框";
    [SerializeField] private string inventoryNameBannerAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/文字背景框.png";
    [SerializeField] private string inventoryStepperButtonResourcePath = "Art/UI/UI/ShopAssistantUI/加减按钮";
    [SerializeField] private string inventoryStepperButtonAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/加减按钮.png";
    [SerializeField] private string inventoryStepperMinusSpriteName = "减号";
    [SerializeField] private string inventoryStepperPlusSpriteName = "加号";
    [SerializeField] private string inventoryCloseButtonResourcePath = "Art/UI/UI/ShopAssistantUI/关闭按钮";
    [SerializeField] private string inventoryCloseButtonAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/关闭按钮.png";
    [SerializeField] private string inventoryCloseButtonSpriteName = "关闭按钮";

    [Header("Message Feed")]
    [SerializeField] private string messageBubbleResourcePath = "Art/UI/UI/ShopAssistantUI/消息栏";
    [SerializeField] private string messageBubbleAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/消息栏.png";
    [SerializeField] private string messageBubbleSpriteName = "消息栏深色";
    [SerializeField] private string messageSourceFrameResourcePath = "Art/UI/UI/ShopAssistantUI/UI补丁";
    [SerializeField] private string messageSourceFrameAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/UI补丁.png";

    [Header("Player Panel")]
    [SerializeField] private string playerUiResourcePath = "Art/UI/UI/ShopAssistantUI/玩家UI";
    [SerializeField] private string playerUiAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/玩家UI.png";
    [SerializeField] private string playerAvatarSelectedSpriteName = "头像底框1";
    [SerializeField] private string playerAvatarNormalSpriteName = "头像底框2";
    [SerializeField] private string playerPanelBackgroundSpriteName = "玩家UI背景板";
    [SerializeField] private string playerAvatarBackgroundResourcePath = "Art/UI/UI/ShopAssistantUI/头像背景";
    [SerializeField] private string playerAvatarBackgroundAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/头像背景.png";
    [SerializeField] private string[] playerAvatarBackgroundSpriteNames =
    {
        "头像背景蓝",
        "头像背景红",
        "头像背景紫",
        "头像背景_黄"
    };
    [SerializeField] private string[] playerDisplayNames =
    {
        "林墨墨（画家）",
        "江凡（钓鱼佬）",
        "钟启恒（银行家）",
        "石老谋（奸商）"
    };
    [SerializeField] private string playerUiPatchResourcePath = "Art/UI/UI/ShopAssistantUI/UI补丁";
    [SerializeField] private string playerUiPatchAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/UI补丁.png";
    [SerializeField] private string playerHpFrameSpriteName = "血条框";
    [SerializeField] private string[] playerAttributeIconSpriteNames =
    {
        "饥饿值",
        "体力值",
        "水分值"
    };
    [SerializeField] private string playerMoneyIconSpriteName = "资金";
    [SerializeField] private string[] playerAttributeBarSpriteNames =
    {
        "红条",
        "绿条",
        "蓝条"
    };
    [SerializeField] private string[] playerAvatarResourcePaths =
    {
        "Art/Characters/Characters/Character1",
        "Art/Characters/Characters/character2",
        "Art/Characters/Characters/character3",
        "Art/Characters/Characters/character4"
    };
    [SerializeField] private string[] playerAvatarAssetPaths =
    {
        "Assets/Resources/Art/Characters/Characters/Character1.png",
        "Assets/Resources/Art/Characters/Characters/character2.png",
        "Assets/Resources/Art/Characters/Characters/character3.png",
        "Assets/Resources/Art/Characters/Characters/character4.png"
    };
    [SerializeField] private string[] playerAvatarSpriteNames =
    {
        "Character1_79",
        "character2_59",
        "character3_57",
        "character4_58"
    };

    [Header("Round Transition")]
    [SerializeField] private string roundStartResourcePath = "Art/UI/UI/ShopAssistantUI/回合开始";
    [SerializeField] private string roundStartAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/回合开始.png";
    [SerializeField] private string roundStartSpriteName = "回合开始";
    [SerializeField] private Key roundStartDebugKey = Key.B;
    [SerializeField] private string roundEndResourcePath = "Art/UI/UI/ShopAssistantUI/回合结束";
    [SerializeField] private string roundEndAssetPath = "Assets/Resources/Art/UI/UI/ShopAssistantUI/回合结束.png";
    [SerializeField] private string roundEndSpriteName = "回合结束";
    [SerializeField] private Key roundEndDebugKey = Key.E;
    [SerializeField] private float roundStartIntroSeconds = 0.32f;
    [SerializeField] private float roundStartHoldSeconds = 1.25f;
    [SerializeField] private float roundStartOutroSeconds = 0.28f;
    [SerializeField] private float roundEndCountSeconds = 0.62f;
    [SerializeField] private string roundStartAudioResourcePath = "Audio/ShopAssistant/round_start_notice";
    [SerializeField] private string roundEndAudioResourcePath = "Audio/ShopAssistant/round_end_notice";
    [SerializeField] [Range(0f, 1f)] private float roundTransitionAudioVolume = 1f;

    private TMP_FontAsset uiFont;
    private TMP_FontAsset runtimeDynamicChineseFont;
    private GameObject inventoryOverlayRoot;
    private GameObject roundStartOverlayRoot;
    private RectTransform roundStartPanel;
    private CanvasGroup roundStartCanvasGroup;
    private CanvasGroup roundStartTextCanvasGroup;
    private TextMeshProUGUI roundStartNumberText;
    private GameObject roundEndOverlayRoot;
    private RectTransform roundEndPanel;
    private RectTransform roundEndIncomeRow;
    private CanvasGroup roundEndCanvasGroup;
    private CanvasGroup roundEndTextCanvasGroup;
    private TextMeshProUGUI roundEndAmountText;
    private AudioSource roundTransitionAudioSource;
    private AudioClip roundStartAudioClip;
    private AudioClip roundEndAudioClip;
    private AudioClip fallbackRoundStartAudioClip;
    private AudioClip fallbackRoundEndAudioClip;
    private Coroutine roundStartRoutine;
    private Coroutine roundEndRoutine;
    private Coroutine openInventoryAfterRoundStartRoutine;
    private RectTransform uiRootRect;
    private Button openInventoryButton;
    private Button stockInventoryButton;
    private ScrollRect messageFeedScrollRect;
    private RectTransform messageFeedContent;
    private Sprite messageBubbleSprite;
    private int messageFeedCount;
    private readonly List<Image> playerAvatarFrameImages = new();
    private Image playerInfoAvatarBackgroundImage;
    private Image playerInfoAvatarImage;
    private TextMeshProUGUI playerInfoNameText;
    private readonly Image[] playerAttributeFillImages = new Image[3];
    private readonly TextMeshProUGUI[] playerAttributeValueTexts = new TextMeshProUGUI[3];
    private readonly List<AgentModel> agentModels = new();
    private readonly Dictionary<string, AgentModel> agentModelByName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AgentModel> agentModelByCode = new(StringComparer.OrdinalIgnoreCase);
    private TextMeshProUGUI playerMoneyValueText;
    private int selectedPlayerFrameIndex;
    private TextMeshProUGUI roundText;
    private TextMeshProUGUI moneyText;
    private TextMeshProUGUI rightPanelMoneyText;
    private TextMeshProUGUI stateText;
    private RectTransform inventoryContentRoot;
    private readonly ShopAssistantPlayerModel playerModel = new();
    private readonly Dictionary<string, Sprite> productImageLookup = new();
    private readonly List<ShopProductModel> marketProducts = new();
    private readonly List<ShopUiStepper> inventorySteppers = new();
    private bool canEditStockPlan;
    private int currentRoundValue;
    private string currentGameStateValue;
    private static readonly Color StatusStaticTextColor = new(0.03f, 0.035f, 0.04f, 1f);
    private static readonly Color StatusDynamicTextColor = new(0.12f, 0.58f, 0.18f, 1f);
    private static readonly Color StatusSettlementTextColor = new(0.75f, 0.08f, 0.08f, 1f);
    private static readonly Color PriceWarningTextColor = new(0.88f, 0.62f, 0.08f, 1f);
    private static readonly Color PriceDangerTextColor = new(0.78f, 0.08f, 0.08f, 1f);
    private static string pendingMarketInformationJson;
    private static string pendingAgentInformationJson;
    private static readonly List<string> pendingBroadcastMessagesJson = new();
    private static readonly string[] ProductNameBannerSpriteCycle =
    {
        "文字背景框_蓝",
        "文字背景框_褐",
        "文字背景框_棕",
        "文字背景框_紫",
        "文字背景框_橙"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<ShopAssistantDisplayUI>() != null)
        {
            return;
        }

        var root = new GameObject("UI_ShopAssistant_Display1");
        root.AddComponent<ShopAssistantDisplayUI>();
    }

    public static ShopAssistantDisplayUI EnsureInstance()
    {
        var ui = FindObjectOfType<ShopAssistantDisplayUI>();
        if (ui != null)
        {
            return ui;
        }

        var root = new GameObject("UI_ShopAssistant_Display1");
        return root.AddComponent<ShopAssistantDisplayUI>();
    }

    private void Awake()
    {
        uiFont = ResolveUiFont();
        BuildProductImageLookup();
        marketProducts.Clear();
        marketProducts.AddRange(BuildMockProducts());
        BuildUI();
        RefreshTopLeftStatus(initialRound, initialMoney, initialGameState);

        if (!string.IsNullOrWhiteSpace(pendingMarketInformationJson))
        {
            OnMarketInformationReceived(pendingMarketInformationJson);
            pendingMarketInformationJson = null;
        }

        if (!string.IsNullOrWhiteSpace(pendingAgentInformationJson))
        {
            ApplyAgentInformation(pendingAgentInformationJson);
            pendingAgentInformationJson = null;
        }

        if (pendingBroadcastMessagesJson.Count > 0)
        {
            foreach (var pendingJson in pendingBroadcastMessagesJson)
            {
                ApplyBroadcastMessages(pendingJson);
            }
            pendingBroadcastMessagesJson.Clear();
        }
    }

    /// <summary>
    /// Entry for WsAgentClient information push.
    /// </summary>
    public static void PushMarketInformationJson(string infoJson)
    {
        var ui = FindObjectOfType<ShopAssistantDisplayUI>();
        if (ui == null)
        {
            pendingMarketInformationJson = infoJson;
            return;
        }

        ui.OnMarketInformationReceived(infoJson);
    }

    public static void PushAgentInformationJson(string infoJson)
    {
        var ui = FindObjectOfType<ShopAssistantDisplayUI>();
        if (ui == null)
        {
            pendingAgentInformationJson = infoJson;
            return;
        }

        ui.ApplyAgentInformation(infoJson);
    }

    public static void PushBroadcastMessagesJson(string infoJson)
    {
        var ui = FindObjectOfType<ShopAssistantDisplayUI>();
        if (ui == null)
        {
            pendingBroadcastMessagesJson.Add(infoJson);
            return;
        }

        ui.ApplyBroadcastMessages(infoJson);
    }

    private void OnMarketInformationReceived(string infoJson)
    {
        if (string.IsNullOrWhiteSpace(infoJson))
        {
            return;
        }

        var products = ParseMarketInformation(infoJson);
        if (products.Count == 0)
        {
            Debug.LogWarning("[ShopAssistantUI] Market payload parsed, but no products found.");
            return;
        }

        marketProducts.Clear();
        marketProducts.AddRange(products);
        ApplyPlayerInformation(infoJson);
        RebuildProductCells();
        RefreshTopLeftStatus(currentRoundValue, playerModel.CurrentMoney, currentGameStateValue);
        Debug.Log($"[ShopAssistantUI] Loaded {products.Count} products from backend market information.");
    }

    private TMP_FontAsset ResolveUiFont()
    {
        var simhei = Resources.Load<TMP_FontAsset>("Fonts & Materials/SIMHEI SDF");
        runtimeDynamicChineseFont = TryCreateDynamicFromTmpSource(simhei);
        if (runtimeDynamicChineseFont == null)
        {
            runtimeDynamicChineseFont = TryCreateDynamicChineseFont();
        }

        // Always prefer dynamic font when available to avoid static SDF glyph gaps.
        if (runtimeDynamicChineseFont != null)
        {
            if (simhei != null)
            {
                if (runtimeDynamicChineseFont.fallbackFontAssetTable == null)
                {
                    runtimeDynamicChineseFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
                }

                if (!runtimeDynamicChineseFont.fallbackFontAssetTable.Contains(simhei))
                {
                    runtimeDynamicChineseFont.fallbackFontAssetTable.Add(simhei);
                }
            }

            return runtimeDynamicChineseFont;
        }

        if (simhei != null)
        {
            // Secondary fallback chain for static SDF.
            if (simhei.fallbackFontAssetTable == null)
            {
                simhei.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }
            return simhei;
        }

        return TMP_Settings.defaultFontAsset;
    }

    private TMP_FontAsset TryCreateDynamicFromTmpSource(TMP_FontAsset tmpFont)
    {
        if (tmpFont == null || tmpFont.sourceFontFile == null)
        {
            return null;
        }

        try
        {
            var tmp = TMP_FontAsset.CreateFontAsset(
                tmpFont.sourceFontFile,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            if (tmp != null)
            {
                tmp.name = $"RuntimeTMP_{tmpFont.name}_Dynamic";
                return tmp;
            }
        }
        catch
        {
            // ignore and fallback
        }

        return null;
    }

    private void BuildProductImageLookup()
    {
        productImageLookup.Clear();

        LoadProductMappingsFromCsv(productImageMappingCsvResourcePath);

        if (productImageMappings == null)
        {
            return;
        }

        foreach (var item in productImageMappings)
        {
            RegisterProductImage(item);
        }
    }

    private void LoadProductMappingsFromCsv(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return;
        }

        var csvAsset = Resources.Load<TextAsset>(resourcePath.Trim());
        if (csvAsset == null)
        {
            Debug.LogWarning($"[ShopAssistantUI] Product mapping CSV not found: Resources/{resourcePath}");
            return;
        }

        var lines = csvAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 2)
            {
                continue;
            }

            RegisterProductImage(new ProductImageMapping
            {
                productName = cols[0].Trim(),
                imagePath = cols[1].Trim(),
                spriteName = cols.Length > 2 ? cols[2].Trim() : string.Empty,
                sprite = null
            });
        }
    }

    private void RegisterProductImage(ProductImageMapping item)
    {
        if (string.IsNullOrWhiteSpace(item.productName))
        {
            return;
        }

        string key = item.productName.Trim();
        Sprite sprite = item.sprite;

        if (sprite == null && !string.IsNullOrWhiteSpace(item.imagePath))
        {
            string path = item.imagePath.Trim();
            const string pngExt = ".png";
            if (path.EndsWith(pngExt, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - pngExt.Length);
            }
            sprite = ResolveSpriteFromResources(path, item.spriteName, key);
        }

        if (sprite != null)
        {
            productImageLookup[key] = sprite;
        }
    }

    private Sprite TryResolveProductSprite(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return null;
        }

        productImageLookup.TryGetValue(productName.Trim(), out var sprite);
        return sprite;
    }

    private static Sprite ResolveSpriteFromResources(string resourcePath, string spriteName, string productName)
    {
        var sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites == null || sprites.Length == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(spriteName))
        {
            foreach (var s in sprites)
            {
                if (s != null && SpriteNameMatches(s.name, spriteName))
                {
                    return s;
                }
            }
        }

        foreach (var s in sprites)
        {
            if (s != null && SpriteNameMatches(s.name, productName))
            {
                return s;
            }
        }

        return sprites.Length == 1 ? sprites[0] : null;
    }

    private static bool SpriteNameMatches(string actualName, string requestedName)
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

    private TMP_FontAsset TryCreateDynamicChineseFont()
    {
        string[] candidates =
        {
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "SimHei",
            "SimSun",
            "Arial Unicode MS"
        };

        foreach (var name in candidates)
        {
            try
            {
                var osFont = Font.CreateDynamicFontFromOSFont(name, 48);
                if (osFont == null)
                {
                    continue;
                }

                var tmp = TMP_FontAsset.CreateFontAsset(
                    osFont,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true);

                if (tmp != null)
                {
                    tmp.name = $"RuntimeTMP_{name}";
                    return tmp;
                }
            }
            catch
            {
                // Try next candidate.
            }
        }

        return null;
    }

    private void BuildUI()
    {
        var hostCanvas = ResolveOrCreateHostCanvas();

        var uiRoot = new GameObject("ShopAssistantUIRoot", typeof(RectTransform));
        uiRoot.transform.SetParent(hostCanvas.transform, false);
        var uiRootRt = (RectTransform)uiRoot.transform;
        uiRootRect = uiRootRt;
        uiRootRt.anchorMin = Vector2.zero;
        uiRootRt.anchorMax = Vector2.one;
        uiRootRt.offsetMin = Vector2.zero;
        uiRootRt.offsetMax = Vector2.zero;

        BuildTopLeftStatusPanel(uiRoot.transform);
        BuildTopRightPlayerPanel(uiRoot.transform);
        BuildBottomLeftMessageFeed(uiRoot.transform);
        BuildOpenInventoryButton(uiRoot.transform);
        BuildInventoryOverlay(uiRoot.transform);
        BuildRoundStartTransition(uiRoot.transform);
        BuildRoundEndTransition(uiRoot.transform);
    }

    private void Update()
    {
        GameEndVictoryUI.HandleDebugInput();
        GameEndFailureUI.HandleDebugInput();

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[roundStartDebugKey].wasPressedThisFrame)
        {
            Debug.Log($"[ShopAssistantUI] Debug key {roundStartDebugKey} pressed; showing round start transition and playing notice sound.");
            ShowRoundStartTransition(initialRound);
        }

        if (keyboard != null && keyboard[roundEndDebugKey].wasPressedThisFrame)
        {
            int debugAmount = UnityEngine.Random.Range(0, 10001);
            int debugDelta = UnityEngine.Random.value >= 0.5f ? debugAmount : -debugAmount;
            Debug.Log($"[ShopAssistantUI] Debug key {roundEndDebugKey} pressed; showing round end transition, today delta={debugDelta}.");
            ShowRoundEndTransition(debugDelta);
        }

        if (keyboard != null && (keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame))
        {
            AddDebugBroadcastMessages();
        }

    }

    private Canvas ResolveOrCreateHostCanvas()
    {
        var allCanvases = FindObjectsOfType<Canvas>(true);

        foreach (var existing in allCanvases)
        {
            if (existing == null) continue;
            if (IsDisplay1NamedCanvas(existing.gameObject))
            {
                existing.targetDisplay = targetDisplay;
                EnsureCanvasInputComponents(existing);
                return existing;
            }
        }

        var displayObject = FindDisplay1NamedObject();
        if (displayObject != null)
        {
            var existing = displayObject.GetComponentInChildren<Canvas>(true);
            if (existing != null)
            {
                existing.targetDisplay = targetDisplay;
                EnsureCanvasInputComponents(existing);
                return existing;
            }

            var added = displayObject.AddComponent<Canvas>();
            added.renderMode = RenderMode.ScreenSpaceOverlay;
            added.targetDisplay = targetDisplay;
            added.sortingOrder = 300;
            added.pixelPerfect = true;
            EnsureCanvasInputComponents(added);
            return added;
        }

        var canvasGo = new GameObject("ShopAssistantCanvas_Display1", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = targetDisplay;
        canvas.sortingOrder = 300;
        canvas.pixelPerfect = true;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(baseResolutionX, baseResolutionY);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private bool IsDisplay1NamedCanvas(GameObject go)
    {
        if (go == null) return false;
        if (NameMatchesDisplay1(go.name)) return true;
        var parent = go.transform.parent;
        return parent != null && NameMatchesDisplay1(parent.name);
    }

    private GameObject FindDisplay1NamedObject()
    {
        foreach (var t in FindObjectsOfType<Transform>(true))
        {
            if (t != null && NameMatchesDisplay1(t.name))
            {
                return t.gameObject;
            }
        }

        return null;
    }

    private bool NameMatchesDisplay1(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        var lowered = objectName.ToLowerInvariant();
        return lowered.Contains(displayCanvasNameHint.ToLowerInvariant()) || lowered.Contains("display 1");
    }

    private void EnsureCanvasInputComponents(Canvas canvas)
    {
        if (canvas == null) return;

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

}

public sealed class InventoryGridAutoColumns : MonoBehaviour
{
    private GridLayoutGroup grid;
    private RectTransform viewport;
    private int minColumns;
    private int maxColumns;
    private float lastWidth = -1f;

    public void Bind(GridLayoutGroup targetGrid, RectTransform targetViewport, int min, int max)
    {
        grid = targetGrid;
        viewport = targetViewport;
        minColumns = Mathf.Max(1, min);
        maxColumns = Mathf.Max(minColumns, max);
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (grid == null || viewport == null)
        {
            return;
        }

        float width = viewport.rect.width;
        if (Mathf.Abs(width - lastWidth) < 0.2f || width <= 0f)
        {
            return;
        }

        lastWidth = width;

        float cellWidth = grid.cellSize.x;
        float spacing = grid.spacing.x;
        float available = width - grid.padding.left - grid.padding.right + spacing;
        int columns = Mathf.FloorToInt(available / (cellWidth + spacing));
        columns = Mathf.Clamp(columns, minColumns, maxColumns);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
    }
}

public sealed class ShopUiStepper : MonoBehaviour
{
    private Button minusButton;
    private Button plusButton;
    private ShopUiPressRepeater minusRepeater;
    private ShopUiPressRepeater plusRepeater;
    private TextMeshProUGUI valueText;
    private Action<int> onValueChanged;
    private Func<bool> canIncrease;
    private Func<int, Color> valueColorSelector;
    private int maxValue = int.MaxValue;
    private bool baseInteractable = true;
    private bool editAllowed = true;
    private int value;

    public void Bind(
        Button minus,
        Button plus,
        TextMeshProUGUI valueLabel,
        int initialValue,
        Action<int> valueChanged = null,
        Func<bool> canIncreaseValue = null,
        int maxAllowedValue = int.MaxValue,
        bool canEdit = true,
        Func<int, Color> colorSelector = null)
    {
        minusButton = minus;
        plusButton = plus;
        valueText = valueLabel;
        onValueChanged = valueChanged;
        canIncrease = canIncreaseValue;
        valueColorSelector = colorSelector;
        maxValue = Mathf.Max(0, maxAllowedValue);
        editAllowed = canEdit;
        value = Mathf.Clamp(initialValue, 0, maxValue);
        UpdateLabel();

        if (minusButton != null)
        {
            minusRepeater = minusButton.GetComponent<ShopUiPressRepeater>();
            if (minusRepeater == null)
            {
                minusRepeater = minusButton.gameObject.AddComponent<ShopUiPressRepeater>();
            }
            minusRepeater.Bind(Decrease);
        }

        if (plusButton != null)
        {
            plusRepeater = plusButton.GetComponent<ShopUiPressRepeater>();
            if (plusRepeater == null)
            {
                plusRepeater = plusButton.gameObject.AddComponent<ShopUiPressRepeater>();
            }
            plusRepeater.Bind(Increase);
        }

        RefreshButtons();
    }

    private void OnDestroy()
    {
        if (minusRepeater != null)
        {
            minusRepeater.Unbind();
        }

        if (plusRepeater != null)
        {
            plusRepeater.Unbind();
        }
    }

    private void Decrease()
    {
        if (!baseInteractable || value <= 0)
        {
            RefreshButtons();
            return;
        }

        value = Mathf.Max(0, value - 1);
        UpdateLabel();
    }

    private void Increase()
    {
        if (!baseInteractable || value >= maxValue || (canIncrease != null && !canIncrease.Invoke()))
        {
            RefreshButtons();
            return;
        }

        value = Mathf.Min(maxValue, value + 1);
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (valueText != null)
        {
            valueText.text = value.ToString();
            if (valueColorSelector != null)
            {
                valueText.color = valueColorSelector.Invoke(value);
            }
        }
        onValueChanged?.Invoke(value);
        RefreshButtons();
    }

    public void SetInteractable(bool interactable)
    {
        baseInteractable = interactable && editAllowed;
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        SetButtonVisual(minusButton, baseInteractable && value > 0);
        SetButtonVisual(plusButton, baseInteractable && value < maxValue && (canIncrease == null || canIncrease.Invoke()));
    }

    private static void SetButtonVisual(Button button, bool interactable)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = interactable;
        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = interactable ? Color.white : new Color(0.78f, 0.78f, 0.78f, 1f);
        }
    }
}

public sealed class ShopUiPressRepeater : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private float holdDelaySeconds = 0.35f;
    [SerializeField] private float fastModeHoldSeconds = 1.5f;
    [SerializeField] private float repeatIntervalSeconds = 0.06f;
    [SerializeField] private int fastRepeatStepCount = 5;

    private Action onStep;
    private bool isHolding;
    private bool hasRepeated;
    private float holdElapsed;
    private float repeatElapsed;

    public void Bind(Action onStepAction)
    {
        onStep = onStepAction;
    }

    public void Unbind()
    {
        onStep = null;
        isHolding = false;
        hasRepeated = false;
        holdElapsed = 0f;
        repeatElapsed = 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        hasRepeated = false;
        holdElapsed = 0f;
        repeatElapsed = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHolding = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Keep single-click as exactly one step; suppress extra click when hold-repeat already triggered.
        if (!hasRepeated)
        {
            onStep?.Invoke();
        }
    }

    private void Update()
    {
        if (!isHolding || onStep == null)
        {
            return;
        }

        holdElapsed += Time.unscaledDeltaTime;
        if (holdElapsed < holdDelaySeconds)
        {
            return;
        }

        hasRepeated = true;
        repeatElapsed += Time.unscaledDeltaTime;
        while (repeatElapsed >= repeatIntervalSeconds)
        {
            repeatElapsed -= repeatIntervalSeconds;
            int stepCount = holdElapsed >= fastModeHoldSeconds ? Mathf.Max(1, fastRepeatStepCount) : 1;
            for (int i = 0; i < stepCount; i++)
            {
                onStep.Invoke();
            }
        }
    }
}
