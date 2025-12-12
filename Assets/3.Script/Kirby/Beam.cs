using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beam : MonoBehaviour
{
    // 외부 컴포넌트 참조
    private Kirby_Controller controller;
    private Animator anim;
    public bool IsAttacking { get; private set; } = false;

    [Header("빔 공격 설정")]
    [SerializeField] private int beamDamage = 2;              // 빔 공격의 피해량
    [SerializeField] private float beamSweepDuration = 0.6f;  // 빔이 훑는 시간
    [SerializeField] private float beamRange = 4f;          // 빔의 최대 사거리
    [SerializeField] private float startAngle = 60f;          // 시작 각도 (위쪽)
    [SerializeField] private float endAngle = -60f;           // 끝 각도 (아래쪽)
    [SerializeField] private int numberOfRays = 15;           // Raycast 개수 (많을수록 부드러움)
    [SerializeField] private LayerMask enemyLayer;            // 공격 대상 레이어

    private HashSet<IInhalable> hitEnemiesThisAttack = new HashSet<IInhalable>();

    private void Awake()
    {
        controller = GetComponent<Kirby_Controller>();
        anim = GetComponent<Animator>();


        Debug.Log("빔 능력 활성화됨.");
    }

    private void Update()
    {
        HandleAbilityInput();
    }

    /// <summary>
    /// Z 키 입력을 감지하고 공격을 시작합니다.
    /// </summary>
    private void HandleAbilityInput()
    {
        // 공격 중이거나, 다른 주요 동작 중에는 입력 무시
        if (IsAttacking || controller.isBurping || controller.isDucking || controller.isTackling)
        {
            return;
        }

        // Z 키를 누르는 순간 공격 시작
        if (Input.GetKeyDown(KeyCode.Z))
        {
            StartCoroutine(BeamSweepAttack_co());
        }

        // ★ C 키를 누르는 순간: 능력 해제
        if (Input.GetKeyDown(KeyCode.C))
        {
            ReleaseAbility();
        }
    }

    /// <summary>
    /// 빔 공격 시퀀스를 관리하는 코루틴
    /// </summary>
    private IEnumerator BeamSweepAttack_co()
    {
        IsAttacking = true;
        hitEnemiesThisAttack.Clear();

        if (controller != null)
        {
            controller.StopJumpInput();
        }

        anim.SetTrigger("BeamAttack");

        if (AudioManager.Instance != null && AudioManager.Instance.beamAttackSFX != null)
        {
            AudioManager.Instance.PlayLoopSFX(AudioManager.Instance.beamAttackSFX);
        }


        float rayInterval = beamSweepDuration / numberOfRays;

        for (int i = 0; i < numberOfRays; i++)
        {
            // 매번 새로 계산
            float direction = controller.transform.localScale.x;
            Vector2 beamOrigin = (Vector2)controller.transform.position + new Vector2(direction * 0.3f, 0.1f);

            // 각도 계산
            float actualStartAngle = (direction < 0) ? endAngle : startAngle;
            float actualEndAngle = (direction < 0) ? startAngle : endAngle;

            float progress = (float)i / (numberOfRays - 1);
            float currentAngle = Mathf.Lerp(actualStartAngle, actualEndAngle, progress);
            float angleAdjustment = (direction < 0) ? 180f : 0f;
            float finalAngle = currentAngle + angleAdjustment;

            Vector2 rayDirection = Quaternion.Euler(0, 0, finalAngle) * Vector2.right;

            RaycastHit2D hit = Physics2D.Raycast(beamOrigin, rayDirection, beamRange, enemyLayer);

            Color rayColor = (hit.collider != null) ? Color.red : Color.yellow;
            Debug.DrawRay(beamOrigin, rayDirection * beamRange, rayColor, 0.5f);

            if (hit.collider != null)
            {
                IInhalable enemy = hit.collider.GetComponent<IInhalable>();
                if (enemy != null && !hitEnemiesThisAttack.Contains(enemy))
                {
                    enemy.TakeDamage(beamDamage);
                    hitEnemiesThisAttack.Add(enemy);
                }
            }

            yield return new WaitForSeconds(rayInterval);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopSFX();
        }

        yield return new WaitForSeconds(0.2f);
        IsAttacking = false;
    }

    // 이 능력이 비활성화될 때 호출되어야 합니다. (예: 다른 능력을 얻거나 피해를 입었을 때)
    public void DeactivateAbility()
    {
        // 진행 중인 모든 공격 코루틴을 중지
        StopAllCoroutines();

        IsAttacking = false;

        // 이 컴포넌트 비활성화
        this.enabled = false;
    }

    private void ReleaseAbility()
    {
        Debug.Log("빔 능력 해제!");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCopyCancelSFX();
        }

        // AbilityManager를 통해 노말로 돌아감
        AbilityManager abilityManager = GetComponent<AbilityManager>();
        if (abilityManager != null)
        {
            abilityManager.ResetToNormal();
        }
    }
}
