using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // TMP_InputField �� TMP_Dropdown ��� ��

public class DynamicTableController : MonoBehaviour
{
    // --- UI ��� ���� ---
    public GameObject dataRowPrefab;        // ���� �� ������ (HeaderRow�� ������ ũ�� �� ���� ����)
    public Transform contentParent;          // ScrollView�� Content ������Ʈ
    public TMP_InputField betaInputField;   // 'Beta' Input Field (�˻��� �Է�)
    public TMP_Dropdown conditionDropdown;   // ��Ӵٿ� (�˻� ���� ����)
    public Color oddRowColor = new Color(0.925f, 0.925f, 0.925f, 1f); // ECECEC
    public Color evenRowColor = Color.white; // FFFFFF

    // --- ������ ���� ---
    private List<DataRow> allDataRows = new List<DataRow>(); // ��ü ���̺� ������ (ALPHA ���̺�)
    private List<DataRow> currentDisplayedRows = new List<DataRow>(); // ���� ǥ�� ���� ������

    // ������ ������ ���� (���� DB ���̺� ������ �°� ���� �ʿ�)
    public class DataRow
    {
        public int id;
        public string column1;
        public string column2;
        // ... �ʿ��� ��� �÷� �߰�
    }

    void Start()
    {
        // 1. �ʱ� ������ �ε� �� ���̺� ����
        StartCoroutine(LoadDataAndBuildTable());

        // 2. 'Beta' Input Field �̺�Ʈ ������ �߰�
        // Enter Ű �Է� �� (EndEdit) �Ǵ� ��Ŀ�� ���� �� �˻� ����
        betaInputField.onEndEdit.AddListener(OnSearchEndEdit);
    }

    // --- ������ �ε� (DB ����) ---
    private IEnumerator LoadDataAndBuildTable()
    {
        // **[����Ŭ DB ���� �� ������ �ε� ���� ����]**
        // �����δ� DB ����, 'ALPHA' ���̺� SELECT ���� ����, ������ �Ľ� �ڵ尡 ���ϴ�.
        // �񵿱�/���� ������ ó���� ����˴ϴ�.
        yield return StartCoroutine(FetchDataFromOracleDB());

        // ������ �ε� �� ���̺� �ʱ�ȭ
        UpdateTable(allDataRows);
    }

    // **������ ������ �ε� �޼ҵ�** (���� DB ���� �ڵ�� ��ü�ؾ� �մϴ�)
    private IEnumerator FetchDataFromOracleDB()
    {
        Debug.Log("Oracle DB���� 'ALPHA' ���̺� ���� �ε� ��...");

        // **[���� DB ���� �ڵ� ����]**
        // ���� ������ ä���:
        for (int i = 0; i < 20; i++)
        {
            allDataRows.Add(new DataRow { id = i + 1, column1 = $"������_{i + 1}", column2 = $"�˻�Ű_{i % 5}" });
        }

        // DB ���� ��� �ð� ����
        yield return new WaitForSeconds(0.5f);

        Debug.Log($"�� {allDataRows.Count}���� ������ �ε� �Ϸ�.");
    }

    // --- �˻� ó�� ---
    private void OnSearchEndEdit(string searchText)
    {
        // 'Beta' Input Field ���� ���� Ŭ�� �ÿ��� OnEndEdit�� �߻��մϴ�.
        // Input Field�� Ȱ��ȭ ����(Enter �Է�)�� ���� ó���ϵ��� isFocused Ȯ�� �ʿ� (������)
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter) && betaInputField.isFocused)
        {
            // Input Field�� ���� ��Ŀ���� ���� �ְ� Enter�� ������ �ʾҴٸ� ����
            return;
        }

        // �˻� ���� �� �˻��� ����
        string selectedCondition = conditionDropdown.options[conditionDropdown.value].text;
        string trimmedSearchText = searchText.Trim();

        Debug.Log($"�˻� ����: {selectedCondition}, �˻���: {trimmedSearchText}");

        // �˻� ���͸�
        List<DataRow> filteredList = FilterData(allDataRows, selectedCondition, trimmedSearchText);

        // ���̺� ������Ʈ
        UpdateTable(filteredList);
    }

    private List<DataRow> FilterData(List<DataRow> data, string condition, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            return data; // �˻�� ������ ��ü ����Ʈ ��ȯ
        }

        // ���� �˻� ���ǿ� ���� ���͸� ���� ���� (��: condition�� ���� �˻� ��� �÷� ����)
        return data.FindAll(row =>
            // ����: column2�� �˻� ������� ����
            row.column2.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0
        // ���⿡ 'condition' ������ ����Ͽ� ���� �÷� �˻� ������ �߰��ؾ� �մϴ�.
        );
    }


    // --- ���̺� ����/���� ---
    private void UpdateTable(List<DataRow> displayData)
    {
        currentDisplayedRows = displayData;

        // 1. ���� ���� �� ��� ����
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            // HeaderRow�� �����, ���� ��(DataRowPrefab�� ������)�� ����
            if (contentParent.GetChild(i).name != "HeaderRow")
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }

        // 2. �� ���� �� ���� �� ������ ���ε�
        for (int i = 0; i < currentDisplayedRows.Count; i++)
        {
            GameObject rowObject = Instantiate(dataRowPrefab, contentParent);
            rowObject.name = $"DataRow_{i + 1}";

            // ������ ���ε� (���� ������Ʈ ���� ���� �ʿ�)
            // ��: rowObject.transform.Find("Column1Text").GetComponent<Text>().text = currentDisplayedRows[i].column1;

            // Ȧ¦ �� ���� ����
            Image rowImage = rowObject.GetComponent<Image>();
            if (rowImage != null)
            {
                rowImage.color = (i % 2 == 0) ? oddRowColor : evenRowColor; // i=0�� ù ��° ���� �� (Ȧ����°)
            }

            // �� Ŭ�� �̺�Ʈ �߰� (��ư ������Ʈ �Ǵ� ������ ��ũ��Ʈ �̿�)
            Button rowButton = rowObject.GetComponent<Button>();
            if (rowButton == null)
            {
                // �����տ� Button ������Ʈ�� ���ٸ� �߰�
                rowButton = rowObject.AddComponent<Button>();
            }

            int rowIndex = i; // Ŭ���� ���� ������ ���� ���� ���� ���
            rowButton.onClick.RemoveAllListeners(); // ���� ������ ����
            rowButton.onClick.AddListener(() => OnRowClicked(currentDisplayedRows[rowIndex]));
        }
    }

    // --- �� Ŭ�� �̺�Ʈ ó�� ---
    private void OnRowClicked(DataRow clickedData)
    {
        Debug.Log($"�� Ŭ��! ID: {clickedData.id}, �÷�1: {clickedData.column1}");
        // ���⿡ Ŭ���� ���� �����͸� Ȱ���� �߰� ������ �����մϴ�.
    }
}