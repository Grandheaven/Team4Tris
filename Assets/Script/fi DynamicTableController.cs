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

public class FFDynamicTableController : MonoBehaviour
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
    public Button rentButton;   // '대여' 버튼 (기존 modifyButton 대체)
    public Button returnButton; // '반납' 버튼 (기존 deleteButton 대체)

    // 팝업 UI 오브젝트 (대여 관리 화면에서는 사용하지 않을 수 있으므로 비활성화 처리 또는 주석 처리)
    [Header("Pop-Up UI")]
    public GameObject popup2_Modify; // 수정 버튼 클릭 시
    public GameObject popup3_DeleteConfirm; // 삭제 버튼 클릭 시

    [Header("테이블 스타일")]
    public Color selectedRowColor = new Color(0.5f, 0.8f, 1f); // 선택 시 강조 색상

    // --- 데이터 저장 및 상태 ---
    // [!!!] DataRow 타입이 RENT 테이블 정보로 변경됨
    private List<DataRow> allDataRows = new List<DataRow>(); // DB에서 가져온 원본 데이터
    private DataRow selectedRow = null; // 현재 선택된 행의 데이터
    private GameObject selectedRowObject = null; // 현재 선택된 행의 GameObject
    private Color originalSelectedRowColor; // 선택된 행의 원래 (줄무늬) 색상

    // [!!!] RENT 테이블 구조에 맞춘 DataRow 클래스 (BOOK, MEMBER 정보 포함)
    public class DataRow
    {
        public int rent_no { get; set; }        // RENT.RNO
        public string book_title { get; set; }  // BOOK.TITLE -> 도서정보 (column0)
        public string book_isbn { get; set; }   // BOOK.ISBN -> ISBN (column1)
        public string member_name { get; set; } // MEMBER.NAME -> 회원정보 (column2)
        public string rent_date { get; set; }   // RENT.RENT_DATE -> 대여일 (column3)
        public string due_date { get; set; }    // RENT.DUE_DATE -> 반납예정일 (column4)
        public string is_returned { get; set; } // RENT.IS_RETURNED -> 반납여부 (column5)
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

        // 3. '대여'/'반납' 버튼 리스너 추가 (이름 변경)
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

    // [!!!] (핵심 수정) DB에서 RENT, BOOK, MEMBER 테이블을 조인하여 데이터 로드
    private IEnumerator FetchDataFromOracleDB()
    {
        Debug.Log("Oracle DB에서 'RENT' 및 관련 테이블 정보 로드 중...");
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
                    // [!!!] RENT, BOOK, MEMBER 테이블을 조인하는 쿼리
                    string sql = @"
                        SELECT 
                            R.RNO, B.TITLE, B.ISBN, M.NAME, R.RENT_DATE, R.DUE_DATE, R.IS_RETURNED
                        FROM 
                            RENT R
                        INNER JOIN BOOK B ON R.ISBN = B.ISBN
                        INNER JOIN MEMBER M ON R.MNO = M.MNO
                        ORDER BY R.RNO DESC";
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DataRow row = new DataRow
                                {
                                    rent_no = reader.GetInt32(reader.GetOrdinal("RNO")),
                                    book_title = ReadString(reader["TITLE"]),
                                    book_isbn = ReadString(reader["ISBN"]),
                                    member_name = ReadString(reader["NAME"]),
                                    // 날짜 포맷팅: yyyy-MM-dd
                                    rent_date = reader.GetDateTime(reader.GetOrdinal("RENT_DATE")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                    due_date = reader.GetDateTime(reader.GetOrdinal("DUE_DATE")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                    // [!!!] 'Y'/'N'을 '반납 완료'/'대여 중' 등으로 포맷팅할 수도 있으나, 여기서는 원본 그대로 사용
                                    is_returned = ReadString(reader["IS_RETURNED"]) // 'Y' 또는 'N'
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
            Debug.Log($"총 {allDataRows.Count}개의 대여 데이터 로드 완료.");
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

        // 1. TableUI 에셋의 행과 열 개수 설정
        // 이미지에 따르면 컬럼은 6개 (도서정보, ISBN, 회원정보, 대여일, 반납예정일, 반납여부)
        table.Rows = dataToDisplay.Count + 1;
        table.Columns = 6;

        // 2. Header 설정 (Row 0) - 이미지에 맞춰 한국어 헤더 설정
        table.GetCell(0, 0).text = "도서정보";
        table.GetCell(0, 1).text = "ISBN";
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
            // 도서정보 (BOOK.TITLE)
            TMP_Text bookTitleText = table.GetCell(tableRowIndex, 0);
            bookTitleText.text = rowData.book_title;
            bookTitleText.raycastTarget = false;

            // ISBN (BOOK.ISBN)
            TMP_Text isbnText = table.GetCell(tableRowIndex, 1);
            isbnText.text = rowData.book_isbn;
            isbnText.raycastTarget = false;

            // 회원정보 (MEMBER.NAME)
            TMP_Text memberNameText = table.GetCell(tableRowIndex, 2);
            memberNameText.text = rowData.member_name;
            memberNameText.raycastTarget = false;

            // 대여일 (RENT.RENT_DATE)
            TMP_Text rentDateText = table.GetCell(tableRowIndex, 3);
            rentDateText.text = rowData.rent_date;
            rentDateText.raycastTarget = false;

            // 반납예정일 (RENT.DUE_DATE)
            TMP_Text dueDateText = table.GetCell(tableRowIndex, 4);
            dueDateText.text = rowData.due_date;
            dueDateText.raycastTarget = false;

            // 반납여부 (RENT.IS_RETURNED)
            TMP_Text isReturnedText = table.GetCell(tableRowIndex, 5);
            // 'Y'/'N'을 '반납 완료'/'대여 중'으로 표시
            isReturnedText.text = rowData.is_returned == "Y" ? "반납 완료" : "대여 중";
            isReturnedText.raycastTarget = false;

            // 3-2. 행(Row) GameObject에 클릭 이벤트(Button) 추가 (기존 로직 유지)
            GameObject rowObject = table.GetCell(tableRowIndex, 0).transform.parent.parent.gameObject;
            rowObject.transform.localScale = Vector3.one;
            rowObject.transform.localPosition = new Vector3(
                rowObject.transform.localPosition.x,
                rowObject.transform.localPosition.y,
                0f
            );

            Button rowButton = rowObject.GetComponent<Button>();
            if (rowButton == null)
                rowButton = rowObject.AddComponent<Button>();

            Image rowImage = rowObject.transform.Find("panel").GetComponent<Image>();
            if (rowImage != null)
            {
                rowButton.targetGraphic = rowImage;
            }

            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(() => OnRowClicked(rowData, rowObject));
        }
    }


    // (유지) 검색 처리
    private void OnSearchEndEdit(string searchText)
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && betaInputField.isFocused)
            return;

        string selectedCondition = conditionDropdown.options[conditionDropdown.value].text;
        string trimmedSearchText = searchText.Trim();
        List<DataRow> filteredList = FilterData(allDataRows, selectedCondition, trimmedSearchText);
        PopulateTable(filteredList);
    }

    // [!!!] (수정) 대여 관리 테이블에 맞는 검색 로직 (조건에 따른 필터링)
    private List<DataRow> FilterData(List<DataRow> data, string condition, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return data;

        // Dropdown 값에 따라 검색 컬럼을 선택하도록 구현 (예시)
        return data.FindAll(row =>
        {
            string columnValue = "";
            switch (condition)
            {
                case "도서정보":
                    columnValue = row.book_title;
                    break;
                case "ISBN":
                    columnValue = row.book_isbn;
                    break;
                case "회원정보":
                    columnValue = row.member_name;
                    break;
                // '대여일', '반납예정일', '반납여부' 등 다른 조건 추가 가능
                default:
                    columnValue = row.book_title; // 기본값
                    break;
            }
            return columnValue.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
        });
    }

    // (유지) 행 클릭 이벤트 (선택 / 팝업1)
    private void OnRowClicked(DataRow clickedData, GameObject rowObject)
    {
        Image panelImage = rowObject.transform.Find("panel").GetComponent<Image>();
        if (panelImage == null) return;

        if (selectedRowObject == rowObject)
        {
        }
        else
        {
            // 다른 행 클릭 (선택 상태 변경)
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
            Debug.Log($"대여 행 선택! RNO: {selectedRow.rent_no}, TITLE: {selectedRow.book_title}");
        }
    }

    // [!!!] (수정) 하단 버튼 이벤트: '대여' 버튼 클릭
    private void OnRentButtonClicked()
    {
        // '대여'는 목록의 선택된 행과는 독립적인 '신규 대여' 기능일 수 있습니다.
        // 현재 로직은 선택된 행이 있는 경우를 가정하고 있지만, 이 화면에서 '대여'는 보통 새 대여 팝업을 엽니다.
        // 여기서는 임시로 '새 대여' 팝업을 엽니다. (기존 '수정' 버튼 위치)
        Debug.Log($"[대여] 버튼 클릭 -> 새 대여 팝업을 엽니다.");
        // [!!!] 새 대여 입력 팝업 로직 구현 (popup2_Modify는 재활용 가능)
        popup2_Modify.SetActive(true);
    }

    // [!!!] (수정) 하단 버튼 이벤트: '반납' 버튼 클릭
    private void OnReturnButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("반납 처리할 대여 행을 먼저 선택하세요.");
            return;
        }

        if (selectedRow.is_returned == "Y")
        {
            Debug.LogWarning("이미 반납 완료된 건입니다.");
            return;
        }

        Debug.Log($"[반납] 버튼 클릭: RNO {selectedRow.rent_no} -> 반납 확인 팝업을 엽니다.");
        // [!!!] Popup 3에 'selectedRow' 정보로 반납 확인 텍스트 설정
        popup3_DeleteConfirm.SetActive(true); // 반납 확인 팝업 사용
    }

    // [!!!] (수정) DB 반납 처리 로직 (기존 DeleteMemberFromDB를 UpdateRentToReturned로 변경)
    private IEnumerator UpdateRentToReturned(DataRow rowToReturn)
    {
        Debug.Log($"RNO: {rowToReturn.rent_no} 반납 처리 중...");
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
                    // [!!!] RENT 테이블의 IS_RETURNED를 'Y'로 업데이트
                    string sql = "UPDATE RENT SET IS_RETURNED = 'Y', RETURN_DATE = SYSDATE WHERE RNO = :rno";
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add("rno", OracleDbType.Int32).Value = rowToReturn.rent_no;
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
            Debug.Log($"RNO: {rowToReturn.rent_no} 반납 처리 완료.");
            // 데이터 새로고침 (반납 여부 '반납 완료'로 변경 확인)
            StartCoroutine(LoadDataAndBuildTable());
        }
    }

    // [!!!] (수정) 팝업 버튼 이벤트 처리 (ConfirmDelete 대신 ConfirmReturn으로 명칭 변경)
    public void ConfirmReturn()
    {
        if (selectedRow != null && selectedRow.is_returned == "N")
        {
            // '반납' 처리 로직 시작
            StartCoroutine(UpdateRentToReturned(selectedRow));
        }
        else if (selectedRow != null && selectedRow.is_returned == "Y")
        {
            Debug.LogWarning("이미 반납된 건이므로 다시 처리하지 않습니다.");
        }
        else
        {
            Debug.LogError("반납할 행이 선택되지 않았는데 ConfirmReturn이 호출되었습니다.");
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

    // --- (유지) 유틸리티 함수 ---
    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }

    // 이전 코드의 FormatSex, FormatTel 함수는 대여 관리 테이블에서 직접 사용하지 않으므로 제거하거나 주석 처리합니다.
    // private string FormatSex(string rawSex) { ... }
    // private string FormatTel(string rawTel) { ... }

    private void SetButtonsInteractable(bool interactable)
    {
        rentButton.interactable = true; // '대여' 버튼은 항상 활성화하는 경우가 많음
        returnButton.interactable = interactable; // '반납' 버튼은 행 선택 시 활성화
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
        SetButtonsInteractable(false); // 선택 해제 시 '반납' 버튼 비활성화
    }
}