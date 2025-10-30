using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System; // DBNull, Exception
using System.Data; // CommandType
using Oracle.ManagedDataAccess.Client; // Oracle
using System.Threading.Tasks; // Task.Run (�񵿱� ó��)
using System.Globalization; // CultureInfo (��¥ ������)
using UnityEngine.UI.TableUI; // [!!!] TableUI ������ ���ӽ����̽� �߰�

public class FFDynamicTableController : MonoBehaviour
{
    // --- DB ���� ���� ---
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    // --- UI ��� ���� ---
    [Header("���̺� �� �˻� UI")]
    public TableUI table;
    public TMP_InputField betaInputField;
    public TMP_Dropdown conditionDropdown;

    [Header("���̺� �ϴ� ��ư")]
    public Button rentButton;   // '�뿩' ��ư (���� modifyButton ��ü)
    public Button returnButton; // '�ݳ�' ��ư (���� deleteButton ��ü)

    // �˾� UI ������Ʈ (�뿩 ���� ȭ�鿡���� ������� ���� �� �����Ƿ� ��Ȱ��ȭ ó�� �Ǵ� �ּ� ó��)
    [Header("Pop-Up UI")]
    public GameObject popup2_Modify; // ���� ��ư Ŭ�� ��
    public GameObject popup3_DeleteConfirm; // ���� ��ư Ŭ�� ��

    [Header("���̺� ��Ÿ��")]
    public Color selectedRowColor = new Color(0.5f, 0.8f, 1f); // ���� �� ���� ����

    // --- ������ ���� �� ���� ---
    // [!!!] DataRow Ÿ���� RENT ���̺� ������ �����
    private List<DataRow> allDataRows = new List<DataRow>(); // DB���� ������ ���� ������
    private DataRow selectedRow = null; // ���� ���õ� ���� ������
    private GameObject selectedRowObject = null; // ���� ���õ� ���� GameObject
    private Color originalSelectedRowColor; // ���õ� ���� ���� (�ٹ���) ����

    // [!!!] RENT ���̺� ������ ���� DataRow Ŭ���� (BOOK, MEMBER ���� ����)
    public class DataRow
    {
        public int rent_no { get; set; }        // RENT.RNO
        public string book_title { get; set; }  // BOOK.TITLE -> �������� (column0)
        public string book_isbn { get; set; }   // BOOK.ISBN -> ISBN (column1)
        public string member_name { get; set; } // MEMBER.NAME -> ȸ������ (column2)
        public string rent_date { get; set; }   // RENT.RENT_DATE -> �뿩�� (column3)
        public string due_date { get; set; }    // RENT.DUE_DATE -> �ݳ������� (column4)
        public string is_returned { get; set; } // RENT.IS_RETURNED -> �ݳ����� (column5)
    }

    void Start()
    {
        if (table == null)
        {
            Debug.LogError("TableUI ������Ʈ�� Inspector�� �Ҵ���� �ʾҽ��ϴ�!");
            return;
        }

        // 1. �ʱ� ������ �ε�
        StartCoroutine(LoadDataAndBuildTable());

        // 2. �˻� �̺�Ʈ ������ �߰�
        betaInputField.onEndEdit.AddListener(OnSearchEndEdit);

        // 3. '�뿩'/'�ݳ�' ��ư ������ �߰� (�̸� ����)
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

    // [!!!] (�ٽ� ����) DB���� RENT, BOOK, MEMBER ���̺��� �����Ͽ� ������ �ε�
    private IEnumerator FetchDataFromOracleDB()
    {
        Debug.Log("Oracle DB���� 'RENT' �� ���� ���̺� ���� �ε� ��...");
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
                    // [!!!] RENT, BOOK, MEMBER ���̺��� �����ϴ� ����
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
                                    // ��¥ ������: yyyy-MM-dd
                                    rent_date = reader.GetDateTime(reader.GetOrdinal("RENT_DATE")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                    due_date = reader.GetDateTime(reader.GetOrdinal("DUE_DATE")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                    // [!!!] 'Y'/'N'�� '�ݳ� �Ϸ�'/'�뿩 ��' ������ �������� ���� ������, ���⼭�� ���� �״�� ���
                                    is_returned = ReadString(reader["IS_RETURNED"]) // 'Y' �Ǵ� 'N'
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
            Debug.Log($"�� {allDataRows.Count}���� �뿩 ������ �ε� �Ϸ�.");
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

        // 1. TableUI ������ ��� �� ���� ����
        // �̹����� ������ �÷��� 6�� (��������, ISBN, ȸ������, �뿩��, �ݳ�������, �ݳ�����)
        table.Rows = dataToDisplay.Count + 1;
        table.Columns = 6;

        // 2. Header ���� (Row 0) - �̹����� ���� �ѱ��� ��� ����
        table.GetCell(0, 0).text = "��������";
        table.GetCell(0, 1).text = "ISBN";
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
            // �������� (BOOK.TITLE)
            TMP_Text bookTitleText = table.GetCell(tableRowIndex, 0);
            bookTitleText.text = rowData.book_title;
            bookTitleText.raycastTarget = false;

            // ISBN (BOOK.ISBN)
            TMP_Text isbnText = table.GetCell(tableRowIndex, 1);
            isbnText.text = rowData.book_isbn;
            isbnText.raycastTarget = false;

            // ȸ������ (MEMBER.NAME)
            TMP_Text memberNameText = table.GetCell(tableRowIndex, 2);
            memberNameText.text = rowData.member_name;
            memberNameText.raycastTarget = false;

            // �뿩�� (RENT.RENT_DATE)
            TMP_Text rentDateText = table.GetCell(tableRowIndex, 3);
            rentDateText.text = rowData.rent_date;
            rentDateText.raycastTarget = false;

            // �ݳ������� (RENT.DUE_DATE)
            TMP_Text dueDateText = table.GetCell(tableRowIndex, 4);
            dueDateText.text = rowData.due_date;
            dueDateText.raycastTarget = false;

            // �ݳ����� (RENT.IS_RETURNED)
            TMP_Text isReturnedText = table.GetCell(tableRowIndex, 5);
            // 'Y'/'N'�� '�ݳ� �Ϸ�'/'�뿩 ��'���� ǥ��
            isReturnedText.text = rowData.is_returned == "Y" ? "�ݳ� �Ϸ�" : "�뿩 ��";
            isReturnedText.raycastTarget = false;

            // 3-2. ��(Row) GameObject�� Ŭ�� �̺�Ʈ(Button) �߰� (���� ���� ����)
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


    // (����) �˻� ó��
    private void OnSearchEndEdit(string searchText)
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && betaInputField.isFocused)
            return;

        string selectedCondition = conditionDropdown.options[conditionDropdown.value].text;
        string trimmedSearchText = searchText.Trim();
        List<DataRow> filteredList = FilterData(allDataRows, selectedCondition, trimmedSearchText);
        PopulateTable(filteredList);
    }

    // [!!!] (����) �뿩 ���� ���̺� �´� �˻� ���� (���ǿ� ���� ���͸�)
    private List<DataRow> FilterData(List<DataRow> data, string condition, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return data;

        // Dropdown ���� ���� �˻� �÷��� �����ϵ��� ���� (����)
        return data.FindAll(row =>
        {
            string columnValue = "";
            switch (condition)
            {
                case "��������":
                    columnValue = row.book_title;
                    break;
                case "ISBN":
                    columnValue = row.book_isbn;
                    break;
                case "ȸ������":
                    columnValue = row.member_name;
                    break;
                // '�뿩��', '�ݳ�������', '�ݳ�����' �� �ٸ� ���� �߰� ����
                default:
                    columnValue = row.book_title; // �⺻��
                    break;
            }
            return columnValue.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
        });
    }

    // (����) �� Ŭ�� �̺�Ʈ (���� / �˾�1)
    private void OnRowClicked(DataRow clickedData, GameObject rowObject)
    {
        Image panelImage = rowObject.transform.Find("panel").GetComponent<Image>();
        if (panelImage == null) return;

        if (selectedRowObject == rowObject)
        {
        }
        else
        {
            // �ٸ� �� Ŭ�� (���� ���� ����)
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
            Debug.Log($"�뿩 �� ����! RNO: {selectedRow.rent_no}, TITLE: {selectedRow.book_title}");
        }
    }

    // [!!!] (����) �ϴ� ��ư �̺�Ʈ: '�뿩' ��ư Ŭ��
    private void OnRentButtonClicked()
    {
        // '�뿩'�� ����� ���õ� ����� �������� '�ű� �뿩' ����� �� �ֽ��ϴ�.
        // ���� ������ ���õ� ���� �ִ� ��츦 �����ϰ� ������, �� ȭ�鿡�� '�뿩'�� ���� �� �뿩 �˾��� ���ϴ�.
        // ���⼭�� �ӽ÷� '�� �뿩' �˾��� ���ϴ�. (���� '����' ��ư ��ġ)
        Debug.Log($"[�뿩] ��ư Ŭ�� -> �� �뿩 �˾��� ���ϴ�.");
        // [!!!] �� �뿩 �Է� �˾� ���� ���� (popup2_Modify�� ��Ȱ�� ����)
        popup2_Modify.SetActive(true);
    }

    // [!!!] (����) �ϴ� ��ư �̺�Ʈ: '�ݳ�' ��ư Ŭ��
    private void OnReturnButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("�ݳ� ó���� �뿩 ���� ���� �����ϼ���.");
            return;
        }

        if (selectedRow.is_returned == "Y")
        {
            Debug.LogWarning("�̹� �ݳ� �Ϸ�� ���Դϴ�.");
            return;
        }

        Debug.Log($"[�ݳ�] ��ư Ŭ��: RNO {selectedRow.rent_no} -> �ݳ� Ȯ�� �˾��� ���ϴ�.");
        // [!!!] Popup 3�� 'selectedRow' ������ �ݳ� Ȯ�� �ؽ�Ʈ ����
        popup3_DeleteConfirm.SetActive(true); // �ݳ� Ȯ�� �˾� ���
    }

    // [!!!] (����) DB �ݳ� ó�� ���� (���� DeleteMemberFromDB�� UpdateRentToReturned�� ����)
    private IEnumerator UpdateRentToReturned(DataRow rowToReturn)
    {
        Debug.Log($"RNO: {rowToReturn.rent_no} �ݳ� ó�� ��...");
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
                    // [!!!] RENT ���̺��� IS_RETURNED�� 'Y'�� ������Ʈ
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
            Debug.LogError($"DB �ݳ� ó�� ����: {errorMessage}");
        }
        else
        {
            Debug.Log($"RNO: {rowToReturn.rent_no} �ݳ� ó�� �Ϸ�.");
            // ������ ���ΰ�ħ (�ݳ� ���� '�ݳ� �Ϸ�'�� ���� Ȯ��)
            StartCoroutine(LoadDataAndBuildTable());
        }
    }

    // [!!!] (����) �˾� ��ư �̺�Ʈ ó�� (ConfirmDelete ��� ConfirmReturn���� ��Ī ����)
    public void ConfirmReturn()
    {
        if (selectedRow != null && selectedRow.is_returned == "N")
        {
            // '�ݳ�' ó�� ���� ����
            StartCoroutine(UpdateRentToReturned(selectedRow));
        }
        else if (selectedRow != null && selectedRow.is_returned == "Y")
        {
            Debug.LogWarning("�̹� �ݳ��� ���̹Ƿ� �ٽ� ó������ �ʽ��ϴ�.");
        }
        else
        {
            Debug.LogError("�ݳ��� ���� ���õ��� �ʾҴµ� ConfirmReturn�� ȣ��Ǿ����ϴ�.");
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

    // --- (����) ��ƿ��Ƽ �Լ� ---
    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }

    // ���� �ڵ��� FormatSex, FormatTel �Լ��� �뿩 ���� ���̺��� ���� ������� �����Ƿ� �����ϰų� �ּ� ó���մϴ�.
    // private string FormatSex(string rawSex) { ... }
    // private string FormatTel(string rawTel) { ... }

    private void SetButtonsInteractable(bool interactable)
    {
        rentButton.interactable = true; // '�뿩' ��ư�� �׻� Ȱ��ȭ�ϴ� ��찡 ����
        returnButton.interactable = interactable; // '�ݳ�' ��ư�� �� ���� �� Ȱ��ȭ
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
        SetButtonsInteractable(false); // ���� ���� �� '�ݳ�' ��ư ��Ȱ��ȭ
    }
}