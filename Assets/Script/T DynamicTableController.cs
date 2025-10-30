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

public class DynamicTableController : MonoBehaviour
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
    public Button modifyButton; // '����' ��ư
    public Button deleteButton; // '����' ��ư

    // �˾� UI ������Ʈ
    [Header("Pop-Up UI")]
    public GameObject popup1_Details; // �� ��Ŭ�� ��
    public GameObject popup2_Modify; // ���� ��ư Ŭ�� ��
    public GameObject popup3_DeleteConfirm; // ���� ��ư Ŭ�� ��

    [Header("���̺� ��Ÿ��")]
    public Color selectedRowColor = new Color(0.5f, 0.8f, 1f); // ���� �� ���� ����

    // --- ������ ���� �� ���� ---
    private List<DataRow> allDataRows = new List<DataRow>(); // DB���� ������ ���� ������
    private DataRow selectedRow = null; // ���� ���õ� ���� ������
    private GameObject selectedRowObject = null; // ���� ���õ� ���� GameObject
    private Color originalSelectedRowColor; // ���õ� ���� ���� (�ٹ���) ����

    // MEMBER ���̺� ������ ���� DataRow Ŭ����
    public class DataRow
    {
        public int mno { get; set; }
        public string name { get; set; }
        public string birth { get; set; }
        public string sex { get; set; } // "����" �Ǵ� "����"�� �����
        public string tel { get; set; } // "010-1234-5678"�� �����
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

        // 3. ����/���� ��ư ������ �߰�
        modifyButton.onClick.AddListener(OnModifyButtonClicked);
        deleteButton.onClick.AddListener(OnDeleteButtonClicked);

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

    // (����) DB���� �����͸� �񵿱�� �������鼭 ������
    private IEnumerator FetchDataFromOracleDB()
    {
        Debug.Log("Oracle DB���� 'MEMBER' ���̺� ���� �ε� ��...");
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

                                    // [!!!] (�ٽ� ����) ������ �Լ��� ���⼭ ����
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
            Debug.LogError($"DB �۾� ����: {errorMessage}");
        }
        else if (loadedData != null)
        {
            allDataRows = loadedData;
            Debug.Log($"�� {allDataRows.Count}���� ������ �ε� �Ϸ�.");
        }
        else
        {
            Debug.LogWarning("DB �۾��� ����������, �ε�� �����Ͱ� �����ϴ�.");
        }
    }

    // (����) TableUI ���¿� �����͸� ä���ִ� �޼ҵ�
    private void PopulateTable(List<DataRow> dataToDisplay)
    {
        ClearSelection();

        // 1. TableUI ������ ��� �� ���� ����
        table.Rows = dataToDisplay.Count + 1;
        table.Columns = 5;

        // 2. Header ���� (Row 0)
        table.GetCell(0, 0).text = "MNO";
        table.GetCell(0, 1).text = "�̸�";
        table.GetCell(0, 2).text = "�������";
        table.GetCell(0, 3).text = "����";
        table.GetCell(0, 4).text = "����ó";

        // 3. ������ ��(Row) ä��� �� Ŭ�� �̺�Ʈ �߰�
        for (int i = 0; i < dataToDisplay.Count; i++)
        {
            DataRow rowData = dataToDisplay[i];
            int tableRowIndex = i + 1;

            // 3-1. ���� ������ �Ҵ�
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
            sexText.text = rowData.sex; // "����" �Ǵ� "����"
            sexText.raycastTarget = false;

            TMP_Text telText = table.GetCell(tableRowIndex, 4);
            telText.text = rowData.tel; // "010-1234-5678"
            telText.raycastTarget = false;

            // 3-2. ��(Row) GameObject�� Ŭ�� �̺�Ʈ(Button) �߰�
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

    private List<DataRow> FilterData(List<DataRow> data, string condition, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return data;

        // [!!!] �˻� ����(condition)�� ���� �˻� �÷� �ٲٴ� ���� �߰� �ʿ�
        return data.FindAll(row =>
            row.name.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0
        );
    }

    // (����) �� Ŭ�� �̺�Ʈ (���� / �˾�1)
    private void OnRowClicked(DataRow clickedData, GameObject rowObject)
    {
        Image panelImage = rowObject.transform.Find("panel").GetComponent<Image>();
        if (panelImage == null) return;

        if (selectedRowObject == rowObject)
        {
            // Popup 1 ����
            Debug.Log($"���õ� ��({selectedRow.name})�� �ٽ� Ŭ���߽��ϴ�. Popup 1�� ���ϴ�.");
            // [!!!] Popup 1�� 'clickedData'�� �� ������ ä���ִ� ���� ����
            popup1_Details.SetActive(true);
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
            Debug.Log($"�� ����! MNO: {selectedRow.mno}, NAME: {selectedRow.name}");
        }
    }

    // (����) �ϴ� ��ư �̺�Ʈ (�˾�2, �˾�3)
    private void OnModifyButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("������ ���� ���� �����ϼ���.");
            return;
        }
        Debug.Log($"[����] ��ư Ŭ��: {selectedRow.name} (MNO: {selectedRow.mno}) -> Popup 2�� ���ϴ�.");
        // [!!!] Popup 2�� InputField�� 'selectedRow' ������ ä���ֱ�
        popup2_Modify.SetActive(true);
    }

    private void OnDeleteButtonClicked()
    {
        if (selectedRow == null)
        {
            Debug.LogWarning("������ ���� ���� �����ϼ���.");
            return;
        }
        Debug.Log($"[����] ��ư Ŭ��: {selectedRow.name} (MNO: {selectedRow.mno}) -> Popup 3�� ���ϴ�.");
        // [!!!] Popup 3�� �ؽ�Ʈ ����
        popup3_DeleteConfirm.SetActive(true);
    }

    // (����) DB ���� ����
    private IEnumerator DeleteMemberFromDB(DataRow rowToDelete)
    {
        // ... (���� ����) ...
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
            Debug.LogError($"DB ���� ����: {errorMessage}");
        }
        else
        {
            Debug.Log($"MNO: {rowToDelete.mno} ���� �Ϸ�.");
            allDataRows.Remove(rowToDelete);
            PopulateTable(allDataRows);
        }
    }

    // (����) �˾� ��ư �̺�Ʈ ó��
    public void ConfirmDelete()
    {
        if (selectedRow != null)
        {
            StartCoroutine(DeleteMemberFromDB(selectedRow));
        }
        else
        {
            Debug.LogError("������ ���� ���õ��� �ʾҴµ� ConfirmDelete�� ȣ��Ǿ����ϴ�.");
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

    //
    // --- (�ű�) ������ ���� �Լ� ---
    //

    /// <summary>
    /// ���� �ڵ�('M', 'F')�� '����', '����'���� ��ȯ�մϴ�.
    /// </summary>
    private string FormatSex(string rawSex)
    {
        if (rawSex == "M") return "����";
        if (rawSex == "F") return "����";
        return rawSex; // �� ���� ��� ���� ��ȯ
    }

    /// <summary>
    /// 10�ڸ� ���� ����ó(��: 1012345678)�� "010-1234-5678" �������� ��ȯ�մϴ�.
    /// </summary>
    private string FormatTel(string rawTel)
    {
        if (string.IsNullOrEmpty(rawTel))
        {
            return string.Empty;
        }

        // 1. "0" �����̱�
        string telWithZero = "0" + rawTel; // ��: "01012345678"

        // 2. 11�ڸ��� ��� (010-1234-5678), 3-4-4 ���� ����
        if (telWithZero.Length == 11)
        {
            try
            {
                // Substring(startIndex, length)
                return $"{telWithZero.Substring(0, 3)}-{telWithZero.Substring(3, 4)}-{telWithZero.Substring(7, 4)}";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"��ȭ��ȣ ������ ���� (�Է�: {rawTel}): {ex.Message}");
                return telWithZero; // ���� ���� �� '0'�� ���� ���� ��ȯ
            }
        }

        // 11�ڸ��� �ƴ� ��� (�����Ͱ� 10�ڸ��� �ƴ� ���)
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