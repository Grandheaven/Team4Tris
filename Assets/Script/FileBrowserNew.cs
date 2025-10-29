using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI; // RawImage를 위해 필요
using SimpleFileBrowser;

public class FileBrowserTest : MonoBehaviour
{
    // 1. 파일 이름을 표시할 InputField
    public TMP_InputField fileInputField;

    // 2. 이미지를 표시할 RawImage 개체
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
        // 1단계: 파일 선택 (사용자가 파일을 고를 때까지 대기)
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Load Image File", "Load");

        if (FileBrowser.Success)
        {
            string fullPath = FileBrowser.Result[0];

            // 2단계: 파일 이름 및 내용 읽기 (Oracle 관련 변수 제거)
            string selectedFileName = FileBrowserHelpers.GetFilename(fullPath);
            byte[] selectedFileBytes = FileBrowserHelpers.ReadBytesFromFile(fullPath); // 파일 내용을 바이트 배열로 읽어옴

            // 파일 이름을 InputField에 표시
            if (fileInputField != null)
            {
                fileInputField.text = selectedFileName;
            }

            // 3단계 및 4단계: 이미지 표시
            if (rawImageDisplay != null && selectedFileBytes != null)
            {
                DisplayImageOnRawImage(selectedFileBytes);
            }
        }
    }

    private void DisplayImageOnRawImage(byte[] imageBytes)
    {
        // 3단계: 바이트 배열을 Texture2D로 변환
        Texture2D texture = new Texture2D(2, 2);

        // LoadImage는 이미지 파일 형식(PNG, JPG)의 바이트 배열을 텍스처로 디코딩합니다.
        if (texture.LoadImage(imageBytes))
        {
            rawImageDisplay.texture = texture;

            // -----------------------------------------------------
            // 핵심 수정: '칸'에 맞추면서 비율 유지

            RectTransform rawImageRect = rawImageDisplay.GetComponent<RectTransform>();

            // 1. 이미지의 종횡비(Aspect Ratio) 계산
            float imageAspect = (float)texture.width / texture.height;

            // 2. RawImage (칸)의 종횡비 계산
            float rectAspect = rawImageRect.rect.width / rawImageRect.rect.height;

            // 3. RawImage의 비율(Aspect)과 이미지 비율을 비교하여 이미지의 UV 또는 스케일 조정

            if (imageAspect > rectAspect)
            {
                // 이미지가 RawImage보다 가로로 길 때: 가로에 맞춤
                float scaleY = rectAspect / imageAspect;
                // UV Rect를 사용하여 이미지 비율 조정 (가운데 정렬)
                rawImageDisplay.uvRect = new Rect(0, (1f - scaleY) / 2f, 1f, scaleY);
            }
            else
            {
                // 이미지가 RawImage보다 세로로 길거나 같을 때: 세로에 맞춤
                float scaleX = imageAspect / rectAspect;
                // UV Rect를 사용하여 이미지 비율 조정 (가운데 정렬)
                rawImageDisplay.uvRect = new Rect((1f - scaleX) / 2f, 0, scaleX, 1f);
            }
        }
        else
        {
            Debug.LogError("Failed to load image from bytes. File might be corrupted or not a valid image format.");
        }
    }
}