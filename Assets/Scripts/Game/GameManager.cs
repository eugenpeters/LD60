using System;
using Rewind.Audio;
using Rewind.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Rewind.Game
{
    public enum Flow { Menu, Playing, Paused, Won }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] EconomyConfig config;
        [SerializeField] float autosaveInterval = 10;

        public Economy Economy { get; private set; }
        public Flow Flow { get; private set; } = Flow.Playing;

        public event Action<Flow> FlowChanged;
        public event Action<BigNumber> Clicked;
        public event Action<int> TierPurchased;
        public event Action<int> MilestoneReached;
        public event Action<int> TierRevealed;
        public event Action GoalReached;

        float autosaveTimer;
        bool[] revealed = new bool[GameState.TierCount];

        void Awake()
        {
            Instance = this;
            if (config == null) config = EconomyConfig.CreateDefault();
            Economy = new Economy(config, SaveSystem.Load());
            Economy.Purchased += (tier, _) => { TierPurchased?.Invoke(tier); Save(); };
            Economy.MilestoneReached += tier => { AudioManager.Instance?.Play(Sfx.Milestone, 1, 0); MilestoneReached?.Invoke(tier); };
            Economy.GoalReached += OnGoal;
            SyncRevealed(notify: false);
        }

        void Start()
        {
            SetFlow(Flow.Playing);
        }

        public bool HasProgress => Economy.State.TotalDistance > BigNumber.Zero;

        public void Continue() => SetFlow(Flow.Playing);

        public void NewGame()
        {
            SaveSystem.Delete();
            Economy.Reset(new GameState());
            Array.Clear(revealed, 0, revealed.Length);
            SyncRevealed(notify: false);
            SetFlow(Flow.Playing);
        }

        public void Pause() { if (Flow == Flow.Playing) SetFlow(Flow.Paused); }
        public void Resume() { if (Flow == Flow.Paused) SetFlow(Flow.Playing); }
        public void KeepPlayingAfterWin() => SetFlow(Flow.Playing);

        void SetFlow(Flow f)
        {
            Flow = f;
            if (f == Flow.Playing) AudioManager.Instance?.PlayGameMusic();
            else if (f == Flow.Menu) AudioManager.Instance?.PlayMenuMusic();
            FlowChanged?.Invoke(f);
        }

        public bool TryBuy(int tier, int count)
        {
            if (Flow != Flow.Playing) return false;
            var ok = Economy.TryBuy(tier, count);
            AudioManager.Instance?.Play(ok ? Sfx.Buy : Sfx.Denied);
            return ok;
        }

        void OnGoal()
        {
            Save();
            AudioManager.Instance?.Play(Sfx.Win, 1, 0);
            GoalReached?.Invoke();
            SetFlow(Flow.Won);
        }

        void Update()
        {
            if (Flow != Flow.Playing) return;

            if (SpinPressedThisFrame()) Spin();
            Economy.Tick(Time.deltaTime);
            SyncRevealed(notify: true);

            autosaveTimer += Time.deltaTime;
            if (autosaveTimer >= autosaveInterval) Save();
        }

        static bool SpinPressedThisFrame()
        {
            var pressed = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) pressed = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) pressed = true;
            if (pressed && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) pressed = false;
            return pressed;
        }

        public void Spin()
        {
            var gain = Economy.Click();
            AudioManager.Instance?.Play(Sfx.Click, 0.7f, 0.12f);
            Clicked?.Invoke(gain);
        }

        void SyncRevealed(bool notify)
        {
            for (var i = 0; i < revealed.Length; i++)
            {
                if (revealed[i] || !Economy.IsRevealed(i)) continue;
                revealed[i] = true;
                if (notify && i > 0)
                {
                    AudioManager.Instance?.Play(Sfx.Unlock);
                    TierRevealed?.Invoke(i);
                }
            }
        }

        public void Save()
        {
            autosaveTimer = 0;
            SaveSystem.Save(Economy.State);
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationFocus(bool focus) { if (!focus && Flow == Flow.Playing) Save(); }
        void OnApplicationQuit() => Save();
    }
}
