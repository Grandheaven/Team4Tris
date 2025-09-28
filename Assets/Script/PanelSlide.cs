using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class PanelSlide : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Transform panel;

    [Header("Y축 크기 설정")]
    public float defaultScaleY = 0.001f;
    public float expandedScaleY = 0.1f;

    [Header("애니메이션")]
    public float duration = 0.3f;

    private bool isExpanded = false;
    private Tween currentTween;

    public void OnPointerEnter(PointerEventData eventData)              //버튼에 마우스 가져다 대면 
    {
        isExpanded = true;
        ScalePanel();
    }

    public void OnPointerExit(PointerEventData eventData)                //버튼에 마우스 치우면
    {
        isExpanded = false;
        ScalePanel();
    }

    private void ScalePanel()
    {
        if (currentTween != null && currentTween.IsActive()) currentTween.Kill();       //애니메이션이 겹치는걸 방지

        float targetY = isExpanded ? expandedScaleY : defaultScaleY;

        Vector3 targetScale = new Vector3(
            panel.localScale.x,   // X축 유지
            targetY,              // Y축만 변경
            panel.localScale.z    // Z축 유지
        );

        currentTween = panel.DOScale(targetScale, duration)
                            .SetEase(isExpanded ? Ease.OutBack : Ease.InCubic);
    }
}


