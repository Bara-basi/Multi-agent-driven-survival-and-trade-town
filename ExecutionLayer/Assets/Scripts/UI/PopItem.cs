using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PopItem : MonoBehaviour
{
    [Header("Refs")]
    public Image icon;
    public TMP_Text label;
    public CanvasGroup cg;
    public RectTransform rt;

    [Header("Anim")]
    public float riseDistance = 6f;      // 自身动画上飘高度
    public float duration = 2.4f;         // 总时长
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1.1f, 1, 1f);

    [Header("Icon")]
    [SerializeField] private Vector2 iconDisplaySize = new Vector2(2.25f, 2.25f);
    [SerializeField] private float iconCenterX = -0.72f;

    [Header("Label")]
    [SerializeField] private float labelFontSize = 1.34f;
    [SerializeField] private FontStyles labelFontStyle = FontStyles.Normal;
    [SerializeField] private float labelOutlineWidth = 0.16f;
    [SerializeField] private float labelStartX = 0.48f;
    [SerializeField] private Vector2 labelBoxSize = new Vector2(4.6f, 2.1f);

    [Header("Lane")]
    public int laneIndex = 0;             // 0 在最上方

    private System.Action<PopItem> _onComplete;
    private bool _playing;
    private Vector2 _laneBase;            // 由 lane 决定的基础位置
    private Vector2 visual;
    private Material _labelRuntimeMaterial;
    private float _t;                     // 动画计时

    void Reset()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        icon = transform.Find("Icon")?.GetComponent<Image>();
        label = transform.Find("Label")?.GetComponent<TMP_Text>() ?? transform.Find("Label ")?.GetComponent<TMP_Text>();

    }

    public void SetLane(int lane, bool snap = false)
    {
        laneIndex = lane;
        var newBase = new Vector2(0f, 0f);

        if (snap || rt == null)
        {
            _laneBase = newBase;

        }
        else
        {
            // 平滑换道：保持视觉位置不跳变
            visual = rt.anchoredPosition;
            float p = Mathf.Clamp01(_t / Mathf.Max(0.0001f, duration));
            float moveT = moveCurve.Evaluate(p);
            Vector2 animRise = new Vector2(0f, riseDistance * moveT);
            _laneBase = visual - animRise;
        }
    }

    public void UpdateLane(int newLaneIndex)
    {
        SetLane(newLaneIndex, snap: false);
    }

    public void Play(Sprite s, string text, Color textColor, System.Action<PopItem> onComplete)
    {
        ResolveRefs();

        _onComplete = onComplete;
        _playing = true;

        ApplyIcon(s);
        ApplyLabel(text, textColor);

        StopAllCoroutines();
        StartCoroutine(CoPlay());
    }

    private void ResolveRefs()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        if (cg == null) cg = GetComponent<CanvasGroup>();
        if (icon == null) icon = transform.Find("Icon")?.GetComponent<Image>();
        if (label == null)
        {
            label = transform.Find("Label")?.GetComponent<TMP_Text>()
                ?? transform.Find("Label ")?.GetComponent<TMP_Text>()
                ?? GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void ApplyIcon(Sprite s)
    {
        if (icon == null)
        {
            return;
        }

        icon.sprite = s;
        icon.preserveAspect = true;
        icon.useSpriteMesh = true;
        icon.enabled = s != null;

        var iconRt = icon.rectTransform;
        if (iconRt == null)
        {
            return;
        }

        Vector2 targetSize = iconDisplaySize;
        if (targetSize.x <= 0f || targetSize.y <= 0f)
        {
            targetSize = new Vector2(2f, 2f);
        }

        iconRt.localScale = Vector3.one;
        iconRt.sizeDelta = targetSize;
        iconRt.anchoredPosition = new Vector2(iconCenterX, iconRt.anchoredPosition.y);
    }

    private void ApplyLabel(string text, Color textColor)
    {
        if (label == null)
        {
            return;
        }

        EnsureLabelRuntimeMaterial();

        label.text = text;
        label.fontSize = labelFontSize;
        label.fontStyle = labelFontStyle;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableAutoSizing = false;
        label.enableVertexGradient = false;
        label.overrideColorTags = true;
        label.alpha = textColor.a;
        label.color = textColor;
        ApplyCompactLabelRect();
        label.ForceMeshUpdate();
    }

    private void ApplyCompactLabelRect()
    {
        var labelRt = label.rectTransform;
        if (labelRt == null)
        {
            return;
        }

        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.pivot = new Vector2(0f, 0.5f);
        labelRt.anchoredPosition = new Vector2(labelStartX, labelRt.anchoredPosition.y);
        labelRt.sizeDelta = labelBoxSize;
    }

    private void EnsureLabelRuntimeMaterial()
    {
        if (label == null || _labelRuntimeMaterial != null)
        {
            return;
        }

        var sourceMaterial = label.fontMaterial != null ? label.fontMaterial : label.fontSharedMaterial;
        if (sourceMaterial == null)
        {
            return;
        }

        _labelRuntimeMaterial = new Material(sourceMaterial)
        {
            name = $"{sourceMaterial.name}_PopItemRuntime"
        };

        if (_labelRuntimeMaterial.HasProperty(ShaderUtilities.ID_FaceColor))
        {
            _labelRuntimeMaterial.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
        }

        if (_labelRuntimeMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth))
        {
            _labelRuntimeMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, labelOutlineWidth);
        }

        if (_labelRuntimeMaterial.HasProperty(ShaderUtilities.ID_OutlineColor))
        {
            _labelRuntimeMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.white);
        }

        label.fontMaterial = _labelRuntimeMaterial;
    }

    private void OnDestroy()
    {
        if (_labelRuntimeMaterial != null)
        {
            Destroy(_labelRuntimeMaterial);
        }
    }

    IEnumerator CoPlay()
    {
        cg.alpha = 1f;
        _t = 0f;

        while (_t < duration)
        {
            _t += Time.deltaTime;
            float p = Mathf.Clamp01(_t / duration);

            float moveT = moveCurve.Evaluate(p);
            var animRise = new Vector2(0f, riseDistance * moveT);

            cg.alpha = fadeCurve.Evaluate(p);
            rt.localScale = Vector3.one * scaleCurve.Evaluate(p);
            rt.anchoredPosition = _laneBase + animRise;

            yield return null;
        }

        _playing = false;
        _onComplete?.Invoke(this);
    }

    public bool IsPlaying() => _playing;
}
