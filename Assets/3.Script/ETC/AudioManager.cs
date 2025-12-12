using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // 싱글톤 패턴
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;      // BGM 전용
    [SerializeField] private AudioSource sfxSource;      // 효과음 전용
    [SerializeField] private AudioSource loopSfxSource;  // 루프 효과음 전용

    [Header("BGM Clips")]
    [SerializeField] private AudioClip titleBGM;
    [SerializeField] private AudioClip stage1BGM;
    [SerializeField] private AudioClip bossBGM;

    [Header("Kirby SFX")]
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip floatSFX;
    [SerializeField] private AudioClip landSFX;
    [SerializeField] private AudioClip runSFX;
    [SerializeField] private AudioClip inhaleSFX;
    [SerializeField] private AudioClip inhaleSuccessSFX;
    [SerializeField] private AudioClip swallowSFX;
    [SerializeField] private AudioClip spitSFX;
    [SerializeField] private AudioClip damageSFX;
    [SerializeField] private AudioClip tackleSFX;
    public AudioClip beamAttackSFX;
    [SerializeField] private AudioClip burpSFX;
    [SerializeField] private AudioClip skidSFX;
    [SerializeField] private AudioClip explodeSFX;
    [SerializeField] private AudioClip abilityCopySFX;
    public AudioClip sparkAttackSFX;
    [SerializeField] private AudioClip CopyCancelSFX;

    [Header("Enemy SFX")]
    [SerializeField] private AudioClip enemyHitSFX;
    [SerializeField] private AudioClip enemyDefeatSFX;

    [Header("UI SFX")]
    [SerializeField] private AudioClip menuSelectSFX;
    [SerializeField] private AudioClip pauseSFX;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // AudioSource 초기화
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;
        }

        if (loopSfxSource != null)
        {
            loopSfxSource.loop = true;
            loopSfxSource.volume = sfxVolume;
        }
    }

    private void Update()
    {
        // 볼륨 실시간 적용
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    // ==================== BGM 관련 ====================

    /// <summary>
    /// BGM 재생
    /// </summary>
    public void PlayBGM(AudioClip clip, bool fadeIn = false)
    {
        if (bgmSource == null || clip == null) return;

        if (fadeIn)
        {
            StartCoroutine(FadeInBGM(clip, 1f));
        }
        else
        {
            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }

    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM(bool fadeOut = false)
    {
        if (bgmSource == null) return;

        if (fadeOut)
        {
            StartCoroutine(FadeOutBGM(1f));
        }
        else
        {
            bgmSource.Stop();
        }
    }

    /// <summary>
    /// BGM 일시정지
    /// </summary>
    public void PauseBGM()
    {
        if (bgmSource != null) bgmSource.Pause();
    }

    /// <summary>
    /// BGM 재개
    /// </summary>
    public void ResumeBGM()
    {
        if (bgmSource != null) bgmSource.UnPause();
    }

    // ==================== 효과음 관련 ====================

    /// <summary>
    /// 효과음 재생 (기본)
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>
    /// 효과음 재생 (볼륨 조절)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    // ==================== 커비 액션별 효과음 ====================

    public void PlayJumpSFX() => PlaySFX(jumpSFX);
    public void PlayFloatSFX() => PlaySFX(floatSFX);
    public void PlayRunSFX() => PlaySFX(runSFX);
    public void PlayLandSFX() => PlaySFX(landSFX);
    public void PlayInhaleSFX() => PlaySFX(inhaleSFX);
    public void PlayInhaleSuccessSFX() => PlaySFX(inhaleSuccessSFX);
    public void PlaySwallowSFX() => PlaySFX(swallowSFX);
    public void PlaySpitSFX() => PlaySFX(spitSFX);
    public void PlayDamageSFX() => PlaySFX(damageSFX);
    public void PlayTackleSFX() => PlaySFX(tackleSFX);
    public void PlayBeamAttackSFX() => PlaySFX(beamAttackSFX);

    public void PlayEnemyHitSFX() => PlaySFX(enemyHitSFX);
    public void PlayEnemyDefeatSFX() => PlaySFX(enemyDefeatSFX);

    public void PlayMenuSelectSFX() => PlaySFX(menuSelectSFX);
    public void PlayPauseSFX() => PlaySFX(pauseSFX);
    public void PlayBurpSFX() => PlaySFX(burpSFX);
    public void PlaySkidSFX() => PlaySFX(skidSFX);
    public void PlayexplodeSFX() => PlaySFX(explodeSFX);
    public void PlayAbilityCopySFX() => PlaySFX(abilityCopySFX);
    public void PlaySparkAttackSFX() => PlaySFX(sparkAttackSFX);
    public void PlayCopyCancelSFX() => PlaySFX(CopyCancelSFX);

    

    // ==================== 페이드 효과 ====================

    private IEnumerator FadeInBGM(AudioClip clip, float duration)
    {
        bgmSource.clip = clip;
        bgmSource.volume = 0f;
        bgmSource.Play();

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume, timer / duration);
            yield return null;
        }

        bgmSource.volume = bgmVolume;
    }

    private IEnumerator FadeOutBGM(float duration)
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = bgmVolume;
    }

    // ==================== 유틸리티 ====================

    /// <summary>
    /// 모든 소리 정지
    /// </summary>
    public void StopAll()
    {
        if (bgmSource != null) bgmSource.Stop();
        if (sfxSource != null) sfxSource.Stop();
    }

    /// <summary>
    /// 볼륨 설정
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    // ==================== 루프 효과음 관련 ====================

    /// <summary>
    /// 루프 효과음 재생 시작
    /// </summary>
    public void PlayLoopSFX(AudioClip clip)
    {
        if (loopSfxSource == null || clip == null) return;

        loopSfxSource.clip = clip;
        loopSfxSource.Play();
    }

    /// <summary>
    /// 루프 효과음 정지
    /// </summary>
    public void StopLoopSFX()
    {
        if (loopSfxSource == null) return;
        loopSfxSource.Stop();
    }
}