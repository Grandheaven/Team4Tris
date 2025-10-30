using UnityEngine;
using UnityEngine.UI;

public class popup : MonoBehaviour
{
    [Header("팝업 오브젝트 (각 버튼별로 다른 팝업)")]
    [SerializeField] private GameObject targetPopup;

    [Header("안내문 오브젝트 (각 버튼별로 다름)")]
    [SerializeField] private GameObject guideMessage;

    [Header("다른 버튼 오브젝트 (상호 제어용)")]
    [SerializeField] private GameObject otherButton;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        // 1️⃣ 다른 버튼 숨김
        if (otherButton != null)
            otherButton.SetActive(false);

        // 2️⃣ 내 팝업 토글
        if (targetPopup != null)
        {
            bool nextState = !targetPopup.activeSelf;
            targetPopup.SetActive(nextState);
        }

        // 3️⃣ 안내문 표시 (팝업이 켜질 때만)
        if (guideMessage != null)
            guideMessage.SetActive(targetPopup.activeSelf);
    }

    // 4️⃣ 팝업 닫을 때 다시 다른 버튼 보이게
    public void ClosePopup()
    {
        if (targetPopup != null)
            targetPopup.SetActive(false);

        if (guideMessage != null)
            guideMessage.SetActive(false);

        if (otherButton != null)
            otherButton.SetActive(true);
    }
}
