using UnityEngine;

/// <summary>
/// Projectiles will attack the player and do damage once they hit
/// </summary>

[RequireComponent(typeof(Rigidbody2D))]
public class Projectle : DamageObject
{
    [SerializeField] private float _liveTime;
    [SerializeField] private float _speed;

    private Rigidbody2D _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Walls"))
        {
            DestroyProjectile();
            return;
        }

        HitPlayer(collider);
    }

    public void MoveTo(Vector3 pos)
    {
        Vector3 direction = (pos - transform.position).normalized;

        _rigidbody.linearVelocity = direction * _speed;

        StartCoroutine(HelperWait.ActionAfterWait(_liveTime, DestroyProjectile));
    }

    protected override bool HitPlayer(Collider2D collider, DamageType type = DamageType.Other)
    {
        bool hitPlayer = base.HitPlayer(collider, DamageType.Boss);

        if (hitPlayer)
        {
            DestroyProjectile();
        }

        return hitPlayer;
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}
