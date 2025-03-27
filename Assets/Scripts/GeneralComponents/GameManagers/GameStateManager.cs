using UnityEngine;

public enum GameState
{
    None = 0,
    Start = 1,
    Day1 = 2,
    Day2 = 3,
    Day3 = 4,
    Day4 = 5,
    Day5 = 6,
    Day6 = 7,
    Day7 = 8,
}

public class GameStateManager : MonoBehaviour
{
    protected static GameStateManager _instance;
    protected GameState _state = GameState.None;

    // State Change Event.
    public delegate void OnStateChange(GameState newState);
    public OnStateChange onStateChangeCallback;

    public static GameStateManager Instance
    {
        get { return _instance; }
    }

    public GameState State
    {
        get { return _state; }
        set
        {
            // Current games state are lineary, so we don't want to go back a state.
            if (value > _state)
            {
                onStateChangeCallback?.Invoke(value);
                _state = value;
            }
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            _instance = this;
            _instance.State++;
        }
    }

    public void AdvanceGameState()
    {
        Instance.State++;
    }
}
