using UnityEngine;
using System;

/// <summary>
/// QTE 결과에 따른 데미지를
/// Animation Event 타이밍에 맞춰 적용하는 중앙 Resolver
/// </summary>
public class QTEDamageResolver : MonoBehaviour
{
    private Action<PrisonerHitTimingEvent> _onPrisonerHit;
    private Action<PlayerHitTimingEvent> _onPlayerHit;

    private void Awake()
    {
        _onPrisonerHit = OnPrisonerHit;
        _onPlayerHit = OnPlayerHit;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPrisonerHit);
        EventBus.Subscribe(_onPlayerHit);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPrisonerHit);
        EventBus.Unsubscribe(_onPlayerHit);
    }

    // =========================
    // QTE 성공 → 죄수 피해
    // =========================
    private void OnPrisonerHit(PrisonerHitTimingEvent e)
    {
        // ★ 이미 데미지 처리했으면 어떤 이벤트가 또 와도 무시
        if (PrisonerQTEContext.DamageConsumed)
            return;

        if (PrisonerQTEContext.CurrentResult != QTEResult.Success)
            return;

        var action = PrisonerQTEContext.CurrentAction;
        if (action == null)
            return;

        var attackerGO = PrisonerQTEContext.CurrentAttacker;
        if (attackerGO == null)
            return;

        var controller = attackerGO.GetComponent<PrisonerController>();
        if (controller == null)
            return;

        int damage = action.damageToPrisonerOnSuccess;
        if (damage <= 0)
            return;

        controller.ApplyDamage(
            damage,
            attackerGO.transform.position,
            -attackerGO.transform.forward
        );

        // ★ 여기서 "1회 소비" 확정
        PrisonerQTEContext.DamageConsumed = true;

        // 선택: QTE 종료 직후 애니 이벤트가 더 올 수 있으니 즉시 클리어해도 안전
        PrisonerQTEContext.Clear();
    }

    // =========================
    // QTE 실패 → 플레이어 피해
    // =========================
    private void OnPlayerHit(PlayerHitTimingEvent e)
    {
        if (PrisonerQTEContext.DamageConsumed)
            return;

        if (PrisonerQTEContext.CurrentResult != QTEResult.Fail &&
            PrisonerQTEContext.CurrentResult != QTEResult.Timeout)
            return;

        var action = PrisonerQTEContext.CurrentAction;
        if (action == null)
            return;

        var player = GameObject.FindWithTag("Player");
        if (player == null)
            return;

        var health = player.GetComponent<Health>();
        if (health == null)
            return;

        int damage = action.damageToPlayerOnFail;
        if (damage <= 0)
            return;

        health.TakeDamage(damage);

        PrisonerQTEContext.DamageConsumed = true;
        PrisonerQTEContext.Clear();
    }
}





