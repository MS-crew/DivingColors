using System;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(fileName = "ComboAudioConfig",menuName = "Combo Audio Config")]
public class ComboAudioConfig : ScriptableObject
{
    public List<ComboAudio> ComboAudios;

    public AudioClip GetRandomClip(int x)
    {
        int minX = -1;
        ComboAudio selected = null;

        foreach (ComboAudio comboAudio in ComboAudios)
        {
            if (x >= comboAudio.MinX&& comboAudio.MinX > minX)
            {
                minX = comboAudio.MinX;
                selected = comboAudio;
            }
        }

        if (selected == null || selected.Variants == null || selected.Variants.Count == 0)
            return null;

        return selected.Variants.GetRandomValue();
    }

    [Serializable]
    public class ComboAudio
    {
        public int MinX;
        public List<AudioClip> Variants;
    }
}
