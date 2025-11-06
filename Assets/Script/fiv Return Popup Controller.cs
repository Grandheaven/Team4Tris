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

public class ReturnPopupController : MonoBehaviour
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
    public Button returnButton; // '반납' 버튼
    public Button closeButton;  // '취소' 버튼

    [Header("팝업 상단 정보")]
    public TextMeshProUGUI nameplateText; // 이름 및 전화번호 표시 (예: 홍길동 010-1234-5678)

    [Header("메시지 팝업")]
    public GameObject popupA_Success; // 반납 성공 시 팝업 (요구사항: 팝업 A)
    public GameObject popupB_Cancel;  // 취소 버튼 클릭 시 팝업 (요구사항: 팝업 B)

    [Header("테이블 스타일")]
    public Color selectedRowColor = new Color(0.5f, 0.8f, 1f);
    public Color returnedTextColor = new Color(100f / 255f, 100f / 255f, 100f / 255f); // #646464 (회색)
    public Color oriTextColor = new Color(0, 0, 0);

    // --- 데이터 저장 및 상태 ---
    private List<RentDataRow> allRentData = new List<RentDataRow>();
    private RentDataRow selectedRentRow = null;
    private GameObject selectedRowObject = null;
    private Color originalSelectedRowColor;

    // 현재 팝업의 검색 기준이 되는 MNO
    private int currentMNO = -1;

    // RENT 및 BOOK 테이블 정보를 조합한 DataRow
    public class RentDataRow
    {
        public int rno { get; set; }        // RENT.RNO (PK)
        public int mno { get; set; }        // RENT.MNO (선택된 회원 MNO)
        public int bno { get; set; }        // RENT.BNO
        public string title { get; set; }   // BOOK.TITLE
        public string isbn { get; set; }    // BOOK.ISBN
        public string rentDate { get; set; }
        public string dueDate { get; set; }
        public string returnDate { get; set; }
        public string isReturned { get; set; } // 'Y' or 'N'
        public bool isSelectable { get; set; } // 반납 완료 여부에 따른 선택 가능 여부
    }

    void Start()
    {
        // 1. 드롭다운 옵션 설정
        SetupDropdown();

        // 2. 검색 이벤트 리스너 추가
        searchInputField.onEndEdit.AddListener(OnSearchEndEdit);

        // 3. 버튼 리스너 추가
        returnButton.onClick.AddListener(OnReturnBookButtonClicked);
        closeButton.onClick.AddListener(OnCloseButtonClicked);

        // 4. 초기 버튼 비활성화
        SetButtonsInteractable(false);
    }

    // DynamicTableController에서 호출되어 MNO를 설정하고 데이터 로드를 시작
    public void Initialize(int mno, string nameTel)
    {
        currentMNO = mno;
        if (nameplateText != null)
        {
            nameplateText.text = nameTel; // 이름 \n 전화번호 표시
        }

        // 팝업이 열릴 때마다 MNO를 기준으로 데이터 로드
        StartCoroutine(LoadRentDataAndBuildTable());
    }

    private void SetupDropdown()
    {
        conditionDropdown.ClearOptions();
        List<string> options = new List<string> { "선택", "제목", "ISBN" };
        conditionDropdown.AddOptions(options);
    }

    // 팝업이 열릴 때 호출되는 데이터 로드 코루틴
    private IEnumerator LoadRentDataAndBuildTable()
    {
        ClearSelection();
        yield return StartCoroutine(FetchRentData(currentMNO));

        // 초기 로드 시 전체 데이터 정렬 후 테이블에 표시
        List<RentDataRow> initialData = new List<RentDataRow>(allRentData);
        initialData.Sort((a, b) =>
        {
            if (a.isReturned == "N" && b.isReturned == "Y") return -1;
            if (a.isReturned == "Y" && b.isReturned == "N") return 1;
            return b.rentDate.CompareTo(a.rentDate); // 같은 상태라면 대여일 내림차순
        });

        PopulateTable(initialData);
    }

    // 🌟 RENT 테이블에서 MNO를 기준으로 데이터 가져오기 (BOOK 정보 조인)
    private IEnumerator FetchRentData(int mno)
    {
        Debug.Log($"MNO {mno} 회원의 대여 기록 로드 중...");
        List<RentDataRow> loadedData = null;
        bool isError = false;
        string errorMessage = "";

        // MNO가 유효하지 않으면 데이터 로드 중지
        if (mno <= 0)
        {
            Debug.LogError("유효하지 않은 MNO입니다. 대여 기록을 로드할 수 없습니다.");
            yield break;
        }

        Task dbTask = Task.Run(() =>
        {
            List<RentDataRow> tempData = new List<RentDataRow>();
            string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";

            try
            {
                using (OracleConnection connection = new OracleConnection(connString))
                {
                    connection.Open();
                    // RENT와 BOOK을 조인하여 제목, ISBN을 가져오고 MNO로 필터링
                    // IS_RETURNED='N' 인 데이터를 먼저 정렬하도록 ORDER BY에 IS_RETURNED ASC 추가
                    string sql = @"
                        SELECT 
                            R.RNO, R.MNO, R.BNO, B.TITLE, B.ISBN, 
                            R.RENT_DATE, R.DUE_DATE, R.RETURN_DATE, R.IS_RETURNED
                        FROM RENT R
                        JOIN BOOK B ON R.BNO = B.BNO
                        WHERE R.MNO = :mno
                        ORDER BY R.IS_RETURNED ASC, R.RENT_DATE DESC";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add("mno", OracleDbType.Int32).Value = mno;

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string isReturned = ReadString(reader["IS_RETURNED"]);
                                RentDataRow row = new RentDataRow
                                {
                                    rno = reader.GetInt32(reader.GetOrdinal("RNO")),
                                    mno = reader.GetInt32(reader.GetOrdinal("MNO")),
                                    bno = reader.GetInt32(reader.GetOrdinal("BNO")),
                                    title = ReadString(reader["TITLE"]),
                                    isbn = ReadString(reader["ISBN"]),
                                    rentDate = reader.GetDateTime(reader.GetOrdinal("RENT_DATE")).ToString("yyyy-MM-dd"),
                                    dueDate = reader.GetDateTime(reader.GetOrdinal("DUE_DATE")).ToString("yyyy-MM-dd"),
                                    returnDate = reader["RETURN_DATE"] == DBNull.Value ? "" : reader.GetDateTime(reader.GetOrdinal("RETURN_DATE")).ToString("yyyy-MM-dd"),
                                    isReturned = isReturned,
                                    isSelectable = (isReturned == "N") // 반납 'N'일 때만 선택 가능
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
            allRentData = loadedData; // 전체 데이터를 저장
            Debug.Log($"총 {allRentData.Count}개의 대여 기록 로드 완료.");
        }
    }

    // 🌟 테이블에 데이터 채우기 (정렬 및 스타일링 적용)
    private void PopulateTable(List<RentDataRow> dataToDisplay)
    {
        ClearSelection();

        // 5개 컬럼: 제목, ISBN, 대여일, 반납예정일, 반납여부
        table.Rows = dataToDisplay.Count + 1;
        table.Columns = 5;

        // Header 설정
        table.GetCell(0, 0).text = "제목";
        table.GetCell(0, 1).text = "ISBN";
        table.GetCell(0, 2).text = "대여일";
        table.GetCell(0, 3).text = "반납예정일";
        table.GetCell(0, 4).text = "반납여부";

        for (int i = 0; i < dataToDisplay.Count; i++)
        {
            RentDataRow rowData = dataToDisplay[i];
            int tableRowIndex = i + 1;
            bool isReturnedY = (rowData.isReturned == "Y");

            // 셀에 데이터 할당
            table.GetCell(tableRowIndex, 0).text = rowData.title;
            table.GetCell(tableRowIndex, 1).text = rowData.isbn;
            table.GetCell(tableRowIndex, 2).text = rowData.rentDate;
            table.GetCell(tableRowIndex, 3).text = rowData.dueDate;

            // 반납 여부 텍스트 설정
            if (isReturnedY)
            {
                table.GetCell(tableRowIndex, 4).text = "반납 완료";
            }
            else
            {
                table.GetCell(tableRowIndex, 4).text = "대여 중";
            }

            // 🌟 행 오브젝트 가져오기 (전체 행의 부모)
            GameObject rowObject = table.GetCell(tableRowIndex, 0).transform.parent.parent.gameObject;

            // **👇 요청하신 Z값과 Scale 값 강제 설정 코드 추가 👇**

            // 1. Z 위치를 -1로 설정 (UI 렌더링 순서 문제 해결 시도)
            rowObject.transform.localPosition = new Vector3(rowObject.transform.localPosition.x, rowObject.transform.localPosition.y, -1f);

            // 2. Scale을 (1, 1, 1)로 설정 (행이 축소되어 보이지 않는 문제 해결 시도)
            rowObject.transform.localScale = Vector3.one;

            // **👆 요청하신 Z값과 Scale 값 강제 설정 코드 추가 👆**

            Button rowButton = rowObject.GetComponent<Button>();
            if (rowButton == null) rowButton = rowObject.AddComponent<Button>();

            // 폰트 색상 및 버튼 활성화 설정
            for (int col = 0; col < table.Columns; col++)
            {
                TMP_Text cellText = table.GetCell(tableRowIndex, col);
                cellText.raycastTarget = false;
                if (isReturnedY)
                {
                    // 반납 완료(Y)인 경우: 글자색을 회색(#646464)으로 설정
                    cellText.color = returnedTextColor;
                }
                else
                {
                    // 반납 미완료(N)인 경우: 기본 텍스트 색상 사용
                    cellText.color = oriTextColor; // 기존 테이블의 기본 텍스트 색상
                }
            }

            // 행 선택 이벤트 추가 (반납 미완료된 행만)
            if (rowData.isSelectable)
            {
                // 선택 가능 (반납 미완료)
                rowButton.onClick.RemoveAllListeners();
                rowButton.onClick.AddListener(() => OnRowClicked(rowData, rowObject));
                rowButton.interactable = true;
            }
            else
            {
                // 선택 불가능 (반납 완료)
                rowButton.onClick.RemoveAllListeners();
                rowButton.interactable = false;
            }
        }
    }

    // 🌟 검색 처리 (MNO 필터링 필수 포함) 및 공란 처리
    private void OnSearchEndEdit(string searchText)
    {
        // Enter 키 입력 시에만 실행되도록 검사 (기존 코드 패턴 유지)
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && searchInputField.isFocused)
            return;

        string selectedCondition = conditionDropdown.options[conditionDropdown.value].text;
        string trimmedSearchText = searchText.Trim();

        List<RentDataRow> dataToDisplay;

        if (string.IsNullOrEmpty(trimmedSearchText))
        {
            // #2-1. 검색어가 공란일 경우: 전체 대여 기록 (allRentData) 표시
            dataToDisplay = allRentData;
        }
        else
        {
            // #2-2. 검색어가 있을 경우: allRentData를 기준으로 검색 (누적 필터링 방지)
            dataToDisplay = FilterData(allRentData, selectedCondition, trimmedSearchText);
        }

        // 반납여부('N' 우선)를 기준으로 다시 정렬 (PopulateTable에서 정렬 및 스타일링 적용)
        dataToDisplay.Sort((a, b) =>
        {
            if (a.isReturned == "N" && b.isReturned == "Y") return -1;
            if (a.isReturned == "Y" && b.isReturned == "N") return 1;
            return b.rentDate.CompareTo(a.rentDate); // 같은 상태라면 대여일 내림차순
        });

        PopulateTable(dataToDisplay);
    }

    // 🌟 검색 조건에 따라 필터링하는 메소드
    private List<RentDataRow> FilterData(List<RentDataRow> data, string condition, string searchText)
    {
        // 검색어가 비어있지 않음이 보장됨 (OnSearchEndEdit에서 처리됨)
        var comparison = System.StringComparison.OrdinalIgnoreCase;

        return data.FindAll(row =>
        {
            switch (condition)
            {
                case "제목":
                    return row.title.IndexOf(searchText, comparison) >= 0;

                case "ISBN":
                    return row.isbn.IndexOf(searchText, comparison) >= 0;

                case "선택":
                default:
                    // '선택'이거나 예상치 못한 값일 경우, 제목, ISBN에서 모두 검색
                    return row.title.IndexOf(searchText, comparison) >= 0 ||
                               row.isbn.IndexOf(searchText, comparison) >= 0;
            }
        });
    }

    // 🌟 행 클릭 이벤트 (선택)
    private void OnRowClicked(RentDataRow clickedData, GameObject rowObject)
    {
        // 반납 완료된 행은 선택 불가능하게 이미 처리됨 (isSelectable = false)
        if (!clickedData.isSelectable) return;

        Image panelImage = rowObject.transform.Find("panel").GetComponent<Image>();
        if (panelImage == null) return;

        // 선택 해제
        if (selectedRowObject == rowObject)
        {
            ClearSelection();
        }
        // 새로운 행 선택
        else
        {
            // 이전에 선택된 행의 색상 원복
            if (selectedRowObject != null)
            {
                Image prevImage = selectedRowObject.transform.Find("panel").GetComponent<Image>();
                if (prevImage != null)
                {
                    prevImage.color = originalSelectedRowColor;
                }
            }

            // 새로운 행 선택 및 하이라이트
            selectedRentRow = clickedData;
            selectedRowObject = rowObject;
            originalSelectedRowColor = panelImage.color;
            panelImage.color = selectedRowColor;

            // 버튼 활성화
            SetButtonsInteractable(true);
            Debug.Log($"대여 기록 행 선택! RNO: {selectedRentRow.rno}, Title: {selectedRentRow.title}");
        }
    }

    // 🌟 '반납' 버튼 클릭 이벤트 (DB 업데이트 및 팝업 A)
    private void OnReturnBookButtonClicked()
    {
        if (selectedRentRow == null || selectedRentRow.isReturned == "Y")
        {
            Debug.LogWarning("반납할 대여 기록을 먼저 선택하세요. (이미 반납된 기록은 선택할 수 없습니다.)");
            return;
        }

        // DB 업데이트 코루틴 시작
        StartCoroutine(UpdateReturnStatus(selectedRentRow));

        // 성공 팝업 A 띄우기 (DB 업데이트 성공 여부와 관계없이 일단 호출, 실제로는 성공 시점에서 띄우는 것이 좋음)
        // 여기서는 비동기 Task 완료 후 띄우도록 코루틴 내에 포함시킵니다.
    }

    // 🌟 DB 업데이트 로직 (RETURN_DATE 삽입, IS_RETURNED 'Y'로 수정)
    private IEnumerator UpdateReturnStatus(RentDataRow rowToUpdate)
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
                    // RETURN_DATE를 현재 SYSDATE로, IS_RETURNED를 'Y'로 업데이트
                    string sql = @"
                        UPDATE RENT 
                        SET RETURN_DATE = SYSDATE, IS_RETURNED = 'Y'
                        WHERE RNO = :rno";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add("rno", OracleDbType.Int32).Value = rowToUpdate.rno;
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
            // 실패 시 처리 (팝업 A 대신 다른 오류 팝업을 띄울 수 있음)
        }
        else
        {
            Debug.Log($"RNO: {rowToUpdate.rno} 반납 처리 완료. 테이블 갱신 시작.");

            // 팝업 A 띄우기
            if (popupA_Success != null) popupA_Success.SetActive(true);

            // 데이터 갱신 및 테이블 재구성
            yield return StartCoroutine(LoadRentDataAndBuildTable());
        }
    }


    // 🌟 '취소' 버튼 클릭 이벤트 (팝업 B)
    private void OnCloseButtonClicked()
    {
        Debug.Log("[취소] 버튼 클릭: Popup B를 띄웁니다.");
        if (popupB_Cancel != null)
        {
            popupB_Cancel.SetActive(true); // 팝업 B 띄우기
        }
        else
        {
            // 팝업 B가 없다면, 팝업 닫기 (요구사항은 팝업 B지만, 혹시 모를 상황 대비)
            ClosePopup();
        }
    }

    // 팝업을 닫고 메인 UI를 재활성화하는 최종 함수
    public void ClosePopup()
    {
        // 이 스크립트가 붙은 팝업 GameObject를 비활성화합니다.
        gameObject.SetActive(false);

        // DynamicTableController에게 메인 UI를 재활성화하라고 알림 (직접 호출)
        DynamicTableController mainController = FindObjectOfType<DynamicTableController>();
        if (mainController != null)
        {
            mainController.ClosePopUp(mainController.gameObject); // mainController의 ClosePopUp을 재사용
        }
    }

    // (이하 유틸리티 함수는 기존 패턴 유지)
    private void SetButtonsInteractable(bool interactable)
    {
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
        selectedRentRow = null;
        selectedRowObject = null;
        SetButtonsInteractable(false);
    }

    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }
}