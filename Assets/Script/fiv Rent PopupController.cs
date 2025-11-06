using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Threading.Tasks;
using System.Globalization;
using UnityEngine.UI.TableUI;

public class RentPopupController : MonoBehaviour
{
    // --- DB 접속 정보 (DynamicTableController와 동일) ---
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    // --- UI 요소 연결 ---
    [Header("테이블 및 검색 UI")]
    public TableUI table;
    public TMP_Dropdown conditionDropdown;
    public TMP_InputField searchInputField;

    [Header("팝업 내부 버튼")]
    public Button rentButton;       // '대여' 버튼
    public Button closeButton;      // '취소' 버튼

    [Header("팝업 상단 정보")]
    public TextMeshProUGUI nameplateText; // 회원 이름 및 전화번호 표시

    [Header("메시지 팝업")]
    public GameObject popup_Success; // 대여 성공 시 팝업
    public GameObject popup_Error;   // 대여 불가 시 팝업 (대여 중/DB 에러)
    public GameObject popup_Cancel;  // 취소 버튼 클릭 시 팝업

    [Header("테이블 스타일")]
    public Color selectedRowColor = new Color(0.5f, 0.8f, 1f);

    // --- 데이터 저장 및 상태 ---
    private List<BookDataRow> allBookData = new List<BookDataRow>();
    private BookDataRow selectedBookRow = null;
    private GameObject selectedRowObject = null;
    private Color originalSelectedRowColor;

    // 현재 대여를 시도하는 회원의 MNO
    private int currentMNO = -1;

    public class BookDataRow
    {
        public int bno { get; set; }        // BOOK.BNO (PK)
        public string title { get; set; }   // BOOK.TITLE
        public string author { get; set; }  // BOOK.AUTHOR
        public string publisher { get; set; } // BOOK.PUBLISHER
        public string price { get; set; }   // BOOK.PRICE
        public bool isRented { get; set; }  // True: 대여 중, False: 대여 가능
    }

    void Start()
    {
        SetupDropdown();
        searchInputField.onEndEdit.AddListener(OnSearchEndEdit);
        rentButton.onClick.AddListener(OnRentBookButtonClicked);
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        SetButtonsInteractable(false);
    }

    public void Initialize(int mno, string nameTel)
    {
        currentMNO = mno;
        if (nameplateText != null)
        {
            nameplateText.text = nameTel;
        }

        StartCoroutine(LoadBookDataAndBuildTable());
    }

    public void CloseErrorPopup()
    {
        if (popup_Error != null)
        {
            popup_Error.SetActive(false);
            Debug.Log("Error 팝업이 닫혔습니다.");
        }
    }

    private void SetupDropdown()
    {
        conditionDropdown.ClearOptions();
        List<string> options = new List<string> { "선택", "제목", "저자", "출판사" };
        conditionDropdown.AddOptions(options);
    }

    private IEnumerator LoadBookDataAndBuildTable()
    {
        ClearSelection();
        yield return StartCoroutine(FetchBookData());
        PopulateTable(allBookData);
    }

    private IEnumerator FetchBookData()
    {
        Debug.Log("도서 목록 및 대여 상태 로드 중...");
        List<BookDataRow> loadedData = null;
        bool isError = false;
        string errorMessage = "";

        Task dbTask = Task.Run(() =>
        {
            List<BookDataRow> tempData = new List<BookDataRow>();
            string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";

            try
            {
                using (OracleConnection connection = new OracleConnection(connString))
                {
                    connection.Open();
                    string sql = @"
                        SELECT 
                            B.BNO, B.TITLE, B.AUTHOR, B.PUBLISHER, B.PRICE,
                            CASE 
                                WHEN EXISTS (
                                    SELECT 1 FROM RENT R 
                                    WHERE R.BNO = B.BNO AND R.IS_RETURNED = 'N'
                                ) THEN 'True'
                                ELSE 'False'
                            END AS IS_RENTED
                        FROM BOOK B
                        ORDER BY B.BNO";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                BookDataRow row = new BookDataRow
                                {
                                    bno = reader.GetInt32(reader.GetOrdinal("BNO")),
                                    title = ReadString(reader["TITLE"]),
                                    author = ReadString(reader["AUTHOR"]),
                                    publisher = ReadString(reader["PUBLISHER"]),
                                    price = reader["PRICE"] == DBNull.Value ? "" : reader["PRICE"].ToString(),
                                    isRented = ReadString(reader["IS_RENTED"]) == "True"
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
                errorMessage = ex.Message + "\nStackTrace: " + ex.StackTrace;
            }
        });

        yield return new WaitUntil(() => dbTask.IsCompleted);

        if (isError)
        {
            Debug.LogError($"DB 작업 실패: {errorMessage}");
        }
        else if (loadedData != null)
        {
            allBookData = loadedData;
            Debug.Log($"총 {allBookData.Count}개의 도서 정보 로드 완료.");
        }
    }

    private void PopulateTable(List<BookDataRow> dataToDisplay)
    {
        ClearSelection();

        table.Rows = dataToDisplay.Count + 1;
        table.Columns = 5;

        table.GetCell(0, 0).text = "제목";
        table.GetCell(0, 1).text = "저자";
        table.GetCell(0, 2).text = "출판사";
        table.GetCell(0, 3).text = "가격";
        table.GetCell(0, 4).text = "대여여부";

        for (int i = 0; i < dataToDisplay.Count; i++)
        {
            BookDataRow rowData = dataToDisplay[i];
            int tableRowIndex = i + 1;

            string statusText = rowData.isRented ? "대여 중" : "대여 가능";

            table.GetCell(tableRowIndex, 0).text = rowData.title;
            table.GetCell(tableRowIndex, 1).text = rowData.author;
            table.GetCell(tableRowIndex, 2).text = rowData.publisher;
            table.GetCell(tableRowIndex, 3).text = rowData.price;
            table.GetCell(tableRowIndex, 4).text = statusText;

            GameObject rowObject = table.GetCell(tableRowIndex, 0).transform.parent.parent.gameObject;

            // 🌟🌟🌟 추가된 코드: Transform 설정 🌟🌟🌟
            // Z 값이 -1로 설정되어 가려지는 문제를 해결하기 위해 Z 값을 0으로 설정하거나, 
            // 아예 localPosition을 (X, Y, 0)으로 설정하는 것이 좋습니다.
            // UI 요소이므로 로컬 스케일을 (1, 1, 1)로 설정하여 크기 문제를 방지합니다.
            rowObject.transform.localPosition = new Vector3(rowObject.transform.localPosition.x, rowObject.transform.localPosition.y, 0f);
            rowObject.transform.localScale = Vector3.one;
            // 🌟🌟🌟 추가된 코드 끝 🌟🌟🌟

            Button rowButton = rowObject.GetComponent<Button>();
            if (rowButton == null) rowButton = rowObject.AddComponent<Button>();

            for (int col = 0; col < table.Columns; col++)
            {
                table.GetCell(tableRowIndex, col).raycastTarget = false;
            }

            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(() => OnRowClicked(rowData, rowObject));
            rowButton.interactable = true;
        }
    }

    private void OnSearchEndEdit(string searchText)
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && searchInputField.isFocused)
            return;

        string selectedCondition = conditionDropdown.options[conditionDropdown.value].text;
        string trimmedSearchText = searchText.Trim();

        List<BookDataRow> filteredList = FilterData(allBookData, selectedCondition, trimmedSearchText);
        PopulateTable(filteredList);
    }

    private List<BookDataRow> FilterData(List<BookDataRow> data, string condition, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return data;

        var comparison = System.StringComparison.OrdinalIgnoreCase;

        return data.FindAll(row =>
        {
            switch (condition)
            {
                case "제목":
                    return row.title.IndexOf(searchText, comparison) >= 0;

                case "저자":
                    return row.author.IndexOf(searchText, comparison) >= 0;

                case "출판사":
                    return row.publisher.IndexOf(searchText, comparison) >= 0;

                case "선택":
                default:
                    return row.title.IndexOf(searchText, comparison) >= 0 ||
                           row.author.IndexOf(searchText, comparison) >= 0 ||
                           row.publisher.IndexOf(searchText, comparison) >= 0;
            }
        });
    }

    private void OnRowClicked(BookDataRow clickedData, GameObject rowObject)
    {
        Image panelImage = rowObject.transform.Find("panel").GetComponent<Image>();
        if (panelImage == null) return;

        if (selectedRowObject == rowObject)
        {
            ClearSelection();
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

            selectedBookRow = clickedData;
            selectedRowObject = rowObject;
            originalSelectedRowColor = panelImage.color;
            panelImage.color = selectedRowColor;

            SetButtonsInteractable(true);
            Debug.Log($"도서 선택! BNO: {selectedBookRow.bno}, Title: {selectedBookRow.title}, isRented: {selectedBookRow.isRented}");
        }
    }

    private void OnRentBookButtonClicked()
    {
        if (selectedBookRow == null)
        {
            Debug.LogWarning("대여할 도서를 먼저 선택하세요.");
            return;
        }
        if (currentMNO <= 0)
        {
            Debug.LogError("회원 정보(MNO)가 유효하지 않습니다. 대여를 진행할 수 없습니다.");
            return;
        }

        Debug.Log($"선택 도서: {selectedBookRow.title}, 대여 상태(isRented): {selectedBookRow.isRented}");

        if (selectedBookRow.isRented)
        {
            Debug.Log($"[로직 분기] 대여 중 도서입니다. Error 팝업 표시.");
            if (popup_Error != null) popup_Error.SetActive(true);
        }
        else // isRented == false ('대여 가능')일 경우
        {
            Debug.Log($"[로직 분기] 대여 가능 도서입니다. DB 삽입 시도.");
            StartCoroutine(InsertRentRecord(selectedBookRow.bno, currentMNO));
        }
    }

    // 🌟 (핵심 수정) RNO와 시퀀스를 제거하고 MNO, BNO만 삽입
    private IEnumerator InsertRentRecord(int bno, int mno)
    {
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
                    // 🌟 RNO를 포함한 자동 생성되는 컬럼들을 SQL에서 제거
                    // RENT_DATE, DUE_DATE, IS_RETURNED는 DATA_DEFAULT가 설정되어 있으므로 생략 가능
                    string sql = @"
                        INSERT INTO RENT (MNO, BNO)
                        VALUES (:mno, :bno)";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add("mno", OracleDbType.Int32).Value = mno;
                        command.Parameters.Add("bno", OracleDbType.Int32).Value = bno;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                isError = true;
                errorMessage = ex.Message + "\nStackTrace: " + ex.StackTrace;
            }
        });

        yield return new WaitUntil(() => dbTask.IsCompleted);

        if (isError)
        {
            Debug.LogError($"DB 대여 기록 삽입 실패 (BNO: {bno}, MNO: {mno}): {errorMessage}");
            if (popup_Error != null) popup_Error.SetActive(true);
        }
        else
        {
            Debug.Log($"BNO: {bno} 도서, MNO: {mno} 회원 대여 기록 삽입 성공. RNO는 DB에서 자동 생성됨.");

            if (popup_Success != null) popup_Success.SetActive(true);

            yield return StartCoroutine(LoadBookDataAndBuildTable());
        }
    }

    private void OnCloseButtonClicked()
    {
        Debug.Log("[취소] 버튼 클릭: Popup.cancel을 띄웁니다.");
        if (popup_Cancel != null)
        {
            popup_Cancel.SetActive(true);
        }
        else
        {
            ClosePopup();
        }
    }

    public void ClosePopup()
    {
        gameObject.SetActive(false);

        DynamicTableController mainController = FindObjectOfType<DynamicTableController>();
        if (mainController != null)
        {
            mainController.ClosePopUp(mainController.gameObject);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        rentButton.interactable = interactable;
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
        selectedBookRow = null;
        selectedRowObject = null;
        SetButtonsInteractable(false);
    }

    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }
}