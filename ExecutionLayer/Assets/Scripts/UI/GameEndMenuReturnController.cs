using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameEndMenuReturnController : MonoBehaviour
{
    private const string CoverSceneName = "AITown_CoverScene";
    private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
    private static bool isReturningToMenu;

    public static void BeginReturnToMenu()
    {
        if (isReturningToMenu)
        {
            return;
        }

        var go = new GameObject("GameEndMenuReturnController");
        DontDestroyOnLoad(go);
        go.AddComponent<GameEndMenuReturnController>().StartReturn();
    }

    private void StartReturn()
    {
        StartCoroutine(ReturnToMenuRoutine());
    }

    private IEnumerator ReturnToMenuRoutine()
    {
        isReturningToMenu = true;

        var transition = CreateTransitionOverlay();
        yield return Fade(transition.group, 0f, 1f, 0.18f);

        ShopAssistantDisplayUI.ResetFrontendStateForNewGame();
        WsAgentClient.RequestGameResetAfterSceneReload();

        var loadOperation = SceneManager.LoadSceneAsync(CoverSceneName);
        if (loadOperation == null)
        {
            Debug.LogError($"Failed to load cover scene: {CoverSceneName}");
            yield return Fade(transition.group, 1f, 0f, 0.25f);
            Cleanup(transition.root);
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return null;
        yield return Fade(transition.group, 1f, 0f, 0.45f);
        Cleanup(transition.root);
    }

    private void Cleanup(GameObject transitionRoot)
    {
        if (transitionRoot != null)
        {
            Destroy(transitionRoot);
        }

        isReturningToMenu = false;
        Destroy(gameObject);
    }

    private static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
    {
        group.alpha = from;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = seconds <= 0f ? 1f : Mathf.Clamp01(elapsed / seconds);
            t = t * t * (3f - 2f * t);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    private static TransitionOverlay CreateTransitionOverlay()
    {
        var root = new GameObject("ReturnToMenuBlackFade", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        DontDestroyOnLoad(root);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = 0;
        canvas.sortingOrder = 6000;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        var group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = true;
        group.blocksRaycasts = true;

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
}
