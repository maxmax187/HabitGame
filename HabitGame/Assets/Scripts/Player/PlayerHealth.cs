using UnityEngine;

/// <summary>
/// Handles player health, represented as remaining time.
/// </summary>
public class PlayerHealth : Health
{
    public static PlayerHealth Instance;

    // Public accessor so outside classes like GameManager can read remaining health/time
    public float CurrentPlayerHealth => CurrentHealth;

    [Header("Audio")]
    [SerializeField] private AudioSource _damageAudio;

    private CountDown _countdown;
    private Camera _mainCamera;
    private bool _isPlaying;

    protected override void Start()
    {
        base.Start();
        Instance = this;
        _mainCamera = Camera.main;
    }

    public void SetData(float time, CountDown countdown)
    {
        SetHealth(time);
        _countdown = countdown;
        _isPlaying = true;
    }

    public override void TakeDamage(float damage, DamageType type)
    {
        if (IsTakingDamage || HealthAmount <= 0)
        {
            return;
        }

        _ = StartCoroutine(Damage());

        void Action()
        {
            base.TakeDamage(damage, type);
            if (_damageAudio != null)
            {
                _damageAudio.Play();
            }
            _countdown.UpdateTimer(CurrentHealth);
        }

        Vector3 screenPosition = _mainCamera.WorldToScreenPoint(transform.position);
        _countdown.LoseTime(damage, screenPosition, Action);
    }

    private void Update()
    {
        if (!_isPlaying || CurrentHealth <= 0)
        {
            return;
        }

        CurrentHealth -= Time.deltaTime;
        _countdown.UpdateTimer(CurrentHealth);
    }
}