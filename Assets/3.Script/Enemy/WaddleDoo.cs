
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaddleDoo : EnemyBase
{
    public override bool HasAbility => true;
    public override AbilityType Ability => AbilityType.Beam;


    [Header("움직임")]
    [SerializeField] private float moveSpeed = 0.8f;
    [SerializeField] private LayerMask WallLayer;
    [SerializeField] private float wallCheckDistance = 0.1f;

    private int currentDirection = -1; // -1: 왼쪽, 1: 오른쪽
    private float originalXScale;


    [Header("Waddle Doo Ability Settings")]
    [SerializeField] private float jumpForce = 8f;     // 점프 힘
    [SerializeField] private LayerMask groundLayer;    // 땅 레이어 마스크
    [SerializeField] private float groundCheckDistance = 0.2f; // 땅 감지 거리 (Collider 중심 아래부터)


    [Header("빔 공격 설정")]
    [SerializeField] private int beamDamage = 2;              // 빔 공격 데미지
    [SerializeField] private float beamWindupTime = 2f;     // 빔 공격 준비 시간
    [SerializeField] private float beamSweepDuration = 1f;  // 빔이 훑는 시간
    [SerializeField] private LayerMask KirbyLayer;    // 플레이어 Layer Mask (빔 공격용)

    [Header("사운드")]
    [SerializeField] private AudioSource enemyAudioSource;
    [SerializeField] private AudioClip beamAttackClip;

    private bool isAttacking = false;

    protected override void Awake()
    {
        base.Awake();
        if (_rb)
        {
            _rb.isKinematic = false;
            _rb.gravityScale = originalGravityScale;
        }

        // AudioSource가 없으면 자동으로 추가
        if (enemyAudioSource == null)
        {
            enemyAudioSource = GetComponent<AudioSource>();
            if (enemyAudioSource == null)
            {
                enemyAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // 3D 사운드 설정
        enemyAudioSource.spatialBlend = 1f; // 완전히 3D
        enemyAudioSource.rolloffMode = AudioRolloffMode.Linear;
        enemyAudioSource.minDistance = 5f;
        enemyAudioSource.maxDistance = 20f;
    }

    protected void Start()
    {
        originalXScale = Mathf.Abs(transform.localScale.x);

        // 초기 방향 설정 및 스프라이트 반전
        transform.localScale = new Vector3(originalXScale * currentDirection * -1, transform.localScale.y, transform.localScale.z);

        // 능력 루프 시작
        StartCoroutine(AbilityLoop_co());
    }

    private void FixedUpdate()
    {
        if (isDead || isInhaled || isKnockedBack) return;

        // 카메라 범위 밖이면 움직이지 않음
        if (!isInCameraView) return;

        // 능력 수행 중이 아닐 때만 Waddle Dee 순찰 움직임을 실행
        if (!isPerformingAbility)
        {
            // 이동
            _rb.linearVelocity = new Vector2(currentDirection * moveSpeed, _rb.linearVelocity.y);

            // 벽 감지 및 방향 전환
            if (IsWallAhead())
            {
                ChangeDirection();
            }
        }
    }

    //방향 전환 로직
    private void ChangeDirection()
    {
        currentDirection *= -1;
        transform.localScale = new Vector3(originalXScale * currentDirection * -1, transform.localScale.y, transform.localScale.z);
    }


    // 벽 감지 로직
    private bool IsWallAhead()
    {
        if (_collider == null) return false;

        // Raycast 시작점 계산: Collider의 현재 이동 방향쪽 가장자리 중앙
        float rayXOffset = _collider.bounds.extents.x + 0.02f; // 충돌체보다 약간 더 멀리
        Vector2 rayOrigin = new Vector2(_collider.bounds.center.x, _collider.bounds.center.y);

        // Raycast를 쏘아 WallLayer와 충돌하는지 확인
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * currentDirection, wallCheckDistance + rayXOffset, WallLayer);

        return hit.collider != null;
    }
    private bool IsGrounded()
    {
        // Collider의 아래쪽 경계 중앙에서 아래로 Raycast 발사
        Vector2 rayOrigin = new Vector2(_collider.bounds.center.x, _collider.bounds.min.y);

        // Raycast를 쏘아 groundLayer 마스크를 가진 콜라이더와 충돌하는지 확인
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundCheckDistance, groundLayer);

        return hit.collider != null;
    }


    private IEnumerator AbilityLoop_co()
    {
        yield return new WaitForSeconds(1f);

        while (!isDead && !isInhaled)
        {
            // 다음 행동을 결정하기 전에 잠시 대기
            yield return new WaitForSeconds(3f);

            // 상태 확인: 능력 수행 중이거나 넉백 중이면 스킵
            if (isKnockedBack || isPerformingAbility) continue;

            // 0: Jump, 1: Beam Attack, 2: Do Nothing (Continue Walking)
            int randomAbility = Random.Range(0, 3); // 0, 1, 2 중 하나를 선택하도록 범위 변경

            if (randomAbility == 0)
            {
                StartCoroutine(Jump_co());
            }
            else if (randomAbility == 1)
            {
                StartCoroutine(BeamAttack_co());
            }
            else { } // randomAbility == 2 (아무 일 없음)
        }
    }

    //점프 동작 코루틴
    private IEnumerator Jump_co()
    {
        // 땅에 닿아 있지 않으면 점프하지 않고 코루틴을 종료합니다.
        if (!IsGrounded())
        {
            yield break;
        }

        isPerformingAbility = true;
        if (_anim) _anim.SetBool("IsJumping", true);

        // 점프 힘 적용
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // 땅에서 떨어질 때까지 잠깐 대기 (프레임 스킵 방지 및 IsGrounded()가 false가 될 때까지)
        yield return new WaitForSeconds(0.1f);

        // 땅에 다시 닿을 때까지 대기
        yield return new WaitUntil(() => IsGrounded());

        // 착지 후 마무리
        if (_anim) _anim.SetBool("IsJumping", false);
        isPerformingAbility = false;
    }

    // 전방 45도에서 -45도까지 훑는 빔 공격 코루틴
    private IEnumerator BeamAttack_co()
    {
        isPerformingAbility = true;
        isAttacking = true;

        if (_anim) _anim.SetBool("IsAttacking", true);


        // 준비 시간 (Wind-up): 이 시간 동안 멈춰서 공격 준비 애니메이션 재생
        _rb.linearVelocity = Vector2.zero; // 이동 중지
        yield return new WaitForSeconds(beamWindupTime);

        // 3D 사운드로 빔 공격 효과음 재생
        if (enemyAudioSource != null && beamAttackClip != null)
        {
            enemyAudioSource.clip = beamAttackClip;
            enemyAudioSource.loop = true;
            enemyAudioSource.Play();
        }

        // 빔 스윕 공격 시작
        float startAngle = 60f;         // 위쪽 60도
        float endAngle = -60f;         // 아래쪽 -60도

        // 캐릭터가 왼쪽을 바라볼 때(currentDirection < 0),
        // 180도 회전 후에도 위에서 아래로 스윕하려면 Lerp의 시작과 끝 각도를 바꿔야 합니다.
        if (currentDirection < 0)
        {
            startAngle = -60f;
            endAngle = 60f;   
        }

        int numberOfRays = 15;
        float rayInterval = beamSweepDuration / numberOfRays;

        for (int i = 0; i < numberOfRays; i++)
        {
            float progress = (float)i / (numberOfRays - 1);

            // 시작 각도에서 끝 각도까지 진행합니다.
            float currentAngle = Mathf.Lerp(startAngle, endAngle, progress);

            // 빔 방향을 Waddle Doo의 현재 방향에 따라 180도 회전시킵니다.
            float angleAdjustment = (currentDirection < 0) ? 180f : 0f; // 왼쪽을 바라보면 180도 추가
            float finalAngle = currentAngle + angleAdjustment;

            // 빔 방향 계산: finalAngle을 사용하여 Vector2.right를 회전
            Vector2 rayDirection = Quaternion.Euler(0, 0, finalAngle) * Vector2.right;

            // 빔 사거리 3f로 제한
            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, 3f, KirbyLayer);

            if (hit.collider != null)
            {
                var kirby = hit.collider.GetComponent<Kirby_Controller>();
                if (kirby != null && !isDead)
                {
                    kirby.TakeDamage(beamDamage, transform.position);
                }
            }

            Debug.DrawRay(transform.position, rayDirection * 3f, Color.red, rayInterval);

            yield return new WaitForSeconds(rayInterval);
        }

        // 공격 종료
        isAttacking = false;
        if (_anim) _anim.SetBool("IsAttacking", false);

        // 3D 사운드 정지
        if (enemyAudioSource != null)
        {
            enemyAudioSource.loop = false;
            enemyAudioSource.Stop();
        }

        isPerformingAbility = false;
    }
}