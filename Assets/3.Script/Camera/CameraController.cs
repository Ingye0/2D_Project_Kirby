using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    public static CameraController Instance { get; private set; } //싱글턴


    [Header("추적 대상 및 참조")]
    [SerializeField] private Transform target;                  // ★ 커비 캐릭터의 Transform
    [SerializeField] private SpriteRenderer foreGroundRenderer; // ★ ForeGround 맵의 SpriteRenderer

    [Header("경계 보정")]
    [Tooltip("ForeGround 맵 경계에서 카메라를 안쪽으로 밀어낼 거리 (경계선 숨김용)")]
    [SerializeField] private float borderPadding = 0.1f; // ★ 0.1f는 일반적인 값 (조정 가능)

    // 이 변수는 카메라가 ForeGround 맵 경계에 닿았을 때,
    // 카메라의 중앙이 그 경계에서 얼마나 떨어져야 하는지를 정의합니다.
    private float cameraHalfWidth;

    // 내부 계산용 변수
    private float mapMinX;
    private float mapMaxX;

    private float fixedYPosition;

    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private float currentShakeTime = 0f;

    // 일정 패턴 쉐이크를 위한 변수
    private float constantShakeTimer = 0f;

    void Start()
    {
        // 싱글턴 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 이미 인스턴스가 있으면 새 인스턴스 파괴
            return;
        }
        Instance = this;

        if (target == null || foreGroundRenderer == null)
        {
            Debug.LogError("CameraController: Target 또는 ForeGround Renderer가 할당되지 않았습니다.");
            enabled = false;
            return;
        }

        fixedYPosition = transform.position.y;

        cameraHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;

        float mapBoundsMinX = foreGroundRenderer.bounds.min.x;
        float mapBoundsMaxX = foreGroundRenderer.bounds.max.x;

        // ★★★ 수정된 로직: padding 적용 ★★★

        // 맵의 왼쪽 끝: (맵 끝 + 카메라 절반 폭)에서 안쪽으로 borderPadding만큼 이동
        mapMinX = mapBoundsMinX + cameraHalfWidth + borderPadding;

        // 맵의 오른쪽 끝: (맵 끝 - 카메라 절반 폭)에서 안쪽으로 borderPadding만큼 이동
        mapMaxX = mapBoundsMaxX - cameraHalfWidth - borderPadding;

        // 맵이 카메라보다 짧을 경우 중앙에 고정
        if (mapMinX > mapMaxX)
        {
            float center = (mapBoundsMinX + mapBoundsMaxX) / 2f;
            mapMinX = mapMaxX = center;
            Debug.LogWarning("맵 길이가 너무 짧아 카메라 폭보다 작습니다. 카메라가 중앙에 고정됩니다.");
        }
    }

    void LateUpdate()
    {
        if (target == null || foreGroundRenderer == null) return;

        float targetX = target.position.x;

        // 맵 경계 제한 (Clamping)
        float clampedX = Mathf.Clamp(targetX, mapMinX, mapMaxX);

        Vector3 basePosition = new Vector3(
            clampedX,
            fixedYPosition,
            transform.position.z
        );

        Vector3 shakeOffset = Vector3.zero;
        if (shakeDuration > 0)
        {
            currentShakeTime += Time.deltaTime;
            constantShakeTimer += Time.deltaTime * 50f; // 떨림 속도 제어 (50f는 속도, 조정 가능)

            // ⭐️ Perlin Noise 대신 Mathf.Sin을 사용하여 일정한 패턴 생성 ⭐️
            // Sin 함수는 -1.0 ~ 1.0 사이를 반복하므로, 일정하고 예측 가능한 떨림 패턴을 만듭니다.
            float offsetX = Mathf.Sin(constantShakeTimer);
            float offsetY = Mathf.Sin(constantShakeTimer * 1.5f + 10f); // 약간 다른 속도/위상으로 Y축 떨림

            // 떨림 강도 감쇠 (시작 시 강하게 -> 0으로)
            float currentMagnitude = Mathf.Lerp(shakeMagnitude, 0f, currentShakeTime / shakeDuration);

            // 요청대로 떨림의 세기를 기존보다 '작게' 제한
            // 최대 강도가 0.05f를 넘지 않도록 Clamp (magnitude가 0.2f여도 떨림은 작게 보임)
            currentMagnitude = Mathf.Clamp(currentMagnitude, 0f, 0.05f);

            shakeOffset = new Vector3(offsetX, offsetY, 0f) * currentMagnitude;

            if (currentShakeTime >= shakeDuration)
            {
                shakeDuration = 0f;
                currentShakeTime = 0f;
                constantShakeTimer = 0f; // 쉐이크 종료 시 타이머 초기화
                shakeOffset = Vector3.zero;
            }
        }

        // 3. 최종 위치 적용
        transform.position = basePosition + shakeOffset;
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        if (magnitude >= shakeMagnitude || duration >= shakeDuration)
        {
            shakeDuration = duration;
            shakeMagnitude = magnitude;
            currentShakeTime = 0f;
            // constantShakeTimer는 LateUpdate에서 계속 증가하여 패턴을 만듭니다.
        }
    }
}