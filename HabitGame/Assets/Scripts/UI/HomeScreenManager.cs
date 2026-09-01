using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// The home screen manager handels the home screen UI inputs
/// </summary>

public class HomeScreenManager : MonoBehaviour
{
    private const string SubmitSuccessMessage = "Data submitted succesfully, you may now close the game";
    private const string SubmitErrorMessage = "ERROR SUBMITTING DATA: please try again or download the data and inform the researcher(s)";

    [SerializeField] private Button _playButton;
    [SerializeField] private Button _downloadButton;
    [SerializeField] private Button _submitButton;
    [SerializeField] private Image _donwPlaying;
    [SerializeField] private TMP_Text _submitFeedbackText;

    [SerializeField] private AudioSource _homeScreenAudio;

    private ConfigManager _configManager;

    private void Start()
    {
        _playButton.onClick.AddListener(GameScene);
        DonePanel(false);

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
    }

    private void DonePanel(bool enabled)
    {
        _donwPlaying.gameObject.SetActive(enabled);
        _playButton.gameObject.SetActive(!enabled);
    }

    private void DownloadButton()
    {
        if (_configManager == null)
        {
            return;
        }

        Config.Download(_configManager.Config);
    }

    private void SubmitButton()
    {
        if (_configManager == null)
        {
            return;
        }

        Config.Submit(_configManager.Config, OnSubmitComplete);
    }

    // success/failure are shown in-game as a fixed, friendly message; the
    // real server response/error (message) is already logged to the
    // browser console by Config.Submit itself for troubleshooting.
    private void OnSubmitComplete(bool success, string message)
    {
        if (_submitFeedbackText == null)
        {
            return;
        }

        _submitFeedbackText.text = success ? SubmitSuccessMessage : SubmitErrorMessage;
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
