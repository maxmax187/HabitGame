using TMPro;
using UnityEngine;

public class BossKillCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _killText;
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
        if (_gameManager == null || _killText == null)
        {
            return;
        }

        _killText.text = $"Boss Kills: {_gameManager.BossKillCount}";
    }
}