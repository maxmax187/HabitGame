using DG.Tweening;
using System.Collections;
using UnityEngine;

public class Attack : MonoBehaviour
{
    public static Attack Instance { get; private set; }

    [SerializeField] private SpriteRenderer _attackCircle;
    [SerializeField] private Animator _attackAnimator;

    [Header("Damage Settings")]
    [SerializeField] private float _normalDamage = 5f;
    [SerializeField] private float _upgradedDamage = 10f;
    [SerializeField] private float _timeBetweenAttack = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource _attackAudio;

    private float _currentDamage = 5f;
    private bool _canUpgrade = true;
    private bool _isInBossRoom;
    private bool _bossInRange;
    private BossHealth _bossHealth;

    private Color _attackCircleColor;
    private float _attackCircleAlpha;
    private Vector3 _attackCircleScale;
    private float _lastAttackTime = -999f;

    public Vector2 UpgradeDamageInfo => new Vector2(_normalDamage, _upgradedDamage);
    public float CurrentDamage => _currentDamage;

    private void Awake()
    {
        Instance = this;
        Debug.Log($"[DEBUG Attack] Awake on GameObject '{gameObject.name}', InstanceID={GetInstanceID()}, default damage={_currentDamage}");

        if (_attackCircle != null)
        {
            _attackCircleColor = _attackCircle.color;
            _attackCircleAlpha = _attackCircleColor.a;
            _attackCircleScale = _attackCircle.transform.localScale;

            _attackCircleColor.a = 0f;
            _attackCircle.color = _attackCircleColor;
            _attackCircle.transform.localScale = Vector3.zero;
        }
    }

    // Do NOT reset _currentDamage in Start() because GameManager.SetupPlayerAttackRules()
    // runs during Start() and setting it here would overwrite test/training overrides!

    public void Initialize(bool canUpgrade, float overrideDamage = -1f)
    {
        _canUpgrade = canUpgrade;
        _currentDamage = (overrideDamage > 0f) ? overrideDamage : _normalDamage;
        Debug.Log($"[Attack.Initialize] Damage set to: {_currentDamage}, CanUpgrade: {_canUpgrade}");
    }

    public void UpgradeAttack()
    {
        if (!_canUpgrade)
        {
            Debug.Log("[Attack.UpgradeAttack] Upgrade blocked (canUpgrade is false).");
            return;
        }

        _currentDamage = _upgradedDamage;
        Debug.Log($"[Attack.UpgradeAttack] Upgraded damage to: {_currentDamage}");
    }

    public void SetDamage(float damage)
    {
        _currentDamage = damage;
    }

    public void EnterBossRoom()
    {
        _isInBossRoom = true;
    }

    public bool DoAttack(Vector2 moveInput)
    {
        if (!_isInBossRoom || Time.time - _lastAttackTime < _timeBetweenAttack)
        {
            return false;
        }

        _lastAttackTime = Time.time;
        StartCoroutine(AttackRoutine(moveInput));
        return true;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.TryGetComponent<BossHealth>(out var boss))
        {
            _bossInRange = true;
            _bossHealth = boss;
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.TryGetComponent<BossHealth>(out _))
        {
            _bossInRange = false;
        }
    }

    private IEnumerator AttackRoutine(Vector2 moveInput)
    {
        _attackAnimator.SetFloat("AttackX", moveInput.x);
        _attackAnimator.SetFloat("AttackY", moveInput.y);
        _attackAnimator.SetTrigger("Attack");

        if (_attackAudio != null)
        {
            _attackAudio.Play();
        }

        yield return new WaitForEndOfFrame();

        float duration = _attackAnimator.GetCurrentAnimatorStateInfo(0).length;

        _attackCircle.DOFade(_attackCircleAlpha, duration);
        _attackCircle.transform.DOScale(_attackCircleScale, duration).SetEase(Ease.OutElastic);

        yield return new WaitForSeconds(duration);

        if (_bossInRange && _bossHealth != null)
        {
            Debug.Log($"[Attack] Hitting boss for {_currentDamage} damage.");
            _bossHealth.TakeDamage(_currentDamage, DamageType.Player);
        }

        _attackCircleColor.a = 0f;
        _attackCircle.color = _attackCircleColor;
    }
}