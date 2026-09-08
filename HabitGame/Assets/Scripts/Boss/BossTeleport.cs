using System.Collections;
using UnityEngine;

/// <summary>
/// Boss pattern that teleports the boss to a new location every few seconds.
/// Locations are configured in the inspector and can be visited in order or at random.
/// </summary>

public class BossTeleport : MonoBehaviour
{
    [SerializeField] private Vector2[] _teleportLocations;
    [SerializeField] private float _teleportInterval = 5f;
    [SerializeField] private bool _randomOrder;

    private Coroutine _teleportCoroutine;
    private int _nextLocationIndex;

    public void StartTeleporting()
    {
        if (_teleportLocations == null || _teleportLocations.Length == 0)
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
    }

    private IEnumerator TeleportLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_teleportInterval);
            TeleportToNextLocation();
        }
    }

    private void TeleportToNextLocation()
    {
        int index = _randomOrder ? Random.Range(0, _teleportLocations.Length) : _nextLocationIndex;
        transform.localPosition = _teleportLocations[index];

        _nextLocationIndex++;
        if (_nextLocationIndex >= _teleportLocations.Length)
        {
            _nextLocationIndex = 0;
        }
    }
}
