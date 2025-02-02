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
        startGameAction.performed += ProgressState;
    }

    void OnEnable()
    {
        startGameAction.Enable();
    }

    void OnDisable()
    {
        startGameAction.Disable();
    }

    public void ProgressState(InputAction.CallbackContext context)
    {
        switch (GameStateManager.Instance.State)
        {
            case GameState.Start:
                GameStateManager.Instance.State = GameState.Day1;
                break;
            case GameState.Day1:
                GameStateManager.Instance.State = GameState.Day2;
                break;
        }

        // if (GameStateManager.Instance.State == GameState.Start)
        // {
        //     GameStateManager.Instance.State = GameState.Day1;
        // }
    }
}
