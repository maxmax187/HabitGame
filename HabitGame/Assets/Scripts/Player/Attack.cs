using DG.Tweening;
using System.Collections;
using UnityEngine;

/// <summary>
/// Player attack script is so that the player can attack the boss
/// </summary>

public class Attack : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _attackCircle;

    [SerializeField] private Animator _attackAnimator;
    [SerializeField] private float _normalDamage;
    [SerializeField] private float _upgradeDamage;
    [SerializeField] private float _timeBetweenAttack;

    [Header("Audio")]
    [SerializeField] private AudioSource _attackAudio;

    private bool _isInBossRoom;
    private bool _bossInRange;
    private BossHealth _bossHealth;
    private float _doDamage;

    private Color _attackCircleColor;
    private float _attackCircleAlpha;
    private Vector3 _attackCircleScale;

    private float _lastAttackTime = -999f;

    public Vector2 UpgradeDamage
    {
        get { return new Vector2(_normalDamage, _upgradeDamage); }
    }

    private void Start()
    {
        _attackCircleColor = _attackCircle.color;
        _attackCircleAlpha = _attackCircleColor.a;
        _attackCircleScale = _attackCircle.transform.localScale;

        _attackCircleColor.a = 0f;
        _attackCircle.color = _attackCircleColor;
        _attackCircle.transform.localScale = Vector3.zero;

        _doDamage = _normalDamage;
    }

    public void UpgradeAttack()
    {
        _doDamage = _upgradeDamage;
    }

    //Gets triggerd on input
    public bool DoAttack(Vector2 moveInput)
    {
        if (!_isInBossRoom)
        {
            return false;
        }

        if (Time.time - _lastAttackTime < _timeBetweenAttack)
        {
            return false;
        }

        _lastAttackTime = Time.time;
        StartCoroutine(AttackRoutine(moveInput));

        return true;
    }

    //Gets triggerd when you enter the boss room
    public void BossRoom()
    {
        _isInBossRoom = true;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.TryGetComponent<BossHealth>(out BossHealth bossHealth))
        {
            _bossInRange = true;

            if (_bossHealth == null)
            {
                _bossHealth = bossHealth;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.gameObject.TryGetComponent<BossHealth>(out _))
        {
            _bossInRange = false;
        }
    }

    IEnumerator AttackRoutine(Vector2 moveInput)
    {
        _attackAnimator.SetFloat("AttackX", moveInput.x);
        _attackAnimator.SetFloat("AttackY", moveInput.y);
        _attackAnimator.SetTrigger("Attack");
        _attackAudio.Play();

        yield return new WaitForEndOfFrame();

        float duration = _attackAnimator.GetCurrentAnimatorStateInfo(0).length;

        _attackCircle.DOFade(_attackCircleAlpha, duration);
        _attackCircle.transform.DOScale(_attackCircleScale, duration).SetEase(Ease.OutElastic);

        yield return new WaitForSeconds(duration);

        if (_bossInRange && _bossHealth != null)
        {
            _bossHealth.TakeDamage(_doDamage, DamageType.Player);
        }

        _attackCircleColor.a = 0f;
        _attackCircle.color = _attackCircleColor;
    }
}
