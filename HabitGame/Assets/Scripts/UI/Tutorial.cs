using TMPro;
using UnityEngine;

/// <summary>
/// This tutorial script is to toggle and showcase the tutorial 
/// </summary>

public class Tutorial : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;

    private float _inputCooldown = 0.3f;
    private float _lastShowTime;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void ShowTutorial(string tutorialText)
    {
        _text.text = tutorialText;
        gameObject.SetActive(true);
        Time.timeScale = 0;
        _lastShowTime = Time.unscaledTime;
    }

    public void ContinueGame()
    {
        if (Time.unscaledTime - _lastShowTime < _inputCooldown)
        {
            return;
        }

        gameObject.SetActive(false);
        Time.timeScale = 1;
    }
}