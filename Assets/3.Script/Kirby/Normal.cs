using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Normal : MonoBehaviour
{
    // 커비 컨트롤러 및 기타 컴포넌트 참조
    private Kirby_Controller controller;
    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D kirbyCollider;

    // 능력 상태
    public bool IsSucking { get; private set; } = false;
    public bool HasStar { get; private set; } = false; // 머금고 있는 상태

    public bool IsDelaying = false;
    public bool IsPreInhaling = false;

    [Header("공격 설정")]
    [SerializeField] private float starSpitForce = 10f;
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private LayerMask enemyLayer;

    [Header("노말 능력")] // Normal 능력 변수
    [SerializeField] private float inhaleRange = 2.0f;
    [SerializeField] private float inhalePreDelay = 0.3f;
    [SerializeField] private float inhaleAttractionSpeed = 10f;
    [SerializeField] private AudioClip inhaleLoopClip;

    [Header("능력 복사 연출")]
    [SerializeField] private GameObject timeStopOverlay;  // TimeStopOverlay Canvas
    [SerializeField] private UnityEngine.UI.Image darkOverlay;  // DarkOverlay Image
    [SerializeField] private float darknessFadeSpeed = 2f;  // 어두워지는 속도


    private IInhalable lastInhaledEnemy = null;

    // 내부 상태 및 타이머
    private Collider2D currentInhaledEnemy = null;
    private IInhalable currentTarget = null;
    private bool isInputHeld = false; // Z 키가 눌러지고 있는 중인지 확인

    private void Start()
    {
        controller = GetComponent<Kirby_Controller>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        kirbyCollider = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        if (IsSucking)
        {
            HandleInhalePhysics(); // 매 FixedUpdate마다 빨아들이기 로직 실행
        }
    }

    private void Update()
    {
        HandleAbilityInput();
    }

    // --- 입력 처리 (Update) ---

    public void HandleAbilityInput()
    {
        if (IsDelaying || IsPreInhaling)
        {
            // 딜레이/선딜레이 중 Z 키를 떼면
            if (Input.GetKeyUp(KeyCode.Z))
            {
                // 선딜레이 코루틴을 멈추고 상태를 초기화합니다.
                if (IsPreInhaling)
                {
                    StopAllCoroutines(); // 현재 실행 중인 InhalePreDelay_co를 강제 중단
                    IsPreInhaling = false;
                    anim.SetBool("IsSucking", false); // 애니메이션 상태 초기화
                    anim.SetTrigger("InhaleCancel");
                }
                isInputHeld = false;
                // 일반 딜레이 중에는 취소할 필요가 없으므로 IsPreInhaling만 처리합니다.
                return;
            }

            // Z 키를 뗀 것이 아니라면, 다른 모든 입력은 무시하고 종료
            return;
        }

        if (HasStar)
        {
            // Z 키 누르는 순간: 뱉기 (Spit)
            if (Input.GetKeyDown(KeyCode.Z))
            {
                SpitStar();
                return;
            }

            // S/아래방향 키 삼키기 (Swallow)
            if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && controller.isGrounded)
            {
                SwallowEnemy();
                return;
            }

            // HasStar 상태에서는 다른 입력 무시 (Inhale 방지)
            return;
        }

        if (controller.isBurping)
        {
            return;
        }

        // 점프, 태클, 숙이기 중이거나, 이미 별을 머금고 있다면 Inhale 불가
        if (controller.isFloating || controller.isTackling || controller.isDucking || HasStar)
        {
            // 이 상태에서 Z 키를 뗄 경우, 빨아들이기 상태였으면 취소 애니메이션만 실행
            if (IsSucking && Input.GetKeyUp(KeyCode.Z))
            {
                CancelInhale(false); // 흡입 중이었으면 취소 (애니메이션X, 상태 초기화O)
            }
            return;
        }


        // 2. Z 키 누르고 있는 중 (Inhale 지속)
        if (Input.GetKey(KeyCode.Z))
        {
            isInputHeld = true;
            if (!IsSucking)
            {
                PreInhale();
            }
        }

        // 3. Z 키 떼는 순간 (Inhale 취소 또는 Spit)
        if (Input.GetKeyUp(KeyCode.Z))
        {
            isInputHeld = false;
            if (IsSucking)
            {
                CancelInhale(true);
            }
        }
    }

    // --- 능력 발동 로직 ---

    // 1. 빨아들이기 선딜레이 시작 (PreInhale)
    public void PreInhale()
    {
        // 점프 입력을 즉시 멈춰 최대 점프 높이에 도달하는 것을 방지
        if (controller != null)
        {
            controller.StopJumpInput();
        }

        anim.SetBool("IsSucking", true);
        anim.SetTrigger("InhaleStart");

        // 선딜레이 코루틴 시작 (딜레이 후 StartInhale 호출)
        StartCoroutine(InhalePreDelay_co(inhalePreDelay));
    }

    // 1-1. Inhale 선딜레이 코루틴
    private IEnumerator InhalePreDelay_co(float duration)
    {
        IsPreInhaling = true;

        yield return new WaitForSeconds(duration);

        IsPreInhaling = false;

        // 딜레이가 끝나면 실제 흡입을 시작합니다.
        StartInhale();
    }
    public void StartInhale()
    {
        if (!isInputHeld)
        {
            // 애니메이션 상태도 초기화 (PreInhale에서 켜놓았을 수 있으므로)
            anim.SetBool("IsSucking", false);
            return;
        }
        IsSucking = true;

        if (AudioManager.Instance != null && inhaleLoopClip != null)
        {
            AudioManager.Instance.PlayLoopSFX(inhaleLoopClip);
        }
    }

    // 2. 물리 로직 (FixedUpdate에서 실행)
    private void HandleInhalePhysics()
    {
        // 1. 적 감지 및 끌어당기기
        if (currentInhaledEnemy == null)
        {
            DetectAndAttractEnemies();
        }
        else
        {
            AttractEnemyToMouth();
        }

        // 2. Z키를 뗐는데 아무도 안 빨린 경우: 실패 처리
        if (!isInputHeld && currentInhaledEnemy == null)
        {
            HandleInhaleFailure();
        }
    }

    // 3. 빨아들이기 취소 (Z 키를 떼거나, 다른 동작을 시도할 때)
    public void CancelInhale(bool isInputCancellation)
    {
        if (!IsSucking) return;

        IsSucking = false;
        anim.SetBool("IsSucking", false);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopSFX();
        }

        // 입력에 의해 취소된 경우 (Z 키 뗐을 때)
        if (isInputCancellation)
        {
            // 애니메이션: 취소 애니메이션 ("InhaleCancel")
            anim.SetTrigger("InhaleCancel");
        }

        /// 적 놓아주기 및 물리 복구
        if (currentTarget != null)
        {
            // ★ 인터페이스 메서드 호출 (상태 복구)
            currentTarget.OnInhaleCancel();

            // 충돌 무시 해제
            if (currentTarget.collider != null && kirbyCollider != null)
            {
                Physics2D.IgnoreCollision(currentTarget.collider, kirbyCollider, false);
            }

            currentInhaledEnemy = null;
            currentTarget = null;
        }
    }

    // 4. 빨아들이기 성공 (적을 삼켜 별을 머금은 상태로 전환)
    private void HandleInhaleSuccess()
    {
        IsSucking = false;
        HasStar = true;
        anim.SetBool("IsSucking", false);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopSFX();
        }

        anim.SetBool("HasStar", true);

        // 애니메이션: 빨아들이기 성공 애니메이션 ("InhaleSuccess")
        anim.SetTrigger("InhaleSuccess");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayInhaleSuccessSFX();
        }

        Debug.Log("빨아들이기 성공. 별을 머금었습니다.");
    }

    // 5. 빨아들이기 실패 (Z키를 뗐는데 아무도 안 빨렸을 때)
    private void HandleInhaleFailure()
    {
        if (!IsSucking) return;

        IsSucking = false;
        anim.SetBool("IsSucking", false);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopSFX();
        }

        // 애니메이션: 실패 애니메이션
        anim.SetTrigger("InhaleCancel"); // 취소 애니메이션을 실패로 재활용

        Debug.Log("빨아들이기 실패.");
    }

    // 6. 뱉어내기 및 삼키기
    public void SpitStar()
    {
        if (!HasStar) return;

        // 점프 입력 즉시 멈춤
        if (controller != null)
        {
            controller.StopJumpInput();
        }

        HasStar = false;
        anim.SetBool("HasStar", false);
        anim.SetTrigger("Spit");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySpitSFX();
        }


        StartCoroutine(AbilityDelay_co(0.3f));
    }

    public void LaunchStar()
    {
        // HasStar가 false인지 다시 확인 (SpitStar에서 이미 false로 설정됨)
        // 발사 위치 설정
        Vector3 spitPos = transform.position + new Vector3(transform.localScale.x * 0.5f, 0, 0);

        // 스타 프리팹 인스턴스화
        GameObject star = Instantiate(starPrefab, spitPos, Quaternion.identity);

        // Star_Projectile 스크립트 참조
        StarShooting starScript = star.GetComponent<StarShooting>();

        if (starScript != null)
        {
            // 커비가 바라보는 방향으로 Launch 함수 호출
            float direction = transform.localScale.x;

            // ★★★ Launch 함수 호출로 속도와 방향 설정 ★★★
            starScript.Launch(direction);
        }
        else
        {
            Debug.LogError("Star Prefab에 Star_Projectile 스크립트가 없습니다!");
            // 만약 스크립트가 없다면, 기존처럼 Rigidbody에 힘을 가합니다. (백업 로직)
            Rigidbody2D starRb = star.GetComponent<Rigidbody2D>();
            if (starRb != null)
            {
                starRb.linearVelocity = new Vector2(transform.localScale.x * starSpitForce, 0);
            }
        }
    }

    // IsDelaying 플래그를 Kirby_Controller의 입력 무시 로직과 연동해야 합니다. 
    private IEnumerator AbilityDelay_co(float duration) // 이름 변경: Delay_co -> AbilityDelay_co
    {
        IsDelaying = true; // ★ 플래그 활성화: 능력 입력 무시 시작
        yield return new WaitForSeconds(duration);
        IsDelaying = false; // 플래그 비활성화: 입력 다시 받기
    }

    public void SwallowEnemy()
    {
        if (!HasStar) return;

        HasStar = false;
        anim.SetBool("HasStar", false);
        // 능력 복사 로직
        if (lastInhaledEnemy != null && lastInhaledEnemy.HasAbility)
        {
            AbilityType copiedAbility = lastInhaledEnemy.Ability;

            Debug.Log($"능력 복사 중: {copiedAbility}");

            // ★ 능력 복사 연출 코루틴 시작
            StartCoroutine(AbilityCopyCutscene_co(copiedAbility));

            // 사용 후 초기화
            lastInhaledEnemy = null;
        }
        else
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySwallowSFX();
            }

            anim.SetTrigger("Swallow");
            Debug.Log("별 삼키기 완료. Normal 상태 유지.");
            StartCoroutine(AbilityDelay_co(0.5f));
        }
    }

    private IEnumerator AbilityCopyCutscene_co(AbilityType copiedAbility)
    {
        IsDelaying = true;

        if (timeStopOverlay != null)
        {
            timeStopOverlay.SetActive(true);
        }


        // 1. 게임 먼저 멈춤
        Time.timeScale = 0f;

        // 2. Animator를 Unscaled Time으로 변경
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySwallowSFX();
        }

        // 삼키기 애니메이션 (멈춘 상태에서 재생)
        anim.SetTrigger("Swallow");

        // ★ 4. 어두워지는 효과 (페이드 인)
        if (darkOverlay != null)
        {
            float fadeTimer = 0f;
            float targetAlpha = 0.6f; // 60% 어둡게

            while (fadeTimer < 0.3f) // 0.3초 동안 페이드
            {
                fadeTimer += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, targetAlpha, fadeTimer / 0.3f);

                Color color = darkOverlay.color;
                color.a = alpha;
                darkOverlay.color = color;

                yield return null;
            }
        }

        // 삼키기 애니메이션 대기 (realtime 사용)
        yield return new WaitForSecondsRealtime(0.3f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAbilityCopySFX();
        }

        // 능력 전환
        AbilityManager abilityManager = GetComponent<AbilityManager>();
        if (abilityManager != null)
        {
            abilityManager.CopyAbility(copiedAbility);
        }

        // 능력 복사 연출 대기
        yield return new WaitForSecondsRealtime(0.3f);

        if (darkOverlay != null)
        {
            float fadeTimer = 0f;
            float startAlpha = darkOverlay.color.a;

            while (fadeTimer < 0.3f) // 0.3초 동안 페이드
            {
                fadeTimer += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, fadeTimer / 0.3f);

                Color color = darkOverlay.color;
                color.a = alpha;
                darkOverlay.color = color;

                yield return null;
            }

            // 완전히 투명하게
            Color finalColor = darkOverlay.color;
            finalColor.a = 0f;
            darkOverlay.color = finalColor;
        }

        // 9. 오버레이 비활성화
        if (timeStopOverlay != null)
        {
            timeStopOverlay.SetActive(false);
        }


        // Animator를 다시 Normal로 복구
        anim.updateMode = AnimatorUpdateMode.Normal;

        // 게임 재개
        Time.timeScale = 1f;

        IsDelaying = false;
    }

    private void DetectAndAttractEnemies()
    {
        // 커비가 바라보는 방향 (1 또는 -1)
        float direction = transform.localScale.x;

        // 1. OverlapBox의 크기와 위치 설정 (흡입 입구 역할)
        // Box의 폭: inhaleRange 만큼 설정 (흡입이 미치는 최대 거리)
        // Box의 높이: 커비의 크기나 원하는 흡입 높이에 따라 조절 (예: 1.0f)
        Vector2 boxSize = new Vector2(inhaleRange, 0.5f);

        // Box의 중심 위치: 
        // 커비 위치(rb.position) + (방향 * (OverlapBox의 폭/2))
        // 즉, 커비 몸통에서 흡입 박스 길이의 절반만큼 떨어진 정면 위치
        Vector2 boxCenter = rb.position + new Vector2(direction * boxSize.x / 2.0f, 0f);

        // 2. OverlapBox로 적 탐지 (0f는 회전 각도)
        Collider2D hit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, enemyLayer);

        // Debugging을 위해 탐지 영역 그리기
        // Debug.DrawRay(boxCenter - new Vector2(boxSize.x/2 * direction, boxSize.y/2), new Vector2(boxSize.x * direction, 0), Color.yellow);
        if (hit != null && hit.CompareTag("Enemy"))
        {
            IInhalable inhalable = hit.GetComponent<IInhalable>();

            if (inhalable != null)
            {
                currentInhaledEnemy = hit; // (물리 제어용으로 Collider2D는 필요할 수 있음)
                currentTarget = inhalable; // 인터페이스 저장

                // 인터페이스 메서드 호출 -> 적 스스로 상태 변경 (애니메이션 재생 등)
                inhalable.OnInhaleStart();

                // 커비 측에서의 물리 제어 (필요한 경우)
                if (inhalable.rb != null)
                {
                    inhalable.rb.linearVelocity = Vector2.zero;
                }

                // 충돌 무시
                if (inhalable.collider != null && kirbyCollider != null)
                {
                    Physics2D.IgnoreCollision(inhalable.collider, kirbyCollider, true);
                }
            }
        }
    }

    private void AttractEnemyToMouth()
    {
        if (currentTarget == null || currentTarget.gameObject == null) // 객체가 파괴되었는지 확인
        {
            CancelInhale(false);
            return;
        }

        // 인터페이스를 통해 Transform 접근
        Transform targetTransform = currentTarget.transform;

        Vector3 targetPos = transform.position;
        float step = inhaleAttractionSpeed * Time.fixedDeltaTime;

        targetTransform.position = Vector3.MoveTowards(targetTransform.position, targetPos, step);

        float distance = Vector3.Distance(targetTransform.position, targetPos);

        if (distance < 0.1f)
        {
            lastInhaledEnemy = currentTarget;

            currentTarget.OnSwallowed(); // 적이 스스로 Destroy 처리

            currentInhaledEnemy = null;
            currentTarget = null; // 참조 해제

            HandleInhaleSuccess();
        }
    }
}