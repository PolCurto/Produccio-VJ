using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    #region Variables
    [SerializeField] private GameObject[] _attackAreas;
    [SerializeField] private PlayerController _playerController;

    [Header("Attack settings")]
    [SerializeField] private float _attackDuration;
    [SerializeField] private float _attackCd;
    [SerializeField] private float _maxTimeBetweenAttacks;
    [SerializeField] private int _maxComboLength;

    [Header("Dodge settings")]
    [SerializeField] private float _dodgeCd;

    private PlayerInputActions _playerInputActions;
    private Queue<AttackTypes> _attackBuffer;
    private bool _isAttacking;
    private float _attackCdTimer;
    private int _currComboLength;
    private float _attackPerformed;
    private float _timer;
    private bool _isCombo;

    private bool _dodgeStance;
    private bool _isDodging;
    private float _dodgeCdTimer;
    private Animator _myAnimator;

    public enum DodgeType { HighDodge, LowDodge }
    public enum AttackTypes { FastAttack, SlowAttack }

    /// <summary>
    /// Player combo states to keep track of the current attack playing. The f for fast and s for slow
    /// </summary>
    public enum ComboStates { Idle, F, Ff, Fff, S, Sf}

    private ComboStates _comboState;
    private DodgeType _dodgeType;

    #endregion

    #region Unity methods
    private void Awake()
    {
        _playerInputActions = new PlayerInputActions();
        _playerInputActions.Player.Enable();

        _playerInputActions.Player.FastAttack.performed += FastAttackInput;
        _playerInputActions.Player.SlowAttack.performed += SlowAttackInput;
        _playerInputActions.Player.Throw.performed += ThrowBottle;
        _myAnimator = GetComponent<Animator>();
        _attackBuffer = new Queue<AttackTypes>();
    }

    private void OnDestroy()
    {
        _playerInputActions.Player.FastAttack.performed -= FastAttackInput;
        _playerInputActions.Player.SlowAttack.performed -= SlowAttackInput;
    }

    void Start()
    {
        foreach(GameObject attackArea in _attackAreas)
        {
            attackArea.SetActive(false);
        }
        _comboState = ComboStates.Idle;
    }

    void Update()
    {
        HandleTimers();
        HandleDodge();
        ExecuteAttack();
    }
    #endregion

    /// <summary>
    /// Controls the attack and dodge timers
    /// </summary>
    private void HandleTimers()
    {
        if (!_isAttacking) _attackCdTimer += Time.deltaTime;
        if (!_isDodging) _dodgeCdTimer += Time.deltaTime;
        _timer += Time.deltaTime;
    }

    #region Attack
    /// <summary>
    /// Method called when the fastAttack button is pressed
    /// </summary>
    public void FastAttackInput(InputAction.CallbackContext context)
    {
        //Only attacks if the player is not dodging or it is not in coolDown
        if (context.performed && !_dodgeStance && !_isDodging && _attackCdTimer > _attackCd)
        {
            if (_isAttacking) _attackBuffer.Clear();
            _attackBuffer.Enqueue(AttackTypes.FastAttack);
        }
    }

    /// <summary>
    /// Method called when the slowAttack button is pressed
    /// </summary>
    public void SlowAttackInput(InputAction.CallbackContext context)
    {
        //Only attacks if the player is not dodging or it is not in coolDown
        if (context.performed && !_dodgeStance && !_isDodging && _attackCdTimer > _attackCd)
        {
            if (_isAttacking) _attackBuffer.Clear();
            _attackBuffer.Enqueue(AttackTypes.SlowAttack);
        }
    }

    public void ThrowBottle(InputAction.CallbackContext context)
    {
        if (context.performed && !_dodgeStance && !_isDodging && !_isCombo)
        {
            if (_playerController.TryGetItem(InventoryItem.ItemType.Bottle, out InventoryItem bottleData))
            {
                Vector2 direction = _playerInputActions.Player.Aim.ReadValue<Vector2>();
                Bottle bottle = (Bottle)bottleData;
                bottle.Object.GetComponent<BottleScript>().Throw(direction);
            }
        }
    }

    /// <summary>
    /// Executes the next attack stored in the input buffer
    /// </summary>
    private void ExecuteAttack()
    {
        if (_isAttacking) return;

        if (_timer - _attackPerformed > _maxTimeBetweenAttacks && _isCombo)
        {
            _myAnimator.SetBool("isCombo", false);
            _isCombo = false;
            _comboState = ComboStates.Idle;
            _attackCdTimer = 0;
        }

        if (_attackBuffer.TryDequeue(out AttackTypes attack))
        {
            _isAttacking = true;
            _myAnimator.SetBool("isCombo", true);

            HandleCombos(attack);
        }
    }

    private void HandleCombos(AttackTypes attack)
    {
        _isCombo = true;

        if (_playerController.IsGrounded)
        {
            switch (_comboState)
            {
                case ComboStates.Idle:
                    if (attack == AttackTypes.FastAttack)
                    {
                        _myAnimator.SetTrigger("FastAttack");
                        _comboState = ComboStates.F;
                    }
                    else if (attack == AttackTypes.SlowAttack)
                    {
                        _myAnimator.SetTrigger("SlowAttack");
                        _comboState = ComboStates.S;
                    }
                    break;

                case ComboStates.F:
                    if (attack == AttackTypes.FastAttack)
                    {
                        _myAnimator.SetTrigger("FastAttack");
                        _comboState = ComboStates.Ff;
                    }
                    else if (attack == AttackTypes.SlowAttack)
                    {
                        _myAnimator.SetTrigger("SlowAttack");
                        _isCombo = false;
                    }
                    break;

                case ComboStates.Ff:
                    if (attack == AttackTypes.FastAttack)
                    {
                        _myAnimator.SetTrigger("FastAttack");
                        _isCombo = false;
                    }
                    else if (attack == AttackTypes.SlowAttack)
                    {
                        _myAnimator.SetTrigger("SlowAttack");
                        _isCombo = false;
                    }
                    break;

                case ComboStates.S:
                    if (attack == AttackTypes.FastAttack)
                    {
                        _myAnimator.SetTrigger("FastAttack");
                        _comboState = ComboStates.Sf;
                    }
                    else if (attack == AttackTypes.SlowAttack)
                    {
                        _myAnimator.SetTrigger("SlowAttack");
                        _isCombo = false;
                    }
                    break;

                case ComboStates.Sf:
                    if (attack == AttackTypes.FastAttack)
                    {
                        _isAttacking = false;
                        EndCombo();
                    }
                    else if (attack == AttackTypes.SlowAttack)
                    {
                        _myAnimator.SetTrigger("SlowAttack");
                        _isCombo = false;
                    }
                    break;

            }
        }
        else
        {
            switch (_comboState)
            {
                case ComboStates.Idle:
                    if (attack == AttackTypes.FastAttack)
                    {
                        _myAnimator.SetTrigger("FastAttack");
                        _comboState = ComboStates.F;
                    }
                    else if (attack == AttackTypes.SlowAttack)
                    {
                        _myAnimator.SetTrigger("SlowAttack");
                        _isCombo = false;
                        MovingDownAttackStart(50);
                    }
                    break;

                case ComboStates.F:
                    if (attack == AttackTypes.FastAttack)
                    {
                        _myAnimator.SetTrigger("FastAttack");
                        _comboState = ComboStates.Ff;
                    }
                    else if (attack == AttackTypes.SlowAttack)
                    {
                        _myAnimator.SetTrigger("SlowAttack");
                        _isCombo = false;
                        MovingDownAttackStart(50);
                    }
                    break;

                case ComboStates.Ff:
                    if (attack == AttackTypes.FastAttack)
                    {
                        _myAnimator.SetTrigger("FastAttack");
                        _comboState = ComboStates.Fff;
                    }
                    else if (attack == AttackTypes.SlowAttack)
                    {
                        _myAnimator.SetTrigger("SlowAttack");
                        _isCombo = false;
                        MovingDownAttackStart(50);
                    }
                    break;

                case ComboStates.Fff:
                    if (attack == AttackTypes.FastAttack)
                    {
                        _isAttacking = false;
                        EndCombo();
                    }
                    else if (attack == AttackTypes.SlowAttack)
                    {
                        _myAnimator.SetTrigger("SlowAttack");
                        _isCombo = false;
                        MovingDownAttackStart(50);
                    }
                    break;
            }
        }
    }

    public void EndDownAttack()
    {
        _attackAreas[2].SetActive(false);
        AttackFinished(1);
    }

    private void EndCombo()
    {
        _comboState = ComboStates.Idle;
        _attackCdTimer = 0;
        _myAnimator.SetBool("isCombo", false);
        _attackBuffer.Clear();
    }

    /// <summary>
    /// Enables the attack area (animator method)
    /// </summary>
    private void EnableAttackArea(int type)
    {
        AttackTypes attackType = (AttackTypes)type;

        switch (attackType)
        {
            case AttackTypes.FastAttack:
                _attackAreas[0].SetActive(true);
                break;
            case AttackTypes.SlowAttack:
                _attackAreas[1].SetActive(true);
                break;
        }
    }

    private void MovingSideAttackStart(int velocity)
    {
        _playerController.IsOverride = true;
        _playerController.Rigidbody.velocity = new Vector2(velocity, 0) * transform.right;
    }

    private void MovingDownAttackStart(int velocity)
    {
        _attackAreas[2].SetActive(true);
        _playerController.IsOverride = true;
        _playerController.Rigidbody.velocity = new Vector2(0, -30);
        _playerController.IsAttackingDown = true;
    }

    /// <summary>
    /// Disables the attack area (animator method)
    /// </summary>
    private void DisableAttackArea(int type)
    {
        AttackTypes attackType = (AttackTypes)type;

        switch (attackType)
        {
            case AttackTypes.FastAttack:
                _attackAreas[0].SetActive(false);
                break;
            case AttackTypes.SlowAttack:
                _attackAreas[1].SetActive(false);
                break;
        }
    }

    /// <summary>
    /// Method called when the attack animation is over (animator method)
    /// </summary>
    private void AttackFinished(int isMoving)
    {
        if (isMoving == 1)
            _playerController.IsOverride = false;

        if (!_isCombo)
            EndCombo();

        _attackPerformed = _timer;
        _isAttacking = false;          
    }
    #endregion

    #region Dodge
    /// <summary>
    /// Handles the dodge logic when holding the dodge trigger buttond and moving to the upper or lower direction axis
    /// </summary>
    private void HandleDodge()
    {
        if (!_playerController.IsGrounded) return;

        if (_playerInputActions.Player.DodgeTrigger.ReadValue<float>() == 1) _dodgeStance = true;
        else _dodgeStance = false;

        _myAnimator.SetBool("isDodging", _dodgeStance);

        if (_dodgeStance)
        {
            float dodgeDirection = _playerInputActions.Player.DodgeInput.ReadValue<float>();

            if (_dodgeCdTimer > _dodgeCd)
            {
                if (dodgeDirection == 1)
                {
                    ExecuteDodge(DodgeType.HighDodge);
                }
                if (dodgeDirection == -1)
                {
                    ExecuteDodge(DodgeType.LowDodge);
                }
            }
        }
    }
    
    /// <summary>
    /// Executes the dodge action
    /// </summary>
    /// <param name="dodgeType">The dodge type</param>
    private void ExecuteDodge(DodgeType dodgeType)
    {
        Debug.Log("Dodging");
        _dodgeCdTimer = 0;
        _isDodging = true;
        _dodgeType = dodgeType;

        switch (dodgeType)
        {
            case DodgeType.LowDodge:
                _myAnimator.SetTrigger("DodgeDown");
                break;
            case DodgeType.HighDodge:
                _myAnimator.SetTrigger("DodgeUp");
                break;
        }
    }

    /// <summary>
    /// Stops the dodge action
    /// </summary>
    public void StopDodge()
    {
        _isDodging = false;
    }

    /// <summary>
    /// Method called when the player successfully dodges an attack
    /// </summary>
    public void OnDodge()
    {
        Debug.Log("Dodged");
    }
    #endregion

    public void Die()
    {
        _playerInputActions.Player.Disable();
    }

    #region Getters
    /// <summary>
    /// Returns the dodgeStance bool
    /// </summary>
    public bool DodgeStance => _dodgeStance;

    /// <summary>
    /// Returns the dodge type
    /// </summary>
    public DodgeType GetDodgeType => _dodgeType;

    /// <summary>
    /// Returns wheter the player is dodging or not
    /// </summary>
    public bool IsDodging => _isDodging;

    /// <summary>
    /// Returns wheter the player is attacking or not
    /// </summary>
    public bool IsAttacking => _isAttacking;

    /// <summary>
    /// Returns wheter the player is doing a combo or not
    /// </summary>
    public bool IsCombo => _isCombo;
    #endregion

}