using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSequenceTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameStateManager.Instance.State = GameState.Start;
    }

    public void StartGame()
    {
        if (GameStateManager.Instance.State == GameState.Start)
        {
            GameStateManager.Instance.State = GameState.Day1;
        }
    }
}
