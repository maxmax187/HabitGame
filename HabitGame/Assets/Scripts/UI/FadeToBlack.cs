using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FadeToBlack : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private EndLevelPopup _endLevelPopup;
    [SerializeField] private float _fadeTime;
    private Image _image;

    private void Start()
    {
        gameObject.SetActive(true);
        _image = GetComponent<Image>();
        Color color = _image.color;
        color.a = 0f;
        _image.color = color;
    }

    public void Fade()
    {
        _gameManager.FadeAudio(_fadeTime);

        _image.DOFade(1f, _fadeTime)
            .OnComplete(() =>
            {
                if (_endLevelPopup != null && ConfigManager.Instance != null)
                {
                    var config = ConfigManager.Instance.Config;
                    int currentIndex = config.LevelsData.Count - 1;
                    bool killedBoss = currentIndex >= 0 && config.LevelsData[currentIndex].KilledTheBoss;
                    int currentLevel = config.LevelsData.Count;
                    int kills = ConfigManager.Instance.GetBossKillCount();

                    _endLevelPopup.Show(killedBoss, currentLevel, kills, () =>
                    {
                        SceneSwitchManager.Instance.SwitchScene(Scenes.HomeScene);
                    });
                }
                else
                {
                    SceneSwitchManager.Instance.SwitchScene(Scenes.HomeScene);
                }
            });
    }
}