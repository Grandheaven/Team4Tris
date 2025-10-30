using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.TableUI;

public class ffDynamicTableController : MonoBehaviour
{
    // --- DB 접속 정보 (생략) ---
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    // --- UI 요소 연결 (생략) ---
    [Header("테이블 및 검색 UI")]
    public TableUI table;
    public TMP_InputField betaInputField;
    public TMP_Dropdown conditionDropdown;

    [Header("테이블 하단 버튼")]
    public Button rentButton;
    public Button returnButton;

    [Header("Pop-Up UI")]
    public GameObject popup1_Details;
    public GameObject popup2_Modify;
    public GameObject popup3_DeleteConfirm;

    [Header("테이블 스타일")]
    public Color selectedRowColor = new Color(0.5f, 0.8f, 1f);

    // --- 데이터 저장 및 상태 (생략) ---
    private List<DataRow> allDataRows = new List<DataRow>();
    private DataRow selectedRow = null;
    private GameObject selectedRowObject = null;
    private Color originalSelectedRowColor;

    // [!!!] DataRow 클래스: ISBN 대신 BNO를 관계 키로 사용
    public class DataRow
    {
        // BOOK 테이블 정보 (항상 존재)
        public string book_title { get; set; }  // BOOK.TITLE
        public int book_bno { get; set; }       // BOOK.BNO (관계 키로 사용)
        public string book_isbn { get; set; }   // BOOK.ISBN (정보 출력용)

        // RENT/MEMBER 테이블 정보 (LEFT JOIN 결과, 없을 수 있음)
        public int? rent_no { get; set; }        // RENT.RNO
        public string member_name { get; set; } // MEMBER.NAME
        public string rent_date { get; set; }   // RENT.RENT_DATE
        public string due_date { get; set; }    // RENT.DUE_DATE
        public string is_returned { get; set; } // RENT.IS_RETURNED ('N' 또는 '-')
    }

    void Start()
    {
        if (table == null)
        {
            Debug.LogError("TableUI 컴포넌트가 Inspector에 할당되지 않았습니다!");
            return;
        }


        StartCoroutine(LoadDataAndBuildTable());

        // 2. 검색 이벤트 리스너 추가
        betaInputField.onEndEdit.AddListener(OnSearchEndEdit);

        // 3. '대여'/'반납' 버튼 리스너 추가
        rentButton.onClick.AddListener(OnRentButtonClicked);
        returnButton.onClick.AddListener(OnReturnButtonClicked);

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

    // [!!!] (핵심 수정) BNO를 사용하여 LEFT JOIN을 수행합니다.
    private IEnumerator FetchDataFromOracleDB()
    {
        Debug.Log("Oracle DB에서 BOOK을 기준으로 모든 도서 및 대여 정보 로드 중...");
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
                    // [!!!] BNO를 조인 키로 사용하도록 쿼리 수정
                    string sql = @"
                        SELECT 
                            B.TITLE, B.BNO, B.ISBN, R.RNO, M.NAME, R.RENT_DATE, R.DUE_DATE, R.IS_RETURNED
                        FROM 
                            BOOK B
                        LEFT JOIN RENT R 
                            ON B.BNO = R.BNO AND R.IS_RETURNED = 'N' -- RENT 테이블은 BNO를 사용
                        LEFT JOIN MEMBER M 
                            ON R.MNO = M.MNO
                        ORDER BY B.BNO";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // DBNull.Value 처리
                                int? rno = reader.IsDBNull(reader.GetOrdinal("RNO")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("RNO"));
                                string memberName = ReadString(reader["NAME"]);

                                // RENT_DATE, DUE_DATE가 DATE 타입이므로 GetDateTime 사용
                                string rentDate = reader.IsDBNull(reader.GetOrdinal("RENT_DATE")) ? "-" : reader.GetDateTime(reader.GetOrdinal("RENT_DATE")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                                string dueDate = reader.IsDBNull(reader.GetOrdinal("DUE_DATE")) ? "-" : reader.GetDateTime(reader.GetOrdinal("DUE_DATE")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                                string isReturned = ReadString(reader["IS_RETURNED"]);

                                // RENT 정보가 없는 경우 ('-'로 표시)
                                if (rno == null)
                                {
                                    memberName = "-";
                                    rentDate = "-";
                                    dueDate = "-";
                                    isReturned = "-";
                                }

                                DataRow row = new DataRow
                                {
                                    book_title = ReadString(reader["TITLE"]),
                                    book_bno = reader.GetInt32(reader.GetOrdinal("BNO")), // BNO는 BOOK 테이블의 PK이므로 항상 존재
                                    book_isbn = ReadString(reader["ISBN"]), // ISBN도 BOOK 테이블에 존재
                                    rent_no = rno,
                                    member_name = memberName,
                                    rent_date = rentDate,
                                    due_date = dueDate,
                                    is_returned = isReturned
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
            Debug.Log($"총 {allDataRows.Count}개의 도서/대여 데이터 로드 완료.");
        }
        else
        {
            Debug.LogWarning("DB 작업은 성공했으나, 로드된 데이터가 없습니다.");
        }
    }

    // [!!!] (수정) TableUI 에셋에 데이터를 채워넣는 메소드
    private void PopulateTable(List<DataRow> dataToDisplay)
    {
        ClearSelection();

        table.Rows = dataToDisplay.Count + 1;
        table.Columns = 6;


        // 2. Header 설정 (Row 0)
        table.GetCell(0, 0).text = "도서정보";
        table.GetCell(0, 1).text = "ISBN"; // [!!!] Header를 'ISBN'으로만 표시
        table.GetCell(0, 2).text = "회원정보";
        table.GetCell(0, 3).text = "대여일";
        table.GetCell(0, 4).text = "반납예정일";
        table.GetCell(0, 5).text = "반납여부";

        // 3. 데이터 행(Row) 채우기 및 클릭 이벤트 추가
        for (int i = 0; i < dataToDisplay.Count; i++)
        {
            DataRow rowData = dataToDisplay[i];
            int tableRowIndex = i + 1;

            // 3-1. 셀에 데이터 할당
            // 컬럼 0: 도서정보 (TITLE)
            table.GetCell(tableRowIndex, 0).text = rowData.book_title;

            // [!!!] 컬럼 1: ISBN만 출력
            table.GetCell(tableRowIndex, 1).text = rowData.book_isbn;

            // 컬럼 2-5: RENT 정보 (이하 동일)
            table.GetCell(tableRowIndex, 2).text = rowData.member_name;
            table.GetCell(tableRowIndex, 3).text = rowData.rent_date;
            table.GetCell(tableRowIndex, 4).text = rowData.due_date;

            string returnStatus = "";
            if (rowData.is_returned == "N")
            {
                returnStatus = "대여 중";
            }
            else if (rowData.is_returned == "-")
            {
                returnStatus = "대여 가능";
            }
            else
            {
                returnStatus = rowData.is_returned;
            }
            table.GetCell(tableRowIndex, 5).text = returnStatus;


            // 3-2. 행(Row) GameObject에 클릭 이벤트(Button) 추가 (생략)
            GameObject rowObject = table.GetCell(tableRowIndex, 0).transform.parent.parent.gameObject;
            Button rowButton = rowObject.GetComponent<Button>();
            if (rowButton == null)
                rowButton = rowObject.AddComponent<Button>();

            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(() => OnRowClicked(rowData, rowObject));
        }
    }


    // (유지) 검색 처리 (생략)
    private void OnSearchEndEdit(string searchText)
    {
        // Enter 키가 눌리지 않았거나 포커스가 있는 상태에서는 무시
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && betaInputField.isFocused)
            return;

        string trimmedSearchText = searchText.Trim();

        // [!!!] 검색어가 공백(empty or whitespace)일 경우, 필터링 없이 전체 데이터를 출력합니다.
        if (string.IsNullOrEmpty(trimmedSearchText))
        {
            Debug.Log("검색어 공백 감지: 전체 목록 로드");
            SceneManager.LoadScene(5);
            return;
        }

        // 검색어가 있을 경우: 기존 필터링 로직 수행
        string selectedCondition = conditionDropdown.options[conditionDropdown.value].text;
        List<DataRow> filteredList = FilterData(allDataRows, selectedCondition, trimmedSearchText);
        PopulateTable(filteredList);
    }

    // (수정) FilterData: ISBN만 검색
    private List<DataRow> FilterData(List<DataRow> data, string condition, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return data;

        return data.FindAll(row =>
        {
            string columnValue = "";
            switch (condition)
            {
                case "도서":
                    columnValue = row.book_title;
                    break;
                case "회원":
                    columnValue = row.member_name;
                    break;
                default:
                    columnValue = row.book_title;
                    break;
            }
            return columnValue.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
        });
    }

    // (유지) 행 클릭 이벤트 (생략)
    private void OnRowClicked(DataRow clickedData, GameObject rowObject)
    {
        Image panelImage = rowObject.transform.Find("panel").GetComponent<Image>();
        if (panelImage == null) return;

        if (selectedRowObject == rowObject)
        {
            Debug.Log($"선택된 도서 ({selectedRow.book_title})를 다시 클릭했습니다. Popup 1을 엽니다.");
            popup1_Details.SetActive(true);
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
            Debug.Log($"행 선택! BNO: {selectedRow.book_bno}, TITLE: {selectedRow.book_title}");
        }
    }

    // (유지) 하단 버튼 이벤트: '대여' 버튼 클릭 (생략)
    private void OnRentButtonClicked()
    {
        if (selectedRow != null)
        {
            if (selectedRow.is_returned == "N")
            {
                Debug.LogWarning($"선택된 도서({selectedRow.book_title})는 현재 대여 중입니다.");
                return;
            }
            Debug.Log($"[대여] 버튼 클릭 -> 도서: {selectedRow.book_title}의 새 대여 팝업을 엽니다.");
            popup2_Modify.SetActive(true);
        }
        else
        {
            Debug.Log($"[대여] 버튼 클릭 -> 새 대여 팝업을 엽니다.");
            popup2_Modify.SetActive(true);
        }
    }

    // (유지) 하단 버튼 이벤트: '반납' 버튼 클릭 (생략)
    private void OnReturnButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("반납 처리할 도서 행을 먼저 선택하세요.");
            return;
        }

        if (selectedRow.is_returned == "N")
        {
            Debug.Log($"[반납] 버튼 클릭: RNO {selectedRow.rent_no} 도서 {selectedRow.book_title} -> 반납 확인 팝업을 엽니다.");
            popup3_DeleteConfirm.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"선택된 도서({selectedRow.book_title})는 현재 대여 중이 아닙니다.");
            return;
        }
    }

    // (유지) DB 반납 처리 로직 (생략)
    private IEnumerator UpdateRentToReturned(DataRow rowToReturn)
    {
        if (rowToReturn.rent_no == null)
        {
            Debug.LogError("RNO가 NULL이어서 반납 처리를 할 수 없습니다.");
            yield break;
        }

        Debug.Log($"RNO: {rowToReturn.rent_no.Value} 반납 처리 중...");
        bool isError = false;
        string errorMessage = "";
        int rnoValue = rowToReturn.rent_no.Value;

        Task dbTask = Task.Run(() =>
        {
            string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
            try
            {
                using (OracleConnection connection = new OracleConnection(connString))
                {
                    connection.Open();
                    string sql = "UPDATE RENT SET IS_RETURNED = 'Y', RETURN_DATE = SYSDATE WHERE RNO = :rno";
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add("rno", OracleDbType.Int32).Value = rnoValue;
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
            Debug.LogError($"DB 반납 처리 실패: {errorMessage}");
        }
        else
        {
            Debug.Log($"RNO: {rnoValue} 반납 처리 완료.");
            StartCoroutine(LoadDataAndBuildTable());
        }
    }

    // (유지) 팝업 버튼 이벤트 처리 (생략)
    public void ConfirmReturn()
    {
        if (selectedRow != null && selectedRow.is_returned == "N" && selectedRow.rent_no.HasValue)
        {
            StartCoroutine(UpdateRentToReturned(selectedRow));
        }
        else
        {
            Debug.LogError("유효한 대여 건이 선택되지 않았습니다.");
        }
        popup3_DeleteConfirm.SetActive(false);
    }

    public void ClosePopUp(GameObject popupToClose)
    {
        if (popupToClose != null)
        {
            popupToClose.SetActive(false);
        }
    }

    // --- (유지) 유틸리티 함수 (생략) ---
    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        rentButton.interactable = true;
        returnButton.interactable = interactable && (selectedRow != null && selectedRow.is_returned == "N");
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