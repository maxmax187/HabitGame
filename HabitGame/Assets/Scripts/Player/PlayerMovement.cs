using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The players movement script takes the input and sets them to velocity
/// </summary>

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _movementSpeed = 5f;
    [SerializeField] private Attack _attack;

    [Header("Audio")]
    [SerializeField] private AudioSource _walkingAudio;

    private UIManager _uiManager;
    private Rigidbody2D _rigidbody;
    private Vector2 _moveInput;
    private bool _isInMinigameRange;
    private CameraFollow _cameraFollow;
    private Animator _animator;

    private bool _isAttacking;
    private bool _movementLocked;

    public bool IsInMinigameRange
    {
        set => _isInMinigameRange = value;
    }

    #region Unity methods
    private void Start()
    {
        _cameraFollow = Camera.main.gameObject.GetComponent<CameraFollow>();
        _uiManager = UIManager.Instance;

        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
    }

    public void SetMovementLocked(bool locked)
    {
        _movementLocked = locked;
        if (locked)
        {
            _rigidbody.linearVelocity = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        if (_isAttacking || _movementLocked)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            return;
        }

        _rigidbody.linearVelocity = _moveInput * _movementSpeed;
    }

    #endregion

    public void Move(InputAction.CallbackContext callbackContext)
    {

        _moveInput = callbackContext.ReadValue<Vector2>();
        Vector3 newRotation = Vector3.zero;
        if (_moveInput.x < 0)
        {
            newRotation.y = 180;
        }
        _animator.transform.rotation = Quaternion.Euler(newRotation);

        if (callbackContext.canceled)
        {
            _walkingAudio.Stop();
            _animator.SetFloat("LastInputX", _moveInput.x);
            _animator.SetFloat("LastInputY", _moveInput.y);
            _animator.SetBool("isWalking", false);
            return;
        }

        _walkingAudio.Play();
        _animator.SetFloat("InputX", _moveInput.x);
        _animator.SetFloat("InputY", _moveInput.y);
        _animator.SetBool("isWalking", true);
    }

    public void Attack()
    {
        if (_isAttacking || !_attack.DoAttack(_moveInput))
        {
            return;
        }

        StartCoroutine(AttackRoutine());
    }

    //Todo Move player to next entrance and make them move X amount forward
    public void Entrance(Vector3 newPos)
    {
        //Set camera 
        transform.localPosition = newPos;
        _cameraFollow.PosToTarget();
    }

    #region Inputs
    public void OpenMinigamePopup()
    {
        if (_uiManager == null)
        {
            return;
        }

        if (_isInMinigameRange)
        {
            //OpenChest
            _uiManager.ShowMinigame(true);
        }
    }

    public void MinigameTap()
    {
        if (_uiManager == null)
        {
            return;
        }

        _uiManager.MinigameTap();
    }

    public void TutorialClick()
    {
        if (_uiManager == null)
        {
            return;
        }

        _uiManager.TutorialClick();
    }

    #endregion

    IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        // Set your parameters and trigger
        _animator.SetFloat("AttackX", _moveInput.x);
        _animator.SetFloat("AttackY", _moveInput.y);
        _animator.SetTrigger("Attack");

        // Wait for a tiny bit of time for the animator to transition
        yield return new WaitForEndOfFrame();

        // Get the actual length of the animation
        float duration = _animator.GetCurrentAnimatorStateInfo(0).length;

        // Wait for the animation to finish
        yield return new WaitForSeconds(duration);

        _isAttacking = false;
    }
}
