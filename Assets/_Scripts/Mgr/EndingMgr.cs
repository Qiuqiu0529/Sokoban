using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingMgr : Singleton<EndingMgr>
{
    private int pairCount;
    private int currentCount;
    private bool isCheckingEnd;
    private bool completionTriggered;
    private GameObject completionCanvas;
    private CanvasGroup completionCanvasGroup;
    private RectTransform completionTextTransform;
    private TextMeshProUGUI completionText;

    public void AddNeedPair()
    {
        ++pairCount;
    }

    public void DeleteNeedPair()
    {
        pairCount = Mathf.Max(0, pairCount - 1);
    }

    public void AddCorrectPair()
    {
        ++currentCount;
        CheckEnd();
    }

    public void DeletePair()
    {
        currentCount = Mathf.Max(0, currentCount - 1);
        CheckEnd();
    }

    public void CheckEnd()
    {
        if (pairCount > 0 && currentCount >= pairCount && !isCheckingEnd && !completionTriggered)
        {
            Debug.Log("All clear!");
            isCheckingEnd = true;
            StartCoroutine(Check());
        }
        else
        {
            Debug.Log("Not finish");
        }
    }

    public IEnumerator Check()
    {
        yield return new WaitForSeconds(0.3f);
        while (pairCount > 0 && currentCount >= pairCount)
        {
            if (GameMgr.Instance.CanCheckEnd())
            {
                completionTriggered = true;
                if (GameMgr.Instance.IsEditorTestLevel)
                {
                    ShowTestLevelCompleted();
                }
                else
                {
                    ProgressMgr.Instance.MarkLevelCleared(GameMgr.Instance.levelName);
                    LevelMgr.Instance.AfterLevelClear(GameMgr.Instance.levelName);
                }
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        isCheckingEnd = false;
    }

    private void ShowTestLevelCompleted()
    {
        EnsureCompletionNotice();
        completionCanvas.SetActive(true);
        completionText.text = "Clear!";
        completionCanvasGroup.alpha = 0f;
        completionTextTransform.localScale = Vector3.one * 0.82f;

        DOTween.Kill(completionCanvasGroup);
        DOTween.Sequence()
            .Append(completionCanvasGroup.DOFade(1f, 0.25f))
            .Join(completionTextTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
            .AppendInterval(1.8f)
            .Append(completionCanvasGroup.DOFade(0f, 0.45f))
            .OnComplete(() => completionCanvas.SetActive(false));
    }

    private void EnsureCompletionNotice()
    {
        if (completionCanvas != null) return;

        completionCanvas = new GameObject("TestLevelCompletionNotice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = completionCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = completionCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        completionCanvasGroup = completionCanvas.GetComponent<CanvasGroup>();

        GameObject textObject = new GameObject("CompletionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(completionCanvas.transform, false);
        completionTextTransform = textObject.GetComponent<RectTransform>();
        completionTextTransform.anchorMin = new Vector2(0.1f, 0.4f);
        completionTextTransform.anchorMax = new Vector2(0.9f, 0.6f);
        completionTextTransform.offsetMin = Vector2.zero;
        completionTextTransform.offsetMax = Vector2.zero;

        completionText = textObject.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            completionText.font = TMP_Settings.defaultFontAsset;
        completionText.text = "Clear!";
        completionText.alignment = TextAlignmentOptions.Center;
        completionText.fontSize = 64f;
        completionText.fontStyle = FontStyles.Bold;
        completionText.color = Color.white;
        completionText.outlineWidth = 0.2f;
        completionText.outlineColor = new Color(0.12f, 0.08f, 0.16f, 1f);
        completionCanvas.SetActive(false);
    }
}
