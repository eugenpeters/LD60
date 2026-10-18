using System;
using UnityEngine;

namespace Rewind.Audio
{
    public static class VolumeSettings
    {
        const string MusicKey = "vol_music";
        const string SfxKey = "vol_sfx";

        public static event Action Changed;

        public static float Music
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.33f);
            set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static float Sfx
        {
            get => PlayerPrefs.GetFloat(SfxKey, 0.33f);
            set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }
    }
}
