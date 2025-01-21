using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameSequenceTest : MonoBehaviour
{
    [SerializeField]
    protected InputAction startGameAction = null;

    // Start is called before the first frame update
    void Start()
    {
        GameStateManager.Instance.State = GameState.Start;
        startGameAction.performed += StartGame;
    }

    void OnEnable()
    {
        startGameAction.Enable();
    }

    void OnDisable()
    {
        startGameAction.Disable();
    }

    public void StartGame(InputAction.CallbackContext context)
    {
        if (GameStateManager.Instance.State == GameState.Start)
        {
            GameStateManager.Instance.State = GameState.Day1;
        }
    }
}
