using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarShooting : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float speed = 20f; // 스타의 비행 속도
    [SerializeField] private int damage = 2; // 스타가 적에게 주는 대미지
    [SerializeField] private float lifetime = 1.0f; // 스타의 최대 생존 시간

    [Header("Layer 설정")]
    [SerializeField] private LayerMask collisionLayers; // 충돌 시 사라질 Layer (적, 벽, 땅 포함)

    private Rigidbody2D rb;
    private Animator anim;
    private bool isHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // 커비가 발사할 때 호출하여 방향을 설정하고 발사합니다.
    public void Launch(float direction)
    {
        // 방향 설정 및 속도 적용
        rb.linearVelocity = new Vector2(direction * speed, 0f);

        // 스타의 스프라이트 방향 설정
        transform.localScale = new Vector3(direction, 1, 1);

        // 일정 시간 후 자동으로 파괴하는 코루틴 시작
        StartCoroutine(DestroyAfterTime(lifetime));
    }

    private void Update()
    {
        // 충돌 여부와 관계없이 최대 속도 유지 (선택 사항)
        if (!isHit)
        {
            rb.linearVelocity = new Vector2(transform.localScale.x * speed, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 충돌 처리가 진행 중이면 무시
        if (isHit) return;

        // 충돌 Layer에 포함되는지 확인
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            isHit = true;

            // 적 충돌 처리
            IInhalable enemy = other.GetComponent<IInhalable>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // 파괴 애니메이션 실행 및 오브젝트 제거
            StartDestruction();
        }
    }

    private IEnumerator DestroyAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 시간 초과 시 충돌하지 않았다면 파괴 시작
        if (!isHit)
        {
            isHit = true;
            StartDestruction();
        }
    }

    // 파괴 애니메이션을 시작하고 물리적인 움직임을 멈춥니다.
    private void StartDestruction()
    {
        // 움직임 즉시 중지
        rb.linearVelocity = Vector2.zero;

        // Collider 비활성화
        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = false;

        // 애니메이션 트리거
        if (anim != null)
        {
            anim.SetTrigger("Destroy");
        }
        else
        {
            // 애니메이션이 없으면 즉시 파괴
            Destroy(gameObject);
        }

        // 타이머 코루틴 중지 (중복 파괴 방지)
        StopAllCoroutines();
    }

    // 애니메이션 이벤트에서 호출될 파괴 함수
    public void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}
