using System;

public static class EventManager
{
    public static event Action OnLevelFinished;
    public static void LevelFinished() => OnLevelFinished?.Invoke();


    public static event Action<Slide> OnSlideUsed;
    public static void SlideUsed(Slide slide) => OnSlideUsed?.Invoke(slide);
}
