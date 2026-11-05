using UnityEngine;

/// <summary>
/// Handles player health, represented as remaining time.
/// </summary>
public class PlayerHealth : Health
{
    public static PlayerHealth Instance;

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
        if (!_isPlaying)
        {
            return;
        }

        if (CurrentHealth <= 0)
        {
            _isPlaying = false;
            CurrentHealth = 0f;
            _countdown.UpdateTimer(0f);

            // Trigger timeout loss
            FindFirstObjectByType<GameManager>()?.EndGame(killedBoss: false, timeLeft: 0f);
            return;
        }

        CurrentHealth -= Time.deltaTime;
        _countdown.UpdateTimer(CurrentHealth);
    }
}