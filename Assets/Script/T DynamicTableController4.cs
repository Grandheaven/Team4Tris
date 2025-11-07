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

public class DynamicTableController4 : MonoBehaviour
{
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    [Header("테이블 및 검색 UI")]
    public TableUI table;
    public TMP_InputField betaInputField;
    public TMP_Dropdown conditionDropdown;

    [Header("테이블 하단 버튼")]
    public Button modifyButton;
    public Button deleteButton;

    [Header("Pop-Up UI")]
    public GameObject popup1_Details;
    public GameObject popup2_Modify;
    public GameObject popup3_DeleteConfirm;

    [Header("테이블 스타일")]
    public Color selectedRowColor = new Color(0.5f, 0.8f, 1f); // 파란색 강조 색상

    private List<DataRow> allDataRows = new List<DataRow>();
    private DataRow selectedRow = null;
    private GameObject selectedRowObject = null;
    private Color originalSelectedRowColor;

    private string rentStatus;

    // BOOK 테이블 구조 매핑 + 대여상태 필드 추가
    public class DataRow
    {
        public int bno { get; set; }
        public string title { get; set; }
        public string author { get; set; }
        public string publisher { get; set; }
        public double price { get; set; }
        public decimal isbn { get; set; }
        public string rent_status { get; set; } // 대여여부 (UI용)
    }

    void Start()
    {
        if (table == null)
        {
            Debug.LogError("TableUI 컴포넌트가 Inspector에 할당되지 않았습니다!");
            return;
        }

        StartCoroutine(LoadDataAndBuildTable());
        betaInputField.onEndEdit.AddListener(OnSearchEndEdit);
        modifyButton.onClick.AddListener(OnModifyButtonClicked);
        deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        SetButtonsInteractable(false);
    }

    private IEnumerator LoadDataAndBuildTable()
    {
        ClearSelection();
        yield return StartCoroutine(FetchDataFromOracleDB());
        yield return StartCoroutine(PopulateTableCoroutine(allDataRows));
    }

    // --- DB에서 BOOK 데이터 불러오기 ---
    private IEnumerator FetchDataFromOracleDB()
    {
        Debug.Log("Oracle DB에서 'BOOK' 테이블 정보 로드 중...");
        List<DataRow> loadedData = null;
        bool isError = false;
        string errorMessage = "";

        Task dbTask = Task.Run(() =>
        {
            List<DataRow> tempData = new List<DataRow>();
            string connString =
                $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";

            try
            {
                using (OracleConnection connection = new OracleConnection(connString))
                {
                    connection.Open();
                    string sql = "SELECT BNO, TITLE, AUTHOR, PUBLISHER, PRICE, ISBN FROM BOOK ORDER BY BNO";
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    using (OracleDataReader reader = command.ExecuteReader())
                    {
                        string sqlrent = "SELECT B.BNO, CASE WHEN RENT_STATUS.IS_RETURNED IS NULL THEN 'Y' ELSE RENT_STATUS.IS_RETURNED END AS IS_RETURNED FROM BOOK B LEFT JOIN(SELECT BNO, MIN(IS_RETURNED) AS IS_RETURNED FROM RENT GROUP BY BNO) RENT_STATUS ON B.BNO = RENT_STATUS.BNO ORDER BY B.BNO";
                        OracleCommand commandrent = new OracleCommand(sqlrent, connection);
                        OracleDataReader readerrent = commandrent.ExecuteReader();
                        
                        while (reader.Read())
                        {
                            if (readerrent.Read())
                            {
                                rentStatus = ReadString(readerrent["IS_RETURNED"]); 
                            }

                            DataRow row = new DataRow
                            {
                                bno = reader.GetInt32(reader.GetOrdinal("BNO")),
                                title = ReadString(reader["TITLE"]),
                                author = ReadString(reader["AUTHOR"]),
                                publisher = ReadString(reader["PUBLISHER"]),
                                price = reader.IsDBNull(reader.GetOrdinal("PRICE")) ? 0 : reader.GetDouble(reader.GetOrdinal("PRICE")),
                                isbn = reader.GetDecimal(reader.GetOrdinal("ISBN")),
                                rent_status = rentStatus == "Y" ? "대여 가능" : "대여중" // 랜덤 표시
                            };
                            tempData.Add(row);
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
            Debug.Log($"총 {allDataRows.Count}개의 BOOK 데이터 로드 완료.");
        }
        else
        {
            Debug.LogWarning("DB 작업은 성공했으나, 로드된 데이터가 없습니다.");
        }
    }

    // ✅ TableUI 초기화 및 한 프레임 지연 포함 버전
    private IEnumerator PopulateTableCoroutine(List<DataRow> dataToDisplay)
    {
        ClearSelection();

        // --- TableUI 초기화 ---
        table.Rows = dataToDisplay.Count + 1;
        table.Columns = 5; // TITLE, AUTHOR, PUBLISHER, PRICE, RENT_STATUS

        try
        {
            table.Initialize();
        }
        catch (NotImplementedException)
        {
            Debug.LogWarning("[TableUI] Initialize() 미구현 버전입니다. 수동 초기화 필요 시 TableUI 업데이트 권장.");
        }

        yield return null; // 내부 셀 생성 대기

        // --- Header 설정 ---
        table.GetCell(0, 0).text = "제목";
        table.GetCell(0, 1).text = "저자";
        table.GetCell(0, 2).text = "출판사";
        table.GetCell(0, 3).text = "가격";
        table.GetCell(0, 4).text = "대여여부";

        // --- 데이터 행 ---
        for (int i = 0; i < dataToDisplay.Count; i++)
        {
            DataRow rowData = dataToDisplay[i];
            int rowIndex = i + 1;

            table.GetCell(rowIndex, 0).text = rowData.title;
            table.GetCell(rowIndex, 1).text = rowData.author;
            table.GetCell(rowIndex, 2).text = rowData.publisher;
            table.GetCell(rowIndex, 3).text = $"{rowData.price:N0}원"; // 가격에 원 단위 표시
            table.GetCell(rowIndex, 4).text = rowData.rent_status;

            // --- 클릭 이벤트 ---
            GameObject rowObject = table.GetCell(rowIndex, 0).transform.parent.parent.gameObject;
            Button rowButton = rowObject.GetComponent<Button>() ?? rowObject.AddComponent<Button>();
            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(() => OnRowClicked(rowData, rowObject));
        }

        Debug.Log($"PopulateTable 완료: {dataToDisplay.Count}개의 도서 표시됨.");
    }

    // --- 검색 ---
    private void OnSearchEndEdit(string searchText)
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && betaInputField.isFocused)
            return;

        string selectedCondition = conditionDropdown.options[conditionDropdown.value].text;
        string trimmed = searchText.Trim();
        List<DataRow> filtered = FilterData(allDataRows, selectedCondition, trimmed);
        StartCoroutine(PopulateTableCoroutine(filtered));
    }

    private List<DataRow> FilterData(List<DataRow> data, string condition, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return data;

        return data.FindAll(row =>
            row.title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
        );
    }

    // --- ✅ 행 클릭 시 행 전체 색상 변경 기능 포함 ---
    private void OnRowClicked(DataRow clickedData, GameObject rowObject)
    {
        // 같은 행을 다시 클릭하면 상세 팝업 열기
        if (selectedRowObject == rowObject)
        {
            popup1_Details.SetActive(true);
            Page4_DB.run = true;
            Page4_DB.dataisbn = selectedRow.isbn.ToString();
            return;
        }

        // 이전 행 색상 복원
        if (selectedRowObject != null)
        {
            foreach (Transform cell in selectedRowObject.transform)
            {
                Image cellImage = cell.GetComponent<Image>();
                if (cellImage != null)
                    cellImage.color = originalSelectedRowColor;
            }
        }

        // 현재 선택 행 저장
        selectedRow = clickedData;
        selectedRowObject = rowObject;

        // 첫 번째 셀 색상을 기준으로 원래 색상 저장
        Transform firstCell = rowObject.transform.GetChild(0);
        Image firstCellImage = firstCell.GetComponent<Image>();
        if (firstCellImage != null)
            originalSelectedRowColor = firstCellImage.color;

        // 행 전체 색상 변경
        foreach (Transform cell in rowObject.transform)
        {
            Image cellImage = cell.GetComponent<Image>();
            if (cellImage != null)
                cellImage.color = selectedRowColor;
        }

        // 버튼 활성화
        SetButtonsInteractable(true);
        Debug.Log($"선택된 도서: {selectedRow.title} / {selectedRow.rent_status}");
    }

    private void OnModifyButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("수정할 책을 먼저 선택하세요.");
            return;
        }

        popup2_Modify.SetActive(true);
        Page4_DB.run = true;
        Page4_DB.dataisbn = selectedRow.isbn.ToString();
    }

    private void OnDeleteButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("삭제할 책을 먼저 선택하세요.");
            return;
        }

        Page4_DB.delete = true;
        Page4_DB.run = true;

        Page4_DB.dataisbn = selectedRow.isbn.ToString();

        Page4_DB.datatitle = selectedRow.title;
        Page4_DB.dataauthor = selectedRow.author;
        Page4_DB.datapublisher = selectedRow.publisher;
        Page4_DB.dataprice = selectedRow.price.ToString();
    }

    private IEnumerator DeleteBookFromDB(DataRow rowToDelete)
    {
        bool isError = false;
        string errorMessage = "";

        Task dbTask = Task.Run(() =>
        {
            string connString =
                $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";

            try
            {
                using (OracleConnection connection = new OracleConnection(connString))
                {
                    connection.Open();
                    string sql = "DELETE FROM BOOK WHERE BNO = :bno";
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add("bno", OracleDbType.Int32).Value = rowToDelete.bno;
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
            Debug.LogError($"DB 삭제 실패: {errorMessage}");
        else
        {
            allDataRows.Remove(rowToDelete);
            yield return StartCoroutine(PopulateTableCoroutine(allDataRows));
        }
    }

    public void ConfirmDelete()
    {
        if (selectedRow != null)
            StartCoroutine(DeleteBookFromDB(selectedRow));

        popup3_DeleteConfirm.SetActive(false);
    }

    public void ClosePopUp(GameObject popupToClose)
    {
        if (popupToClose != null)
            popupToClose.SetActive(false);
    }

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
        if (selectedRowObject != null)
        {
            foreach (Transform cell in selectedRowObject.transform)
            {
                Image cellImage = cell.GetComponent<Image>();
                if (cellImage != null)
                    cellImage.color = originalSelectedRowColor;
            }
        }

        selectedRow = null;
        selectedRowObject = null;
        SetButtonsInteractable(false);
    }
}