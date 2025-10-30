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
    // --- DB ���� ���� (����) ---
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    // --- UI ��� ���� (����) ---
    [Header("���̺� �� �˻� UI")]
    public TableUI table;
    public TMP_InputField betaInputField;
    public TMP_Dropdown conditionDropdown;

    [Header("���̺� �ϴ� ��ư")]
    public Button rentButton;
    public Button returnButton;

    [Header("Pop-Up UI")]
    public GameObject popup1_Details;
    public GameObject popup2_Modify;
    public GameObject popup3_DeleteConfirm;

    [Header("���̺� ��Ÿ��")]
    public Color selectedRowColor = new Color(0.5f, 0.8f, 1f);

    // --- ������ ���� �� ���� (����) ---
    private List<DataRow> allDataRows = new List<DataRow>();
    private DataRow selectedRow = null;
    private GameObject selectedRowObject = null;
    private Color originalSelectedRowColor;

    // [!!!] DataRow Ŭ����: ISBN ��� BNO�� ���� Ű�� ���
    public class DataRow
    {
        // BOOK ���̺� ���� (�׻� ����)
        public string book_title { get; set; }  // BOOK.TITLE
        public int book_bno { get; set; }       // BOOK.BNO (���� Ű�� ���)
        public string book_isbn { get; set; }   // BOOK.ISBN (���� ��¿�)

        // RENT/MEMBER ���̺� ���� (LEFT JOIN ���, ���� �� ����)
        public int? rent_no { get; set; }        // RENT.RNO
        public string member_name { get; set; } // MEMBER.NAME
        public string rent_date { get; set; }   // RENT.RENT_DATE
        public string due_date { get; set; }    // RENT.DUE_DATE
        public string is_returned { get; set; } // RENT.IS_RETURNED ('N' �Ǵ� '-')
    }

    void Start()
    {
        if (table == null)
        {
            Debug.LogError("TableUI ������Ʈ�� Inspector�� �Ҵ���� �ʾҽ��ϴ�!");
            return;
        }


        StartCoroutine(LoadDataAndBuildTable());

        // 2. �˻� �̺�Ʈ ������ �߰�
        betaInputField.onEndEdit.AddListener(OnSearchEndEdit);

        // 3. '�뿩'/'�ݳ�' ��ư ������ �߰�
        rentButton.onClick.AddListener(OnRentButtonClicked);
        returnButton.onClick.AddListener(OnReturnButtonClicked);

        // 4. �ʱ⿡�� ��ư ��Ȱ��ȭ
        SetButtonsInteractable(false);
    }

    // --- ������ �ε� (DB ����) ---
    private IEnumerator LoadDataAndBuildTable()
    {
        ClearSelection();
        yield return StartCoroutine(FetchDataFromOracleDB());
        PopulateTable(allDataRows);
    }

    // [!!!] (�ٽ� ����) BNO�� ����Ͽ� LEFT JOIN�� �����մϴ�.
    private IEnumerator FetchDataFromOracleDB()
    {
        Debug.Log("Oracle DB���� BOOK�� �������� ��� ���� �� �뿩 ���� �ε� ��...");
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
                    // [!!!] BNO�� ���� Ű�� ����ϵ��� ���� ����
                    string sql = @"
                        SELECT 
                            B.TITLE, B.BNO, B.ISBN, R.RNO, M.NAME, R.RENT_DATE, R.DUE_DATE, R.IS_RETURNED
                        FROM 
                            BOOK B
                        LEFT JOIN RENT R 
                            ON B.BNO = R.BNO AND R.IS_RETURNED = 'N' -- RENT ���̺��� BNO�� ���
                        LEFT JOIN MEMBER M 
                            ON R.MNO = M.MNO
                        ORDER BY B.BNO";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // DBNull.Value ó��
                                int? rno = reader.IsDBNull(reader.GetOrdinal("RNO")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("RNO"));
                                string memberName = ReadString(reader["NAME"]);

                                // RENT_DATE, DUE_DATE�� DATE Ÿ���̹Ƿ� GetDateTime ���
                                string rentDate = reader.IsDBNull(reader.GetOrdinal("RENT_DATE")) ? "-" : reader.GetDateTime(reader.GetOrdinal("RENT_DATE")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                                string dueDate = reader.IsDBNull(reader.GetOrdinal("DUE_DATE")) ? "-" : reader.GetDateTime(reader.GetOrdinal("DUE_DATE")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                                string isReturned = ReadString(reader["IS_RETURNED"]);

                                // RENT ������ ���� ��� ('-'�� ǥ��)
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
                                    book_bno = reader.GetInt32(reader.GetOrdinal("BNO")), // BNO�� BOOK ���̺��� PK�̹Ƿ� �׻� ����
                                    book_isbn = ReadString(reader["ISBN"]), // ISBN�� BOOK ���̺� ����
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
            Debug.LogError($"DB �۾� ����: {errorMessage}");
        }
        else if (loadedData != null)
        {
            allDataRows = loadedData;
            Debug.Log($"�� {allDataRows.Count}���� ����/�뿩 ������ �ε� �Ϸ�.");
        }
        else
        {
            Debug.LogWarning("DB �۾��� ����������, �ε�� �����Ͱ� �����ϴ�.");
        }
    }

    // [!!!] (����) TableUI ���¿� �����͸� ä���ִ� �޼ҵ�
    private void PopulateTable(List<DataRow> dataToDisplay)
    {
        ClearSelection();

        table.Rows = dataToDisplay.Count + 1;
        table.Columns = 6;


        // 2. Header ���� (Row 0)
        table.GetCell(0, 0).text = "��������";
        table.GetCell(0, 1).text = "ISBN"; // [!!!] Header�� 'ISBN'���θ� ǥ��
        table.GetCell(0, 2).text = "ȸ������";
        table.GetCell(0, 3).text = "�뿩��";
        table.GetCell(0, 4).text = "�ݳ�������";
        table.GetCell(0, 5).text = "�ݳ�����";

        // 3. ������ ��(Row) ä��� �� Ŭ�� �̺�Ʈ �߰�
        for (int i = 0; i < dataToDisplay.Count; i++)
        {
            DataRow rowData = dataToDisplay[i];
            int tableRowIndex = i + 1;

            // 3-1. ���� ������ �Ҵ�
            // �÷� 0: �������� (TITLE)
            table.GetCell(tableRowIndex, 0).text = rowData.book_title;

            // [!!!] �÷� 1: ISBN�� ���
            table.GetCell(tableRowIndex, 1).text = rowData.book_isbn;

            // �÷� 2-5: RENT ���� (���� ����)
            table.GetCell(tableRowIndex, 2).text = rowData.member_name;
            table.GetCell(tableRowIndex, 3).text = rowData.rent_date;
            table.GetCell(tableRowIndex, 4).text = rowData.due_date;

            string returnStatus = "";
            if (rowData.is_returned == "N")
            {
                returnStatus = "�뿩 ��";
            }
            else if (rowData.is_returned == "-")
            {
                returnStatus = "�뿩 ����";
            }
            else
            {
                returnStatus = rowData.is_returned;
            }
            table.GetCell(tableRowIndex, 5).text = returnStatus;


            // 3-2. ��(Row) GameObject�� Ŭ�� �̺�Ʈ(Button) �߰� (����)
            GameObject rowObject = table.GetCell(tableRowIndex, 0).transform.parent.parent.gameObject;
            Button rowButton = rowObject.GetComponent<Button>();
            if (rowButton == null)
                rowButton = rowObject.AddComponent<Button>();

            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(() => OnRowClicked(rowData, rowObject));
        }
    }


    // (����) �˻� ó�� (����)
    private void OnSearchEndEdit(string searchText)
    {
        // Enter Ű�� ������ �ʾҰų� ��Ŀ���� �ִ� ���¿����� ����
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && betaInputField.isFocused)
            return;

        string trimmedSearchText = searchText.Trim();

        // [!!!] �˻�� ����(empty or whitespace)�� ���, ���͸� ���� ��ü �����͸� ����մϴ�.
        if (string.IsNullOrEmpty(trimmedSearchText))
        {
            Debug.Log("�˻��� ���� ����: ��ü ��� �ε�");
            SceneManager.LoadScene(5);
            return;
        }

        // �˻�� ���� ���: ���� ���͸� ���� ����
        string selectedCondition = conditionDropdown.options[conditionDropdown.value].text;
        List<DataRow> filteredList = FilterData(allDataRows, selectedCondition, trimmedSearchText);
        PopulateTable(filteredList);
    }

    // (����) FilterData: ISBN�� �˻�
    private List<DataRow> FilterData(List<DataRow> data, string condition, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return data;

        return data.FindAll(row =>
        {
            string columnValue = "";
            switch (condition)
            {
                case "����":
                    columnValue = row.book_title;
                    break;
                case "ȸ��":
                    columnValue = row.member_name;
                    break;
                default:
                    columnValue = row.book_title;
                    break;
            }
            return columnValue.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
        });
    }

    // (����) �� Ŭ�� �̺�Ʈ (����)
    private void OnRowClicked(DataRow clickedData, GameObject rowObject)
    {
        Image panelImage = rowObject.transform.Find("panel").GetComponent<Image>();
        if (panelImage == null) return;

        if (selectedRowObject == rowObject)
        {
            Debug.Log($"���õ� ���� ({selectedRow.book_title})�� �ٽ� Ŭ���߽��ϴ�. Popup 1�� ���ϴ�.");
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
            Debug.Log($"�� ����! BNO: {selectedRow.book_bno}, TITLE: {selectedRow.book_title}");
        }
    }

    // (����) �ϴ� ��ư �̺�Ʈ: '�뿩' ��ư Ŭ�� (����)
    private void OnRentButtonClicked()
    {
        if (selectedRow != null)
        {
            if (selectedRow.is_returned == "N")
            {
                Debug.LogWarning($"���õ� ����({selectedRow.book_title})�� ���� �뿩 ���Դϴ�.");
                return;
            }
            Debug.Log($"[�뿩] ��ư Ŭ�� -> ����: {selectedRow.book_title}�� �� �뿩 �˾��� ���ϴ�.");
            popup2_Modify.SetActive(true);
        }
        else
        {
            Debug.Log($"[�뿩] ��ư Ŭ�� -> �� �뿩 �˾��� ���ϴ�.");
            popup2_Modify.SetActive(true);
        }
    }

    // (����) �ϴ� ��ư �̺�Ʈ: '�ݳ�' ��ư Ŭ�� (����)
    private void OnReturnButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("�ݳ� ó���� ���� ���� ���� �����ϼ���.");
            return;
        }

        if (selectedRow.is_returned == "N")
        {
            Debug.Log($"[�ݳ�] ��ư Ŭ��: RNO {selectedRow.rent_no} ���� {selectedRow.book_title} -> �ݳ� Ȯ�� �˾��� ���ϴ�.");
            popup3_DeleteConfirm.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"���õ� ����({selectedRow.book_title})�� ���� �뿩 ���� �ƴմϴ�.");
            return;
        }
    }

    // (����) DB �ݳ� ó�� ���� (����)
    private IEnumerator UpdateRentToReturned(DataRow rowToReturn)
    {
        if (rowToReturn.rent_no == null)
        {
            Debug.LogError("RNO�� NULL�̾ �ݳ� ó���� �� �� �����ϴ�.");
            yield break;
        }

        Debug.Log($"RNO: {rowToReturn.rent_no.Value} �ݳ� ó�� ��...");
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
            Debug.LogError($"DB �ݳ� ó�� ����: {errorMessage}");
        }
        else
        {
            Debug.Log($"RNO: {rnoValue} �ݳ� ó�� �Ϸ�.");
            StartCoroutine(LoadDataAndBuildTable());
        }
    }

    // (����) �˾� ��ư �̺�Ʈ ó�� (����)
    public void ConfirmReturn()
    {
        if (selectedRow != null && selectedRow.is_returned == "N" && selectedRow.rent_no.HasValue)
        {
            StartCoroutine(UpdateRentToReturned(selectedRow));
        }
        else
        {
            Debug.LogError("��ȿ�� �뿩 ���� ���õ��� �ʾҽ��ϴ�.");
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

    // --- (����) ��ƿ��Ƽ �Լ� (����) ---
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