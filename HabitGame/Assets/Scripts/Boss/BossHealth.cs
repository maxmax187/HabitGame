using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BossHealth is derived from Health and takes care of 
/// showcasing the health and what happens if the boss is dead
/// </summary>

[RequireComponent(typeof(BossAttack))]
public class BossHealth : Health
{
    [SerializeField] private GameManager _gameManager;

    [SerializeField] private Animator _animator;
    [SerializeField] private Slider _healthSlider;

    [Header("Audio")]
    [SerializeField] private AudioSource _bossHitAudio;
    [SerializeField] private AudioSource _bossDeathAudio;

    private PlayerHealth _playerHealth;
    private BossAttackController _attack;
    private BossTeleport _teleport;

    protected override void Start()
    {
        base.Start();

        SetBossHealth();
        SetHealthSlider();

        _playerHealth = PlayerHealth.Instance;
        _attack = GetComponent<BossAttackController>();
        _teleport = GetComponent<BossTeleport>();

        _attack.BossActivate(_playerHealth, this);
        _teleport?.StartTeleporting();
        _playerHealth.ActivateAttack();

        if (_gameManager == null)
        {
            Phase phase = GetComponentInParent<Phase>();
            _gameManager = phase.GameManager;
        }

        _gameManager.EnterBossRoom(CurrentHealth);
    }

    // ------- IF BOSS NOT DEFEATED, BRING REMAINING HEALTH TO THE NEXT LEVEL -------
    //public void SetBossHealth()
    //{
    //    if (ConfigManager == null)
    //    {
    //        return;
    //    }
    //
    //    float bossHealth = ConfigManager.GetBossHealth();
    //    if (bossHealth == 0)
    //    {
    //        SetHealth(HealthAmount);
    //        return;
    //    }
    //    SetHealth(bossHealth);
    //}

    // ------- RESET BOSS HEALTH FOR EACH LEVEL ----------
    public void SetBossHealth()
    {
        SetHealth(HealthAmount);
    }

    public override void TakeDamage(float damage, DamageType type)
    {
        if (!CanTakeDamage())
        {
            return;
        }

        _ = StartCoroutine(Damage());
        _bossHitAudio.Play();
        base.TakeDamage(damage, type);

        SetHealthSlider();

        #region Boss died
        if (CurrentHealth <= 0)
        {
            //Boss dies you win
            _attack?.StopAttacks();
            _teleport?.StopTeleporting();
            _animator.SetTrigger("Dead");
            _bossDeathAudio.Play();
            float animationLenght = _animator.GetCurrentAnimatorStateInfo(0).length;
            StartCoroutine(HelperWait.ActionAfterWait(animationLenght, EndGame));
            return;
        }
        #endregion
    }

    private void EndGame()
    {
        _gameManager.EndGame(true, _playerHealth.GetCurrentHealth);
    }

    private void SetHealthSlider()
    {
        float healthPercentage = CurrentHealth / HealthAmount;
        _healthSlider.value = healthPercentage;
    }
}
