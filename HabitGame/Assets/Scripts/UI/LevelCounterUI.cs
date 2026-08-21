using TMPro;
using UnityEngine;

public class LevelCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _roundText;
    [SerializeField] private GameManager _gameManager;

    private void Start()
    {
        if (_gameManager == null)
        {
            _gameManager = FindFirstObjectByType<GameManager>();
        }

        UpdateText();
    }

    private void UpdateText()
    {
        if (_gameManager == null || _roundText == null)
        {
            return;
        }

        _roundText.text = $"{_gameManager.CurrentRound}/{GameManager.TotalRoundCount}";
    }
}