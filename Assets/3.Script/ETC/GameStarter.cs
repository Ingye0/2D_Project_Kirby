using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStarter : MonoBehaviour
{
    [Header("Stage BGM")]
    [SerializeField] private AudioClip stageBGM;

    [Header("Settings")]
    [SerializeField] private bool fadeInBGM = true;
    [SerializeField] private float fadeInDuration = 2f;

    private void Start()
    {
        // 게임 시작 시 BGM 재생
        if (AudioManager.Instance != null && stageBGM != null)
        {
            if (fadeInBGM)
            {
                AudioManager.Instance.PlayBGM(stageBGM, fadeIn: true);
            }
            else
            {
                AudioManager.Instance.PlayBGM(stageBGM, fadeIn: false);
            }

            Debug.Log($"BGM 재생 시작: {stageBGM.name}");
        }
        else
        {
            if (AudioManager.Instance == null)
                Debug.LogError("AudioManager를 찾을 수 없습니다! Hierarchy에 AudioManager가 있는지 확인하세요.");

            if (stageBGM == null)
                Debug.LogWarning("Stage BGM이 할당되지 않았습니다.");
        }
    }

    // 게임 중 BGM 변경 (예: 보스 등장)
    public void ChangeBGM(AudioClip newBGM, bool fadeTransition = true)
    {
        if (AudioManager.Instance == null || newBGM == null) return;

        if (fadeTransition)
        {
            StartCoroutine(CrossFadeBGM_co(newBGM));
        }
        else
        {
            AudioManager.Instance.PlayBGM(newBGM);
        }
    }

    // BGM 크로스페이드
    private System.Collections.IEnumerator CrossFadeBGM_co(AudioClip newBGM)
    {
        // 현재 BGM 페이드 아웃
        AudioManager.Instance.StopBGM(fadeOut: true);

        // 페이드 아웃 시간만큼 대기
        yield return new WaitForSeconds(1f);

        // 새 BGM 페이드 인
        AudioManager.Instance.PlayBGM(newBGM, fadeIn: true);
    }
}
