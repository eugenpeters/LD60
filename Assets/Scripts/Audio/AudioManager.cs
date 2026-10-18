using UnityEngine;

namespace Rewind.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] AudioLibrary library;
        [SerializeField] int sfxVoices = 12;

        AudioSource music;
        AudioSource motor;
        AudioSource[] voices;
        int nextVoice;
        float motorTargetPitch = 1, motorTargetVolume;

        AudioClip[] clickClips;
        AudioClip buy, denied, milestone, unlock, zap, win, uiClick;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;

            motor = gameObject.AddComponent<AudioSource>();
            motor.loop = true;
            motor.playOnAwake = false;

            voices = new AudioSource[sfxVoices];
            for (var i = 0; i < sfxVoices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }

            var lib = library != null ? library : ScriptableObject.CreateInstance<AudioLibrary>();
            clickClips = lib.click != null && lib.click.Length > 0 ? lib.click : new[] { ProceduralSfx.Click() };
            buy = lib.buy ? lib.buy : ProceduralSfx.Buy();
            denied = lib.denied ? lib.denied : ProceduralSfx.Denied();
            milestone = lib.milestone ? lib.milestone : ProceduralSfx.Milestone();
            unlock = lib.unlock ? lib.unlock : ProceduralSfx.Unlock();
            zap = lib.zap ? lib.zap : ProceduralSfx.Zap();
            win = lib.win ? lib.win : ProceduralSfx.Win();
            uiClick = lib.uiClick ? lib.uiClick : ProceduralSfx.UiClick();
            motor.clip = lib.motorLoop ? lib.motorLoop : ProceduralSfx.MotorLoop();
            library = lib;

            VolumeSettings.Changed += ApplyVolumes;
            ApplyVolumes();
        }

        void OnDestroy()
        {
            VolumeSettings.Changed -= ApplyVolumes;
            if (Instance == this) Instance = null;
        }

        void ApplyVolumes()
        {
            music.volume = VolumeSettings.Music;
        }

        public void PlayMenuMusic() => PlayMusic(library.musicMenu ? library.musicMenu : library.musicGame);
        public void PlayGameMusic() => PlayMusic(library.musicGame ? library.musicGame : library.musicMenu);

        void PlayMusic(AudioClip clip)
        {
            if (clip == null || music.clip == clip && music.isPlaying) return;
            music.clip = clip;
            music.Play();
        }

        public void Play(Sfx sfx, float volume = 1, float pitchJitter = 0.06f)
        {
            var clip = sfx switch
            {
                Sfx.Click => clickClips[Random.Range(0, clickClips.Length)],
                Sfx.Buy => buy,
                Sfx.Denied => denied,
                Sfx.Milestone => milestone,
                Sfx.Unlock => unlock,
                Sfx.Zap => zap,
                Sfx.Win => win,
                Sfx.UiClick => uiClick,
                _ => null,
            };
            if (clip == null) return;
            var v = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            v.pitch = 1 + Random.Range(-pitchJitter, pitchJitter);
            v.volume = volume * VolumeSettings.Sfx;
            v.clip = clip;
            v.Play();
        }

        public void SetMotor(float speed01, bool active)
        {
            motorTargetPitch = 0.5f + speed01 * 1.5f;
            motorTargetVolume = active ? Mathf.Clamp01(speed01 * 1.2f) * 0.5f : 0;
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            motor.pitch = Mathf.Lerp(motor.pitch, motorTargetPitch, dt * 4);
            motor.volume = Mathf.Lerp(motor.volume, motorTargetVolume * VolumeSettings.Sfx, dt * 6);
            if (motor.volume > 0.001f && !motor.isPlaying) motor.Play();
            else if (motor.volume <= 0.001f && motor.isPlaying) motor.Pause();
        }
    }
}
