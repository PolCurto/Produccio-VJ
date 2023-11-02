using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HomuraCombat : MonoBehaviour
{
    #region Variables
    [Header("Attack settings")]
    [SerializeField] private GameObject _attackArea;
    [SerializeField] private float _attackDuration;
    [SerializeField] private float _attackCd;

    [Header("Dodge settings")]
    [SerializeField] private GameObject _hurtbox;
    [SerializeField] private float _dodgeDuration;
    [SerializeField] private float _dodgeCd;

    [Header("Other settings")]
    [SerializeField] private float _hitStopDuration;

    private PlayerInputActions _playerInputActions;
    private bool _isAttacking;
    private float _attackCdTimer;

    private bool _dodgeStance;
    private bool _isDodgingUp;
    private bool _isDodgingDown;
    private float _dodgeCdTimer;

    private HitStopController _hitStopController;

    Animator _myAnimator;
    #endregion

    #region Unity methods
    private void Awake()
    {
        _playerInputActions = new PlayerInputActions();
        _playerInputActions.Player.Enable();

        _playerInputActions.Player.Attack.performed += AttackInput;
        _hitStopController = FindAnyObjectByType<HitStopController>();

        if (_attackArea == null)
        {
            Debug.LogError("[PlayerAttack] La refer�ncia a Attack Area �s null");
        }
        if (_hurtbox == null)
        {
            Debug.LogError("[PlayerAttack] La refer�ncia a Hurtbox �s null");
        }

        _myAnimator = GetComponentInChildren<Animator>();
    }

    private void OnDestroy()
    {
        _playerInputActions.Player.Attack.performed -= AttackInput;
    }

    void Start()
    {
        _attackArea.SetActive(false);
    }

    void Update()
    {
        HandleTimers();
    }

    private void FixedUpdate()
    {
        HandleDodge();
    }
    #endregion

    private void HandleTimers()
    {
        if (!_isAttacking) _attackCdTimer += Time.deltaTime;
        if (!_isDodgingUp && !_isDodgingDown) _dodgeCdTimer += Time.deltaTime;
    }

    /// <summary>
    /// Handles the dodge logic when holding the dodge trigger buttond and moving to the upper or lower direction axis
    /// </summary>
    private void HandleDodge()
    {
        if (_playerInputActions.Player.DodgeTrigger.ReadValue<float>() == 1) _dodgeStance = true;
        else _dodgeStance = false;

        if (_dodgeStance)
        {
            float dodgeDirection = _playerInputActions.Player.DodgeInput.ReadValue<float>();

            if (_dodgeCdTimer > _dodgeCd)
            {

                if (dodgeDirection == 1)
                {
                    _isDodgingUp = true;
                    Invoke(nameof(StopDodge), _dodgeDuration);
                }
                if (dodgeDirection == -1)
                {
                    _isDodgingDown = true;
                    Invoke(nameof(StopDodge), _dodgeDuration);
                }
            }
        }
    }

    /// <summary>
    /// Executes the attack coroutine when the button is pressed and the player is not pressing the dodge trigger button
    /// </summary>
    public void AttackInput(InputAction.CallbackContext context)
    {
        if (context.performed && !_dodgeStance)
        {
            if (_attackCdTimer > _attackCd)
            {
                
                StartCoroutine(ExecuteAttack());
                
            }
                

        }
    }

    /// <summary>
    /// Activates the attack area during the set time and disables it afterwards
    /// </summary>
    public IEnumerator ExecuteAttack()
    {
        _myAnimator.ResetTrigger("endAttack");
        _myAnimator.SetTrigger("startAttack");
        _isAttacking = true;
        _attackArea.SetActive(true);
        _attackCdTimer = 0;

        yield return new WaitForSeconds(_attackDuration);

        _myAnimator.ResetTrigger("startAttack");
        _myAnimator.SetTrigger("endAttack");
        _attackArea.SetActive(false);
        _isAttacking = false;
    }

    /// <summary>
    /// Determines the dodging direction of the player and deactivates it after the set time has passed
    /// </summary>
    public void StopDodge()
    {
        _isDodgingUp = false;
        _isDodgingDown = false;
    }

    /// <summary>
    /// Returns the dodgeStance bool
    /// </summary>
    public bool DodgeStance()
    {
        return _dodgeStance;
    }

    /// <summary>
    /// Handles all the collision interactions
    /// </summary>
    public void OnHurtboxTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyCollisionHandler(collision);
        }
    }

    /// <summary>
    /// Handles all the enemy collisions
    /// </summary>
    private void EnemyCollisionHandler(Collider2D collision)
    {
        if (_isDodgingUp)
        {
            if (collision.GetComponent<TestDodge>().GetAttackType()) OnDodge();
            else OnEnemyHit();
        }
        else if (_isDodgingDown)
        {
            if (collision.GetComponent<TestDodge>().GetAttackType()) OnEnemyHit();
            else OnDodge();
        }
        else
        {
            OnEnemyHit();
        }
    }

    private void OnEnemyHit()
    {
        Debug.Log("Enemy Hit");
        _hitStopController.StopTime(0f, _hitStopDuration);
    }

    private void OnDodge()
    {
        Debug.Log("Dodged");
    }
}