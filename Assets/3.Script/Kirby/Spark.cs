using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spark : MonoBehaviour
{
    // 외부 컴포넌트 참조
    private Kirby_Controller controller;
    private Animator anim;
    public bool IsAttacking { get; private set; } = false;

    [Header("스파크 공격 설정")]
    [SerializeField] private int sparkDamage = 2;              // 스파크 공격의 피해량
    [SerializeField] private float sparkRadius = 1.8f;         // 스파크 범위 (원형)
    [SerializeField] private float hitInterval = 0.1f;         // 타격 간격
    [SerializeField] private float sparkStartDuration = 0.1f;  // 시작 애니메이션 길이
    [SerializeField] private LayerMask enemyLayer;             // 공격 대상 레이어

    private HashSet<IInhalable> hitEnemiesThisFrame = new HashSet<IInhalable>();
    private float lastHitTime = 0f;
    private bool isHoldingAttack = false;
    private bool isStartingAttack = false;  // 시작 애니메이션 재생 중
    private Kirby_Controller kirby;

    private void Start()
    {
        kirby = GetComponent<Kirby_Controller>();
    }
    private void Awake()
    {
        controller = GetComponent<Kirby_Controller>();
        anim = GetComponent<Animator>();

        Debug.Log("스파크 능력 활성화됨.");
    }

    private void Update()
    {
        HandleAbilityInput();

        // 홀딩 중일 때 지속적으로 공격 (시작 애니메이션 끝난 후)
        if (isHoldingAttack && !isStartingAttack)
        {
            PerformSparkAttack();
        }
    }

    /// <summary>
    /// Z, C 키 입력을 감지합니다.
    /// </summary>
    private void HandleAbilityInput()
    {
        // 다른 주요 동작 중에는 입력 무시
        if (controller.isBurping || controller.isDucking || controller.isTackling)
        {
            if (isHoldingAttack)
            {
                StopSparkAttack();
            }
            return;
        }

        // Z 키를 누르는 순간: 스파크 공격 시작
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (!isHoldingAttack)
            {
                StartCoroutine(StartSparkAttack_co());
            }
        }
        // Z 키를 누르고 있는 중: 홀딩 유지
        else if (Input.GetKey(KeyCode.Z))
        {
            // 홀딩 상태 유지
        }
        // Z 키를 뗀 순간: 스파크 공격 중지
        else if (Input.GetKeyUp(KeyCode.Z))
        {
            if (isHoldingAttack)
            {
                StopSparkAttack();
            }
        }

        // C 키를 누르는 순간: 능력 해제
        if (Input.GetKeyDown(KeyCode.C))
        {
            ReleaseAbility();
        }
    }

    /// <summary>
    /// 스파크 공격 시작 (시작 애니메이션 포함)
    /// </summary>
    private IEnumerator StartSparkAttack_co()
    {
        isHoldingAttack = true;
        isStartingAttack = true;
        IsAttacking = true;
        lastHitTime = 0f;

        if (kirby != null)
            kirby.isInvincible = true;

        if (controller != null)
        {
            controller.StopJumpInput();
        }

        // ★ 시작 애니메이션 트리거
        anim.SetTrigger("SparkStart");

        // ★ 시작 애니메이션 동안 대기
        yield return new WaitForSeconds(sparkStartDuration);

        // Z키를 여전히 누르고 있는지 확인
        if (!Input.GetKey(KeyCode.Z))
        {
            // Z키를 이미 뗐다면 중지
            StopSparkAttack();
            yield break;
        }

        isStartingAttack = false;

        // ★ 홀딩 애니메이션으로 전환
        anim.SetBool("IsSparkHolding", true);

        // ★ 스파크 루프 효과음 시작
        if (AudioManager.Instance != null && AudioManager.Instance.sparkAttackSFX != null)
        {
            AudioManager.Instance.PlayLoopSFX(AudioManager.Instance.sparkAttackSFX);
        }
    }

    /// <summary>
    /// 스파크 공격 중지
    /// </summary>
    private void StopSparkAttack()
    {
        // 시작 중이었다면 코루틴 중지
        if (isStartingAttack)
        {
            StopAllCoroutines();
            isStartingAttack = false;
        }

        if (kirby != null)
            kirby.isInvincible = false;

        isHoldingAttack = false;
        IsAttacking = false;
        hitEnemiesThisFrame.Clear();

        anim.SetBool("IsSparkHolding", false);

        // ★ 스파크 루프 효과음 정지
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopSFX();
        }
    }

    /// <summary>
    /// 스파크 공격 실행 (Update에서 지속적으로 호출)
    /// </summary>
    private void PerformSparkAttack()
    {
        // 일정 간격으로만 타격 판정
        if (Time.time < lastHitTime + hitInterval)
        {
            return;
        }

        lastHitTime = Time.time;
        hitEnemiesThisFrame.Clear();

        // 원형 범위 내의 모든 적 감지
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, sparkRadius, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            IInhalable enemy = hit.GetComponent<IInhalable>();
            if (enemy != null && !hitEnemiesThisFrame.Contains(enemy))
            {
                enemy.TakeDamage(sparkDamage);
                hitEnemiesThisFrame.Add(enemy);
            }
        }

        // 디버그용 원 그리기
        DrawCircle(transform.position, sparkRadius, Color.yellow, hitInterval);
    }

    /// <summary>
    /// 능력 해제 (노말 커비로 돌아감)
    /// </summary>
    private void ReleaseAbility()
    {
        Debug.Log("스파크 능력 해제!");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCopyCancelSFX();
        }
        
        // 공격 중이었다면 중지
        if (isHoldingAttack)
        {
            StopSparkAttack();
        }

        // AbilityManager를 통해 노말로 돌아감
        AbilityManager abilityManager = GetComponent<AbilityManager>();
        if (abilityManager != null)
        {
            abilityManager.ResetToNormal();
        }
    }

    /// <summary>
    /// 디버그용 원 그리기
    /// </summary>
    private void DrawCircle(Vector3 center, float radius, Color color, float duration)
    {
        int segments = 36;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);

            Debug.DrawLine(point1, point2, color, duration);
        }
    }

    // 이 능력이 비활성화될 때 호출되어야 합니다.
    public void DeactivateAbility()
    {
        // 공격 중이었다면 중지
        if (isHoldingAttack)
        {
            StopSparkAttack();
        }

        if (kirby != null)
            kirby.isInvincible = false;


        // 진행 중인 모든 코루틴을 중지
        StopAllCoroutines();

        IsAttacking = false;

        // 이 컴포넌트 파괴
        this.enabled = false;
    }
}