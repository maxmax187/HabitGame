using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private MinigamePopup _minigamePopup;
    [SerializeField] private Tutorial _tutorial;

    private void Awake()
    {
        Instance = this;
        _tutorial.gameObject.SetActive(true);
    }

    public void ShowMinigame(bool show)
    {
        _minigamePopup.ShowPopup(show);
    }

    public void MinigameTap()
    {
        if (!_minigamePopup.isActiveAndEnabled)
        {
            return;
        }
        _minigamePopup.TapInput();
    }

    public void TutorialClick()
    {
        _tutorial.ContinueGame();
    }

    public void ShowTutorial(string tutorialText)
    {
        // _tutorial.gameObject.SetActive(true);
        _tutorial.ShowTutorial(tutorialText);
    }
}