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
    //private bool _isShowing;

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
        //_isShowing = true;
    }

    public void ContinueGame()
    {
        //if (!_isShowing)
        //{
        //    return;
        //}

        if (Time.unscaledTime - _lastShowTime < _inputCooldown)
        {
            return;
        }

        gameObject.SetActive(false);
        Time.timeScale = 1;
        //_isShowing = false;
    }
}