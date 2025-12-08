using System;
using System.Collections.Generic;

using static Assets.PublicEnums;

[Serializable]
public abstract class SlideCombo
{
    public abstract byte Priority { get; }

    public abstract string ComboName { get; }

    public abstract ComboTier ComboTier { get; }

    public abstract float CameraShakeDuration { get; }

    public abstract float CameraShakeIntensity { get; }

    public int Multiplier { get; set; }

    public abstract bool IsCanApply(LevelManager lm, List<ColorObject> selecteds);

    public virtual void Apply(LevelManager lm, List<ColorObject> selecteds)
    {
        if (CameraShakeController.Instance != null)
            CameraShakeController.Instance.Shake(CameraShakeIntensity + Multiplier * 0.08f, CameraShakeDuration + Multiplier * 0.05f, true);

        EventManager.ComboUsed(ComboName, ComboTier, Multiplier);
    }
}

