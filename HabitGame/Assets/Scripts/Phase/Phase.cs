using UnityEngine;

public class Phase : MonoBehaviour
{
    [SerializeField] private Phases _phase;

    //not all phases will have these
    [SerializeField] private WalkData _walkData;
    [SerializeField] private BossHealth _boss;

    public GameManager GameManager;

    [SerializeField]
    public SwitchPhase MainEntrance;

    [SerializeField]
    public PlayerSpawnpoint Spawnpoint;

    public Phase NextPhase => GameManager.NextPhase();

    private void OnEnable()
    {
        _walkData?.Record(true, PlayerHealth.Instance);
    }

    public void InitializePhase2()
    {
        if (_phase != Phases.Phase2 || GameManager == null)
        {
            return;
        }

        if (PlayerHealth.Instance != null)
        {
            GameManager.Phase2Entered(PlayerHealth.Instance.GetCurrentHealth);
        }
    }

    public bool BossRoom(out BossHealth boss)
    {
        boss = _boss;
        return _phase == Phases.Phase3;
    }

    public void ExitPhase()
    {
        GameManager.ExitPhase(_phase);
        EndPhase();
    }

    public void EndPhase()
    {
        _walkData?.Record(false);
    }

    public void ShowTutorial(string tutorialText)
    {
        if (GameManager == null)
        {
            return;
        }

        switch (_phase)
        {
            case Phases.Phase1:
                GameManager.ShowSpikeTutorial(tutorialText);
                break;
            case Phases.Phase2:
                GameManager.ShowChestTutorial(tutorialText);
                break;
            case Phases.Phase3:
                GameManager.ShowBossTutorial(tutorialText);
                break;
            default:
                GameManager.ShowTutorial(tutorialText);
                break;
        }
    }

    private void Reset()
    {
        Spawnpoint = transform.GetComponentInChildren<PlayerSpawnpoint>(true);
    }
}
