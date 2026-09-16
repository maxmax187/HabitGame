using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text _bossKillText;
    [SerializeField] private Button _closeButton;

    private System.Action _onClose;

    private void Awake()
    {
        gameObject.SetActive(false);
        _closeButton.onClick.AddListener(HandleClose);
    }

    public void Show(int totalBossKills, System.Action onClose)
    {
        _onClose = onClose;
        _bossKillText.text = $"{totalBossKills}";
        gameObject.SetActive(true);
    }

    private void HandleClose()
    {
        gameObject.SetActive(false);
        _onClose?.Invoke();
        _onClose = null;
    }
}