using UnityEngine;

[RequireComponent(typeof(ActionOnTrigger))]
public class SwitchPhase : MonoBehaviour
{
    [Header("Health Parameters")]
    [SerializeField] private float _moveForwardAmount;

    private Phase _phase;
    private bool _isEntering;

    private void Start()
    {
        SetPhase();
    }

    public void GoToNextPhase(PlayerMovement playerMovement)
    {
        if (_isEntering)
        {
            return;
        }
        _isEntering = true;

        // Load in next phase
        Phase next = _phase.NextPhase;
        if (next == null)
        {
            return;
        }
        Phase nextPhase = Instantiate(next);
        nextPhase.GameManager = _phase.GameManager;
        _phase.GameManager.CurrentPhase = nextPhase;
        nextPhase.InitializePhase2();

        // Send player to the entrance of the next phase
        nextPhase.MainEntrance.PlayerEnter(playerMovement);
        _phase.ExitPhase();

        // Turn off current phase
        _phase.gameObject.SetActive(false);
    }

    public void PlayerEnter(PlayerMovement playerMovement)
    {
        SetPhase();
        if (_phase.BossRoom(out _))
        {
            if (Attack.Instance != null)
            {
                Attack.Instance.EnterBossRoom();
            }
        }

        _isEntering = true;
        playerMovement.Entrance(transform.position);
    }

    private void SetPhase()
    {
        if (_phase == null)
        {
            _phase = GetComponentInParent<Phase>();
        }
    }
}