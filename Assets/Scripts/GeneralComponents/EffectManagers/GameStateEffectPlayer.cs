using UnityEngine;

public class GameStateEffectPlayer : MonoBehaviour
{
    [SerializeField]
    protected Effect gameStartEffect = null;

    [SerializeField]
    protected Effect day1Effect = null;

    [SerializeField]
    protected Effect day2Effect = null;

    [SerializeField]
    protected Effect day3Effect = null;

    [SerializeField]
    protected Effect day4Effect = null;

    [SerializeField]
    protected Effect day5Effect = null;

    [SerializeField]
    protected Effect day6Effect = null;

    [SerializeField]
    protected Effect day7Effect = null;

    void Start()
    {
        GameStateManager.Instance.onStateChangeCallback += playStateEffect;
    }

    private void playStateEffect(GameState newState)
    {
        switch (newState)
        {
            case GameState.Start:
                gameStartEffect?.PlayFromEvent();
                break;
            case GameState.Day1:
                day1Effect?.PlayFromEvent();
                break;
            case GameState.Day2:
                day2Effect?.PlayFromEvent();
                break;
            case GameState.Day3:
                day3Effect?.PlayFromEvent();
                break;
            case GameState.Day4:
                day4Effect?.PlayFromEvent();
                break;
            case GameState.Day5:
                day5Effect?.PlayFromEvent();
                break;
            case GameState.Day6:
                day6Effect?.PlayFromEvent();
                break;
            case GameState.Day7:
                day7Effect?.PlayFromEvent();
                break;
            default:
                break;
        }
    }
}
