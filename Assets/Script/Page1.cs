using UnityEngine;
using UnityEngine.SceneManagement;

public class Page1 : MonoBehaviour
{
    private const string TargetSceneName = "MainScene 1";

    // 버튼 OnClick에 이 메서드 연결
    public void Click()
    {
        SceneManager.LoadScene(TargetSceneName, LoadSceneMode.Single);
    }
}