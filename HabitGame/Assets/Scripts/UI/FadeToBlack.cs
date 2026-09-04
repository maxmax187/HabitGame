using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The script will fade the screen to black 
/// </summary>

[RequireComponent(typeof(Image))]
public class FadeToBlack : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private float _fadeTime;
    private Image _image;

    private void Start()
    {
        gameObject.SetActive(true);
        _image = GetComponent<Image>();
        Color color = _image.color;
        color.a = 0f;
        _image.color = color;
    }

    public void Fade()
    {
        _gameManager.FadeAudio(_fadeTime);

        _image.DOFade(1f, _fadeTime)
            .OnComplete(() =>
            {
                SceneSwitchManager.Instance.SwitchScene(Scenes.HomeScene);
            });
    }
}