using UnityEngine;
using UnityEngine.UI;

public class popup : MonoBehaviour
{
    [Header("보였다/숨길 대상 오브젝트")]
    [SerializeField] private GameObject targetObject;

    private Button button;

    void Awake()
    {
        // 버튼 컴포넌트 가져오기
        button = GetComponent<Button>();

        // 버튼 클릭 이벤트 등록
        button.onClick.AddListener(ToggleObject);
    }

    private void ToggleObject()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("⚠️ targetObject가 연결되지 않았습니다!");
            return;
        }

        // 활성화 상태를 반전시킴
        bool nextState = !targetObject.activeSelf;
        targetObject.SetActive(nextState);
    }
}