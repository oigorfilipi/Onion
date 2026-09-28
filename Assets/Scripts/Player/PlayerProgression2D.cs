using System;
using UnityEngine;

public sealed class PlayerProgression2D : MonoBehaviour
{
    public const int MaximumLevel = 200;

    [SerializeField, Range(1, MaximumLevel)] private int level = 1;
    [SerializeField, Min(0)] private int currentExperience;
    [SerializeField, Min(1)] private int firstLevelExperience = 100;
    [SerializeField, Min(0)] private int experienceIncreasePerLevel = 25;

    private float levelUpNotificationUntil;

    public int Level => level;
    public int CurrentExperience => currentExperience;
    public bool IsAtMaximumLevel => level >= MaximumLevel;
    public int ExperienceRequiredForNextLevel => IsAtMaximumLevel
        ? 0
        : firstLevelExperience + (level - 1) * experienceIncreasePerLevel;
    public float ExperienceProgress => IsAtMaximumLevel
        ? 1f
        : Mathf.Clamp01((float)currentExperience / Mathf.Max(1, ExperienceRequiredForNextLevel));
    public bool ShowLevelUpNotification => Time.time < levelUpNotificationUntil;

    public event Action ProgressionChanged;
    public event Action<int> LeveledUp;

    private void Awake()
    {
        level = Mathf.Clamp(level, 1, MaximumLevel);
        currentExperience = Mathf.Max(0, currentExperience);
        if (IsAtMaximumLevel)
        {
            currentExperience = 0;
        }
        else
        {
            currentExperience = Mathf.Min(currentExperience, ExperienceRequiredForNextLevel - 1);
        }
    }

    public void AwardExperience(int amount)
    {
        if (amount <= 0 || IsAtMaximumLevel)
        {
            return;
        }

        currentExperience += amount;
        while (!IsAtMaximumLevel && currentExperience >= ExperienceRequiredForNextLevel)
        {
            currentExperience -= ExperienceRequiredForNextLevel;
            level++;
            levelUpNotificationUntil = Time.time + 3f;
            LeveledUp?.Invoke(level);
        }

        if (IsAtMaximumLevel)
        {
            currentExperience = 0;
        }

        ProgressionChanged?.Invoke();
    }
}
