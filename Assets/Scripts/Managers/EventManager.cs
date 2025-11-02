using System;

public static class EventManager
{
    #region Game Core Events
    public static event Action OnLevelFinished;
    public static void LevelFinished() => OnLevelFinished?.Invoke();


    public static event Action<Slide> OnSlideUsed;
    public static void SlideUsed(Slide slide) => OnSlideUsed?.Invoke(slide);

    #endregion

    #region Score Events

    public static event Action<int> OnUpdateScore;
    public static void UpdateScore(int newScore) => OnUpdateScore?.Invoke(newScore);


    public static event Action OnResetScore;
    public static void ResetScore() => OnResetScore?.Invoke();

    #endregion
}
