using DTT.Utils.Extensions;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HomeScreenManager : MonoBehaviour
{
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _downloadButton;
    [SerializeField] private Image _donwPlaying;
    [SerializeField] private EndLevelPopup _endLevelPopup;

    [SerializeField] private AudioSource _homeScreenAudio;

    private ConfigManager _configManager;

    private void Start()
    {
        _playButton.onClick.AddListener(GameScene);
        DonePanel(false);

        _configManager = ConfigManager.Instance;
        _downloadButton.onClick.AddListener(DownloadButton);

        if (_configManager == null)
        {
            return;
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
                DonePanel(true);
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
    }

    private void DownloadButton()
    {
        if (_configManager == null)
        {
            return;
        }

        Config.Download(_configManager.Config);
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