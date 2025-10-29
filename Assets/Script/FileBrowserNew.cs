using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI; // RawImage�� ���� �ʿ�
using SimpleFileBrowser;

public class FileBrowserTest : MonoBehaviour
{
    // 1. ���� �̸��� ǥ���� InputField
    public TMP_InputField fileInputField;

    // 2. �̹����� ǥ���� RawImage ��ü
    public RawImage rawImageDisplay;

    public void ShowFileBrowser()
    {
        // Set filters (optional)
        // It is sufficient to set the filters just once (instead of each time before showing the file browser dialog), 
        // if all the dialogs will be using the same filters
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Files", ".jpg", ".png", ".json"), new FileBrowser.Filter("Text Files", ".txt", ".pdf"));

        // Set default filter that is selected when the dialog is shown (optional)
        // Returns true if the default filter is set successfully
        // In this case, set Images filter as the default filter
        FileBrowser.SetDefaultFilter(".json");

        // Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
        // Note that when you use this function, .lnk and .tmp extensions will no longer be
        // excluded unless you explicitly add them as parameters to the function
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        // Add a new quick link to the browser (optional) (returns true if quick link is added successfully)
        // It is sufficient to add a quick link just once
        // Name: Users
        // Path: C:\Users
        // Icon: default (folder icon)
        FileBrowser.AddQuickLink("Users", "C:\\Users", null);

        // Show a save file dialog 
        // onSuccess event: not registered (which means this dialog is pretty useless)
        // onCancel event: not registered
        // Save file/folder: file, Allow multiple selection: false
        // Initial path: "C:\", Initial filename: "Screenshot.png"
        // Title: "Save As", Submit button text: "Save"
        // FileBrowser.ShowSaveDialog( null, null, FileBrowser.PickMode.Files, false, "C:\\", "Screenshot.png", "Save As", "Save" );

        // Show a select folder dialog 
        // onSuccess event: print the selected folder's path
        // onCancel event: print "Canceled"
        // Load file/folder: folder, Allow multiple selection: false
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Select Folder", Submit button text: "Select"
        // FileBrowser.ShowLoadDialog( ( paths ) => { Debug.Log( "Selected: " + paths[0] ); },
        //						   () => { Debug.Log( "Canceled" ); },
        //						   FileBrowser.PickMode.Folders, false, null, null, "Select Folder", "Select" );

        // Coroutine example
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        // 1�ܰ�: ���� ���� (����ڰ� ������ �� ������ ���)
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Load Image File", "Load");

        if (FileBrowser.Success)
        {
            string fullPath = FileBrowser.Result[0];

            // 2�ܰ�: ���� �̸� �� ���� �б� (Oracle ���� ���� ����)
            string selectedFileName = FileBrowserHelpers.GetFilename(fullPath);
            byte[] selectedFileBytes = FileBrowserHelpers.ReadBytesFromFile(fullPath); // ���� ������ ����Ʈ �迭�� �о��

            // ���� �̸��� InputField�� ǥ��
            if (fileInputField != null)
            {
                fileInputField.text = selectedFileName;
            }

            // 3�ܰ� �� 4�ܰ�: �̹��� ǥ��
            if (rawImageDisplay != null && selectedFileBytes != null)
            {
                DisplayImageOnRawImage(selectedFileBytes);
            }
        }
    }

    private void DisplayImageOnRawImage(byte[] imageBytes)
    {
        // 3�ܰ�: ����Ʈ �迭�� Texture2D�� ��ȯ
        Texture2D texture = new Texture2D(2, 2);

        // LoadImage�� �̹��� ���� ����(PNG, JPG)�� ����Ʈ �迭�� �ؽ�ó�� ���ڵ��մϴ�.
        if (texture.LoadImage(imageBytes))
        {
            rawImageDisplay.texture = texture;

            // -----------------------------------------------------
            // �ٽ� ����: 'ĭ'�� ���߸鼭 ���� ����

            RectTransform rawImageRect = rawImageDisplay.GetComponent<RectTransform>();

            // 1. �̹����� ��Ⱦ��(Aspect Ratio) ���
            float imageAspect = (float)texture.width / texture.height;

            // 2. RawImage (ĭ)�� ��Ⱦ�� ���
            float rectAspect = rawImageRect.rect.width / rawImageRect.rect.height;

            // 3. RawImage�� ����(Aspect)�� �̹��� ������ ���Ͽ� �̹����� UV �Ǵ� ������ ����

            if (imageAspect > rectAspect)
            {
                // �̹����� RawImage���� ���η� �� ��: ���ο� ����
                float scaleY = rectAspect / imageAspect;
                // UV Rect�� ����Ͽ� �̹��� ���� ���� (��� ����)
                rawImageDisplay.uvRect = new Rect(0, (1f - scaleY) / 2f, 1f, scaleY);
            }
            else
            {
                // �̹����� RawImage���� ���η� ��ų� ���� ��: ���ο� ����
                float scaleX = imageAspect / rectAspect;
                // UV Rect�� ����Ͽ� �̹��� ���� ���� (��� ����)
                rawImageDisplay.uvRect = new Rect((1f - scaleX) / 2f, 0, scaleX, 1f);
            }
        }
        else
        {
            Debug.LogError("Failed to load image from bytes. File might be corrupted or not a valid image format.");
        }
    }
}