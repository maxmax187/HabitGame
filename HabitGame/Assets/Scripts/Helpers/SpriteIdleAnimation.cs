using UnityEngine;

public class SpriteIdleAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _frameRate = 8f;

    private int _currentFrame;
    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        float frameDuration = 1f / _frameRate;

        if (_timer >= frameDuration)
        {
            _timer -= frameDuration;
            _currentFrame = (_currentFrame + 1) % _frames.Length;
            _spriteRenderer.sprite = _frames[_currentFrame];
        }
    }
}