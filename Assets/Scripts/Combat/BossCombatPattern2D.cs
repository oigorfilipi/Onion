using UnityEngine;

public sealed class BossCombatPattern2D : MonoBehaviour
{
    private enum AttackPhase
    {
        FireVolley,
        PauseBeforeDash,
        DashPunch,
        Recovery
    }

    [SerializeField] private EnemyFireShooter2D fireShooter;
    [SerializeField] private EnemyDashPunch2D dashPunch;
    [SerializeField, Min(0.1f)] private float fireVolleyDuration = 3.6f;
    [SerializeField, Min(0f)] private float pauseBeforeDash = 0.7f;
    [SerializeField, Min(0.1f)] private float dashWindowDuration = 1.4f;
    [SerializeField, Min(0f)] private float recoveryDuration = 1.3f;

    private AttackPhase currentPhase;
    private float phaseStartedAt;
    private bool dashAttackStarted;
    private bool patternStarted;

    public void BeginPattern()
    {
        if (!enabled)
        {
            enabled = true;
        }

        patternStarted = true;
        EnterPhase(AttackPhase.FireVolley);
    }

    private void Update()
    {
        if (!patternStarted || Time.timeScale == 0f)
        {
            return;
        }

        float phaseDuration = Time.time - phaseStartedAt;
        switch (currentPhase)
        {
            case AttackPhase.FireVolley:
                if (phaseDuration >= fireVolleyDuration)
                {
                    EnterPhase(AttackPhase.PauseBeforeDash);
                }
                break;

            case AttackPhase.PauseBeforeDash:
                if (phaseDuration >= pauseBeforeDash)
                {
                    EnterPhase(AttackPhase.DashPunch);
                }
                break;

            case AttackPhase.DashPunch:
                if (dashPunch != null && dashPunch.IsAttacking)
                {
                    dashAttackStarted = true;
                    return;
                }

                if (dashAttackStarted || phaseDuration >= dashWindowDuration)
                {
                    EnterPhase(AttackPhase.Recovery);
                }
                break;

            case AttackPhase.Recovery:
                if (phaseDuration >= recoveryDuration)
                {
                    EnterPhase(AttackPhase.FireVolley);
                }
                break;
        }
    }

    private void OnDisable()
    {
        patternStarted = false;
        SetAttackWindows(false, false);
    }

    private void EnterPhase(AttackPhase phase)
    {
        currentPhase = phase;
        phaseStartedAt = Time.time;
        dashAttackStarted = false;

        switch (phase)
        {
            case AttackPhase.FireVolley:
                SetAttackWindows(true, false);
                break;
            case AttackPhase.DashPunch:
                SetAttackWindows(false, true);
                break;
            default:
                SetAttackWindows(false, false);
                break;
        }
    }

    private void SetAttackWindows(bool fireOpen, bool dashOpen)
    {
        if (fireShooter != null)
        {
            fireShooter.SetAttackWindowOpen(fireOpen);
        }

        if (dashPunch != null)
        {
            dashPunch.SetAttackWindowOpen(dashOpen);
        }
    }
}
