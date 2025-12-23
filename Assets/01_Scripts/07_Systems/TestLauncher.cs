using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestLauncher : MonoBehaviour
{
    public void OnClickContinueTest()
    {
        // 1. 데이터 로드 시도 (세이브 파일이 있다면)
        if (GameManager.Instance.LoadPlayerData())
        {
            Debug.Log("데이터 로드 성공! 게임 씬으로 이동합니다.");
        }
        else
        {
            Debug.Log("세이브 파일이 없어 기본 상태로 시작합니다.");
        }

        // 2. 실제 게임 씬으로 이동 (씬 이름이 "PlayScene"이라고 가정)
        SceneManager.LoadScene("05_PlayScene_LSG");
    }
}
