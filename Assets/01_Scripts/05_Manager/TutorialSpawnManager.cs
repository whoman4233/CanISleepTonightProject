using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialSpawnManager : PlayerManager
{
    [SerializeField] private GameObject playerBaton;

    private void Awake()
    {
        _onStepChanged = e =>
        {
            BatonEquipped(e.NewStep);
        };
        SpawnPlayer();
    }
    private void OnEnable()
    {
        EventBus.Subscribe(_onStepChanged);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe(_onStepChanged);
    }
    private Action<DialogueStepChangedEvent> _onStepChanged;
    public override void SpawnPlayer()
    {
        base.SpawnPlayer();
        if (currentPlayer != null)
        {
            StartCoroutine(DeactivateBatonOnStart());
        }
    }

    private IEnumerator DeactivateBatonOnStart()
    {
        yield return null; // 한 프레임 대기 (하이어라키 갱신 완료 시점)

        Transform batonTransform = currentPlayer.transform.FindDeepChild("PW_Baton(Clone)");
        if (batonTransform != null)
        {
            playerBaton = batonTransform.gameObject;
            playerBaton.SetActive(false); // 튜토리얼을 위해 끔
            Debug.Log($"[TutorialSpawnManager] 곤봉 초기화 완료: {playerBaton.name}");
        }
        else
        {
            Debug.LogError("[TutorialSpawnManager] 곤봉을 찾지 못했습니다. 이름을 다시 확인하세요.");
        }
    }
    private void BatonEquipped(DialogueKeys.DialogueType newStep)
    {
        if (newStep == DialogueKeys.DialogueType.BatonEquipped)
        {
            if (playerBaton != null)
            {
                // 1. 자식(곤봉) 활성화
                playerBaton.SetActive(true);

                // 2. [핵심] 부모들을 타고 올라가며 꺼진 부모가 있으면 모두 켬
                Transform parentTr = playerBaton.transform.parent;
                while (parentTr != null)
                {
                    if (!parentTr.gameObject.activeSelf)
                    {
                        Debug.Log($"[TutorialSpawnManager] 꺼져있던 부모 '{parentTr.name}'을(를) 강제로 활성화합니다.");
                        parentTr.gameObject.SetActive(true);
                    }

                    // 최상위 플레이어 오브젝트를 만났다면 중단 (성능 최적화)
                    if (parentTr.CompareTag("Player")) break;

                    parentTr = parentTr.parent;
                }

                // 최종 확인 로그
                Debug.Log($"[TutorialSpawnManager] 최종 상태 - Self: {playerBaton.activeSelf}, Hierarchy: {playerBaton.activeInHierarchy}");
            }
            else
            {
                Debug.LogError("[TutorialSpawnManager] 참조 에러: playerBaton이 할당되지 않았습니다.");
            }
        }
    }
}

