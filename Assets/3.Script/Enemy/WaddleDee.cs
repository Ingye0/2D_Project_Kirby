using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaddleDee : EnemyBase
{
    [Header("움직임")]
    [SerializeField] private float moveSpeed = 0.6f;
    [SerializeField] private LayerMask WallLayer;
    [SerializeField] private float wallCheckDistance = 0.1f;

    private int currentDirection = -1;
    private float originalXScale;

    // Start 대신 Awake나 Start를 override
    protected void Start()
    {
        originalXScale = Mathf.Abs(transform.localScale.x);

        // 초기 방향 설정
        transform.localScale = new Vector3(originalXScale * currentDirection * -1, transform.localScale.y, transform.localScale.z);
    }

    private void FixedUpdate()
    {
        if (isDead || isInhaled) return;

        // 카메라 범위 밖이면 움직이지 않음
        if (!isInCameraView) return;

        // 1. 이동
        _rb.linearVelocity = new Vector2(currentDirection * moveSpeed, _rb.linearVelocity.y);

        // 2. 벽 감지
        if (IsWallAhead())
        {
            ChangeDirection();
        }
    }

    private void ChangeDirection()
    {
        currentDirection *= -1;
        transform.localScale = new Vector3(originalXScale * currentDirection * -1, transform.localScale.y, transform.localScale.z);
    }

    private bool IsWallAhead()
    {
        if (_collider == null) return false;

        // Raycast 시작점 계산: Collider의 현재 이동 방향쪽 가장자리 중앙
        float rayXOffset = _collider.bounds.extents.x + 0.02f; // 충돌체보다 약간 더 멀리
        Vector2 rayOrigin = new Vector2(_collider.bounds.center.x, _collider.bounds.center.y);

        // Raycast를 쏘아 WallLayer와 충돌하는지 확인
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * currentDirection, wallCheckDistance + rayXOffset, WallLayer);

        // 디버그 레이 그리기
        Debug.DrawRay(rayOrigin, Vector2.right * currentDirection * (wallCheckDistance + rayXOffset), hit.collider != null ? Color.red : Color.blue);

        return hit.collider != null;
    }
}