using TMPro;
using UnityEngine;

/// <summary>
/// This tutorial script is to toggle and showcase the tutorial 
/// </summary>

public class Tutorial : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;

    private bool _tutorialShowing = false;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void ShowTutorial(string tutorialText)
    {
        _text.text = tutorialText;
        gameObject.SetActive(true);
        Time.timeScale = 0;
    }

    //Input UI click
    public void ContinueGame()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1;
    }
}