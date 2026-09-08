using System.Collections;
using UnityEngine;

/// <summary>
/// Boss pattern that teleports the boss to a new location every few seconds.
/// Locations are configured in the inspector and can be visited in order or at random.
/// </summary>

public class BossTeleport : MonoBehaviour
{
    [Tooltip("Master on/off switch for this pattern. The boss will not teleport while this is disabled, even if locations are configured below.")]
    [SerializeField] private bool _teleportEnabled;

    [Tooltip("Empty GameObjects placed in the scene marking where the boss can teleport to. Position them visually in the Scene view, then drag their transforms in here.")]
    [SerializeField] private Transform[] _teleportLocations;

    [Tooltip("Time in seconds between each teleport.")]
    [SerializeField] private float _teleportInterval = 5f;

    [Tooltip("When enabled, locations are chosen at random. When disabled, locations are visited in the order listed above.")]
    [SerializeField] private bool _randomOrder;

    [Header("Animation")]
    [Tooltip("The boss's visual sprite transform (not the root). Squished to nothing and back during each teleport. Leave empty to teleport instantly with no animation.")]
    [SerializeField] private Transform _spriteTransform;

    [Tooltip("How long, in seconds, the collapse and the re-appear each take. Keep this short so the boss isn't invisible for long while attacks may still be firing.")]
    [SerializeField] private float _animationDuration = 0.12f;

    [Tooltip("Easing for the scale/collapse over the animation duration. X axis = normalized time (0-1), Y axis = normalized progress (0-1) from the starting scale to the target scale.")]
    [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Easing for the sprite's fade over the animation duration. X axis = normalized time (0-1), Y axis = normalized progress (0-1) from the starting opacity to the target opacity.")]
    [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine _teleportCoroutine;
    private int _nextLocationIndex;
    private float _spriteBaseScaleY = 1f;
    private SpriteRenderer _spriteRenderer;
    private Color _spriteBaseColor = Color.white;

    private void Awake()
    {
        if (_spriteTransform != null)
        {
            _spriteBaseScaleY = _spriteTransform.localScale.y;
            _spriteRenderer = _spriteTransform.GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _spriteBaseColor = _spriteRenderer.color;
            }
        }
    }

    public void StartTeleporting()
    {
        if (!_teleportEnabled || _teleportLocations == null || _teleportLocations.Length == 0)
        {
            return;
        }

        StopTeleporting();
        _teleportCoroutine = StartCoroutine(TeleportLoop());
    }

    public void StopTeleporting()
    {
        if (_teleportCoroutine == null)
        {
            return;
        }

        StopCoroutine(_teleportCoroutine);
        _teleportCoroutine = null;

        RestoreSpriteState();
    }

    private IEnumerator TeleportLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_teleportInterval);

            Transform location = GetNextLocation();
            if (location == null)
            {
                continue;
            }

            yield return TeleportToLocation(location);
        }
    }

    private Transform GetNextLocation()
    {
        int index = _randomOrder ? Random.Range(0, _teleportLocations.Length) : _nextLocationIndex;

        _nextLocationIndex++;
        if (_nextLocationIndex >= _teleportLocations.Length)
        {
            _nextLocationIndex = 0;
        }

        Transform location = _teleportLocations[index];
        if (location == null)
        {
            Debug.LogWarning($"BossTeleport: location at index {index} is not assigned, skipping this teleport.", this);
        }

        return location;
    }

    private IEnumerator TeleportToLocation(Transform location)
    {
        yield return AnimateSprite(1f, 0f, mirrored: false);
        transform.position = location.position;
        yield return AnimateSprite(0f, 1f, mirrored: true);
    }

    private IEnumerator AnimateSprite(float from, float to, bool mirrored)
    {
        if (_spriteTransform == null || _animationDuration <= 0f)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < _animationDuration)
        {
            elapsed += Time.deltaTime;
            ApplySpriteState(from, to, elapsed / _animationDuration, mirrored);
            yield return null;
        }

        ApplySpriteState(from, to, 1f, mirrored);
    }

    private void ApplySpriteState(float from, float to, float t, bool mirrored)
    {
        Vector3 scale = _spriteTransform.localScale;
        scale.y = Mathf.LerpUnclamped(from, to, EvaluateCurve(_scaleCurve, t, mirrored)) * _spriteBaseScaleY;
        _spriteTransform.localScale = scale;

        if (_spriteRenderer != null)
        {
            Color color = _spriteRenderer.color;
            color.a = Mathf.LerpUnclamped(from, to, EvaluateCurve(_fadeCurve, t, mirrored)) * _spriteBaseColor.a;
            _spriteRenderer.color = color;
        }
    }

    // Reappearing after a teleport uses the opposite of the configured curve
    // (mirrored in both time and value) so it doesn't just replay the collapse forwards.
    private static float EvaluateCurve(AnimationCurve curve, float t, bool mirrored)
    {
        return mirrored ? 1f - curve.Evaluate(1f - t) : curve.Evaluate(t);
    }

    private void RestoreSpriteState()
    {
        if (_spriteTransform == null)
        {
            return;
        }

        Vector3 scale = _spriteTransform.localScale;
        scale.y = _spriteBaseScaleY;
        _spriteTransform.localScale = scale;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _spriteBaseColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_teleportLocations == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < _teleportLocations.Length; i++)
        {
            if (_teleportLocations[i] == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(_teleportLocations[i].position, 0.2f);

            if (!_randomOrder && i < _teleportLocations.Length - 1 && _teleportLocations[i + 1] != null)
            {
                Gizmos.DrawLine(_teleportLocations[i].position, _teleportLocations[i + 1].position);
            }
        }
    }
}
