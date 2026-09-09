using DTT.Utils.Extensions;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class HomeScreenManager : MonoBehaviour
{
    private const string SubmitSuccessMessage = "Data submitted succesfully, you may now close the game";
    private const string SubmitErrorMessage = "ERROR SUBMITTING DATA - DO NOT CLOSE THE GAME: \nplease try again, or download the data and inform the researcher(s)\n";
    private const string SubmittingMessage = "Submitting data...";

    [SerializeField] private Button _playButton;
    [SerializeField] private Button _downloadButton;
    [SerializeField] private Button _submitButton;
    [SerializeField] private Image _donwPlaying;
    [SerializeField] private EndLevelPopup _endLevelPopup;
    [SerializeField] private TMP_Text _submitFeedbackText;
    [SerializeField] private Color _submitNormalColor = Color.white;
    [SerializeField] private Color _submitErrorColor = new Color(1f, 0.35f, 0f); // orange-red

    [SerializeField] private AudioSource _homeScreenAudio;

    private ConfigManager _configManager;

    private void Start()
    {
        _playButton.onClick.AddListener(GameScene);
        DonePanel(false);
        SetSubmitButtonVisible(false);

        _configManager = ConfigManager.Instance;
        _downloadButton.onClick.AddListener(DownloadButton);

        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
            _downloadButton.onClick.AddListener(DownloadButton);
            _submitButton.onClick.AddListener(SubmitButton);

            if (_configManager != null)
            {
                DonePanel(_configManager.Config.FinishedAllBosses);
            }
        }

        bool finished = _configManager.Config.FinishedAllBosses;
        DonePanel(finished);

        bool hasPlayedALevel = !_configManager.Config.LevelsData.IsNullOrEmpty();
        if (hasPlayedALevel)
        {
            ShowLastLevelResult(finished);
        }
    }

    private void ShowLastLevelResult(bool isFinalRound)
    {
        int lastIndex = _configManager.Config.LevelsData.Count - 1;
        LevelData lastLevel = _configManager.Config.LevelsData[lastIndex];
        int levelNumber = _configManager.Config.LevelsData.Count;
        int bossKillCount = _configManager.GetBossKillCount();

        _playButton.gameObject.SetActive(false);

        _endLevelPopup.Show(lastLevel.KilledTheBoss, levelNumber, bossKillCount, () =>
        {
            if (isFinalRound)
            {
                _endLevelPopup.Hide();
            }
            else
            {
                GameScene();
            }
        });
    }

    private void DonePanel(bool enabled)
    {
        _donwPlaying.gameObject.SetActive(enabled);
        _playButton.gameObject.SetActive(!enabled);

        // As soon as the done panel shows, try to submit automatically -
        // the player shouldn't have to press anything if it just works.
        if (enabled)
        {
            AttemptSubmit();
        }
    }

    private void DownloadButton()
    {
        if (_configManager == null)
        {
            return;
        }

        Config.Download(_configManager.Config);
    }

    // Used both for the automatic attempt when the done panel appears and
    // for the manual retry button shown after a failed attempt.
    private void SubmitButton()
    {
        AttemptSubmit();
    }

    private void AttemptSubmit()
    {
        if (_configManager == null)
        {
            return;
        }

        // Hide the button while a submission is in flight so it can't be
        // clicked again mid-request, and so it stays hidden through to a
        // successful result without ever flashing visible.
        SetSubmitButtonVisible(false);
        if (_submitFeedbackText != null)
        {
            _submitFeedbackText.text = SubmittingMessage;
            _submitFeedbackText.color = _submitNormalColor;
        }

        Config.Submit(_configManager.Config, OnSubmitComplete);
    }

    // success/failure are shown in-game as a fixed, friendly message; the
    // real server response/error (message) is already logged to the
    // browser console by Config.Submit itself for troubleshooting.
    private void OnSubmitComplete(bool success, string message)
    {
        if (_submitFeedbackText != null)
        {
            _submitFeedbackText.text = success ? SubmitSuccessMessage : SubmitErrorMessage;
            _submitFeedbackText.color = success ? _submitNormalColor : _submitErrorColor;
        }

        // Only offer a retry button when submission actually failed.
        SetSubmitButtonVisible(!success);
    }

    private void SetSubmitButtonVisible(bool visible)
    {
        if (_submitButton != null)
        {
            _submitButton.gameObject.SetActive(visible);
        }
    }

    private void GameScene()
    {
        _homeScreenAudio.DOFade(0f, 1f).OnComplete(() =>
        {
            _homeScreenAudio.Stop();
            SceneSwitchManager.Instance.SwitchScene(Scenes.GameScene);
        });
    }


}