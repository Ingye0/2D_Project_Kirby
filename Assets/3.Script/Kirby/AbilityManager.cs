using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    private Normal normalAbility;
    private Beam beamAbility;
    private Spark sparkAbility;

    private Animator anim;
    private Kirby_Controller controller;

    [Header("애니메이터 컨트롤러")]
    public RuntimeAnimatorController normalAnimator;
    public RuntimeAnimatorController beamAnimator;
    public RuntimeAnimatorController sparkAnimator;


    private void Awake()
    {
        // 컴포넌트 참조
        normalAbility = GetComponent<Normal>();
        beamAbility = GetComponent<Beam>();
        sparkAbility = GetComponent<Spark>();
        anim = GetComponent<Animator>();
        controller = GetComponent<Kirby_Controller>();

        // 시작 시 Normal만 활성화
        if (normalAbility != null)
            normalAbility.enabled = true;

        if (beamAbility != null)
            beamAbility.enabled = false;

        if (sparkAbility != null)
            sparkAbility.enabled = false;

        Debug.Log("AbilityManager 초기화 완료");
    }

    /// <summary>
    /// 능력을 복사합니다. Normal.SwallowEnemy()에서 호출됩니다.
    /// </summary>
    public void CopyAbility(AbilityType type)
    {
        Debug.Log($"CopyAbility 호출됨: {type}");

        switch (type)
        {
            case AbilityType.Beam:
                SwitchToBeam();
                break;

            case AbilityType.Spark:
                SwitchToSpark();
                break;

            case AbilityType.Normal:
            default:
                ResetToNormal();
                break;
        }
    }

    /// <summary>
    /// Normal 능력으로 돌아갑니다. (피격 시 또는 능력 상실 시 호출)
    /// </summary>
    public void ResetToNormal()
    {
        // 1. 모든 능력 스크립트 비활성화 또는 제거
        if (beamAbility != null)
            beamAbility.enabled = false;

        // ★ Spark는 동적으로 추가되므로 제거
        Spark spark = GetComponent<Spark>();
        if (spark != null)
        {
            spark.DeactivateAbility(); // 정리 후 Destroy
        }

        // ★ Kirby_Controller의 spark 참조 null로 설정
        if (controller != null)
        {
            controller.spark = null;
        }

        // 2. Normal 활성화
        if (normalAbility != null)
            normalAbility.enabled = true;

        // ★ Kirby_Controller의 normal 참조 업데이트
        if (controller != null)
        {
            controller.normal = normalAbility;
        }

        // 3. 애니메이터 전환
        if (anim != null && normalAnimator != null)
        {
            anim.runtimeAnimatorController = normalAnimator;
        }
        else if (normalAnimator == null)
        {
            Debug.LogWarning("normalAnimator가 Inspector에 할당되지 않았습니다!");
        }

        Debug.Log("Normal 능력으로 전환됨");
    }

    /// <summary>
    /// Beam 능력으로 전환합니다.
    /// </summary>
    private void SwitchToBeam()
    {
        // Normal 비활성화
        if (normalAbility != null)
            normalAbility.enabled = false;

        // Spark 제거 (혹시 있다면)
        Spark spark = GetComponent<Spark>();
        if (spark != null)
        {
            spark.DeactivateAbility();
        }

        // ★ Kirby_Controller의 spark 참조 null로 설정
        if (controller != null)
        {
            controller.spark = null;
        }

        // Beam 활성화
        if (beamAbility != null)
        {
            beamAbility.enabled = true;
        }
        else
        {
            Debug.LogError("Beam 컴포넌트가 없습니다!");
            ResetToNormal();
            return;
        }

        // ★ Kirby_Controller의 beam 참조 업데이트
        if (controller != null)
        {
            controller.beam = beamAbility;
        }

        // 애니메이터 전환
        if (anim != null && beamAnimator != null)
        {
            anim.runtimeAnimatorController = beamAnimator;
        }
        else if (beamAnimator == null)
        {
            Debug.LogWarning("beamAnimator가 Inspector에 할당되지 않았습니다!");
        }

        Debug.Log("Beam 능력으로 전환됨! Z키로 빔 공격을 사용하세요.");
    }

    private void SwitchToSpark()
    {
        // Normal 비활성화
        if (normalAbility != null)
            normalAbility.enabled = false;

        // Beam 비활성화
        if (beamAbility != null)
            beamAbility.enabled = false;

        // Spark 활성화
        if (sparkAbility == null)
            sparkAbility = GetComponent<Spark>();

        if (sparkAbility == null)
        {
            Debug.LogError("Spark 컴포넌트를 찾을 수 없습니다!");
            ResetToNormal();
            return;
        }

        sparkAbility.enabled = true;

        // Kirby_Controller 업데이트
        if (controller != null)
        {
            controller.spark = sparkAbility;
        }

        // 애니메이터 변경
        if (anim != null && sparkAnimator != null)
            anim.runtimeAnimatorController = sparkAnimator;

        Debug.Log("Spark 능력으로 전환됨!");
    }

    public AbilityType GetCurrentAbility()
    {
        if (beamAbility != null && beamAbility.enabled)
            return AbilityType.Beam;

        Spark spark = GetComponent<Spark>();
        if (spark != null)
            return AbilityType.Spark;

        return AbilityType.Normal;
    }

    /// <summary>
    /// 디버그용: 현재 능력 상태를 콘솔에 출력합니다.
    /// </summary>
    public void PrintCurrentAbility()
    {
        AbilityType current = GetCurrentAbility();
        Debug.Log($"현재 능력: {current}");
    }
}