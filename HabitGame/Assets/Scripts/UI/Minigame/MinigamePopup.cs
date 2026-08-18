using TMPro;
using UnityEngine;

/// <summary>
/// This minigame popup is for showing or not showing the popup
/// </summary>

public class MinigamePopup : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private TMP_Text _turotial;
    [SerializeField] private UpgradeWeaponUI _upgradeWeaponUI;
    [SerializeField] private Minigame _minigameScreen;
    [Space]
    [SerializeField] private float _noTapTime;

    [Header("Audio")]
    [SerializeField] AudioSource _chestOpenAudio;
    [SerializeField] AudioSource _chestCloseAudio;
    [SerializeField] AudioSource _tapHit;
    [SerializeField] AudioSource _tapMiss;
    [SerializeField] AudioSource _gameCompleteAudio;

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
            _waitTime = Mathf.Max(0f, _waitTime - Time.deltaTime);
        }
    }

    public void ShowTutorial(bool show)
    {
        _turotial.gameObject.SetActive(show);
    }

    private void PlayChestAudio(bool open)
    {
        if (open)
        {
            _chestOpenAudio.Play();
        }
        else
        {
            _chestCloseAudio.Play();
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
                PlayChestAudio(show);
            }
            gameObject.SetActive(show);
            if (show)
            {
                PlayChestAudio(show);
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
        _gameManager.MiniGameData(true, true);

        if (PlayerHealth.Instance != null)
        {
            _gameManager.MinigameFinished(PlayerHealth.Instance.GetCurrentHealth);
        }

        _minigameScreen.gameObject.SetActive(false);
        _turotial.gameObject.SetActive(false);

        _upgradeWeaponUI.gameObject.SetActive(true);
        _gameCompleteAudio?.Play();

        if (PlayerHealth.Instance != null)
        {
            Vector2 upgradeDamage = PlayerHealth.Instance.PlayerAttackUpgrade;
            
            if (_gameManager.IsTestLevel)
            {
                _upgradeWeaponUI.SetNoUpgradeState(upgradeDamage.x);
            }
            else
            {
                _upgradeWeaponUI.SetState(upgradeDamage);
            }
        }

        ShowPopup(true);
        _minigameDone = true;
    }

    public void TapInput()
    {
        if (_waitTime > 0)
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

        ShowPopup(false);
    }

    private void StartMiniGame()
    {
        if (_minigameActive)
        {
            return;
        }

        _minigameActive = true;
        _upgradeWeaponUI.gameObject.SetActive(false);
        _minigameScreen.gameObject.SetActive(true);
        _waitTime = _noTapTime;
        _minigameScreen.StartGame();

        bool showHowTo = _gameManager != null && _gameManager.ShouldShowMinigameHowTo();
        ShowTutorial(showHowTo);

        if (PlayerHealth.Instance != null)
        {
            _gameManager.MinigameStarted(PlayerHealth.Instance.GetCurrentHealth);
        }
    }
}
