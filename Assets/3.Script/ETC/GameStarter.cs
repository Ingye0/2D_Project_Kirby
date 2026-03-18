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
        }
    }
}
