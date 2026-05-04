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
    private void BuildRoundStartTransition(Transform parent)
    {
        roundStartOverlayRoot = new GameObject("RoundStartTransitionOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        roundStartOverlayRoot.transform.SetParent(parent, false);
        roundStartOverlayRoot.transform.SetAsLastSibling();

        var overlayRt = (RectTransform)roundStartOverlayRoot.transform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        roundStartCanvasGroup = roundStartOverlayRoot.GetComponent<CanvasGroup>();
        roundStartCanvasGroup.alpha = 0f;
        roundStartCanvasGroup.blocksRaycasts = false;
        roundStartCanvasGroup.interactable = false;

        roundTransitionAudioSource = roundStartOverlayRoot.AddComponent<AudioSource>();
        roundTransitionAudioSource.playOnAwake = false;
        roundTransitionAudioSource.loop = false;
        roundTransitionAudioSource.spatialBlend = 0f;
        roundTransitionAudioSource.ignoreListenerPause = true;
        roundTransitionAudioSource.volume = roundTransitionAudioVolume;
        roundStartAudioClip = LoadRoundTransitionAudio(roundStartAudioResourcePath);
        roundEndAudioClip = LoadRoundTransitionAudio(roundEndAudioResourcePath);
        fallbackRoundStartAudioClip = CreateRoundTransitionClip("Runtime_RoundStartNotice", new[] { 659.25f, 880f, 1174.66f }, 0.105f, 0.018f);
        fallbackRoundEndAudioClip = CreateRoundTransitionClip("Runtime_RoundEndNotice", new[] { 987.77f, 783.99f, 523.25f }, 0.12f, 0.018f);
        PreloadRoundTransitionAudio(roundStartAudioClip);
        PreloadRoundTransitionAudio(roundEndAudioClip);

        var overlayImage = roundStartOverlayRoot.GetComponent<Image>();
        overlayImage.color = new Color(0.02f, 0.025f, 0.035f, 0.28f);
        overlayImage.raycastTarget = false;

        var panelObj = new GameObject("RoundStartPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(roundStartOverlayRoot.transform, false);
        roundStartPanel = (RectTransform)panelObj.transform;
        roundStartPanel.anchorMin = new Vector2(0.5f, 0.5f);
        roundStartPanel.anchorMax = new Vector2(0.5f, 0.5f);
        roundStartPanel.pivot = new Vector2(0.5f, 0.5f);
        roundStartPanel.sizeDelta = new Vector2(842f, 495f);
        roundStartPanel.anchoredPosition = Vector2.zero;

        var panelImage = panelObj.GetComponent<Image>();
        var panelSprite = ResolveUiDecorationSprite(roundStartResourcePath, roundStartAssetPath, roundStartSpriteName);
        if (panelSprite != null)
        {
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Simple;
            panelImage.preserveAspect = true;
            panelImage.color = Color.white;
        }
        else
        {
            panelImage.color = new Color(1f, 1f, 1f, 0.96f);
            Debug.LogWarning("[ShopAssistantUI] Round start sprite missing, fallback color panel is used.");
        }
        panelImage.raycastTarget = false;

        var textRoot = new GameObject("RoundStartTextRoot", typeof(RectTransform), typeof(CanvasGroup));
        textRoot.transform.SetParent(roundStartPanel, false);
        var textRootRt = (RectTransform)textRoot.transform;
        textRootRt.anchorMin = Vector2.zero;
        textRootRt.anchorMax = Vector2.one;
        textRootRt.offsetMin = Vector2.zero;
        textRootRt.offsetMax = Vector2.zero;
        roundStartTextCanvasGroup = textRoot.GetComponent<CanvasGroup>();
        roundStartTextCanvasGroup.alpha = 0f;
        roundStartTextCanvasGroup.blocksRaycasts = false;
        roundStartTextCanvasGroup.interactable = false;

        BuildRoundStartFirstLine(textRootRt);

        var secondLine = CreateRoundTransitionText("BusinessStart", textRootRt, "开始营业", 55f, TextAlignmentOptions.Center, StatusStaticTextColor, 0.06f);
        secondLine.characterSpacing = 5f;
        secondLine.rectTransform.anchorMin = new Vector2(0.25f, 0.155f);
        secondLine.rectTransform.anchorMax = new Vector2(0.75f, 0.305f);
        secondLine.rectTransform.offsetMin = Vector2.zero;
        secondLine.rectTransform.offsetMax = Vector2.zero;

        roundStartOverlayRoot.SetActive(false);
    }

    private void BuildRoundEndTransition(Transform parent)
    {
        roundEndOverlayRoot = new GameObject("RoundEndTransitionOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        roundEndOverlayRoot.transform.SetParent(parent, false);
        roundEndOverlayRoot.transform.SetAsLastSibling();

        var overlayRt = (RectTransform)roundEndOverlayRoot.transform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        roundEndCanvasGroup = roundEndOverlayRoot.GetComponent<CanvasGroup>();
        roundEndCanvasGroup.alpha = 0f;
        roundEndCanvasGroup.blocksRaycasts = false;
        roundEndCanvasGroup.interactable = false;

        var overlayImage = roundEndOverlayRoot.GetComponent<Image>();
        overlayImage.color = new Color(0.02f, 0.025f, 0.035f, 0.28f);
        overlayImage.raycastTarget = false;

        var panelObj = new GameObject("RoundEndPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(roundEndOverlayRoot.transform, false);
        roundEndPanel = (RectTransform)panelObj.transform;
        roundEndPanel.anchorMin = new Vector2(0.5f, 0.5f);
        roundEndPanel.anchorMax = new Vector2(0.5f, 0.5f);
        roundEndPanel.pivot = new Vector2(0.5f, 0.5f);
        roundEndPanel.sizeDelta = new Vector2(842f, 299f);
        roundEndPanel.anchoredPosition = Vector2.zero;

        var panelImage = panelObj.GetComponent<Image>();
        var panelSprite = ResolveUiDecorationSprite(roundEndResourcePath, roundEndAssetPath, roundEndSpriteName);
        if (panelSprite != null)
        {
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Simple;
            panelImage.preserveAspect = true;
            panelImage.color = Color.white;
        }
        else
        {
            panelImage.color = new Color(1f, 1f, 1f, 0.96f);
            Debug.LogWarning("[ShopAssistantUI] Round end sprite missing, fallback color panel is used.");
        }
        panelImage.raycastTarget = false;

        var textRoot = new GameObject("RoundEndTextRoot", typeof(RectTransform), typeof(CanvasGroup));
        textRoot.transform.SetParent(roundEndPanel, false);
        var textRootRt = (RectTransform)textRoot.transform;
        textRootRt.anchorMin = Vector2.zero;
        textRootRt.anchorMax = Vector2.one;
        textRootRt.offsetMin = Vector2.zero;
        textRootRt.offsetMax = Vector2.zero;
        roundEndTextCanvasGroup = textRoot.GetComponent<CanvasGroup>();
        roundEndTextCanvasGroup.alpha = 0f;
        roundEndTextCanvasGroup.blocksRaycasts = false;
        roundEndTextCanvasGroup.interactable = false;

        BuildRoundEndIncomeRow(textRootRt);

        roundEndOverlayRoot.SetActive(false);
    }

    private void BuildRoundEndIncomeRow(RectTransform parent)
    {
        var row = new GameObject("RoundEndIncomeRow", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        roundEndIncomeRow = (RectTransform)row.transform;
        roundEndIncomeRow.anchorMin = new Vector2(0.5f, 0.5f);
        roundEndIncomeRow.anchorMax = new Vector2(0.5f, 0.5f);
        roundEndIncomeRow.pivot = new Vector2(0.5f, 0.5f);
        roundEndIncomeRow.sizeDelta = new Vector2(560f, 78f);
        roundEndIncomeRow.anchoredPosition = new Vector2(34f, -72f);

        var label = CreateRoundTransitionText("TodayLabel", roundEndIncomeRow, "今日", 48f, TextAlignmentOptions.Center, Color.black, 0.045f);
        SetFixedRowItem(label.rectTransform, -170f, 130f, 72f);

        var coinObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        coinObj.transform.SetParent(roundEndIncomeRow, false);
        var coinRt = (RectTransform)coinObj.transform;
        SetLeftRowItem(coinRt, -78f, 58f, 58f);

        var coinImage = coinObj.GetComponent<Image>();
        var coinSprite = ResolveUiDecorationSprite(inventoryCoinFeatherResourcePath, inventoryCoinFeatherAssetPath, inventoryCoinSpriteName);
        coinImage.sprite = coinSprite;
        coinImage.type = Image.Type.Simple;
        coinImage.preserveAspect = true;
        coinImage.color = coinSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        coinImage.raycastTarget = false;

        roundEndAmountText = CreateRoundTransitionText("TodayAmount", roundEndIncomeRow, "+0", 52f, TextAlignmentOptions.Left, new Color(0.08f, 0.50f, 0.16f, 1f), 0.06f);
        roundEndAmountText.enableAutoSizing = true;
        roundEndAmountText.fontSizeMin = 42f;
        roundEndAmountText.fontSizeMax = 52f;
        SetLeftRowItem(roundEndAmountText.rectTransform, -4f, 260f, 72f);
    }

    private AudioClip LoadRoundTransitionAudio(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        var clip = Resources.Load<AudioClip>(resourcePath.Trim());
        if (clip == null)
        {
            Debug.LogWarning($"[ShopAssistantUI] Round transition audio not found: Resources/{resourcePath}");
            clip = resourcePath.IndexOf("end", StringComparison.OrdinalIgnoreCase) >= 0
                ? CreateRoundTransitionClip("Runtime_RoundEndNotice", new[] { 987.77f, 783.99f, 523.25f }, 0.12f, 0.018f)
                : CreateRoundTransitionClip("Runtime_RoundStartNotice", new[] { 659.25f, 880f, 1174.66f }, 0.105f, 0.018f);
        }

        return clip;
    }

    private static AudioClip CreateRoundTransitionClip(string name, float[] frequencies, float noteSeconds, float gapSeconds)
    {
        const int sampleRate = 44100;
        int noteSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * noteSeconds));
        int gapSamples = Mathf.Max(0, Mathf.RoundToInt(sampleRate * gapSeconds));
        int totalSamples = frequencies.Length * (noteSamples + gapSamples);
        var data = new float[totalSamples];
        int writeIndex = 0;

        for (int noteIndex = 0; noteIndex < frequencies.Length; noteIndex++)
        {
            float frequency = frequencies[noteIndex];
            for (int i = 0; i < noteSamples; i++)
            {
                float t = i / (float)sampleRate;
                float env = RoundTransitionEnvelope(t, noteSeconds);
                float sample =
                    Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.68f +
                    Mathf.Sin(2f * Mathf.PI * frequency * 2f * t) * 0.18f +
                    Mathf.Sin(2f * Mathf.PI * frequency * 3f * t) * 0.07f;
                data[writeIndex++] = sample * env * 0.55f;
            }

            writeIndex += gapSamples;
        }

        var clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float RoundTransitionEnvelope(float time, float duration)
    {
        const float attack = 0.01f;
        const float release = 0.08f;
        if (time < attack)
        {
            return Mathf.Clamp01(time / attack);
        }

        if (time > duration - release)
        {
            return Mathf.Clamp01((duration - time) / release);
        }

        return 1f;
    }

    private static void PreloadRoundTransitionAudio(AudioClip clip)
    {
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }
    }

    private void BuildRoundStartFirstLine(RectTransform parent)
    {
        var row = new GameObject("RoundStartLine", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var rowRt = (RectTransform)row.transform;
        rowRt.anchorMin = new Vector2(0.5f, 0.5f);
        rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.sizeDelta = new Vector2(360f, 86f);
        rowRt.anchoredPosition = new Vector2(0f, -24f);

        var prefix = CreateRoundTransitionText("RoundPrefix", rowRt, "第", 64f, TextAlignmentOptions.Center, StatusStaticTextColor, 0.06f);
        SetFixedRowItem(prefix.rectTransform, -118f, 54f, 86f);

        roundStartNumberText = CreateRoundTransitionText("RoundNumber", rowRt, "128", 76f, TextAlignmentOptions.Center, StatusDynamicTextColor, 0.08f);
        roundStartNumberText.enableAutoSizing = true;
        roundStartNumberText.fontSizeMin = 58f;
        roundStartNumberText.fontSizeMax = 76f;
        SetFixedRowItem(roundStartNumberText.rectTransform, -24f, 132f, 86f);

        var suffix = CreateRoundTransitionText("RoundSuffix", rowRt, "回合", 64f, TextAlignmentOptions.Center, StatusStaticTextColor, 0.06f);
        SetFixedRowItem(suffix.rectTransform, 106f, 122f, 86f);
    }

    private TextMeshProUGUI CreateRoundTransitionText(
        string name,
        Transform parent,
        string content,
        float fontSize,
        TextAlignmentOptions align,
        Color color,
        float faceDilate)
    {
        var text = CreateTMPText(name, parent, content, fontSize, FontStyles.Bold, align);
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = Vector4.zero;
        ApplyTextFaceDilate(text, faceDilate);
        return text;
    }

    private static void AddFixedLayout(GameObject go, float preferredWidth)
    {
        var element = go.AddComponent<LayoutElement>();
        element.minWidth = preferredWidth;
        element.preferredWidth = preferredWidth;
        element.flexibleWidth = 0f;
    }

    private static void SetFixedRowItem(RectTransform rt, float x, float width, float height)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, 0f);
    }

    private static void SetLeftRowItem(RectTransform rt, float x, float width, float height)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, 0f);
    }

    public void ShowRoundStartTransition(int round)
    {
        if (roundStartOverlayRoot == null)
        {
            return;
        }

        if (roundStartRoutine != null)
        {
            StopCoroutine(roundStartRoutine);
        }

        if (roundEndRoutine != null)
        {
            StopCoroutine(roundEndRoutine);
            roundEndRoutine = null;
        }
        if (roundEndOverlayRoot != null)
        {
            roundEndOverlayRoot.SetActive(false);
        }

        roundStartRoutine = StartCoroutine(PlayRoundStartTransition(Mathf.Clamp(round, 0, 999)));
    }

    public void ShowRoundStartTransitionThenOpenInventory(int round, float extraDelaySeconds = 1f)
    {
        BeginStockPlanningRound(round);
        ShowRoundStartTransition(round);

        if (openInventoryAfterRoundStartRoutine != null)
        {
            StopCoroutine(openInventoryAfterRoundStartRoutine);
        }

        openInventoryAfterRoundStartRoutine = StartCoroutine(OpenInventoryAfterRoundStart(extraDelaySeconds));
    }

    private IEnumerator OpenInventoryAfterRoundStart(float extraDelaySeconds)
    {
        float waitSeconds =
            Mathf.Max(0f, roundStartIntroSeconds) +
            Mathf.Max(0f, roundStartHoldSeconds) +
            Mathf.Max(0f, roundStartOutroSeconds) +
            Mathf.Max(0f, extraDelaySeconds);

        yield return new WaitForSecondsRealtime(waitSeconds);
        OpenInventory();
        openInventoryAfterRoundStartRoutine = null;
    }

    private void BeginStockPlanningRound(int round)
    {
        RefreshTopLeftStatus(round, playerModel.CurrentMoney, "回合进行中");
        ResetPurchaseQuantitiesToDefaultRestock();
        canEditStockPlan = true;
        RebuildProductCells();
        RefreshStockControlsInteractable();
    }

    private void ResetPurchaseQuantitiesToDefaultRestock()
    {
        foreach (var product in marketProducts)
        {
            if (product == null)
            {
                continue;
            }

            product.PurchaseQuantity = Mathf.Max(product.DefaultStock - product.CurrentStock, 0);
        }
    }

    public void ShowRoundEndTransition(int todayMoneyDelta)
    {
        if (roundEndOverlayRoot == null)
        {
            return;
        }

        playerModel.TodayIncome = Mathf.Clamp(todayMoneyDelta, -10000, 10000);
        RefreshTopLeftStatus(currentRoundValue, playerModel.CurrentMoney, "回合结算中");

        if (roundEndRoutine != null)
        {
            StopCoroutine(roundEndRoutine);
        }

        if (roundStartRoutine != null)
        {
            StopCoroutine(roundStartRoutine);
            roundStartRoutine = null;
        }
        if (roundStartOverlayRoot != null)
        {
            roundStartOverlayRoot.SetActive(false);
        }

        roundEndRoutine = StartCoroutine(PlayRoundEndTransition(playerModel.TodayIncome));
    }

    public float RoundEndTransitionTotalSeconds()
    {
        return Mathf.Max(0f, roundStartIntroSeconds)
            + Mathf.Max(0f, roundEndCountSeconds)
            + 0.22f
            + Mathf.Max(0f, roundStartHoldSeconds)
            + Mathf.Max(0f, roundStartOutroSeconds);
    }

    public void PlayRoundEndNoticeSound()
    {
        PlayRoundTransitionSound(roundEndAudioClip, fallbackRoundEndAudioClip);
    }

    private IEnumerator PlayRoundStartTransition(int round)
    {
        roundStartOverlayRoot.SetActive(true);
        roundStartOverlayRoot.transform.SetAsLastSibling();
        PlayRoundTransitionSound(roundStartAudioClip, fallbackRoundStartAudioClip);

        if (roundStartNumberText != null)
        {
            roundStartNumberText.text = round.ToString();
        }

        if (roundStartCanvasGroup != null)
        {
            roundStartCanvasGroup.alpha = 0f;
        }

        if (roundStartTextCanvasGroup != null)
        {
            roundStartTextCanvasGroup.alpha = 0f;
        }

        Vector2 startPos = new Vector2(0f, 46f);
        Vector2 restPos = Vector2.zero;
        roundStartPanel.anchoredPosition = startPos;
        roundStartPanel.localScale = Vector3.one * 0.92f;

        float intro = Mathf.Max(0.01f, roundStartIntroSeconds);
        float elapsed = 0f;
        while (elapsed < intro)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / intro);
            float eased = EaseOutBack(t);
            roundStartCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            roundStartTextCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, t));
            roundStartPanel.anchoredPosition = Vector2.LerpUnclamped(startPos, restPos, eased);
            roundStartPanel.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, eased);
            yield return null;
        }

        roundStartCanvasGroup.alpha = 1f;
        roundStartTextCanvasGroup.alpha = 1f;
        roundStartPanel.anchoredPosition = restPos;
        roundStartPanel.localScale = Vector3.one;

        float holdUntil = Time.unscaledTime + Mathf.Max(0f, roundStartHoldSeconds);
        while (Time.unscaledTime < holdUntil)
        {
            yield return null;
        }

        float outro = Mathf.Max(0.01f, roundStartOutroSeconds);
        elapsed = 0f;
        while (elapsed < outro)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / outro);
            float eased = t * t;
            roundStartCanvasGroup.alpha = 1f - t;
            roundStartPanel.anchoredPosition = Vector2.Lerp(restPos, new Vector2(0f, -28f), eased);
            roundStartPanel.localScale = Vector3.one * Mathf.Lerp(1f, 0.985f, t);
            yield return null;
        }

        roundStartCanvasGroup.alpha = 0f;
        roundStartOverlayRoot.SetActive(false);
        roundStartRoutine = null;
    }

    private IEnumerator PlayRoundEndTransition(int todayMoneyDelta)
    {
        roundEndOverlayRoot.SetActive(true);
        roundEndOverlayRoot.transform.SetAsLastSibling();

        int amount = Mathf.Abs(todayMoneyDelta);
        bool isIncrease = todayMoneyDelta >= 0;
        Color amountColor = isIncrease ? new Color(0.08f, 0.50f, 0.16f, 1f) : new Color(0.74f, 0.08f, 0.08f, 1f);
        UpdateRoundEndAmountText(isIncrease, 0, amountColor);

        if (roundEndCanvasGroup != null)
        {
            roundEndCanvasGroup.alpha = 0f;
        }

        if (roundEndTextCanvasGroup != null)
        {
            roundEndTextCanvasGroup.alpha = 0f;
        }

        Vector2 startPos = new Vector2(0f, 46f);
        Vector2 restPos = Vector2.zero;
        roundEndPanel.anchoredPosition = startPos;
        roundEndPanel.localScale = Vector3.one * 0.92f;
        if (roundEndIncomeRow != null)
        {
            roundEndIncomeRow.localScale = Vector3.one;
        }

        float intro = Mathf.Max(0.01f, roundStartIntroSeconds);
        float elapsed = 0f;
        while (elapsed < intro)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / intro);
            float eased = EaseOutBack(t);
            roundEndCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            roundEndTextCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, t));
            roundEndPanel.anchoredPosition = Vector2.LerpUnclamped(startPos, restPos, eased);
            roundEndPanel.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, eased);
            yield return null;
        }

        roundEndCanvasGroup.alpha = 1f;
        roundEndTextCanvasGroup.alpha = 1f;
        roundEndPanel.anchoredPosition = restPos;
        roundEndPanel.localScale = Vector3.one;

        float countDuration = Mathf.Max(0.01f, roundEndCountSeconds);
        elapsed = 0f;
        while (elapsed < countDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / countDuration);
            int displayedAmount = Mathf.RoundToInt(Mathf.Lerp(0f, amount, Mathf.SmoothStep(0f, 1f, t)));
            UpdateRoundEndAmountText(isIncrease, displayedAmount, amountColor);
            yield return null;
        }

        UpdateRoundEndAmountText(isIncrease, amount, amountColor);

        const float emphasizeSeconds = 0.22f;
        elapsed = 0f;
        while (elapsed < emphasizeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / emphasizeSeconds);
            float pulse = Mathf.Sin(t * Mathf.PI);
            if (roundEndIncomeRow != null)
            {
                roundEndIncomeRow.localScale = Vector3.one * Mathf.Lerp(1f, 1.12f, pulse);
            }
            yield return null;
        }

        if (roundEndIncomeRow != null)
        {
            roundEndIncomeRow.localScale = Vector3.one;
        }

        float holdUntil = Time.unscaledTime + Mathf.Max(0f, roundStartHoldSeconds);
        while (Time.unscaledTime < holdUntil)
        {
            yield return null;
        }

        float outro = Mathf.Max(0.01f, roundStartOutroSeconds);
        elapsed = 0f;
        while (elapsed < outro)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / outro);
            float eased = t * t;
            roundEndCanvasGroup.alpha = 1f - t;
            roundEndPanel.anchoredPosition = Vector2.Lerp(restPos, new Vector2(0f, -28f), eased);
            roundEndPanel.localScale = Vector3.one * Mathf.Lerp(1f, 0.985f, t);
            yield return null;
        }

        roundEndCanvasGroup.alpha = 0f;
        roundEndOverlayRoot.SetActive(false);
        roundEndRoutine = null;
    }

    private void UpdateRoundEndAmountText(bool isIncrease, int amount, Color amountColor)
    {
        if (roundEndAmountText == null)
        {
            return;
        }

        roundEndAmountText.color = amountColor;
        roundEndAmountText.text = $"{(isIncrease ? "+" : "-")}{Mathf.Clamp(amount, 0, 10000)}";
    }

    private void PlayRoundTransitionSound(AudioClip clip, AudioClip fallbackClip)
    {
        if (clip == null || roundTransitionAudioSource == null)
        {
            Debug.LogWarning($"[ShopAssistantUI] Round transition audio skipped. clip={(clip == null ? "null" : clip.name)}, source={(roundTransitionAudioSource == null ? "null" : "ok")}");
            if (roundTransitionAudioSource != null && fallbackClip != null)
            {
                roundTransitionAudioSource.volume = roundTransitionAudioVolume;
                roundTransitionAudioSource.PlayOneShot(fallbackClip, 1f);
                Debug.Log($"[ShopAssistantUI] Round transition fallback audio played: {fallbackClip.name}, volume={roundTransitionAudioVolume}");
            }
            return;
        }

        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        if (clip.loadState == AudioDataLoadState.Loading)
        {
            StartCoroutine(PlayRoundTransitionSoundWhenLoaded(clip, fallbackClip));
            return;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning($"[ShopAssistantUI] Round transition audio not ready: {clip.name}, loadState={clip.loadState}");
            if (fallbackClip != null)
            {
                roundTransitionAudioSource.volume = roundTransitionAudioVolume;
                roundTransitionAudioSource.PlayOneShot(fallbackClip, 1f);
                Debug.Log($"[ShopAssistantUI] Round transition fallback audio played: {fallbackClip.name}, volume={roundTransitionAudioVolume}");
            }
            return;
        }

        roundTransitionAudioSource.volume = roundTransitionAudioVolume;
        roundTransitionAudioSource.PlayOneShot(clip, 1f);
        Debug.Log($"[ShopAssistantUI] Round transition audio played: {clip.name}, volume={roundTransitionAudioVolume}, listeners={FindObjectsOfType<AudioListener>(true).Length}");
    }

    private IEnumerator PlayRoundTransitionSoundWhenLoaded(AudioClip clip, AudioClip fallbackClip)
    {
        float deadline = Time.realtimeSinceStartup + 0.25f;
        while (clip != null && clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        if (clip != null && clip.loadState == AudioDataLoadState.Loaded && roundTransitionAudioSource != null)
        {
            roundTransitionAudioSource.volume = roundTransitionAudioVolume;
            roundTransitionAudioSource.PlayOneShot(clip, 1f);
        }
        else if (fallbackClip != null && roundTransitionAudioSource != null)
        {
            roundTransitionAudioSource.volume = roundTransitionAudioVolume;
            roundTransitionAudioSource.PlayOneShot(fallbackClip, 1f);
            Debug.Log($"[ShopAssistantUI] Round transition fallback audio played after load timeout: {fallbackClip.name}, volume={roundTransitionAudioVolume}");
        }
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

}

