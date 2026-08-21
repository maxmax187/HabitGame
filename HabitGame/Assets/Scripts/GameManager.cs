using DG.Tweening;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static PhasesData;

/// <summary>
/// GameManager is the script where the level data gets saved
/// </summary>

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FadeToBlack _fadeToBlack;
    [SerializeField] private CountDown _countDown;
    [SerializeField] private UIManager _uiManager;

    [Space]
    [SerializeField] private PhasesData _phases;

    [Space]
    [SerializeField] private Vector2 _timeLeftAfterSpikes;

    [Header("Tutorial Text")]
    [SerializeField] private string _walkTutorialText;

    [Header("Background Audio")]
    [SerializeField] private AudioSource _backgroundAudio;
    [SerializeField] private AudioSource _backgroundBattleAudio;

    private ConfigManager _configManager;
    private int _currentPhase;
    private List<PhaseData> _levelPhases;
    private PlayerHealth _playerHealth;
    private Phase _phase;
    private bool _isLastBoss;
    private AudioSource _currentAudio;

    private const int TutorialRoundCount = 1;
    private const int TrainingRoundCount = 1;
    private const int TestRoundCount = 1;
    public const int TotalRoundCount = TutorialRoundCount + TrainingRoundCount + TestRoundCount;

    public int CurrentRound => _configManager != null ? _configManager.Config.LevelsData.Count : 1;
    public LevelType CurrentLevelType => GetLevelType(CurrentRound);
    public bool IsTutorialLevel => CurrentLevelType == LevelType.Tutorial;
    public bool IsTestLevel => CurrentLevelType == LevelType.Test;

    public bool IsFirstTutorialRound => CurrentRound == 1;
    public bool IsFirstTrainingRound => CurrentRound == TutorialRoundCount + 1; // assuming number of train rounds != 0
    public bool IsFirstTestRound => CurrentRound == TutorialRoundCount + TrainingRoundCount + 1;

    public int BossKillCount => _configManager != null ? _configManager.GetBossKillCount() : 0;

    private LevelType GetLevelType(int round)
    {
        if (round <= TutorialRoundCount)
        {
            return LevelType.Tutorial;
        }
        if (round <= TutorialRoundCount + TrainingRoundCount)
        {
            return LevelType.Training;
        }
        return LevelType.Test;
    }

    public Phase CurrentPhase
    {
        set { _phase = value; }
    }

    public void ShowSpikeTutorial(string text)
    {
        if (_configManager == null || _configManager.Config.Day != 1 || !IsFirstTutorialRound)
        {
            return;
        }
        if (_configManager.Config.SpikeTutorialShown)
        {
            return;
        }
        ShowTutorial(text);
        _configManager.MarkSpikeTutorialShown();
    }

    public void ShowBossTutorial(string text)
    {
        if (_configManager == null || _configManager.Config.Day != 1 || !IsFirstTutorialRound)
        {
            return;
        }
        if (_configManager.Config.BossTutorialShown)
        {
            return;
        }
        ShowTutorial(text);
        _configManager.MarkBossTutorialShown();
    }

    public void ShowChestTutorial(string text)
    {
        if (_configManager == null || !IsFirstTrainingRound)
        {
            return;
        }
        if (_configManager.Config.ChestTutorialShown)
        {
            return;
        }
        ShowTutorial(text);
        _configManager.MarkChestTutorialShown();
    }

    private void Start()
    {
        if (ConfigManager.Instance != null)
        {
            _configManager = ConfigManager.Instance;
            int upcomingRound = _configManager.Config.LevelsData.Count + 1;
            LevelType levelType = GetLevelType(upcomingRound);
            _configManager.StartLevelData(levelType);
        }

        _levelPhases = GetPhases(out float time);
        Phase currentPhase = Instantiate(_levelPhases[_currentPhase].Phase);
        currentPhase.GameManager = this;

        _playerHealth = currentPhase.Spawnpoint.SpawnPlayer();
        _playerHealth.SetData(time, _countDown);

        if (_configManager != null)
        {
            _configManager.SetTotalTime(time);
        }

        SetAudio(_backgroundAudio);

        bool longIntroShown = ShowTutorialIntro() || ShowTrainingIntro() || ShowTestIntro();
        if (longIntroShown)
        {
            _uiManager.OnLongTutorialClosed += HandleLongTutorialClosed;
        }
        else
        {
            ShowTutorial(_walkTutorialText);
        }
    }

    private void HandleLongTutorialClosed()
    {
        _uiManager.OnLongTutorialClosed -= HandleLongTutorialClosed;
        ShowTutorial(_walkTutorialText);
    }

    private void SetAudio(AudioSource newAudio)
    {
        if (newAudio == _currentAudio)
        {
            return;
        }

        if (_currentAudio != null)
        {
            _currentAudio.Stop();
        }

        _currentAudio = newAudio;
        if (_currentAudio != null)
        {
            _currentAudio.Play();
        }   
    }

    public void FadeAudio(float fadeTime)
    {
        if (_currentAudio == null)
        {
            return;
        }

        _currentAudio.DOFade(0f, fadeTime);
    }

    public void ShowTutorial(string text)
    {
        _uiManager.ShowTutorial(text);
    }

    public void SpikeSectionDone(float spikeFinishTimeLeft, int maxDificulty)
    {
        if (!TrySetConfig(out ConfigManager config))
        {
            return;
        }

        #region Get new spike dificulty
        bool updateDificulty = false;
        int currentDificulty = _configManager.Config.CurrentSpikeDificulty;
        if (spikeFinishTimeLeft < _timeLeftAfterSpikes.x)
        {
            updateDificulty = true;
            currentDificulty--;
        }
        else if (spikeFinishTimeLeft > _timeLeftAfterSpikes.y)
        {
            updateDificulty = true;
            currentDificulty++;
        }

        if (updateDificulty)
        {
            currentDificulty = math.clamp(currentDificulty, 0, maxDificulty);
            if (ConfigManager.Instance != null)
            {
                ConfigManager.Instance.UpdateSpikeDificulty(currentDificulty);
            }
        }
        #endregion

        config.SpikeLevelData(spikeFinishTimeLeft, currentDificulty);
    }

    public void MiniGameData(bool hasOpend, bool hasFinished)
    {
        if (!TrySetConfig(out ConfigManager config))
        {
            return;
        }

        config.MinigameData(hasOpend, hasFinished);

        if (hasFinished && !IsTestLevel)
        {
            _playerHealth.UpgradeAttack();
        }
    }

    public void EnterBossRoom(float bossHealth)
    {
        SetAudio(_backgroundBattleAudio);
        if (ConfigManager.Instance == null)
        {
            return;
        }

        _configManager.BossRoom(bossHealth);
    }

    public void EndGame(bool killedBoss = false, float timeLeft = 0f)
    {
        _phase.EndPhase();

        if (!TrySetConfig(out ConfigManager config))
        {
            return;
        }

        config.BossFightEnd(killedBoss, _isLastBoss, timeLeft);
        _fadeToBlack.Fade();
    }

    private bool TrySetConfig(out ConfigManager config)
    {
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
            if (_configManager == null)
            {
                config = null;
                return false;
            }
        }

        config = _configManager;
        return true;
    }

    private List<PhaseData> GetPhases(out float phasesTime)
    {
        List<PhaseData> phases = new()
        {
            GetRandomFirstPhase(),
            GetPhaseTwo(),
            GetBossPhase()
        };

        phasesTime = 0f;
        foreach (PhaseData phase in phases)
        {
            phasesTime += phase.PhaseTime;
        }

        return phases;
    }

    private PhaseData GetPhaseTwo()
    {
        PhaseData[] pool = IsTutorialLevel ? _phases.PhasesTwoTutorial : _phases.PhasesTwoRegular;
        int randomIndex = UnityEngine.Random.Range(0, pool.Length);
        return pool[randomIndex];
    }

    public Phase NextPhase()
    {
        if (_currentPhase >= _levelPhases.Count)
        {
            return null;
        }

        _currentPhase++;
        return _levelPhases[_currentPhase].Phase;
    }

    private PhaseData GetRandomFirstPhase()
    {
        int phaseOneCount = _phases.PhasesOne.Length;
        int randomPhaseOneIndex = UnityEngine.Random.Range(0, phaseOneCount);
        return _phases.PhasesOne[randomPhaseOneIndex];
    }

    private PhaseData GetBossPhase()
    {
        int currentBossIndex = 0;
        if (_configManager != null)
        {
            currentBossIndex = _configManager.CurrentBoss();
        }

        int phaseThreeCount = _phases.PhasesThree.Length;
        int bossPhaseIndex = math.min(currentBossIndex, phaseThreeCount - 1);

        _isLastBoss = CurrentRound >= TotalRoundCount;

        return _phases.PhasesThree[bossPhaseIndex];
    }

    public void ExitPhase(Phases phases)
    {
        if (_configManager == null)
        {
            return;
        }

        float time = _playerHealth.GetCurrentHealth;
        _configManager.AddPhaseTime(phases, time);

        if (phases == Phases.Phase2)
        {
            _configManager.SetPhase2ExitTime(time);
        }
    }

    public void MinigameStarted(float timeLeft)
    {
        if (!TrySetConfig(out ConfigManager config))
        {
            return;
        }
        config.SetMinigameStartTime(timeLeft);
    }

    public void MinigameFinished(float timeLeft)
    {
        if (!TrySetConfig(out ConfigManager config))
        {
            return;
        }
        config.SetMinigameEndTime(timeLeft);
    }

    public void Phase2Entered(float timeLeft)
    {
        if (!TrySetConfig(out ConfigManager config))
        {
            return;
        }
        config.SetPhase2EnterTime(timeLeft);
    }

    public bool ShowTutorialIntro()
    {
        if (_configManager == null || !IsFirstTutorialRound || _configManager.Config.TutorialIntroShown)
        {
            return false;
        }
        _uiManager.ShowLongTutorialIntro();
        _configManager.MarkTutorialIntroShown();
        return true;
    }

    public bool ShowTrainingIntro()
    {
        if (_configManager == null || !IsFirstTrainingRound || _configManager.Config.TrainingIntroShown)
        {
            return false;
        }
        _uiManager.ShowLongTrainingTutorial();
        _configManager.MarkTrainingIntroShown();
        return true;
    }

    public bool ShowTestIntro()
    {
        if (_configManager == null || !IsFirstTestRound || _configManager.Config.TestIntroShown)
        {
            return false;
        }
        _uiManager.ShowLongTestTutorial();
        _configManager.MarkTestIntroShown();
        return true;
    }

    public bool ShouldShowMinigameHowTo()
    {
        if (_configManager == null || !IsFirstTrainingRound || _configManager.Config.MinigameHowToShown)
        {
            return false;
        }
        _configManager.MarkMinigameHowToShown();
        return true;
    }
}