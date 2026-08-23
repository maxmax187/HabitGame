using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene Switch Manager is for all the scene switches
/// </summary>

public class SceneSwitchManager : MonoBehaviour
{
    public static SceneSwitchManager Instance;
    [SerializeField] private int _homeSceneIndex;
    [SerializeField] private int _gameSceneIndex;

    private static bool _hasBooted;

    private void Start()
    {
        Instance = this;

        if (_hasBooted)
        {
            return;
        }
        _hasBooted = true;

        bool skipHome = ConfigManager.Instance != null && ConfigManager.Instance.Config.HasOpenedBefore;

        if (skipHome)
        {
            SwitchScene(Scenes.GameScene);
        }
        else
        {
            if (ConfigManager.Instance != null)
            {
                ConfigManager.Instance.MarkOpenedBefore();
            }
            SwitchScene(Scenes.HomeScene);
        }
    }

    public void SwitchScene(Scenes scene)
    {
        int sceneIndex = scene switch
        {
            Scenes.HomeScene => _homeSceneIndex,
            Scenes.GameScene => _gameSceneIndex,
            _ => 0,
        };

        _ = DOTween.KillAll();
        SceneManager.LoadScene(sceneIndex);
    }
}

public enum Scenes
{
    HomeScene,
    GameScene,
}