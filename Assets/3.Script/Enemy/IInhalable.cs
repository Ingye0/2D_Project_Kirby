using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInhalable
{
    // 흡입 시작 시 호출 (빨려가는 애니메이션, 이동 정지 등)
    void OnInhaleStart();

    // 흡입 취소/중단 시 호출 (원래 상태로 복귀)
    void OnInhaleCancel();

    // 완전히 삼켜졌을 때 호출 (제거 등)
    void OnSwallowed();

    // 대미지를 입을 때 호출
    void TakeDamage(int damage);

    // 현재 오브젝트의 Transform을 반환
    Transform transform { get; }
    GameObject gameObject { get; }
    Rigidbody2D rb { get; }
    Collider2D collider { get; }

    bool HasAbility { get; }
    AbilityType Ability { get; }
}
