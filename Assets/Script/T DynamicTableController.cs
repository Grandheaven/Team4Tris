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

public class DynamicTableController : MonoBehaviour
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

    [Header("테이블 하단 버튼")]
    public Button modifyButton; // '수정' 버튼
    public Button deleteButton; // '삭제' 버튼

    // (신규) 팝업 UI 오브젝트
    [Header("Pop-Up UI")]
    public GameObject popup1_Details; // 행 재클릭 시
    public GameObject popup2_Modify; // 수정 버튼 클릭 시
    public GameObject popup3_DeleteConfirm; // 삭제 버튼 클릭 시

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

        // 1. 초기 데이터 로드
        StartCoroutine(LoadDataAndBuildTable());

        // 2. 검색 이벤트 리스너 추가
        betaInputField.onEndEdit.AddListener(OnSearchEndEdit);

        // 3. 수정/삭제 버튼 리스너 추가
        modifyButton.onClick.AddListener(OnModifyButtonClicked);
        deleteButton.onClick.AddListener(OnDeleteButtonClicked);

        // 4. 초기에는 버튼 비활성화
        SetButtonsInteractable(false);
    }

    // --- 데이터 로드 (DB 연동) ---
    private IEnumerator LoadDataAndBuildTable()
    {
        ClearSelection();
        yield return StartCoroutine(FetchDataFromOracleDB());
        PopulateTable(allDataRows);
    }

    // (유지) DB에서 데이터를 비동기로 가져오는 메소드
    private IEnumerator FetchDataFromOracleDB()
    {
        // ... (내용 동일) ...
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
                                    sex = ReadString(reader["SEX"]),
                                    tel = ReadString(reader["TEL"])
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

    // --- (유지) TableUI 에셋에 데이터를 채워넣는 메소드 ---
    private void PopulateTable(List<DataRow> dataToDisplay)
    {
        ClearSelection();

        // 1. TableUI 에셋의 행과 열 개수 설정
        table.Rows = dataToDisplay.Count + 1;
        table.Columns = 5;

        // 2. Header 설정 (Row 0)
        table.GetCell(0, 0).text = "MNO";
        table.GetCell(0, 1).text = "이름";
        table.GetCell(0, 2).text = "생년월일";
        table.GetCell(0, 3).text = "성별";
        table.GetCell(0, 4).text = "연락처";

        // 3. 데이터 행(Row) 채우기 및 클릭 이벤트 추가
        for (int i = 0; i < dataToDisplay.Count; i++)
        {
            DataRow rowData = dataToDisplay[i];
            int tableRowIndex = i + 1; // 0번은 헤더이므로 데이터는 1번부터 시작

            // 3-1. 셀에 데이터 할당
            table.GetCell(tableRowIndex, 0).text = rowData.mno.ToString();
            table.GetCell(tableRowIndex, 1).text = rowData.name;
            table.GetCell(tableRowIndex, 2).text = rowData.birth;
            table.GetCell(tableRowIndex, 3).text = rowData.sex;
            table.GetCell(tableRowIndex, 4).text = rowData.tel;

            // 3-2. 행(Row) GameObject에 클릭 이벤트(Button) 추가
            GameObject rowObject = table.GetCell(tableRowIndex, 0).transform.parent.parent.gameObject;
            Button rowButton = rowObject.GetComponent<Button>();
            if (rowButton == null)
                rowButton = rowObject.AddComponent<Button>();

            Image rowImage = rowObject.transform.Find("Borders").GetComponent<Image>();
            if (rowImage != null)
            {
                rowButton.targetGraphic = rowImage;
            }

            // (수정) 클릭 시 OnRowClicked 함수 호출 (기능 변경됨)
            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(() => OnRowClicked(rowData, rowObject));
        }
    }


    // --- (유지) 검색 처리 ---
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
        if (string.IsNullOrEmpty(searchText))
            return data;

        // [!!!] 검색 조건(condition)에 따라 검색 컬럼 바꾸는 로직 추가 필요
        return data.FindAll(row =>
            row.name.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0
        );
    }

    //
    // --- (핵심 수정) 행 클릭 이벤트 (선택 / 팝업1) ---
    //
    private void OnRowClicked(DataRow clickedData, GameObject rowObject)
    {
        Image panelImage = rowObject.transform.Find("Borders").GetComponent<Image>();
        if (panelImage == null) return; // 'panel'을 찾지 못하면 중단

        // (신규) 1. 클릭한 행이 이미 선택된 행인지 확인
        if (selectedRowObject == rowObject)
        {
            // 이미 선택된 행을 다시 클릭함 -> Popup 1 열기
            Debug.Log($"선택된 행({selectedRow.name})을 다시 클릭했습니다. Popup 1을 엽니다.");

            // [!!!] 여기에 Popup 1에 'clickedData'의 상세 정보를 채워넣는 로직을 구현하세요.
            // 예: popup1_Details.GetComponent<Popup1Script>().ShowDetails(clickedData);

            popup1_Details.SetActive(true);
        }
        else
        {
            // (기존) 2. 다른 행을 클릭함 -> 선택 상태 변경

            // 2-1. 이전에 선택된 행이 있다면, 원래 색상으로 복원
            if (selectedRowObject != null)
            {
                Image prevImage = selectedRowObject.transform.Find("Borders").GetComponent<Image>();
                if (prevImage != null)
                {
                    prevImage.color = originalSelectedRowColor; // 저장해둔 원래 색상으로 복원
                }
            }

            // 2-2. 새로 선택된 행을 강조하고 데이터 저장
            selectedRow = clickedData;
            selectedRowObject = rowObject;
            originalSelectedRowColor = panelImage.color; // TableUI가 적용한 줄무늬 색상을 저장
            panelImage.color = selectedRowColor; // 강조 색상 적용

            // 2-3. 수정/삭제 버튼 활성화
            SetButtonsInteractable(true);

            Debug.Log($"행 선택! MNO: {selectedRow.mno}, NAME: {selectedRow.name}");
        }
    }

    // --- (수정) 하단 버튼 이벤트 (팝업2, 팝업3) ---

    private void OnModifyButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("수정할 행을 먼저 선택하세요.");
            // [!!!] (선택사항) "행을 선택하세요" 알림 팝업을 띄울 수 있습니다.
            return;
        }

        Debug.Log($"[수정] 버튼 클릭: {selectedRow.name} (MNO: {selectedRow.mno}) -> Popup 2를 엽니다.");

        // [!!!] 여기에 '수정' 팝업(popup2_Modify)을 띄우기 전,
        // 'selectedRow'의 데이터(mno, name 등)를 팝업창의 InputField에 채워 넣는
        // 로직을 구현해야 합니다.
        // 예: popup2_Modify.GetComponent<ModifyPopupScript>().Initialize(selectedRow);

        popup2_Modify.SetActive(true); // (수정) 팝업2 열기
    }

    private void OnDeleteButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("삭제할 행을 먼저 선택하세요.");
            // [!!!] (선택사항) "행을 선택하세요" 알림 팝업을 띄울 수 있습니다.
            return;
        }

        Debug.Log($"[삭제] 버튼 클릭: {selectedRow.name} (MNO: {selectedRow.mno}) -> Popup 3를 엽니다.");

        // [!!!] 여기에 '삭제 확인' 팝업(popup3_DeleteConfirm)의 텍스트를
        // "정말로 '홍길동' 님을 삭제하시겠습니까?" 등으로 설정할 수 있습니다.
        // 예: popup3_DeleteConfirm.GetComponentInChildren<TMP_Text>().text = $"{selectedRow.name}님을 삭제하시겠습니까?";

        popup3_DeleteConfirm.SetActive(true); // (수정) 팝업3 열기

        // StartCoroutine(DeleteMemberFromDB(selectedRow)); // (이동) 이 코드는 ConfirmDelete()로 이동
    }

    // --- (유지) DB 삭제 로직 ---
    private IEnumerator DeleteMemberFromDB(DataRow rowToDelete)
    {
        // ... (내용 동일) ...
        bool isError = false;
        string errorMessage = "";

        Task dbTask = Task.Run(() =>
        {
            string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
            try
            {
                using (OracleConnection connection = new OracleConnection(connString))
                {
                    connection.Open();
                    string sql = "DELETE FROM MEMBER WHERE MNO = :mno";
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add("mno", OracleDbType.Int32).Value = rowToDelete.mno;
                        command.ExecuteNonQuery();
                    }
                }
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
            Debug.LogError($"DB 삭제 실패: {errorMessage}");
        }
        else
        {
            Debug.Log($"MNO: {rowToDelete.mno} 삭제 완료.");
            // 1. 로컬 마스터 데이터 리스트에서도 삭제
            allDataRows.Remove(rowToDelete);
            // 2. 테이블 전체를 다시 그림
            PopulateTable(allDataRows);
            // (ClearSelection은 PopulateTable 내부에서 호출됨)
        }
    }

    // --- (신규) 9. 팝업 버튼 이벤트 처리 ---

    /// <summary>
    /// 삭제 확인 팝업(Popup 3)에서 '예' 버튼을 눌렀을 때 호출됩니다.
    /// (Inspector의 OnClick() 이벤트에 연결)
    /// </summary>
    public void ConfirmDelete()
    {
        if (selectedRow != null)
        {
            // DB 삭제 코루틴 실행
            StartCoroutine(DeleteMemberFromDB(selectedRow));
        }
        else
        {
            Debug.LogError("삭제할 행이 선택되지 않았는데 ConfirmDelete가 호출되었습니다.");
        }

        // 팝업 닫기
        popup3_DeleteConfirm.SetActive(false);
    }

    /// <summary>
    /// 팝업의 '닫기' 또는 '취소' 버튼에서 호출할 수 있는 범용 닫기 함수입니다.
    /// (Inspector의 OnClick() 이벤트에 연결)
    /// </summary>
    /// <param name="popupToClose">닫을 팝업 GameObject</param>
    public void ClosePopUp(GameObject popupToClose)
    {
        if (popupToClose != null)
        {
            popupToClose.SetActive(false);
        }
    }

    // --- (유지) 유틸리티 함수 ---
    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        modifyButton.interactable = interactable;
        deleteButton.interactable = interactable;
    }

    private void ClearSelection()
    {
        // 선택된 것이 있었다면 색상 복원
        if (selectedRowObject != null)
        {
            Image prevImage = selectedRowObject.transform.Find("Borders").GetComponent<Image>();
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