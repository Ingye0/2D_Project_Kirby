using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Ability { Normal, Beam, Spark }

public class Kirby_Controller : MonoBehaviour
{
    [SerializeField] private float JumpForce = 1100f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float doubleTapTimeWindow = 0.5f;

    [Header("점프 지속")]
    [SerializeField] private float jumpDuration = 0.15f;
    [SerializeField] private float variableJumpForce = 150f;

    [Header("체력 및 피격")]
    public int health = 11;
    [SerializeField] private float invincibilityDuration = 2.0f; // 피격 후 무적 시간(초)
    [SerializeField] private float knockbackForce = 25f; // 피격 시 밀려나는 힘

    [Header("관성")]
    [SerializeField] private float groundDeceleration = 5f;
    [SerializeField] private float skidDecelerationWalk = 20f;
    [SerializeField] private float skidDecelerationRun = 50f;
    [SerializeField] private float accelerationRateWalk = 80f;
    [SerializeField] private float accelerationRateRun = 80f;

    [Header("움직임 제한")]
    [SerializeField] private float maxFallSpeed = -8f;

    [Header("풍선상태")]
    [SerializeField] private float floatFallSpeed = -3f;
    [SerializeField] private float floatRiseSpeed = 6f;
    [SerializeField] private float floatMoveFactor = 0.8f;
    [SerializeField] private float floatJumpCooldown = 0.3f;
    [SerializeField] private float floatSoundInterval = 0.4f;

    [Header("오래 낙하 & 박치기")]
    [SerializeField] private float longFallTimeThreshold = 0.6f;
    [SerializeField] private float headBumpForce = 1000f;

    [Header("태클")]
    [SerializeField] private float tackleSpeed = 15f;
    [SerializeField] private float tackleDuration = 0.5f;
    [SerializeField] private int tackleDamage = 1;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Grounded 감지")]
    [SerializeField] private LayerMask GroundLayer;
    [SerializeField] private float raycastDistance = 0.1f;
    [SerializeField] private LayerMask WallLayer;

    [Header("카메라 경계")]
    [SerializeField] private Camera mainCamera;
    private float halfWidth;
    private float halfHeight;


    // 상태 변수
    public bool isGrounded = false;
    public bool isWalking = false;
    public bool isRunning = false;
    public bool isJumping = false;
    public bool isSkidding = false;
    private bool wasSkidding = false;
    public bool isFloating = false;
    public bool isBurping = false;
    public bool isWallHit = false;
    public bool isLongFalling = false;
    public bool isDucking = false;
    public bool isTackling = false;
    public bool wasGrounded = false;
    public bool isInvincible = false;
    public bool isHitStop = false;
    public bool isKnockedBack = false;

    public Normal normal;
    public Beam beam;
    public Spark spark;

    // ★ 경사로 처리를 위한 충돌 정보
    private RaycastHit2D groundHit;

    private float verticalvelocity;
    private float moveInput;
    private float jumpTimeCounter;

    private float moveSpeed = 5f;
    private float lastTapTime = 0f;
    private float currentDirection = 0f;
    private float runStartTime = 0f;
    private float fallTimeCounter = 0f;
    private float tackleTimer = 0f;
    private float fixedTackleDirection = 0f;
    private float fixedHitDirection = 0f;
    private float lastFloatJumpTime = 0f;
    private float lastFloatSoundTime = 0f;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    private Rigidbody2D Kirby_R;
    private AudioSource audio;
    private Collider2D Kirby_C;

    public RuntimeAnimatorController normalAnim; //노말애니메이션
    public RuntimeAnimatorController beamAnim; //빔애니메이션
    public RuntimeAnimatorController sparkAnim; //스파크애니메이션

    private void Start()
    {
        Kirby_R = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audio = GetComponent<AudioSource>();
        Kirby_C = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        normal = GetComponent<Normal>();
        beam = GetComponent<Beam>();
        spark = GetComponent<Spark>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        halfHeight = mainCamera.orthographicSize;
        halfWidth = halfHeight * mainCamera.aspect;
    }

    private void Update()
    {
        HandleBurpInput();
        Jump();
        Duck();
        Tackle();
        HandleRunInput();
        CheckRunEnd();
        LongFall();
        AnimatorParameters();
    }

    void FixedUpdate()
    {
        bool currentlyGrounded = IsGrounded();

        // 착지 판정
        if (!wasGrounded && currentlyGrounded)
        {
            isJumping = false;
            if (isLongFalling)
            {
                isLongFalling = false;
                animator.SetTrigger("HeadBump");
                Kirby_R.linearVelocity = new Vector2(Kirby_R.linearVelocity.x, 0);
                Kirby_R.AddForce(Vector2.up * headBumpForce);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayLandSFX();
                }
            }
        }

        wasGrounded = currentlyGrounded;
        isGrounded = currentlyGrounded;

        Movement();
        ClampPositionToCamera();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {


        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            // 적 Layer에 닿았을 때
            TakeDamage(1, collision.transform.position);
            return; // 다른 충돌 로직 무시
        }

        // 풍선 상태에서는 벽 충돌 로직 무시
        if (isFloating)
        {
            return;
        }

        // WallLayer에 충돌했을 때
        if ((WallLayer & (1 << collision.gameObject.layer)) != 0)
        {
            animator.SetTrigger("WallHit");
            isWallHit = true;
            isRunning = false;
            isWalking = false;
            isSkidding = false;

            Kirby_R.linearVelocity = new Vector2(0, Kirby_R.linearVelocity.y);
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if ((WallLayer & (1 << collision.gameObject.layer)) != 0)
        {
            isWallHit = false;
        }
    }

    public bool IsGrounded()
    {
        if (Kirby_C == null) return false;

        Bounds bounds = Kirby_C.bounds;
        float extraDistance = raycastDistance;

        Vector2 leftOrigin = new Vector2(bounds.min.x + 0.05f, bounds.min.y);
        Vector2 centerOrigin = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 rightOrigin = new Vector2(bounds.max.x - 0.05f, bounds.min.y);

        RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.down, extraDistance, GroundLayer);
        RaycastHit2D hitCenter = Physics2D.Raycast(centerOrigin, Vector2.down, extraDistance, GroundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.down, extraDistance, GroundLayer);

        Debug.DrawRay(leftOrigin, Vector2.down * extraDistance, hitLeft.collider ? Color.green : Color.red);
        Debug.DrawRay(centerOrigin, Vector2.down * extraDistance, hitCenter.collider ? Color.green : Color.red);
        Debug.DrawRay(rightOrigin, Vector2.down * extraDistance, hitRight.collider ? Color.green : Color.red);

        bool hitGround = (hitLeft.collider != null || hitCenter.collider != null || hitRight.collider != null);

        if (hitGround)
        {
            // 중앙 충돌 정보를 우선 저장하고, 없으면 좌/우 충돌 정보를 저장
            if (hitCenter.collider != null)
            {
                groundHit = hitCenter;
            }
            else if (hitLeft.collider != null)
            {
                groundHit = hitLeft;
            }
            else if (hitRight.collider != null)
            {
                groundHit = hitRight;
            }
            return true;
        }

        groundHit = new RaycastHit2D(); // 땅에 닿지 않았으면 초기화
        return false;
    }


    private void HandleRunInput()
    {
        if (isKnockedBack || isFloating || isDucking || isTackling || isBurping ||
            (normal != null && (normal.IsSucking || normal.IsPreInhaling || normal.IsDelaying)) ||
            (beam != null && beam.IsAttacking) ||
            (spark != null && spark.IsAttacking))
        {
            return;
        }

        float inputKeyDirection = 0f;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            inputKeyDirection = -1f;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            inputKeyDirection = 1f;
        }

        if (inputKeyDirection != 0f)
        {
            if (Time.time < lastTapTime + doubleTapTimeWindow && Mathf.Approximately(inputKeyDirection, currentDirection))
            {
                if (!isRunning)
                {
                    isRunning = true;
                    runStartTime = Time.time;

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayRunSFX();
                    }
                }
            }
            lastTapTime = Time.time;
            currentDirection = inputKeyDirection;
        }
    }

    private void HandleBurpInput()
    {
        // 이동이나 다른 능력 입력 차단 상태 확인 (Normal 스크립트의 딜레이 중에는 막힘)
        if (normal != null && (normal.IsSucking || normal.IsPreInhaling || normal.IsDelaying))
        {
            return;
        }

        // 1Z키 입력과 Floating 상태 확인
        if (isFloating && Input.GetKeyDown(KeyCode.Z))
        {
            // 상태 전환 및 애니메이션 트리거
            isFloating = false;
            animator.SetTrigger("Burping");

            isBurping = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBurpSFX();
            }

            // 딜레이 코루틴 시작
            StartCoroutine(BurpNOTTackle_co(0.2f));

            // Burping이 시작되었으므로 여기서 함수 종료
            return;
        }
    }

    private void CheckRunEnd()
    {
        if (!isRunning) return;
        float currentMoveInput = Input.GetAxisRaw("Horizontal");

        if (isSkidding)
            return;

        if (Mathf.Approximately(currentMoveInput, 0f))
        {
            isRunning = false;
        }
    }

    private void AnimatorParameters()
    {
        verticalvelocity = Kirby_R.linearVelocity.y;

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalVelocity", verticalvelocity);
        animator.SetFloat("MoveSpeed", Mathf.Abs(Kirby_R.linearVelocity.x));
        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsRunning", isRunning);
        animator.SetBool("IsSkidding", isSkidding);
        animator.SetBool("IsFloating", isFloating);
        animator.SetBool("IsLongFalling", isLongFalling);
        animator.SetBool("IsDucking", isDucking);
        animator.SetBool("IsTackling", isTackling);
        animator.SetBool("IsBurping", isBurping);
        if (normal != null)
        {
            animator.SetBool("HasStar", normal.HasStar);
        }
    }

    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        if (isInvincible) return;

        health -= damage;
        Debug.Log("커비가 대미지받음: " + damage + ". 남은 체력: " + health);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDamageSFX();
        }
        CameraController.Instance.ShakeCamera(0.2f, 0.5f);

        // 능력 상실
        AbilityManager abilityManager = GetComponent<AbilityManager>();
        if (abilityManager != null && abilityManager.GetCurrentAbility() != AbilityType.Normal)
        {
            abilityManager.ResetToNormal();
            Debug.Log("피격으로 인해 능력 상실!");
        }

        // Hit 애니메이션 트리거
        animator.SetTrigger("Hit");

        if (health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HitAndKnockback_co(attackerPosition));
            StartCoroutine(Invincibility_co(invincibilityDuration));
        }
    }
    private void Die()
    {
        Debug.Log("Kirby is Defeated! Game Over.");
    }

    private IEnumerator Invincibility_co(float duration)
    {
        isInvincible = true;

        // 깜빡임 속도 (초당 횟수)
        float blinkRate = 0.05f; // 0.05초마다 색상 전환 (빠르게)
        Color hitColor = new Color(2f, 2f, 0.5f); // 노란색으로 설정
        Color originalColor = Color.white;

        float startTime = Time.time;

        // 무적 시간 동안 반복
        while (Time.time < startTime + duration)
        {
            // 현재 시간에 따라 색상 토글 (노란색 <-> 원래 색상)
            if (((int)(Time.time / blinkRate)) % 2 == 0)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = hitColor;
                }
            }
            else
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }
            }
            yield return null; // 다음 프레임까지 대기
        }

        // 무적 시간이 끝나면 원래 색상으로 복구
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        isInvincible = false;
    }

    private IEnumerator HitAndKnockback_co(Vector3 attackerPosition)
    {
        isKnockedBack = true; // 넉백 시작 시
        // 적의 위치와 커비의 위치 차이를 계산
        float directionToAttacker = Mathf.Sign(attackerPosition.x - transform.position.x);

        //  피격된 순간, 적을 바라보는 방향으로 fixedHitDirection을 설정
        fixedHitDirection = directionToAttacker;

        // 커비의 스프라이트를 적을 바라보는 방향으로 설정
        transform.localScale = new Vector3(directionToAttacker, 1, 1);

        // 넉백 적용
        float knockbackDirectionX = -directionToAttacker;

        // 현재 수직 속도를 보존 (낙하 중이었다면 그대로)
        float currentVY = Kirby_R.linearVelocity.y;

        // 속도 초기화
        Kirby_R.linearVelocity = new Vector2(0f, currentVY);

        // Impulse 적용
        Vector2 knockbackVector = new Vector2(knockbackDirectionX, 0f).normalized;
        Kirby_R.AddForce(knockbackVector * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.7f); // 넉백 지속 시간

        fixedHitDirection = 0f; //넉백 및 고정 해제
        isKnockedBack = false; // 넉백 끝난 후
    }
    private IEnumerator LoseAbilityAfterHit_co()
    {
        // Hit 애니메이션이 재생될 시간을 줌 (0.2~0.3초)
        yield return new WaitForSeconds(0.2f);

        AbilityManager abilityManager = GetComponent<AbilityManager>();
        if (abilityManager != null)
        {
            // Normal이 아닌 능력을 가지고 있으면 Normal로
            if (abilityManager.GetCurrentAbility() != AbilityType.Normal)
            {
                abilityManager.ResetToNormal();
                Debug.Log("피격으로 인해 능력 상실!");
            }
        }
    }

    private void Jump()
    {
        if (isBurping || isKnockedBack)
        {
            return;
        }

        if (isTackling ||
            (normal != null && (normal.IsSucking || normal.IsPreInhaling || normal.IsDelaying)) ||
              (beam != null && beam.IsAttacking) ||
               (spark != null && spark.IsAttacking))
        {
            return;
        }

        bool isFloatingKeyHeld = Input.GetKey(KeyCode.X) ||
                                 Input.GetKey(KeyCode.W) ||
                                 Input.GetKey(KeyCode.UpArrow);

        bool shouldBeFloatingJumping = isFloating && isFloatingKeyHeld;

        // Floating 상태 지속 상승 로직
        if (shouldBeFloatingJumping)
        {
            Kirby_R.linearVelocity = new Vector2(Kirby_R.linearVelocity.x, floatRiseSpeed);

            // 키를 누르고 있는 동안 주기적으로 효과음 재생
            if (Time.time >= lastFloatSoundTime + floatSoundInterval)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayFloatSFX();
                }
                lastFloatSoundTime = Time.time;
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (isFloating)
            {
                // 쿨타임 체크
                if (Time.time >= lastFloatJumpTime + floatJumpCooldown)
                {
                    Kirby_R.linearVelocity = new Vector2(Kirby_R.linearVelocity.x, floatRiseSpeed);
                    lastFloatJumpTime = Time.time;
                }
            }
            else if (isGrounded)
            {
                if (isDucking)
                {
                    return;
                }

                // 지면 점프
                Kirby_R.linearVelocity = new Vector2(Kirby_R.linearVelocity.x, 0);
                Kirby_R.AddForce(new Vector2(0, JumpForce));
                isSkidding = false;

                isJumping = true;
                jumpTimeCounter = jumpDuration;

                // 점프 효과음
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayJumpSFX();
                }
            }
            else // 공중에서 풍선 진입
            {
                if (normal != null && !normal.HasStar)
                {
                    isFloating = true;
                    isRunning = false;

                    float floatMaxHorizontalSpeed = moveSpeed * floatMoveFactor;
                    float currentVelocityX = Kirby_R.linearVelocity.x;

                    float limitedVelocityX = Mathf.Min(
                        Mathf.Abs(currentVelocityX),
                        floatMaxHorizontalSpeed
                    ) * Mathf.Sign(currentVelocityX);

                    Kirby_R.linearVelocity = new Vector2(limitedVelocityX, floatRiseSpeed);

                    lastFloatJumpTime = Time.time;

                    // 풍선 진입 효과음 (처음 진입할 때만)
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayFloatSFX();
                    }
                    lastFloatSoundTime = Time.time;
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.X))
        {
            isJumping = false;
        }

        animator.SetBool("IsFloatingJumping", shouldBeFloatingJumping);
    }

    private IEnumerator BurpNOTTackle_co(float duration)
    {
        yield return new WaitForSeconds(duration);
        isBurping = false;
    }


    private void LongFall()
    {
        if (isKnockedBack || isFloating ||
             (normal != null && (normal.IsSucking || normal.HasStar)) ||
             (beam != null && beam.IsAttacking) ||
             (spark != null && spark.IsAttacking))
        {
            fallTimeCounter = 0f;
            isLongFalling = false;
            return;
        }

        if (!isGrounded)
        {
            if (Kirby_R.linearVelocity.y < 0)
            {
                fallTimeCounter += Time.deltaTime;

                if (fallTimeCounter >= longFallTimeThreshold)
                {
                    isLongFalling = true;
                }
            }
            else if (Kirby_R.linearVelocity.y > 0)
            {
                fallTimeCounter = 0f;
                isLongFalling = false;
            }
        }
        else
        {
            fallTimeCounter = 0f;
            isLongFalling = false;
        }
    }

    private void Duck()
    {
        if (isKnockedBack || !isGrounded || isFloating || isTackling ||
             (normal != null && (normal.IsSucking || normal.HasStar || normal.IsPreInhaling || normal.IsDelaying)) ||
             (beam != null && beam.IsAttacking) ||
             (spark != null && spark.IsAttacking))
        {
            isDucking = false;
            return;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            isDucking = true;
        }
        else
        {
            isDucking = false;
        }
    }

    private void Tackle()
    {
        if (isKnockedBack || isTackling || !isGrounded || isFloating || isBurping ||
             (normal != null && (normal.IsSucking || normal.HasStar || normal.IsPreInhaling || normal.IsDelaying)) ||
             (beam != null && beam.IsAttacking) ||
             (spark != null && spark.IsAttacking))
        {
            return;
        }

        if (isDucking && (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X)))
        {
            isTackling = true;
            tackleTimer = tackleDuration;
            isDucking = false;
            isSkidding = false;

            float direction = transform.localScale.x;

            Kirby_R.linearVelocity = new Vector2(direction * tackleSpeed, Kirby_R.linearVelocity.y);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayTackleSFX();
            }
        }
    }

    public void StopJumpInput()
    {
        isJumping = false;
    }

    private void Movement()
    {
        if (isHitStop)
        {
            Kirby_R.linearVelocity = Vector2.zero;
            return;
        }

        if (isKnockedBack)
        {
            return;
        }

        float currentVelocityX = Kirby_R.linearVelocity.x;

        if (isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                Kirby_R.AddForce(Vector2.up * variableJumpForce, ForceMode2D.Force);
                jumpTimeCounter -= Time.fixedDeltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

        if (isTackling)
        {
            tackleTimer -= Time.fixedDeltaTime;
            TackleAttack();

            if (!isTackling) return;

            if (!isGrounded)
            {
                tackleTimer = 0f;
            }

            if (tackleTimer <= 0)
            {
                isTackling = false;

                if (isGrounded)
                {
                    Kirby_R.linearVelocity = new Vector2(0, Kirby_R.linearVelocity.y);
                    currentVelocityX = 0f;
                }
            }
            else
            {
                float direction = transform.localScale.x;
                Kirby_R.linearVelocity = new Vector2(direction * tackleSpeed, Kirby_R.linearVelocity.y);
            }
            if (isTackling) return;
        }

        bool abilityOrTackleActive =
            (normal != null && (normal.IsSucking || normal.IsPreInhaling || normal.IsDelaying)) ||
            (beam != null && beam.IsAttacking) ||
            (spark != null && spark.IsAttacking) ||
            isBurping;

        if (abilityOrTackleActive)
        {
            moveInput = 0; // 새로운 입력은 무시
        }
        else if (isDucking)
        {
            moveInput = Input.GetAxisRaw("Horizontal");

            if (moveInput != 0)
            {
                transform.localScale = new Vector3(moveInput, 1, 1);
            }
        }
        else
        {
            moveInput = Input.GetAxisRaw("Horizontal");
        }


        if (isDucking && Mathf.Abs(currentVelocityX) > 0.1f && !isTackling)
        {
            isSkidding = true;
        }

        float baseSpeed = isFloating ? moveSpeed * floatMoveFactor : moveSpeed;
        float currentSpeed = isRunning ? runSpeed : baseSpeed;


        // 2. 관성 판정 및 상태 업데이트 
        bool shouldSkidNow = (Mathf.Abs(currentVelocityX) > moveSpeed * 0.5f) &&
                             (Mathf.Sign(moveInput) != Mathf.Sign(currentVelocityX)) &&
                             (Mathf.Abs(moveInput) > 0.1f) && isGrounded && !isSkidding;

        if (shouldSkidNow)
        {
            isSkidding = true;

            if (!wasSkidding && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySkidSFX();
            }
        }
        else if (isSkidding && (Mathf.Abs(currentVelocityX) < 0.001f || !isGrounded))
        {
            isSkidding = false;
        }

        wasSkidding = isSkidding;

        // 속도 적용: Target과 Acceleration Rate 결정 
        float targetSpeedX = 0f;
        float finalAccelerationRate = 0f;

        if (isSkidding)
        {
            finalAccelerationRate = isRunning ? skidDecelerationRun : skidDecelerationWalk;
            targetSpeedX = 0;
        }
        else if (moveInput == 0 || isDucking || (normal != null && normal.IsSucking))
        {
            finalAccelerationRate = groundDeceleration;
            targetSpeedX = 0;
        }
        else // if (moveInput != 0)
        {
            finalAccelerationRate = isRunning ? accelerationRateRun : accelerationRateWalk;
            targetSpeedX = moveInput * currentSpeed;
        }

        // 최종 수평 속도 계산
        float newVelocityX = Mathf.MoveTowards(
            currentVelocityX,
            targetSpeedX,
            finalAccelerationRate * Time.fixedDeltaTime
        );


        // 수직 속도 (낙하 속도/풍선 상태 속도) 제어
        float newVelocityY = Kirby_R.linearVelocity.y;

        if (isFloating)
        {
            if (newVelocityY < floatFallSpeed)
            {
                newVelocityY = floatFallSpeed;
            }
        }
        else
        {
            if (newVelocityY < maxFallSpeed)
            {
                newVelocityY = maxFallSpeed;
            }
        }

        // 경사로 이동 로직
        if (isGrounded && !isJumping && !isSkidding && !isTackling && groundHit.collider != null)
        {
            Vector2 slopeNormal = groundHit.normal;

            // 경사면의 각도가 너무 가파르지 않은지 확인 (약 45도 이하)
            if (slopeNormal.y > 0.707f)
            {
                // 경사면(기울어진 땅)일 경우
                if (!Mathf.Approximately(slopeNormal.y, 1.0f))
                {
                    Vector2 slopeDirection = Vector2.Perpendicular(slopeNormal).normalized;
                    if (Mathf.Sign(slopeDirection.x) != Mathf.Sign(newVelocityX))
                    {
                        slopeDirection = -slopeDirection;
                    }
                    float moveMagnitude = Mathf.Abs(newVelocityX);
                    Vector2 finalVelocity = slopeDirection * moveMagnitude;

                    newVelocityX = finalVelocity.x;
                    newVelocityY = finalVelocity.y;
                }
            }
        }

        // Rigidbody에 최종 속도 적용
        Kirby_R.linearVelocity = new Vector2(newVelocityX, newVelocityY);


        if (Mathf.Abs(moveInput) > 0.1f && !isRunning && !isSkidding)
        {
            isWalking = true;
        }
        else
        {
            isWalking = false;
        }

        // 캐릭터 방향 전환 
        float directionToFace = 0f;

        if (fixedTackleDirection != 0f) // fixedTackleDirection이 설정되어 있으면
        {
            directionToFace = fixedTackleDirection; // 저장된 방향으로 강제 고정
        }
        else if (fixedHitDirection != 0f)
        {
            directionToFace = fixedHitDirection; // 피격 고정 방향으로 강제 설정
        }
        else if (isSkidding)
        {
            if (Mathf.Abs(currentVelocityX) > 0.01f)
            {
                directionToFace = Mathf.Sign(currentVelocityX);
            }
        }
        else
        {
            if (moveInput != 0)
            {
                directionToFace = moveInput;
            }
            else if (Mathf.Abs(currentVelocityX) > 0.01f)
            {
                directionToFace = Mathf.Sign(currentVelocityX);
            }
        }

        if (directionToFace != 0)
        {
            transform.localScale = new Vector3(directionToFace, 1, 1);
        }
    }

    private void ClampPositionToCamera()
    {
        if (mainCamera == null) return;

        Vector3 cameraCenter = mainCamera.transform.position;
        Vector3 currentPos = transform.position;

        float minX = cameraCenter.x - halfWidth;
        float maxX = cameraCenter.x + halfWidth;
        float minY = cameraCenter.y - halfHeight;
        float maxY = cameraCenter.y + halfHeight;

        currentPos.x = Mathf.Clamp(currentPos.x, minX, maxX);
        currentPos.y = Mathf.Clamp(currentPos.y, minY, maxY);

        transform.position = currentPos;
    }

    private void TackleAttack()
    {
        float attackDistance = 0.5f;
        Vector2 rayOrigin = Kirby_R.position + new Vector2(Kirby_C.bounds.extents.x * transform.localScale.x, 0);

        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            Vector2.right * transform.localScale.x,
            attackDistance,
            enemyLayer
        );
        
        if (hit.collider != null)
        {
            IInhalable enemy = hit.collider.GetComponent<IInhalable>();

            if (enemy != null)
            {
                // 적에게 대미지 적용
                Debug.Log($"적 충돌! {hit.collider.name}에게 {tackleDamage} 대미지 적용.");
                enemy.TakeDamage(tackleDamage);


                Kirby_R.linearVelocity = Vector2.zero;

                StartCoroutine(TackleHitStopAndKnockback_co(0.2f));
            }
            else
            {
                Debug.Log($"적 충돌! {hit.collider.name}는 enemy 스크립트가 없습니다.");
            }
        }
    }

    private IEnumerator TackleHitStopAndKnockback_co(float hitStopDuration)
    {
        // 고정할 방향 저장 (태클을 시작한 방향)
        fixedTackleDirection = transform.localScale.x;
        isHitStop = true;

        yield return new WaitForSeconds(hitStopDuration);

        isHitStop = false; // 멈춤 해제

        isTackling = false;
        tackleTimer = 0f;
        // 멈춤이 끝난 후 순수 반동 적용
        float knockbackForce = 30f; 
        float tackleDirection = transform.localScale.x;

        // 속도 초기화 (혹시 모를 잔여 속도 제거)
        Kirby_R.linearVelocity = Vector2.zero;

        // 45도 반동 벡터
        float bounceX = -tackleDirection;
        Vector2 bounceDirection = new Vector2(bounceX, 1f).normalized;

        // Impulse 적용
        Kirby_R.AddForce(bounceDirection * knockbackForce, ForceMode2D.Impulse);

        isGrounded = false;

        yield return new WaitForSeconds(0.6f);

        fixedTackleDirection = 0f; // 방향 고정 해제
    }

    public void ChangeToBeamAbility()
    {
        // Normal 제거
        Normal normal = GetComponent<Normal>();
        if (normal != null)
            Destroy(normal);

        // Beam 추가
        if (GetComponent<Beam>() == null)
            gameObject.AddComponent<Beam>();

        // 애니메이터 교체
        Animator anim = GetComponent<Animator>();
        anim.runtimeAnimatorController = beamAnim;
    }
}