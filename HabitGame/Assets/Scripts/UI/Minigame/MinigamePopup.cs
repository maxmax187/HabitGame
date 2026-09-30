using TMPro;
using UnityEngine;

public class MinigamePopup : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private TMP_Text _turotial;
    [SerializeField] private UpgradeWeaponUI _upgradeWeaponUI;
    [SerializeField] private Minigame _minigameScreen;
    [Space]
    [SerializeField] private float _noTapTime = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioSource _chestOpenAudio;
    [SerializeField] private AudioSource _chestCloseAudio;
    [SerializeField] private AudioSource _tapHit;
    [SerializeField] private AudioSource _tapMiss;
    [SerializeField] private AudioSource _gameCompleteAudio;
    [SerializeField] private AudioSource _noUpgradeAudio; // Plays when Test Level & Group 0

    private bool _minigameDone;
    private float _waitTime;
    private bool _minigameActive;

    private void Start()
    {
        ShowPopup(false);
    }

    private void Update()
    {
        if (_waitTime > 0f)
        {
            _waitTime = Mathf.Max(0f, _waitTime - Time.unscaledDeltaTime);
        }
    }

    public void ShowTutorial(bool show)
    {
        if (_turotial != null)
        {
            _turotial.gameObject.SetActive(show);
        }
    }

    private void PlayChestAudio(bool open)
    {
        if (open)
        {
            if (_chestOpenAudio != null)
            {
                _chestOpenAudio.Play();
            }
        }
        else
        {
            if (_chestCloseAudio != null)
            {
                // PlayClipAtPoint ensures the audio doesn't get abruptly cut off 
                // when this GameObject is deactivated on the very next line.
                if (_chestCloseAudio.clip != null)
                {
                    AudioSource.PlayClipAtPoint(
                        _chestCloseAudio.clip,
                        Camera.main != null ? Camera.main.transform.position : transform.position,
                        _chestCloseAudio.volume
                    );
                }
                else
                {
                    _chestCloseAudio.Play();
                }
            }
        }
    }

    public void ShowPopup(bool show)
    {
        if (show && _minigameDone)
        {
            return;
        }

        if (show != gameObject.activeSelf)
        {
            if (!show)
            {
                PlayChestAudio(false);
                FindFirstObjectByType<PlayerMovement>()?.SetMovementLocked(false);
                Time.timeScale = 1f;
            }

            gameObject.SetActive(show);

            if (show)
            {
                PlayChestAudio(true);
            }
        }

        if (!show)
        {
            return;
        }

        _gameManager.MiniGameData(true, _minigameDone);

        if (_minigameDone)
        {
            return;
        }

        StartMiniGame();
    }

    public void CompletedMinigame()
    {
        Debug.Log("CompletedMinigame() called");
        _minigameDone = true;
        _minigameActive = false;

        // Hide minigame tutorial text so it does not show during the upgrade view
        ShowTutorial(false);

        if (_gameManager != null)
        {
            float timeLeft = PlayerHealth.Instance != null ? PlayerHealth.Instance.CurrentPlayerHealth : 0f;
            _gameManager.MinigameFinished(timeLeft);
            _gameManager.MiniGameData(true, true);
        }

        bool isTest = _gameManager != null && _gameManager.IsTestLevel;
        bool isNoUpgrade = isTest && _gameManager.Group == 0;

        // Play special audio if it is a test level with Group 0 (no upgrade available)
        if (isNoUpgrade && _noUpgradeAudio != null)
        {
            _noUpgradeAudio.Play();
        }
        else if (_gameCompleteAudio != null)
        {
            _gameCompleteAudio.Play();
        }

        if (_minigameScreen != null)
        {
            _minigameScreen.gameObject.SetActive(false);
        }

        if (_upgradeWeaponUI != null)
        {
            _upgradeWeaponUI.gameObject.SetActive(true);

            Vector2 upgradeDamage = _gameManager.PlayerAttack != null 
                ? _gameManager.PlayerAttack.UpgradeDamageInfo 
                : new Vector2(5f, 10f);

            if (isTest)
            {
                if (_gameManager.Group == 1)
                {
                    _upgradeWeaponUI.SetAlreadyStrongerState(upgradeDamage, _gameManager.TestDamage);
                }
                else
                {
                    _upgradeWeaponUI.SetNoUpgradeState(_gameManager.TestDamage);
                }
            }
            else
            {
                _upgradeWeaponUI.SetState(upgradeDamage, isTest);
            }
        }

        Time.timeScale = 0f;
        _waitTime = _noTapTime;
    }

    public void TapInput()
    {
        if (_waitTime > 0f)
        {
            return;
        }

        _waitTime = _noTapTime;

        if (!_minigameDone)
        {
            if (_minigameScreen.Tap(out bool hit))
            {
                if (hit)
                {
                    _tapHit?.Play();
                }
                else
                {
                    _tapMiss?.Play();
                }
            }
            return;
        }

        // Tapping here closes the popup window after upgrade viewing, triggering PlayChestAudio(false)
        ShowPopup(false);
    }

    private void StartMiniGame()
    {
        if (_minigameActive)
        {
            return;
        }

        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        playerMovement?.SetMovementLocked(true);

        _minigameActive = true;

        if (_upgradeWeaponUI != null)
        {
            _upgradeWeaponUI.gameObject.SetActive(false);
        }

        if (_minigameScreen != null)
        {
            _minigameScreen.gameObject.SetActive(true);
            _minigameScreen.StartGame();
        }

        _waitTime = _noTapTime;

        bool showHowTo = _gameManager != null && _gameManager.ShouldShowMinigameHowTo();
        ShowTutorial(showHowTo);

        if (PlayerHealth.Instance != null && _gameManager != null)
        {
            _gameManager.MinigameStarted(PlayerHealth.Instance.CurrentPlayerHealth);
        }
    }
}