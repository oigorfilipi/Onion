using UnityEngine;

[RequireComponent(typeof(EnemyHealth2D))]
public sealed class EnemyExperienceReward2D : MonoBehaviour
{
    [SerializeField, Min(0)] private int experienceReward = 50;

    private EnemyHealth2D enemyHealth;
    private bool rewardGranted;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth2D>();
    }

    private void OnEnable()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth2D>();
        }

        if (enemyHealth != null)
        {
            enemyHealth.Died -= GrantExperience;
            enemyHealth.Died += GrantExperience;
        }
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.Died -= GrantExperience;
        }
    }

    public void ConfigureReward(int amount)
    {
        experienceReward = Mathf.Max(0, amount);
    }

    private void GrantExperience(EnemyHealth2D defeatedEnemy)
    {
        if (rewardGranted)
        {
            return;
        }

        rewardGranted = true;
        GameObject player = GameObject.Find("Player");
        PlayerProgression2D progression = player != null ? player.GetComponent<PlayerProgression2D>() : null;
        if (progression != null)
        {
            progression.AwardExperience(experienceReward);
        }
    }
}
