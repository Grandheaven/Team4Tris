using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Page0 : MonoBehaviour
{
    //public void GameSceneCtrl()
    //{
    //    SceneManager.LoadScene("User(1)"); //씬 이동
    //    Debug.Log("잘 실행됨");
    //}

    public void Click()
    {
        SceneManager.LoadScene(0);
    }
}
 