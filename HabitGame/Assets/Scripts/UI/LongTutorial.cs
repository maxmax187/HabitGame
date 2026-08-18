using TMPro;
using UnityEngine;

public class LongTutorial : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private TMP_Text _pageIndicator;
    [SerializeField] private string[] _tutorialPages;
    [SerializeField] private string[] _trainingPages;
    [SerializeField] private string[] _testPages;

    private string[] _currentPages;
    private int _currentPage;
    private bool _isShowing;

    private float _inputCooldown = 0.6f;
    private float _lastInputTime;

    public static bool IsShowing { get; private set; }

    public event System.Action OnClosed;

    public void ShowTutorialIntro()
    {
        Show(_tutorialPages);
    }

    public void ShowTrainingTutorial()
    {
        Show(_trainingPages);
    }

    public void ShowTestTutorial()
    {
        Show(_testPages);
    }

    private void Show(string[] pages)
    {
        if (pages == null || pages.Length == 0) return;
        _currentPages = pages;
        _currentPage = 0;
        _isShowing = true;
        IsShowing = true;
        _lastInputTime = Time.unscaledTime; // prevent immediate skip
        gameObject.SetActive(true);
        Time.timeScale = 0;
        UpdatePage();
    }

    public void NextPage()
    {
        if (!_isShowing)
        {
            return;
        }

        if (Time.unscaledTime - _lastInputTime < _inputCooldown)
        {
            return;
        }

        _lastInputTime = Time.unscaledTime;
        _currentPage++;
        if (_currentPage >= _currentPages.Length)
        {
            Close();
            return;
        }
        UpdatePage();
    }

    private void UpdatePage()
    {
        _text.text = _currentPages[_currentPage];
        if (_pageIndicator != null)
            _pageIndicator.text = $"{_currentPage + 1}/{_currentPages.Length}";
    }

    private void Close()
    {
        IsShowing = false;
        _isShowing = false;
        gameObject.SetActive(false);
        Time.timeScale = 1;
        OnClosed?.Invoke();
    }

    private void OnDestroy()
    {
        IsShowing = false;
        Time.timeScale = 1;
    }
}