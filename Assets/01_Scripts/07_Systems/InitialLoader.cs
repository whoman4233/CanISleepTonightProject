using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InitialLoader : MonoBehaviour
{
    [SerializeField] private string uiSceneName = "04_UIADScene_Ryou 1";

    private void Awake()
    {
        if (GameObject.Find("UIRoot") != null)
        {
            Debug.Log("이미 UIRoot가 존재하므로 로딩을 건너뜁니다.");
            Destroy(gameObject);
            return;
        }
        // UI 씬이 로드되어 있지 않다면 Additive로 로드
        if (!IsSceneLoaded(uiSceneName))
        {
            SceneManager.LoadScene(uiSceneName, LoadSceneMode.Additive);
            Debug.Log($"{uiSceneName} 로드 완료");
        }
        Destroy(gameObject);
    }

    private bool IsSceneLoaded(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == name) return true;
        }
        return false;
    }
}
