using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System; // DBNull, Exception
using System.Data; // CommandType
using Oracle.ManagedDataAccess.Client; // Oracle
using System.Threading.Tasks; // Task.Run (비동기 처리)
using System.Globalization; // CultureInfo (날짜 포맷팅)
using UnityEngine.UI.TableUI; // [!!!] TableUI 에셋의 네임스페이스 추가

public class fmDynamicTableController : MonoBehaviour
{
    // --- DB 접속 정보 ---
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    // --- UI 요소 연결 ---
    [Header("테이블 및 검색 UI")]
    public TableUI table;
    public TMP_InputField betaInputField;
    public TMP_Dropdown conditionDropdown;

    [Header("테이블 하단 버튼 (화면 구조 반영)")]
    public Button rentButton;       // '대여' 버튼
    public Button returnButton;     // '반납 및 기록' 버튼
    public Button cancelButton;     // '취소' 버튼

    [Header("Pop-Up UI")]
    public GameObject popup_Rent;     // '대여' 버튼 클릭 시 팝업
    public GameObject popup_Return;   // '반납 및 기록' 버튼 클릭 시 팝업

    // 🌟 (신규) 팝업 내 이름/전화번호 표시 UI
    [Header("Pop-Up Nameplates")]
    public TextMeshProUGUI popup_Rent_Nameplate;
    public TextMeshProUGUI popup_Return_Nameplate;

    [Header("UI 상호작용 제어")]
    public CanvasGroup mainCanvasGroup; // 메인 패널 또는 전체 화면의 CanvasGroup

    [Header("테이블 스타일")]
    public Color selectedRowColor = new Color(0.5f, 0.8f, 1f); // 선택 시 강조 색상

    // --- 데이터 저장 및 상태 ---
    private List<DataRow> allDataRows = new List<DataRow>(); // DB에서 가져온 원본 데이터
    private DataRow selectedRow = null; // 현재 선택된 행의 데이터
    private GameObject selectedRowObject = null; // 현재 선택된 행의 GameObject
    private Color originalSelectedRowColor; // 선택된 행의 원래 (줄무늬) 색상

    // MEMBER 테이블 구조에 맞춘 DataRow 클래스
    public class DataRow
    {
        public int mno { get; set; }
        public string name { get; set; }
        public string birth { get; set; }
        public string sex { get; set; }
        public string tel { get; set; }
    }

    void Start()
    {
        if (table == null)
        {
            Debug.LogError("TableUI 컴포넌트가 Inspector에 할당되지 않았습니다!");
            return;
        }
        if (mainCanvasGroup == null)
        {
            Debug.LogWarning("Main Canvas Group이 Inspector에 할당되지 않았습니다! 팝업 시 배경 비활성화 기능이 불안정할 수 있습니다.");
        }
        // 🌟 (신규) Nameplate 연결 확인
        if (popup_Rent_Nameplate == null || popup_Return_Nameplate == null)
        {
            Debug.LogWarning("Popup Nameplate UI가 Inspector에 할당되지 않았습니다! 이름/전화번호 전달 기능이 작동하지 않습니다.");
        }


        // 1. 초기 데이터 로드
        StartCoroutine(LoadDataAndBuildTable());

        // 2. 검색 이벤트 리스너 추가
        betaInputField.onEndEdit.AddListener(OnSearchEndEdit);

        // 3. 드롭다운 옵션 설정 (요구사항 반영: 이름, 전화번호)
        SetupDropdown();

        // 4. 대여/반납 버튼 리스너 추가
        rentButton.onClick.AddListener(OnRentButtonClicked);
        returnButton.onClick.AddListener(OnReturnButtonClicked);

        // 5. 초기에는 버튼 비활성화
        SetButtonsInteractable(false);
    }

    // 드롭다운 옵션 설정 메소드
    private void SetupDropdown()
    {
        conditionDropdown.ClearOptions();
        List<string> options = new List<string> { "선택", "이름", "전화번호" };
        conditionDropdown.AddOptions(options);
    }

    // (LoadDataAndBuildTable 및 FetchDataFromOracleDB는 내용 변경 없이 유지)
    private IEnumerator LoadDataAndBuildTable()
    {
        ClearSelection();
        yield return StartCoroutine(FetchDataFromOracleDB());
        PopulateTable(allDataRows);
    }

    private IEnumerator FetchDataFromOracleDB()
    {
        Debug.Log("Oracle DB에서 'MEMBER' 테이블 정보 로드 중...");
        List<DataRow> loadedData = null;
        bool isError = false;
        string errorMessage = "";

        Task dbTask = Task.Run(() =>
        {
            List<DataRow> tempData = new List<DataRow>();
            string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";

            try
            {
                using (OracleConnection connection = new OracleConnection(connString))
                {
                    connection.Open();
                    string sql = "SELECT MNO, NAME, BIRTH, SEX, TEL FROM MEMBER ORDER BY MNO";
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DataRow row = new DataRow
                                {
                                    mno = reader.GetInt32(reader.GetOrdinal("MNO")),
                                    name = ReadString(reader["NAME"]),
                                    birth = reader.GetDateTime(reader.GetOrdinal("BIRTH")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                    sex = FormatSex(ReadString(reader["SEX"])),
                                    tel = FormatTel(ReadString(reader["TEL"]))
                                };
                                tempData.Add(row);
                            }
                        }
                    }
                }
                loadedData = tempData;
            }
            catch (Exception ex)
            {
                isError = true;
                errorMessage = ex.Message;
            }
        });

        yield return new WaitUntil(() => dbTask.IsCompleted);

        if (isError)
        {
            Debug.LogError($"DB 작업 실패: {errorMessage}");
        }
        else if (loadedData != null)
        {
            allDataRows = loadedData;
            Debug.Log($"총 {allDataRows.Count}개의 데이터 로드 완료.");
        }
        else
        {
            Debug.LogWarning("DB 작업은 성공했으나, 로드된 데이터가 없습니다.");
        }
    }

    // (PopulateTable, OnSearchEndEdit, FilterData, OnRowClicked는 내용 변경 없이 유지)
    private void PopulateTable(List<DataRow> dataToDisplay)
    {
        ClearSelection();

        table.Rows = dataToDisplay.Count + 1;
        table.Columns = 5;

        table.GetCell(0, 0).text = "MNO";
        table.GetCell(0, 1).text = "이름";
        table.GetCell(0, 2).text = "생년월일";
        table.GetCell(0, 3).text = "성별";
        table.GetCell(0, 4).text = "연락처";

        for (int i = 0; i < dataToDisplay.Count; i++)
        {
            DataRow rowData = dataToDisplay[i];
            int tableRowIndex = i + 1;

            table.GetCell(tableRowIndex, 0).text = rowData.mno.ToString();
            table.GetCell(tableRowIndex, 1).text = rowData.name;
            table.GetCell(tableRowIndex, 2).text = rowData.birth;
            table.GetCell(tableRowIndex, 3).text = rowData.sex;
            table.GetCell(tableRowIndex, 4).text = rowData.tel;

            GameObject rowObject = table.GetCell(tableRowIndex, 0).transform.parent.parent.gameObject;
            rowObject.transform.localScale = Vector3.one;
            rowObject.transform.localPosition = new Vector3(rowObject.transform.localPosition.x, rowObject.transform.localPosition.y, 0f);

            Button rowButton = rowObject.GetComponent<Button>();
            if (rowButton == null) rowButton = rowObject.AddComponent<Button>();

            Image rowImage = rowObject.transform.Find("panel").GetComponent<Image>();
            if (rowImage != null) rowButton.targetGraphic = rowImage;

            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(() => OnRowClicked(rowData, rowObject));
        }
    }

    private void OnSearchEndEdit(string searchText)
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && betaInputField.isFocused)
            return;

        string selectedCondition = conditionDropdown.options[conditionDropdown.value].text;
        string trimmedSearchText = searchText.Trim();
        List<DataRow> filteredList = FilterData(allDataRows, selectedCondition, trimmedSearchText);
        PopulateTable(filteredList);
    }

    private List<DataRow> FilterData(List<DataRow> data, string condition, string searchText)
    {
        if (string.IsNullOrEmpty(searchText)) return data;

        var comparison = System.StringComparison.OrdinalIgnoreCase;

        return data.FindAll(row =>
        {
            switch (condition)
            {
                case "이름":
                    return row.name.IndexOf(searchText, comparison) >= 0;
                case "전화번호":
                    return row.tel.IndexOf(searchText, comparison) >= 0;
                case "선택":
                default:
                    return row.name.IndexOf(searchText, comparison) >= 0 || row.tel.IndexOf(searchText, comparison) >= 0;
            }
        });
    }

    private void OnRowClicked(DataRow clickedData, GameObject rowObject)
    {
        Image panelImage = rowObject.transform.Find("panel").GetComponent<Image>();
        if (panelImage == null) return;

        if (selectedRowObject == rowObject)
        {
            ClearSelection();
            Debug.Log($"선택된 행({clickedData.name})을 다시 클릭하여 선택 해제했습니다.");
        }
        else
        {
            if (selectedRowObject != null)
            {
                Image prevImage = selectedRowObject.transform.Find("panel").GetComponent<Image>();
                if (prevImage != null)
                {
                    prevImage.color = originalSelectedRowColor;
                }
            }

            selectedRow = clickedData;
            selectedRowObject = rowObject;
            originalSelectedRowColor = panelImage.color;
            panelImage.color = selectedRowColor;

            SetButtonsInteractable(true);
            Debug.Log($"행 선택! MNO: {selectedRow.mno}, NAME: {selectedRow.name}. 대여/반납 버튼 활성화.");
        }
    }

    // 🌟 (핵심 수정) '대여' 버튼 클릭 이벤트: Nameplate에 정보 전달 및 팝업 초기화
    private void OnRentButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("대여할 회원을 먼저 선택하세요.");
            return;
        }

        // 이름과 전화번호 포맷 (줄바꿈 적용)
        string displayInfo = $"{selectedRow.name}\n{selectedRow.tel}";

        // 1. 팝업 표시
        if (popup_Rent != null)
        {
            popup_Rent.SetActive(true);

            // 2. RentPopupController 초기화 및 MNO 전달
            RentPopupController rentController = popup_Rent.GetComponent<RentPopupController>();
            if (rentController != null)
            {
                // MNO와 이름/전화번호 정보를 함께 전달하여 테이블 로드 시작
                rentController.Initialize(selectedRow.mno, displayInfo);
                Debug.Log($"Popup_대여 Nameplate에 정보 전달: {displayInfo.Replace("\n", " / ")} 및 MNO: {selectedRow.mno} 전달 완료.");
            }
            else
            {
                Debug.LogError("Popup_Rent GameObject에 RentPopupController 스크립트가 연결되어 있지 않습니다!");
            }
        }

        // 3. 배경 UI 비활성화
        SetCanvasInteractable(false);
    }

    // 🌟 (핵심 수정) '반납 및 기록' 버튼 클릭 이벤트: Nameplate에 정보 전달
    // 🌟 (핵심 수정) '반납 및 기록' 버튼 클릭 이벤트: Nameplate에 정보 전달 및 팝업 초기화
    private void OnReturnButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("반납할 회원을 먼저 선택하세요.");
            return;
        }

        // 이름과 전화번호 포맷 (줄바꿈 적용)
        string displayInfo = $"{selectedRow.name}\n{selectedRow.tel}";

        // 1. 팝업 표시
        if (popup_Return != null)
        {
            popup_Return.SetActive(true);

            // 2. ReturnPopupController 초기화 및 MNO 전달
            ReturnPopupController returnController = popup_Return.GetComponent<ReturnPopupController>();
            if (returnController != null)
            {
                // MNO와 이름/전화번호 정보를 함께 전달하여 테이블 로드 시작
                returnController.Initialize(selectedRow.mno, displayInfo);
                Debug.Log($"Popup_반납,기록 Nameplate에 정보 전달: {displayInfo.Replace("\n", " / ")} 및 MNO: {selectedRow.mno} 전달 완료.");
            }
            else
            {
                Debug.LogError("Popup_Return GameObject에 ReturnPopupController 스크립트가 연결되어 있지 않습니다!");
            }
        }

        // 3. 배경 UI 비활성화
        SetCanvasInteractable(false);
    }

    // (ClosePopUp 및 유틸리티 함수는 내용 변경 없이 유지)
    private void SetCanvasInteractable(bool interactable)
    {
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.interactable = interactable;
            mainCanvasGroup.blocksRaycasts = interactable;
        }
    }

    public void ClosePopUp(GameObject popupToClose)
    {
        if (popupToClose != null)
        {
            popupToClose.SetActive(false);
            SetCanvasInteractable(true);
        }
    }

    // (이하 유틸리티 함수는 변경 없음)
    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }

    private string FormatSex(string rawSex)
    {
        if (rawSex == "M") return "남성";
        if (rawSex == "F") return "여성";
        return rawSex;
    }

    private string FormatTel(string rawTel)
    {
        if (string.IsNullOrEmpty(rawTel))
        {
            return string.Empty;
        }

        string telWithZero = "0" + rawTel;

        if (telWithZero.Length == 11)
        {
            try
            {
                return $"{telWithZero.Substring(0, 3)}-{telWithZero.Substring(3, 4)}-{telWithZero.Substring(7, 4)}";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"전화번호 포맷팅 실패 (입력: {rawTel}): {ex.Message}");
                Debug.LogWarning($"전화번호 포맷팅 실패 (입력: {rawTel}): {ex.Message}");
                return telWithZero;
            }
        }

        return telWithZero;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        rentButton.interactable = interactable;
        returnButton.interactable = interactable;
    }

    private void ClearSelection()
    {
        if (selectedRowObject != null)
        {
            Image prevImage = selectedRowObject.transform.Find("panel").GetComponent<Image>();
            if (prevImage != null)
            {
                prevImage.color = originalSelectedRowColor;
            }
        }
        selectedRow = null;
        selectedRowObject = null;
        SetButtonsInteractable(false);
    }
}