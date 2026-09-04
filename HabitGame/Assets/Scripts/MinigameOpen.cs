using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class MinigameOpen : MonoBehaviour
{
    private bool _isInRange;
    private bool _hasEnterd;

    private void OnTriggerEnter2D(Collider2D col)
    {
        IsInRange(true, col);
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        IsInRange(false, col);
    }

    private void IsInRange(bool isInRange, Collider2D col)
    {
        if (_isInRange != isInRange && col.gameObject.TryGetComponent<PlayerMovement>(out PlayerMovement player))
        {
            _isInRange = isInRange;
            player.IsInMinigameRange = isInRange;

            if (isInRange && !_hasEnterd)
            {
                _hasEnterd = true;
                if (ConfigManager.Instance != null && PlayerHealth.Instance != null)
                {
                    ConfigManager.Instance.TimeLeftInRangeOfChest(PlayerHealth.Instance.GetCurrentHealth);
                }
            }
        }
    }
}