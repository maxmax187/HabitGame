using DTT.Utils.Extensions;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// The config manager is for setting the config data while in game
/// </summary>

public class ConfigManager : MonoBehaviour
{
    public static ConfigManager Instance;
    public Config Config;

    [Header("Session Settings (set manually before each session)")]
    [SerializeField] private ChestSide _sessionChestSide;
    [SerializeField] private int _sessionDay = 1;

    public int SpikeDificulty => Config.CurrentSpikeDificulty;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Config = Config.Load();
        Config.ChestSide = _sessionChestSide;
        Config.Day = _sessionDay;
        Config.Save(Config);
    }

    public void UpdateSpikeDificulty(int newDificulty)
    {
        Config.CurrentSpikeDificulty = newDificulty;
        Config.Save(Config);
    }

    public void TutorialDone(bool done = true)
    {
        Config.TutorialFinished = done;
        Config.Save(Config);
    }

    public void StartLevelData(LevelType levelType)
    {
        int bossIndex = 0;
        float bossHealth = 0;
        bool isTypeTransition = false;

        if (!Config.LevelsData.IsNullOrEmpty())
        {
            LevelData lastLevel = Config.LevelsData[GetCurrentLevelIndex()];
            bossIndex = lastLevel.CurrentBoss;
            if (lastLevel.KilledTheBoss)
            {
                bossIndex++;
                if (!Config.TutorialFinished)
                {
                    TutorialDone(true);
                }
            }
            bossHealth = lastLevel.BossHealthLeft;
            
            // Reset whenever the level type changes (tutorial -> training, training -> test)
            isTypeTransition = levelType != lastLevel.Level;
        }

        if (isTypeTransition)
        {
            bossIndex = 0;
            bossHealth = 0;
        }

        LevelData newLevelData = new()
        {
            Index = Config.LevelsData.Count,
            PhaseTimes = new System.Collections.Generic.List<PhaseTimeData>(),
            SpikeDificulty = Config.CurrentSpikeDificulty,
            CurrentBoss = bossIndex,
            BossHealthLeft = bossHealth,
            Level = levelType,
        };

        Config.LevelsData.Add(newLevelData);
        Config.Save(Config);
    }

    public void SetTotalTime(float levelTime)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.LevelsData[currentIndex].LevelTime = levelTime;
    }

    public void AddPhaseTime(Phases phase, float timeLeft)
    {
        int currentIndex = GetCurrentLevelIndex();
        float totalLevelTime = Config.LevelsData[currentIndex].LevelTime;

        PhaseTimeData phaseTime = new()
        {
            Phase = phase,
            ExitPhaseTime = totalLevelTime - timeLeft,
        };

        Config.LevelsData[currentIndex].PhaseTimes.Add(phaseTime);
    }

    public void Hit(float damage, DamageType type)
    {
        int currentIndex = GetCurrentLevelIndex();

        switch (type)
        {
            case DamageType.Spike:
                Config.LevelsData[currentIndex].SpikesDamageTaken += damage;
                break;
            case DamageType.Boss:
                Config.LevelsData[currentIndex].BossDamageTaken += damage;
                break;
            case DamageType.Player:
                Config.LevelsData[currentIndex].BossDamageDone += damage;
                break;
            case DamageType.Other:
            default:
                break;
        }
    }

    public void SpikeLevelData(float endSpikeTime, int newSpikeDificulty)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.LevelsData[currentIndex].EndSpikeTime = endSpikeTime;

        Config.CurrentSpikeDificulty = newSpikeDificulty;
        Config.Save(Config);
    }

    public void SafeWalkData(WalkData.Data[] walkData)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.LevelsData[currentIndex].WalkData = walkData;
    }

    public void TimeLeftInRangeOfChest(float timeLeft)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.LevelsData[currentIndex].TimeLeftWhenInChestRange = timeLeft;
    }

    public void TimeLeftDoorOpens(float timeLeft)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.LevelsData[currentIndex].TimeLeftWhenDoorOpens = timeLeft;
    }

    public void MinigameData(bool hasOpend, bool hasFinished)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.LevelsData[currentIndex].OpendMinigame = hasOpend;
        Config.LevelsData[currentIndex].FinishedMinigame = hasFinished;
        Config.Save(Config);
    }

    public void SetMinigameStartTime(float timeLeft)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.LevelsData[currentIndex].StartTimeMinigame = timeLeft;
    }

    public void SetMinigameEndTime(float timeLeft)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.LevelsData[currentIndex].EndTimeMinigame = timeLeft;
    }

    public void SetPhase2EnterTime(float timeLeft)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.LevelsData[currentIndex].Phase2EnterTime = timeLeft;
    }

    public void SetPhase2ExitTime(float timeLeft)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.LevelsData[currentIndex].Phase2ExitTime = timeLeft;
    }

    public void BossRoom(float bossHealth)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.LevelsData[currentIndex].EnteredBossRoom = true;
        if (Config.LevelsData[currentIndex].BossHealthLeft == 0)
        {
            Config.LevelsData[currentIndex].BossHealthLeft = bossHealth;
        }
        Config.Save(Config);
    }

    public void BossFightEnd(bool killedBoss, bool isLastBoss, float timeLeft)
    {
        int currentIndex = GetCurrentLevelIndex();
        Config.FinishedAllBosses = isLastBoss;
        Config.LevelsData[currentIndex].KilledTheBoss = killedBoss;
        Config.LevelsData[currentIndex].TimeLeft = timeLeft;

        LevelData levelData = Config.LevelsData[currentIndex];
        float bossHealthLeft = levelData.BossHealthLeft - levelData.BossDamageDone;
        Config.LevelsData[currentIndex].BossHealthLeft = math.max(0, bossHealthLeft);
        Config.Save(Config);
    }

    public int CurrentBoss()
    {
        int currentIndex = GetCurrentLevelIndex();
        return currentIndex < 0 ? 0 : Config.LevelsData[currentIndex].CurrentBoss;
    }

    public float GetBossHealth()
    {
        int currentIndex = GetCurrentLevelIndex();
        if (currentIndex < 0)
        {
            return 0;
        }
        LevelData levelData = Config.LevelsData[currentIndex];
        return levelData.BossHealthLeft;
    }

    private int GetCurrentLevelIndex()
    {
        return Config.LevelsData.Count - 1;
    }

    public void MarkSpikeTutorialShown()
    {
        Config.SpikeTutorialShown = true;
        Config.Save(Config);
    }

    public void MarkBossTutorialShown()
    {
        Config.BossTutorialShown = true;
        Config.Save(Config);
    }

    public void MarkChestTutorialShown()
    {
        Config.ChestTutorialShown = true;
        Config.Save(Config);
    }

    public void MarkTutorialIntroShown()
    {
        Config.TutorialIntroShown = true;
        Config.Save(Config);
    }

    public void MarkTrainingIntroShown()
    {
        Config.TrainingIntroShown = true;
        Config.Save(Config);
    }

    public void MarkTestIntroShown()
    {
        Config.TestIntroShown = true;
        Config.Save(Config);
    }

    public void MarkMinigameHowToShown()
    {
        Config.MinigameHowToShown = true;
        Config.Save(Config);
    }

    public int GetBossKillCount()
    {
        int count = 0;
        foreach (LevelData level in Config.LevelsData)
        {
            if (level.KilledTheBoss)
            {
                count++;
            }
        }
        
        return count;
    }

    public void MarkOpenedBefore()
    {
        Config.HasOpenedBefore = true;
        Config.Save(Config);
    }
}
