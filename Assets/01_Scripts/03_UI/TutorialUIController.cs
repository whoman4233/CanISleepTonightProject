using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image tutorialDisplayImage; // 이미지가 표시될 UI Image
    [SerializeField] private Sprite[] tutorialSprites;   // 튜토리얼 이미지 배열

    [Header("Settings")]
    private int currentIndex = 0;
    private bool isTransitioning = false; // 전환중 변수

    private void Start()
    {
        if (tutorialSprites.Length > 0)
        {
            UpdateUI();
        }
        else
        {
            Debug.LogError("튜토리얼 이미지가 등록되지 않았습니다!");
        }
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (Input.GetKeyDown(KeyCode.Q)) // Q 눌렀을 때 즉시 스킵
        {
            SkipTutorial();
        }
     
        if (Input.GetKeyDown(KeyCode.E)) // E 눌렀을 때 다음 페이지 혹은 시작
        {
            ShowNextPage();
        }
    }

    private void ShowNextPage()
    {
        currentIndex++;

        if (currentIndex < tutorialSprites.Length)
        {
            UpdateUI();
        }
        else
        {
            // 마지막 페이지에서 E를 누르면 게임 시작
            StartGame();
        }
    }

    private void UpdateUI()
    {
        tutorialDisplayImage.sprite = tutorialSprites[currentIndex];
        Debug.Log($"튜토리얼 페이지: {currentIndex + 1} / {tutorialSprites.Length}");
    }

    private void SkipTutorial()
    {
        Debug.Log("튜토리얼 스킵");
        StartGame();
    }

    private void StartGame()
    {
        isTransitioning = true;
        FlowController.Instance.EnterPlayFromTutorial(); // 플레이씬으로 전환
    }
}
