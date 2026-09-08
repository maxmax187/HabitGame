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
    [SerializeField] private float _phaseDuration = 0.12f;

    private Coroutine _teleportCoroutine;
    private int _nextLocationIndex;
    private float _spriteBaseScaleY = 1f;

    private void Awake()
    {
        if (_spriteTransform != null)
        {
            _spriteBaseScaleY = _spriteTransform.localScale.y;
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

        RestoreSpriteScale();
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
        yield return ScaleSpriteVertically(1f, 0f);
        transform.position = location.position;
        yield return ScaleSpriteVertically(0f, 1f);
    }

    private IEnumerator ScaleSpriteVertically(float from, float to)
    {
        if (_spriteTransform == null || _phaseDuration <= 0f)
        {
            yield break;
        }

        Vector3 scale = _spriteTransform.localScale;
        float elapsed = 0f;
        while (elapsed < _phaseDuration)
        {
            elapsed += Time.deltaTime;
            scale.y = Mathf.SmoothStep(from, to, elapsed / _phaseDuration) * _spriteBaseScaleY;
            _spriteTransform.localScale = scale;
            yield return null;
        }

        scale.y = to * _spriteBaseScaleY;
        _spriteTransform.localScale = scale;
    }

    private void RestoreSpriteScale()
    {
        if (_spriteTransform == null)
        {
            return;
        }

        Vector3 scale = _spriteTransform.localScale;
        scale.y = _spriteBaseScaleY;
        _spriteTransform.localScale = scale;
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
