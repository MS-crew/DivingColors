using System;
using System.Collections.Generic;

using static Assets.PublicEnums;

public static class EventManager
{
    #region Game Core Events

    public static event Action OnGameEnded;
    public static void GameEnded() => OnGameEnded?.Invoke();


    public static event Action OnGameFinished;
    public static void GameFinished() => OnGameFinished?.Invoke();


    public static event Action<Slide, List<ColorObject>> OnSlideUsed;
    public static void SlideUsed(Slide slide, List<ColorObject> collectedObjects) => OnSlideUsed?.Invoke(slide, collectedObjects);


    public static event Action<ColorType> OnObjectiveUpdated;
    public static void ObjectiveUpdated(ColorType colorType) => OnObjectiveUpdated?.Invoke(colorType);


    public static event Action<ColorObject> OnObjectiveExpired;
    public static void ObjectiveExpired(ColorObject objectiveObject) => OnObjectiveExpired?.Invoke(objectiveObject);


    public static event Action<int> OnClickAttemtUsed;
    public static void ClickAttemtUsed(int newValue) => OnClickAttemtUsed?.Invoke(newValue);

    #endregion

    #region Score Events

    public static event Action<int> OnUpdateScore;
    public static void UpdateScore(int newScore) => OnUpdateScore?.Invoke(newScore);


    public static event Action OnResetScore;
    public static void ResetScore() => OnResetScore?.Invoke();

    #endregion

    #region Ui Events

    public static event Action<string, ComboTier, int> OnComboUsed;
    public static void ComboUsed(string comboName, ComboTier tier, int x) => OnComboUsed?.Invoke(comboName, tier, x);

    #endregion
}
