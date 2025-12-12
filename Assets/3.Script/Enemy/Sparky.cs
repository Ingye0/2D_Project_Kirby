
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sparky : EnemyBase
{
    public override bool HasAbility => true;
    public override AbilityType Ability => AbilityType.Spark;

    [Header("점프 설정")]
    [SerializeField] private float lowJumpForce = 4f;      // 낮은 점프 힘
    [SerializeField] private float highJumpForce = 8f;    // 높은 점프 힘
    [SerializeField] private float forwardJumpSpeed = 2f;  // 앞으로 점프 시 수평 속도
    [SerializeField] private LayerMask groundLayer;        // 땅 레이어 마스크
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("스파크 공격 설정")]
    [SerializeField] private int sparkDamage = 2;              // 스파크 공격 피해량
    [SerializeField] private float sparkRadius = 2.5f;         // 스파크 공격 범위
    [SerializeField] private float sparkDuration = 0.5f;       // 스파크 지속 시간
    [SerializeField] private int numberOfHits = 3;             // 스파크 타격 횟수
    [SerializeField] private float detectionRadius = 3f;       // 커비 감지 범위
    [SerializeField] private float sparkWindupTime = 0.5f;     // 스파크 공격 준비 시간
    [SerializeField] private LayerMask kirbyLayer;             // 커비 레이어

    [Header("행동 주기")]
    [SerializeField] private float actionCooldown = 2f;        // 행동 간 대기 시간

    [Header("사운드")]
    [SerializeField] private AudioSource enemyAudioSource;
    [SerializeField] private AudioClip sparkAttackClip;
    [SerializeField] private float soundMinDistance = 5f;
    [SerializeField] private float soundMaxDistance = 20f;

    private bool isAttacking = false;
    private Transform kirbyTransform;
    private float originalXScale;

    protected override void Awake()
    {
        base.Awake();

        if (_rb)
        {
            _rb.isKinematic = false;
            _rb.gravityScale = originalGravityScale;
        }

        // AudioSource 설정
        if (enemyAudioSource == null)
        {
            enemyAudioSource = GetComponent<AudioSource>();
            if (enemyAudioSource == null)
            {
                enemyAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        enemyAudioSource.spatialBlend = 1f;
        enemyAudioSource.rolloffMode = AudioRolloffMode.Linear;
        enemyAudioSource.minDistance = soundMinDistance;
        enemyAudioSource.maxDistance = soundMaxDistance;
        enemyAudioSource.playOnAwake = false;
        enemyAudioSource.loop = false;
    }

    protected void Start()
    {
        originalXScale = Mathf.Abs(transform.localScale.x);

        // 커비 찾기
        Kirby_Controller kirby = FindObjectOfType<Kirby_Controller>();
        if (kirby != null)
        {
            kirbyTransform = kirby.transform;
        }

        // 행동 루프 시작
        StartCoroutine(BehaviorLoop_co());
    }

    private void FixedUpdate()
    {
        if (isDead || isInhaled || isKnockedBack) return;

        // 카메라 범위 밖이면 움직이지 않음
        if (!isInCameraView) return;

        // 커비 방향으로 스프라이트 회전
        if (kirbyTransform != null && !isPerformingAbility)
        {
            FaceKirby();
        }

        // ★ 점프 애니메이션 상태 업데이트 (공중에 있을 때)
        if (!IsGrounded() && !isAttacking)
        {
            if (_rb.linearVelocity.y > 0.1f)
            {
                // 상승 중
                if (_anim) _anim.SetBool("IsJumpingUp", true);
                if (_anim) _anim.SetBool("IsJumpingDown", false);
            }
            else if (_rb.linearVelocity.y < -0.1f)
            {
                // 하강 중
                if (_anim) _anim.SetBool("IsJumpingUp", false);
                if (_anim) _anim.SetBool("IsJumpingDown", true);
            }
        }
        else
        {
            // 땅에 있으면 점프 애니메이션 해제
            if (_anim) _anim.SetBool("IsJumpingUp", false);
            if (_anim) _anim.SetBool("IsJumpingDown", false);
        }
    }

    /// <summary>
    /// 커비 방향으로 스프라이트 회전 (수정: 반대 방향 문제 해결)
    /// </summary>
    private void FaceKirby()
    {
        if (kirbyTransform == null) return;

        float directionToKirby = Mathf.Sign(kirbyTransform.position.x - transform.position.x);
        // -1을 곱해서 반대로 (스프라이트가 반대로 그려진 경우)
        transform.localScale = new Vector3(originalXScale * directionToKirby * -1, transform.localScale.y, transform.localScale.z);
    }

    /// <summary>
    /// 땅에 닿아있는지 체크
    /// </summary>
    private bool IsGrounded()
    {
        if (_collider == null) return false;

        Bounds bounds = _collider.bounds;

        Vector2 leftOrigin = new Vector2(bounds.min.x + 0.05f, bounds.min.y);
        Vector2 centerOrigin = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 rightOrigin = new Vector2(bounds.max.x - 0.05f, bounds.min.y);

        RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitCenter = Physics2D.Raycast(centerOrigin, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.down, groundCheckDistance, groundLayer);

        Debug.DrawRay(leftOrigin, Vector2.down * groundCheckDistance, hitLeft.collider != null ? Color.green : Color.yellow);
        Debug.DrawRay(centerOrigin, Vector2.down * groundCheckDistance, hitCenter.collider != null ? Color.green : Color.yellow);
        Debug.DrawRay(rightOrigin, Vector2.down * groundCheckDistance, hitRight.collider != null ? Color.green : Color.yellow);

        return (hitLeft.collider != null || hitCenter.collider != null || hitRight.collider != null);
    }

    /// <summary>
    /// 커비가 감지 범위 내에 있는지 체크
    /// </summary>
    private bool IsKirbyInRange()
    {
        if (kirbyTransform == null) return false;

        float distance = Vector2.Distance(transform.position, kirbyTransform.position);
        return distance <= detectionRadius;
    }

    /// <summary>
    /// 행동 루프
    /// </summary>
    private IEnumerator BehaviorLoop_co()
    {
        yield return new WaitForSeconds(1f);

        while (!isDead && !isInhaled)
        {
            yield return new WaitForSeconds(actionCooldown);

            if (isKnockedBack || isPerformingAbility) continue;

            // 카메라 범위 밖이면 행동하지 않음
            if (!isInCameraView) continue;

            // 커비가 감지 범위 내에 있으면 스파크 공격
            if (IsKirbyInRange())
            {
                StartCoroutine(SparkAttack_co());
            }
            else
            {
                // 커비가 없으면 랜덤 점프
                int randomJump = Random.Range(0, 4);
                switch (randomJump)
                {
                    case 0: // 낮은 제자리 점프
                        StartCoroutine(Jump_co(lowJumpForce, false));
                        break;
                    case 1: // 높은 제자리 점프
                        StartCoroutine(Jump_co(highJumpForce, false));
                        break;
                    case 2: // 앞으로 낮은 점프
                        StartCoroutine(Jump_co(lowJumpForce, true));
                        break;
                    case 3: // 앞으로 높은 점프
                        StartCoroutine(Jump_co(highJumpForce, true));
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 점프 코루틴
    /// </summary>
    private IEnumerator Jump_co(float jumpForce, bool moveForward)
    {
        if (!IsGrounded())
        {
            yield break;
        }

        isPerformingAbility = true;

        // 커비 방향으로 향하기
        FaceKirby();

        // ★ 점프 시작 애니메이션
        if (_anim) _anim.SetBool("IsJumpingUp", true);

        // 점프 힘 적용
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // 앞으로 점프하는 경우
        if (moveForward && kirbyTransform != null)
        {
            float directionToKirby = Mathf.Sign(kirbyTransform.position.x - transform.position.x);
            _rb.linearVelocity = new Vector2(directionToKirby * forwardJumpSpeed, _rb.linearVelocity.y);
        }

        yield return new WaitForSeconds(0.1f);

        // 땅에 닿을 때까지 대기 (FixedUpdate에서 애니메이션 전환 처리)
        yield return new WaitUntil(() => IsGrounded());

        // ★ 착지 시 점프 애니메이션 해제
        if (_anim) _anim.SetBool("IsJumpingUp", false);
        if (_anim) _anim.SetBool("IsJumpingDown", false);

        isPerformingAbility = false;
    }

    /// <summary>
    /// 스파크 공격 코루틴
    /// </summary>
    private IEnumerator SparkAttack_co()
    {
        isPerformingAbility = true;
        isAttacking = true;

        // ★ 공격 애니메이션 시작 (준비 동작 포함)
        if (_anim) _anim.SetTrigger("SparkAttack");

        // ★ 준비 시간 대기 (애니메이션 재생 중)
        yield return new WaitForSeconds(sparkWindupTime);

        // ★ 이 시점에서 실제 공격 시작 (스파크 발동)

        // 스파크 루프 효과음 시작
        if (enemyAudioSource != null && sparkAttackClip != null)
        {
            enemyAudioSource.clip = sparkAttackClip;
            enemyAudioSource.loop = true;
            enemyAudioSource.Play();
        }

        float hitInterval = sparkDuration / numberOfHits;
        HashSet<Kirby_Controller> hitTargets = new HashSet<Kirby_Controller>();

        for (int i = 0; i < numberOfHits; i++)
        {
            // 원형 범위 내의 커비 감지
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, sparkRadius, kirbyLayer);

            foreach (Collider2D hit in hits)
            {
                Kirby_Controller kirby = hit.GetComponent<Kirby_Controller>();
                if (kirby != null && !hitTargets.Contains(kirby) && !isDead)
                {
                    kirby.TakeDamage(sparkDamage, transform.position);
                    hitTargets.Add(kirby);
                }
            }

            // 디버그용 원 그리기
            DrawCircle(transform.position, sparkRadius, Color.yellow, hitInterval);

            yield return new WaitForSeconds(hitInterval);
        }

        // 스파크 루프 효과음 정지
        if (enemyAudioSource != null)
        {
            enemyAudioSource.loop = false;
            enemyAudioSource.Stop();
        }

        isAttacking = false;

        yield return new WaitForSeconds(0.2f);
        isPerformingAbility = false;
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

    /// <summary>
    /// 디버그용 감지 범위 그리기
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 스파크 공격 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sparkRadius);

        // 커비 감지 범위
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}