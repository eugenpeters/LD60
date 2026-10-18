using UnityEngine;

namespace Rewind.Audio
{
    public enum Sfx { Click, Buy, Denied, Milestone, Unlock, Zap, Win, UiClick }

    [CreateAssetMenu(menuName = "Rewind/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Music")]
        public AudioClip musicMenu;
        public AudioClip musicGame;

        [Header("SFX")]
        public AudioClip[] click;
        public AudioClip buy;
        public AudioClip denied;
        public AudioClip milestone;
        public AudioClip unlock;
        public AudioClip zap;
        public AudioClip win;
        public AudioClip uiClick;

        [Header("Loops")]
        public AudioClip motorLoop;
    }
}
