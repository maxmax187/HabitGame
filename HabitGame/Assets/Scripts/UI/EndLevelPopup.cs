using DG.Tweening;
using TMPro;
using UnityEngine;

public class EndLevelPopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _panel;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _bossKillText;

    [Header("Animation")]
    [SerializeField] private float _fadeInDuration = 0.4f;
    [SerializeField] private float _popScaleDuration = 0.4f;
    [SerializeField] private float _countUpDuration = 0.6f;
    [SerializeField] private float _displayHoldTime = 1.5f;

    public void Show(bool killedBoss, int levelNumber, int bossKillCount, System.Action onComplete)
    {
        gameObject.SetActive(true);
        _canvasGroup.alpha = 0f;
        _panel.localScale = Vector3.one * 0.8f;

        _titleText.text = killedBoss ? "BOSS KILLED" : "TIME IS OUT";

        int previousLevel = killedBoss ? levelNumber - 1 : levelNumber;
        int previousKills = killedBoss ? bossKillCount - 1 : bossKillCount;

        _levelText.text = $"Level: {previousLevel}";
        _bossKillText.text = $"Boss Kills: {previousKills}";

        Sequence sequence = DOTween.Sequence();
        sequence.Append(_canvasGroup.DOFade(1f, _fadeInDuration));
        sequence.Join(_panel.DOScale(1f, _popScaleDuration).SetEase(Ease.OutBack));

        if (killedBoss)
        {
            sequence.AppendCallback(() =>
            {
                AnimateCount(_levelText, "Level", previousLevel, levelNumber);
                AnimateCount(_bossKillText, "Boss Kills", previousKills, bossKillCount);
            });
            sequence.AppendInterval(_countUpDuration);
        }

        sequence.AppendInterval(_displayHoldTime);
        sequence.OnComplete(() => onComplete?.Invoke());
    }

    private void AnimateCount(TMP_Text text, string label, int from, int to)
    {
        DOTween.To(() => from, value =>
        {
            text.text = $"{label}: {value}";
        }, to, _countUpDuration).SetEase(Ease.OutQuad);
    }
}