using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = Instance.SetSingleton(this);
    }

    private void GameEnded()
    {
        Debug.Log("Game Ended");
    }



    public enum ColorType
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        Orange,
        Black,
        White,
        Gray
    }
}
