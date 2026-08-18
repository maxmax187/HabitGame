using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private MinigamePopup _minigamePopup;
    [SerializeField] private Tutorial _tutorial;
    [SerializeField] private LongTutorial _longTutorial;

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
        if (LongTutorial.IsShowing)
        {
            _longTutorial.NextPage();
            return;
        }
        _tutorial.ContinueGame();
    }

    public void ShowTutorial(string tutorialText)
    {
        // _tutorial.gameObject.SetActive(true);
        _tutorial.ShowTutorial(tutorialText);
    }

     public void ShowLongTutorialIntro()
    {
        _longTutorial.ShowTutorialIntro();
    }

    public void ShowLongTrainingTutorial()
    {
        _longTutorial.ShowTrainingTutorial();
    }

    public void ShowLongTestTutorial()
    {
        _longTutorial.ShowTestTutorial();
    }

    public event System.Action OnLongTutorialClosed
    {
        add => _longTutorial.OnClosed += value;
        remove => _longTutorial.OnClosed -= value;
    }
}