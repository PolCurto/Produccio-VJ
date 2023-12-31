using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class UIController : MonoBehaviour
{
    #region Global Variables
    public static UIController Instance;
    private PlayerInputActions _playerInputActions;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        Instance = this;
        _playerInputActions = new PlayerInputActions();
        _playerInputActions.Player.Enable();
        _playerInputActions.Player.Pause.performed += TogglePause;

        _playerInputActions.Player.Interact.performed += HideDeathScreen;
    }

    private void OnDestroy()
    {
        _playerInputActions.Player.Pause.performed -= TogglePause;
    }
    #endregion

    #region Pause Menu
    [Header ("Pause Menu")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject _selectedOptionPause;
    private bool _isPaused;

    /// <summary>
    /// Pauses / unpauses the game
    /// </summary>
    /// <param name="context">InputAction</param>
    public void TogglePause(InputAction.CallbackContext context)
    {
        if (!_isPaused)
        {
            _isPaused = true;
            pausePanel.SetActive(true);
            Time.timeScale = 0;

            EventSystem.current.SetSelectedGameObject(_selectedOptionPause);
        }
        else
        {
            Resume();
        }
    }

    /// <summary>
    /// Resumes the game
    /// </summary>
    public void Resume()
    {
        _isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1;
    }

    /// <summary>
    /// Goes to the main menu
    /// </summary>
    public void GoMainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Menu");
    }

    /// <summary>
    /// Exits the game
    /// </summary>
    public void ExitGame()
    {
        Application.Quit();
    }
    #endregion

    #region Death Screen
    [Header ("Death Screen")]
    [SerializeField] private GameObject _deathScreen;
    private bool _isDeathScreen;

    /// <summary>
    /// Shows the death screen
    /// </summary>
    public void ShowDeathScreen()
    {
        _isDeathScreen = true;
        _deathScreen.SetActive(true);
    }

    /// <summary>
    /// Reloads the game after dying
    /// </summary>
    /// <param name="context">InputAction</param>
    public void HideDeathScreen(InputAction.CallbackContext context)
    {
        if (!_isDeathScreen) return;

        _isDeathScreen = false;
        ScenesController.Instance.ReloadScene();
    }
    #endregion

    #region Level Complete Screen
    [Header("Level Complete Screen")]
    [SerializeField] private GameObject _levelCompleteScreen;
    [SerializeField] private GameObject _selectedOptionCompleted;

    /// <summary>
    /// Shows the level complete screen
    /// </summary>
    public void LevelCompleted()
    {
        _levelCompleteScreen.SetActive(true);
        Time.timeScale = 0;

        EventSystem.current.SetSelectedGameObject(_selectedOptionCompleted);
    }

    /// <summary>
    /// Loads the level that goes next to the currently completed one
    /// </summary>
    public void LoadNextLevel()
    {
        ScenesController.Instance.LoadNextScene();
    }
    #endregion
}