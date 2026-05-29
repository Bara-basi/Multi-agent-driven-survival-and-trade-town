using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuCoverBackground : MonoBehaviour
{
    [SerializeField] private string backgroundResourcePath = "Art/UI/UI/ShopAssistantUI/\u5c01\u9762\u957f\u56fe";
    [SerializeField] private string titleResourcePath = "Art/UI/UI/ShopAssistantUI/\u6e38\u620f\u6807\u9898";
    [SerializeField] private string buttonFrameResourcePath = "Art/UI/UI/ShopAssistantUI/\u6309\u94ae\u80cc\u666f\u6846";
    [SerializeField] private string menuButtonAtlasResourcePath = "Art/UI/UI/ShopAssistantUI/\u5c01\u9762\u6309\u94ae";
    [SerializeField] private Vector2 referenceResolution = new(1920f, 1080f);
    [SerializeField] private float panSpeedPixelsPerSecond = 18f;
    [SerializeField] private Vector2 titleSize = new(900f, 293f);
    [SerializeField] private Vector2 titleAnchoredPosition = new(0f, 300f);
    [SerializeField] private string mainSceneName = "AITown_MainScene";
    private static readonly Vector2 ButtonFrameSize = new(530f, 600f);
    private static readonly Vector2 ButtonFrameAnchoredPosition = new(0f, -165f);
    private static readonly Vector2 MenuButtonSize = new(318f, 88f);
    private static readonly float[] MenuButtonSlotY = { 210f, 89f, -34f, -156f };
    private static readonly Color ButtonNormalColor = Color.white;
    private static readonly Color ButtonHighlightedColor = new(1f, 0.96f, 0.86f, 1f);
    private static readonly Color ButtonPressedColor = new(0.76f, 0.72f, 0.66f, 1f);
    private static readonly Color ButtonDisabledColor = new(0.62f, 0.62f, 0.62f, 0.9f);

    private RectTransform canvasRect;
    private RectTransform backgroundRect;
    private Image backgroundImage;
    private Button startButton;
    private readonly Button[] menuButtons = new Button[4];
    private float lastViewportWidth;
    private float lastViewportHeight;
    private float maxPanOffset;
    private bool isStartingGame;

    private void Awake()
    {
        EnsureEventSystem();
        BuildBackgroundCanvas();
        BuildForegroundCanvas();
    }

    private void Update()
    {
        if (backgroundRect == null)
        {
            return;
        }

        RefreshBackgroundLayout();

        float x = maxPanOffset <= 0.01f
            ? 0f
            : Mathf.PingPong(Time.unscaledTime * panSpeedPixelsPerSecond, maxPanOffset * 2f) - maxPanOffset;

        backgroundRect.anchoredPosition = new Vector2(x, 0f);
    }

    private void BuildBackgroundCanvas()
    {
        var canvasGo = new GameObject("CoverBackgroundCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = 0;
        canvas.sortingOrder = 0;
        canvas.pixelPerfect = true;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasGo.GetComponent<RectTransform>();

        var backgroundGo = new GameObject("SlidingCoverBackground", typeof(Image));
        backgroundGo.transform.SetParent(canvasGo.transform, false);

        backgroundImage = backgroundGo.GetComponent<Image>();
        backgroundImage.sprite = LoadSprite(backgroundResourcePath);
        backgroundImage.raycastTarget = false;
        backgroundImage.preserveAspect = false;

        backgroundRect = backgroundGo.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);

        RefreshBackgroundLayout(force: true);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        var inputModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
#else
        eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
    }

    private void BuildForegroundCanvas()
    {
        var canvasGo = new GameObject("CoverForegroundCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = 0;
        canvas.sortingOrder = 10;
        canvas.pixelPerfect = true;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        CreateStaticImage("GameTitle", canvasGo.transform, titleResourcePath, titleSize, titleAnchoredPosition, true);
        var frameRect = CreateStaticImage("ButtonFrame", canvasGo.transform, buttonFrameResourcePath, ButtonFrameSize, ButtonFrameAnchoredPosition, false);
        CreateMenuButtons(frameRect);
    }

    private RectTransform CreateStaticImage(string objectName, Transform parent, string resourcePath, Vector2 size, Vector2 anchoredPosition, bool preserveAspect)
    {
        var go = new GameObject(objectName, typeof(Image));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.sprite = LoadSprite(resourcePath);
        image.raycastTarget = false;
        image.preserveAspect = preserveAspect;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        return rect;
    }

    private void CreateMenuButtons(RectTransform frameRect)
    {
        if (frameRect == null)
        {
            return;
        }

        startButton = CreateMenuButtonImage("StartGameButton", frameRect, "\u5f00\u59cb\u6e38\u620f", 0);
        CreateMenuButtonImage("SaveButton", frameRect, "\u5b58\u6863", 1);
        CreateMenuButtonImage("SettingsButton", frameRect, "\u8bbe\u7f6e", 2);
        CreateMenuButtonImage("QuitButton", frameRect, "\u9000\u51fa\u6e38\u620f", 3);

        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
    }

    private Button CreateMenuButtonImage(string objectName, Transform parent, string spriteName, int index)
    {
        var go = new GameObject(objectName, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.sprite = LoadSprite(menuButtonAtlasResourcePath, spriteName);
        image.raycastTarget = true;
        image.preserveAspect = false;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = ButtonNormalColor,
            highlightedColor = ButtonHighlightedColor,
            pressedColor = ButtonPressedColor,
            selectedColor = ButtonHighlightedColor,
            disabledColor = ButtonDisabledColor,
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = MenuButtonSize;
        float y = index >= 0 && index < MenuButtonSlotY.Length ? MenuButtonSlotY[index] : 0f;
        rect.anchoredPosition = new Vector2(0f, y);

        if (index >= 0 && index < menuButtons.Length)
        {
            menuButtons[index] = button;
        }

        return button;
    }

    private void StartGame()
    {
        if (isStartingGame)
        {
            return;
        }

        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        isStartingGame = true;

        var transition = CreateTransitionOverlay();
        if (startButton != null && startButton.targetGraphic != null)
        {
            yield return FlashGraphic(startButton.targetGraphic, 3, 0.09f);
        }

        SetMenuButtonsInteractable(false);
        yield return FadeTransition(transition.group, 0f, 1f, 0.55f);

        var loadOperation = SceneManager.LoadSceneAsync(mainSceneName);
        if (loadOperation == null)
        {
            Debug.LogError($"[MainMenuCoverBackground] Failed to load scene: {mainSceneName}");
            SetMenuButtonsInteractable(true);
            isStartingGame = false;
            yield return FadeTransition(transition.group, 1f, 0f, 0.25f);
            Destroy(transition.root);
            yield break;
        }

        DontDestroyOnLoad(gameObject);
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        ClearCoverVisuals();
        yield return null;
        yield return FadeTransition(transition.group, 1f, 0f, 0.55f);
        Destroy(transition.root);
        Destroy(gameObject);
    }

    private void SetMenuButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null)
            {
                menuButtons[i].interactable = interactable;
            }
        }
    }

    private IEnumerator FlashGraphic(Graphic graphic, int flashCount, float intervalSeconds)
    {
        Color originalColor = graphic.color;
        Color flashColor = new(1f, 0.82f, 0.42f, 1f);

        for (int i = 0; i < flashCount; i++)
        {
            graphic.color = flashColor;
            yield return new WaitForSecondsRealtime(intervalSeconds);
            graphic.color = originalColor;
            yield return new WaitForSecondsRealtime(intervalSeconds);
        }
    }

    private static IEnumerator FadeTransition(CanvasGroup group, float from, float to, float seconds)
    {
        if (group == null)
        {
            yield break;
        }

        group.alpha = from;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = seconds <= 0f ? 1f : Mathf.Clamp01(elapsed / seconds);
            group.alpha = Mathf.Lerp(from, to, SmoothStep(t));
            yield return null;
        }

        group.alpha = to;
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private TransitionOverlay CreateTransitionOverlay()
    {
        var root = new GameObject("CoverSceneTransitionOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        DontDestroyOnLoad(root);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = 0;
        canvas.sortingOrder = 5000;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        var group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;
        group.interactable = true;

        var imageGo = new GameObject("BlackFade", typeof(Image));
        imageGo.transform.SetParent(root.transform, false);

        var image = imageGo.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        var rect = imageGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return new TransitionOverlay(root, group);
    }

    private void ClearCoverVisuals()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        canvasRect = null;
        backgroundRect = null;
        backgroundImage = null;
    }

    private readonly struct TransitionOverlay
    {
        public readonly GameObject root;
        public readonly CanvasGroup group;

        public TransitionOverlay(GameObject root, CanvasGroup group)
        {
            this.root = root;
            this.group = group;
        }
    }

    private Sprite LoadSprite(string resourcePath, string spriteName)
    {
        var sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].name == spriteName)
                {
                    return sprites[i];
                }
            }
        }

        Debug.LogWarning($"[MainMenuCoverBackground] Sprite not found: {resourcePath}/{spriteName}");
        return LoadSprite(resourcePath);
    }

    private Sprite LoadSprite(string resourcePath)
    {
        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        var sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"[MainMenuCoverBackground] Sprite not found: {resourcePath}");
            return null;
        }

        string expectedName = resourcePath[(resourcePath.LastIndexOf('/') + 1)..];
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name == expectedName)
            {
                return sprites[i];
            }
        }

        return sprites[0];
    }

    private void RefreshBackgroundLayout(bool force = false)
    {
        if (canvasRect == null || backgroundRect == null)
        {
            return;
        }

        float viewportWidth = canvasRect.rect.width;
        float viewportHeight = canvasRect.rect.height;
        if (!force
            && Mathf.Approximately(viewportWidth, lastViewportWidth)
            && Mathf.Approximately(viewportHeight, lastViewportHeight))
        {
            return;
        }

        lastViewportWidth = viewportWidth;
        lastViewportHeight = viewportHeight;

        float spriteAspect = 3f;
        if (backgroundImage != null && backgroundImage.sprite != null)
        {
            var spriteRect = backgroundImage.sprite.rect;
            if (spriteRect.height > 0f)
            {
                spriteAspect = spriteRect.width / spriteRect.height;
            }
        }

        float targetHeight = Mathf.Max(viewportHeight, 1f);
        float targetWidth = Mathf.Max(viewportWidth, targetHeight * spriteAspect);
        backgroundRect.sizeDelta = new Vector2(targetWidth, targetHeight);
        maxPanOffset = Mathf.Max(0f, (targetWidth - viewportWidth) * 0.5f);
    }
}
