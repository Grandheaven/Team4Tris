using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class PanelSlide : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform panel;

    [Header("Y축 크기 설정")]
    public float defaultScaleY = 0.001f;
    public float expandedScaleY = 0.1f;

    [Header("애니메이션")]
    public float duration = 0.3f;
    public CanvasGroup textCanvasGroup;

    private bool isExpanded = false;
    private Tween currentScaleTween;
    private Tween currentAlphaTween;

    public void Start()
    {
        // 시작 시 패널은 축소된 상태로, 텍스트는 투명하게 설정
        panel.localScale = new Vector3(panel.localScale.x, defaultScaleY, panel.localScale.z);
        if (textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 0f;
            textCanvasGroup.blocksRaycasts = false; // 마우스 클릭 방지
        }
    }

    public void OnPointerEnter(PointerEventData eventData)              //버튼에 마우스 가져다 대면 
    {
        isExpanded = true;
        ScalePanel();
        AnimateTextAlpha(1f, true); // 텍스트를 보이게 ( 투명도 1)
    }

    public void OnPointerExit(PointerEventData eventData)                //버튼에 마우스 치우면
    {
        isExpanded = false;
        ScalePanel();
        AnimateTextAlpha(0f, true); // 텍스트를 보이게 ( 투명도 0)
    }

    private void ScalePanel()
    {
        if (currentScaleTween != null && currentScaleTween.IsActive()) currentScaleTween.Kill();       //애니메이션이 겹치는걸 방지

        float targetY = isExpanded ? expandedScaleY : defaultScaleY;

        Vector3 targetScale = new Vector3(
            panel.localScale.x,   // X축 유지
            targetY,              // Y축만 변경
            panel.localScale.z    // Z축 유지
        );

        currentScaleTween = panel.DOScale(targetScale, duration)
                            .SetEase(isExpanded ? Ease.OutBack : Ease.InCubic);
    }

    private void AnimateTextAlpha(float targetAlpha, bool blocksRaycasts)
    {
        if (textCanvasGroup == null) return;
        //이전 투명도 애니메이션 중단
        if (textCanvasGroup != null && currentAlphaTween.IsActive()) currentAlphaTween.Kill();
        //확장시에만 레이캐스트를 활성화
        if (blocksRaycasts)
        {
            textCanvasGroup.blocksRaycasts = true;
        }
        //텍스트 투명도 애니메이션

        currentAlphaTween = textCanvasGroup.DOFade(targetAlpha, duration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                //축소 환료 시에만 레이캐스트를 비활성화
                if (!blocksRaycasts)
                {
                    textCanvasGroup.blocksRaycasts = false;
                }
            });
    }
}


