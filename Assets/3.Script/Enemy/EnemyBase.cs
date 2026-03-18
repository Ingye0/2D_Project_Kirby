using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour, IInhalable
{
    [Header("기본 스텟")]
    [SerializeField] protected int health = 1;

    [Header("최적화")]
    [SerializeField] private bool enableCulling = true; // 카메라 컬링 활성화 여부
    [SerializeField] private float cullingBuffer = 2f;  // 카메라 영역보다 얼마나 더 넓게 체크할지

    [Header("넉백")]
    [SerializeField] private float knockbackForce = 3f; // 넉백 힘
    [SerializeField] private float knockbackVerticalForce = 3f; // 넉백 수직 
    [SerializeField] private float knockbackDuration = 0.2f; // 넉백 지속 시간

    [Header("효과")]
    [SerializeField] private GameObject ExplodePrefab;
    [SerializeField] private float hitStopDuration = 0.2f;

    protected Rigidbody2D _rb;
    protected Collider2D _collider;
    protected Animator _anim;

    protected float originalGravityScale;
    public virtual bool HasAbility => false;
    public virtual AbilityType Ability => AbilityType.Normal;
    protected bool isInCameraView = true;
    private Camera mainCamera;

    // IInhalable 구현을 위한 프로퍼티
    public Transform transform => base.transform;
    public GameObject gameObject => base.gameObject;
    public Rigidbody2D rb => _rb;
    public Collider2D collider => _collider;

    protected bool isDead = false; //죽었나
    protected bool isInhaled = false; // 빨려들어가는중
    protected bool isKnockedBack = false; // 넉백
    protected bool isPerformingAbility = false;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _anim = GetComponent<Animator>();

        if (_rb)
        {
            originalGravityScale = _rb.gravityScale;
        }

        mainCamera = Camera.main;
    }

    protected virtual void Update()
    {
        if (enableCulling && mainCamera != null)
        {
            CheckCameraView();
        }
    }

    // 카메라 범위 체크 메서드
    private void CheckCameraView()
    {
        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(transform.position);

        // Viewport 좌표: (0,0)은 왼쪽 아래, (1,1)은 오른쪽 위
        // Buffer를 추가하여 카메라 영역보다 약간 넓게 체크
        float buffer = cullingBuffer / mainCamera.orthographicSize;

        bool wasInView = isInCameraView;
        isInCameraView = viewportPosition.x >= (0 - buffer) &&
                         viewportPosition.x <= (1 + buffer) &&
                         viewportPosition.y >= (0 - buffer) &&
                         viewportPosition.y <= (1 + buffer) &&
                         viewportPosition.z > 0;

        // 카메라 범위에 들어왔을 때
        if (!wasInView && isInCameraView)
        {
            OnEnterCameraView();
        }
        // 카메라 범위를 벗어났을 때
        else if (wasInView && !isInCameraView)
        {
            OnExitCameraView();
        }
    }

    // 카메라 범위에 들어왔을 때 호출
    protected virtual void OnEnterCameraView()
    {
        if (_anim) _anim.enabled = true;
        if (_rb) _rb.simulated = true;
    }

    //  카메라 범위를 벗어났을 때 호출
    protected virtual void OnExitCameraView()
    {
        if (_anim) _anim.enabled = false;
        if (_rb) _rb.simulated = false;
    }

    // 공통 피격 처리
    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;

        health -= damage;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyHitSFX();
        }

        // 넉백 방향 계산
        Vector2 damageSourcePos = FindObjectOfType<Kirby_Controller>()?.transform.position ?? transform.position - Vector3.right;
        Vector2 knockbackDirection = ((Vector2)transform.position - damageSourcePos).normalized;

        CameraController.Instance.ShakeCamera(0.2f, 0.5f);

        if (isKnockedBack) StopCoroutine("Knockback_co");
        isKnockedBack = false;

        StartCoroutine(HitFreeze_co(knockbackDirection, health <= 0));

        if (health > 0)
        {
            // 생존 시에만 피격 애니메이션 (넉백이 시작되기 전에 발생합니다)
            if (_anim) _anim.SetTrigger("Hit");
        }
    }

    protected IEnumerator HitFreeze_co(Vector2 knockbackDirection, bool shouldDie)
    {
        // 적의 움직임을 잠시 멈춥니다.
        if (_rb)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.gravityScale = 0f; // 중력도 잠시 제거하여 위치 고정을 확실히 합니다.
        }

        // 멈춤 시간만큼 대기합니다.
        yield return new WaitForSeconds(hitStopDuration);

        // 넉백 코루틴을 시작합니다.
        StartCoroutine(Knockback_co(knockbackDirection, shouldDie));
    }

    protected IEnumerator Knockback_co(Vector2 direction, bool shouldDie)
    {
        isKnockedBack = true;
        // 넉백 적용: Impulse로 힘을 가하고, 중력을 0으로 설정하여 공중에 뜨는 효과를 줌
        if (_rb)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.gravityScale = 0;
            // 수평 방향 힘 (direction.x만 사용)
            float knockbackX = direction.x * knockbackForce;
            // 수직 방향 힘 (고정된 튀어오르는 힘)
            float knockbackY = knockbackVerticalForce; // <-- 수직 힘 적용

            Vector2 finalKnockbackVector = new Vector2(knockbackX, knockbackY);

            _rb.AddForce(finalKnockbackVector, ForceMode2D.Impulse); // <-- 수평 + 수직 바운스 힘 적용
        }

        yield return new WaitForSeconds(knockbackDuration);

        // 넉백 종료
        isKnockedBack = false;

        // 물리 복구: 중력 복구 (원래 값 1로 가정) 및 선속도 초기화
        if (shouldDie)
        {
            // 넉백이 끝난 후에야 비로소 사망 처리를 시작합니다.
            DieImmediatePostKnockback();
        }
        else
        {
            // 생존한 경우: 원래 중력 및 물리 상태 복구 (기존 로직 유지)
            if (_rb)
            {
                _rb.isKinematic = false;
                _rb.gravityScale = originalGravityScale;
            }
        }
    }

    protected virtual void DieImmediatePostKnockback()
    {
        if (isDead) return; // 혹시 모를 중복 방지

        isDead = true;

        // 콜라이더 비활성화
        if (_collider) _collider.enabled = false;

        // 넉백으로 얻은 속도와 물리 동작을 최종 정지
        if (_rb)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.isKinematic = true;
            _rb.gravityScale = 0;
        }

        // 애니메이션이 있다면 Die 트리거
        if (_anim)
        {
            _anim.SetTrigger("Die");
        }

        // 폭발 이펙트 트리거 및 파괴 (이전 수정안을 따름)
        TriggerDeathExplosion();
    }

    // 공통 사망 처리
    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // 콜라이더는 비활성화하여 다른 물체와의 상호작용을 막습니다.
        if (_collider) _collider.enabled = false;

        if (_anim)
        {
            _anim.SetTrigger("Die");
        }
        else
        {
            TriggerDeathExplosion();
        }
    }

    public void TriggerDeathExplosion()
    {
        // 폭발 이펙트 생성
        if (ExplodePrefab != null)
        {
            Instantiate(ExplodePrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError($"[EnemyBase] {gameObject.name}의 ExplodePrefab이 할당되지 않았습니다. 인스펙터에 연결해야 합니다.");
        }

        // 오브젝트 파괴
        Destroy(gameObject);
    }

    // IInhalable 구현

    public virtual void OnInhaleStart()
    {
        if (isDead) return;
        isInhaled = true;

        StopAllCoroutines();
        isKnockedBack = false;
        if (_anim) _anim.SetBool("IsInhaled", true);
        if (_rb) _rb.isKinematic = true;
    }

    public virtual void OnInhaleCancel()
    {
        if (isDead) return;
        isInhaled = false;
        if (_anim) _anim.SetBool("IsInhaled", false);
        // 물리 동작 복구
        if (_rb) _rb.isKinematic = false;
    }

    public virtual void OnSwallowed()
    {
        Destroy(gameObject);
    }
}
