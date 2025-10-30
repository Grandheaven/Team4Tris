using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // TMP_InputField 및 TMP_Dropdown 사용 시

public class DynamicTableController : MonoBehaviour
{
    // --- UI 요소 연결 ---
    public GameObject dataRowPrefab;        // 내용 행 프리팹 (HeaderRow와 동일한 크기 및 구성 가정)
    public Transform contentParent;          // ScrollView의 Content 오브젝트
    public TMP_InputField betaInputField;   // 'Beta' Input Field (검색어 입력)
    public TMP_Dropdown conditionDropdown;   // 드롭다운 (검색 조건 선택)
    public Color oddRowColor = new Color(0.925f, 0.925f, 0.925f, 1f); // ECECEC
    public Color evenRowColor = Color.white; // FFFFFF

    // --- 데이터 저장 ---
    private List<DataRow> allDataRows = new List<DataRow>(); // 전체 테이블 데이터 (ALPHA 테이블)
    private List<DataRow> currentDisplayedRows = new List<DataRow>(); // 현재 표시 중인 데이터

    // 가상의 데이터 구조 (실제 DB 테이블 구조에 맞게 수정 필요)
    public class DataRow
    {
        public int id;
        public string column1;
        public string column2;
        // ... 필요한 모든 컬럼 추가
    }

    void Start()
    {
        // 1. 초기 데이터 로드 및 테이블 생성
        StartCoroutine(LoadDataAndBuildTable());

        // 2. 'Beta' Input Field 이벤트 리스너 추가
        // Enter 키 입력 시 (EndEdit) 또는 포커스 잃을 시 검색 수행
        betaInputField.onEndEdit.AddListener(OnSearchEndEdit);
    }

    // --- 데이터 로드 (DB 연동) ---
    private IEnumerator LoadDataAndBuildTable()
    {
        // **[오라클 DB 연동 및 데이터 로드 가상 영역]**
        // 실제로는 DB 연결, 'ALPHA' 테이블 SELECT 쿼리 실행, 데이터 파싱 코드가 들어갑니다.
        // 비동기/별도 스레드 처리가 권장됩니다.
        yield return StartCoroutine(FetchDataFromOracleDB());

        // 데이터 로드 후 테이블 초기화
        UpdateTable(allDataRows);
    }

    // **가상의 데이터 로드 메소드** (실제 DB 연동 코드로 대체해야 합니다)
    private IEnumerator FetchDataFromOracleDB()
    {
        Debug.Log("Oracle DB에서 'ALPHA' 테이블 정보 로드 중...");

        // **[실제 DB 연동 코드 영역]**
        // 예시 데이터 채우기:
        for (int i = 0; i < 20; i++)
        {
            allDataRows.Add(new DataRow { id = i + 1, column1 = $"데이터_{i + 1}", column2 = $"검색키_{i % 5}" });
        }

        // DB 응답 대기 시간 가상
        yield return new WaitForSeconds(0.5f);

        Debug.Log($"총 {allDataRows.Count}개의 데이터 로드 완료.");
    }

    // --- 검색 처리 ---
    private void OnSearchEndEdit(string searchText)
    {
        // 'Beta' Input Field 외의 공간 클릭 시에도 OnEndEdit이 발생합니다.
        // Input Field가 활성화 상태(Enter 입력)일 때만 처리하도록 isFocused 확인 필요 (선택적)
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && betaInputField.isFocused)
        {
            // Input Field가 아직 포커스를 갖고 있고 Enter를 누르지 않았다면 무시
            return;
        }

        // 검색 조건 및 검색어 추출
        string selectedCondition = conditionDropdown.options[conditionDropdown.value].text;
        string trimmedSearchText = searchText.Trim();

        Debug.Log($"검색 조건: {selectedCondition}, 검색어: {trimmedSearchText}");

        // 검색 필터링
        List<DataRow> filteredList = FilterData(allDataRows, selectedCondition, trimmedSearchText);

        // 테이블 업데이트
        UpdateTable(filteredList);
    }

    private List<DataRow> FilterData(List<DataRow> data, string condition, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            return data; // 검색어가 없으면 전체 리스트 반환
        }

        // 실제 검색 조건에 따라 필터링 로직 구현 (예: condition에 따라 검색 대상 컬럼 변경)
        return data.FindAll(row =>
            // 예시: column2를 검색 대상으로 가정
            row.column2.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0
        // 여기에 'condition' 변수를 사용하여 동적 컬럼 검색 로직을 추가해야 합니다.
        );
    }


    // --- 테이블 생성/갱신 ---
    private void UpdateTable(List<DataRow> displayData)
    {
        currentDisplayedRows = displayData;

        // 1. 기존 내용 행 모두 제거
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            // HeaderRow는 남기고, 내용 행(DataRowPrefab의 복제본)만 제거
            if (contentParent.GetChild(i).name != "HeaderRow")
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }

        // 2. 새 내용 행 생성 및 데이터 바인딩
        for (int i = 0; i < currentDisplayedRows.Count; i++)
        {
            GameObject rowObject = Instantiate(dataRowPrefab, contentParent);
            rowObject.name = $"DataRow_{i + 1}";

            // 데이터 바인딩 (실제 컴포넌트 접근 로직 필요)
            // 예: rowObject.transform.Find("Column1Text").GetComponent<Text>().text = currentDisplayedRows[i].column1;

            // 홀짝 행 색상 적용
            Image rowImage = rowObject.GetComponent<Image>();
            if (rowImage != null)
            {
                rowImage.color = (i % 2 == 0) ? oddRowColor : evenRowColor; // i=0이 첫 번째 내용 행 (홀수번째)
            }

            // 행 클릭 이벤트 추가 (버튼 컴포넌트 또는 별도의 스크립트 이용)
            Button rowButton = rowObject.GetComponent<Button>();
            if (rowButton == null)
            {
                // 프리팹에 Button 컴포넌트가 없다면 추가
                rowButton = rowObject.AddComponent<Button>();
            }

            int rowIndex = i; // 클로저 문제 방지를 위해 로컬 변수 사용
            rowButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
            rowButton.onClick.AddListener(() => OnRowClicked(currentDisplayedRows[rowIndex]));
        }
    }

    // --- 행 클릭 이벤트 처리 ---
    private void OnRowClicked(DataRow clickedData)
    {
        Debug.Log($"행 클릭! ID: {clickedData.id}, 컬럼1: {clickedData.column1}");
        // 여기에 클릭된 행의 데이터를 활용한 추가 로직을 구현합니다.
    }
}