using System;
using System.Collections;
using UnityEngine;

public class BossAttackController : MonoBehaviour
{
    [SerializeField] private float _waitForStart;
    [SerializeField] private AttackPaternHealth[] _attackPaternHealth;

    [Header("Attack Warning")]
    [SerializeField] private GameObject _warningSprite;
    [SerializeField] private float _warningLeadTime = 1f;

    private int _attackPaternHealthIndex;
    private int _attackIndex;
    private Coroutine _attackCoroutine;
    private PlayerHealth _player;
    private BossHealth _bossHealth;

    #region structs
    [Serializable]
    public struct AttackPaternHealth
    {
        public float Health;
        public BossAttackPatern[] AttackPatern;
    }

    [Serializable]
    public struct BossAttackPatern
    {
        public BossAttack Attack;
        public float TimeBeforeNextAttack;
    }
    #endregion

    private void Awake()
    {
        if (_warningSprite != null)
        {
            _warningSprite.SetActive(false);
        }
    }

    //Activated once the player enters the boss room
    public void BossActivate(PlayerHealth player, BossHealth bossHealth)
    {
        _player = player;
        _bossHealth = bossHealth;
        SetCurrentAttackPatternIndex(bossHealth.GetCurrentHealth);
        WaitForAttack(_waitForStart);
    }

    private void SetCurrentAttackPatternIndex(float bossHealth)
    {
        _attackPaternHealthIndex = 0;
        for (int i = 0; i < _attackPaternHealth.Length; i++)
        {
            if (bossHealth <= _attackPaternHealth[i].Health)
            {
                _attackPaternHealthIndex = i;
            }
            else
            {
                break;
            }
        }
    }

    private void Attack()
    {
        _attackCoroutine = null;

        if (_attackPaternHealth == null || _attackPaternHealth.Length <= _attackPaternHealthIndex)
        {
            Debug.LogError("Boss Attack Index out of range!");
            return;
        }

        BossAttackPatern[] currentAttackPatern = _attackPaternHealth[_attackPaternHealthIndex].AttackPatern;
        if (_attackIndex >= currentAttackPatern.Length)
        {
            _attackIndex = 0;
            SetCurrentAttackPatternIndex(_bossHealth.GetCurrentHealth);
            Attack();
            return;
        }

        BossAttackPatern bossAttack = currentAttackPatern[_attackIndex];
        _attackIndex++;
        bossAttack.Attack.Attack(_player);
        WaitForAttack(bossAttack.TimeBeforeNextAttack);
    }

    private void WaitForAttack(float time)
    {
        _attackCoroutine = StartCoroutine(AttackAfterDelay(time));
    }

    private IEnumerator AttackAfterDelay(float time)
    {
        float warningTime = Mathf.Min(_warningLeadTime, time);
        float preWarningWait = Mathf.Max(0f, time - warningTime);

        yield return new WaitForSeconds(preWarningWait);

        if (_warningSprite != null)
        {
            _warningSprite.SetActive(true);
        }

        yield return new WaitForSeconds(warningTime);

        if (_warningSprite != null)
        {
            _warningSprite.SetActive(false);
        }

        Attack();
    }

    public void StopAttacks()
    {
        if (_attackCoroutine == null)
        {
            return;
        }
        StopCoroutine(_attackCoroutine);

        if (_warningSprite != null)
        {
            _warningSprite.SetActive(false);
        }
    }
}