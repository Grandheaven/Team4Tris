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

    // 팝업 UI 오브젝트
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
        public string sex { get; set; } // "남성" 또는 "여성"이 저장됨
        public string tel { get; set; } // "010-1234-5678"이 저장됨
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

    // (유지) TableUI 에셋에 데이터를 채워넣는 메소드
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
            int tableRowIndex = i + 1;

            // 3-1. 셀에 데이터 할당
            TMP_Text mnoText = table.GetCell(tableRowIndex, 0);
            mnoText.text = rowData.mno.ToString();
            mnoText.raycastTarget = false;

            TMP_Text nameText = table.GetCell(tableRowIndex, 1);
            nameText.text = rowData.name;
            nameText.raycastTarget = false;

            TMP_Text birthText = table.GetCell(tableRowIndex, 2);
            birthText.text = rowData.birth;
            birthText.raycastTarget = false;

            TMP_Text sexText = table.GetCell(tableRowIndex, 3);
            sexText.text = rowData.sex;
            sexText.raycastTarget = false;

            TMP_Text telText = table.GetCell(tableRowIndex, 4);
            telText.text = rowData.tel;
            telText.raycastTarget = false;

            // 3-2. 행(Row) GameObject에 클릭 이벤트(Button) 추가
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

    //
    // --- [!!!] (핵심 수정) 검색 조건에 따라 필터링하는 메소드 ---
    //
    private List<DataRow> FilterData(List<DataRow> data, string condition, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return data; // 검색어가 없으면 전체 리스트 반환

        // 검색 시 대소문자를 무시하기 위한 설정
        var comparison = System.StringComparison.OrdinalIgnoreCase;

        return data.FindAll(row =>
        {
            switch (condition)
            {
                case "이름":
                    // '이름' 필드에서 검색어가 포함되어 있는지 확인
                    return row.name.IndexOf(searchText, comparison) >= 0;

                case "전화번호":
                    // '연락처' 필드에서 검색어가 포함되어 있는지 확인
                    // (예: "1234"로 "010-1234-5678" 검색 가능)
                    return row.tel.IndexOf(searchText, comparison) >= 0;

                case "선택":
                default:
                    // '선택'이거나 예상치 못한 값일 경우, 모든 필드에서 검색
                    return row.name.IndexOf(searchText, comparison) >= 0 ||
                           row.tel.IndexOf(searchText, comparison) >= 0;
            }
        });
    }

    // (유지) 행 클릭 이벤트 (선택 / 팝업1)
    private void OnRowClicked(DataRow clickedData, GameObject rowObject)
    {
        Image panelImage = rowObject.transform.Find("panel").GetComponent<Image>();
        if (panelImage == null) return;

        if (selectedRowObject == rowObject)
        {
            // Popup 1 열기
            Debug.Log($"선택된 행({selectedRow.name})을 다시 클릭했습니다. Popup 1을 엽니다.");
            popup1_Details.SetActive(true);
            Page2_DB.run = true; // Page2_DB 스크립트의 run 플래그 설정
            Page2_DB.datatel = selectedRow.tel; // Page2_DB 스크립트의 datatel 설정
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
            Debug.Log($"행 선택! MNO: {selectedRow.mno}, NAME: {selectedRow.name}");
        }
    }

    // (유지) 하단 버튼 이벤트 (팝업2, 팝업3)
    private void OnModifyButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("수정할 행을 먼저 선택하세요.");
            return;
        }
        Debug.Log($"[수정] 버튼 클릭: {selectedRow.name} (MNO: {selectedRow.mno}) -> Popup 2를 엽니다.");
        popup2_Modify.SetActive(true);
        Page2_DB.run = true; // Page2_DB 스크립트의 run 플래그 설정
        Page2_DB.datatel = selectedRow.tel; // Page2_DB 스크립트의 datatel 설정
    }

    private void OnDeleteButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("삭제할 행을 먼저 선택하세요.");
            return;
        }
        Debug.Log($"[삭제] 버튼 클릭: {selectedRow.name} (MNO: {selectedRow.mno}) -> Popup 3를 엽니다.");
        popup3_DeleteConfirm.SetActive(true);

        Debug.Log(selectedRow.name + "??");
        Debug.Log(selectedRow.birth + "??");
        Debug.Log(selectedRow.sex + "??");
        Debug.Log(selectedRow.tel + "??");

        Page2_DB.run = true;
        Page2_DB.delete = true;
        Page2_DB.dataname = selectedRow.name;
        Page2_DB.databirth = selectedRow.birth;
        Page2_DB.datasex = selectedRow.sex;
        Page2_DB.datatel = selectedRow.tel;
    }

    // (유지) DB 삭제 로직
    private IEnumerator DeleteMemberFromDB(DataRow rowToDelete)
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
            allDataRows.Remove(rowToDelete);
            PopulateTable(allDataRows);
        }
    }

    // (유지) 팝업 버튼 이벤트 처리
    public void ConfirmDelete()
    {
        if (selectedRow != null)
        {
            StartCoroutine(DeleteMemberFromDB(selectedRow));
        }
        else
        {
            Debug.LogError("삭제할 행이 선택되지 않았는데 ConfirmDelete가 호출되었습니다.");
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

    // (유지) 포맷팅 헬퍼 함수
    private string FormatSex(string rawSex)
    {
        if (rawSex == "M") return "남성";
        if (rawSex == "F") return "여성";
        return rawSex; // 그 외의 경우 원본 반환
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
                return telWithZero;
            }
        }

        return telWithZero;
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