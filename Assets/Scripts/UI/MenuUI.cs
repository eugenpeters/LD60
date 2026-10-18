using Rewind.Audio;
using Rewind.Core;
using Rewind.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rewind.UI
{
    public class MenuUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup backdrop;

        [Header("Pause")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;
        [SerializeField] TextMeshProUGUI musicValue;
        [SerializeField] TextMeshProUGUI sfxValue;

        [Header("Confirm restart")]
        [SerializeField] GameObject confirmPanel;
        [SerializeField] Button confirmYes;
        [SerializeField] Button confirmNo;

        [Header("Win")]
        [SerializeField] GameObject winPanel;
        [SerializeField] TextMeshProUGUI winStats;
        [SerializeField] Button keepPlayingButton;
        [SerializeField] Button winRestartButton;

        void Start()
        {
            var gm = GameManager.Instance;
            Hook(resumeButton, gm.Resume);
            Hook(restartButton, () => Show(confirmPanel));
            Hook(confirmYes, gm.NewGame);
            Hook(confirmNo, () => Show(pausePanel));
            Hook(keepPlayingButton, gm.KeepPlayingAfterWin);
            Hook(winRestartButton, gm.NewGame);

            musicSlider.SetValueWithoutNotify(VolumeSettings.Music);
            sfxSlider.SetValueWithoutNotify(VolumeSettings.Sfx);
            musicSlider.onValueChanged.AddListener(v => { VolumeSettings.Music = v; UpdateVolumeLabels(); });
            sfxSlider.onValueChanged.AddListener(v => { VolumeSettings.Sfx = v; UpdateVolumeLabels(); });
            UpdateVolumeLabels();

            gm.FlowChanged += OnFlow;
            OnFlow(gm.Flow);
        }

        static void Hook(Button b, UnityEngine.Events.UnityAction action)
        {
            b.onClick.AddListener(() => AudioManager.Instance?.Play(Sfx.UiClick));
            b.onClick.AddListener(action);
        }

        void UpdateVolumeLabels()
        {
            musicValue.text = Mathf.RoundToInt(VolumeSettings.Music * 100) + "%";
            sfxValue.text = Mathf.RoundToInt(VolumeSettings.Sfx * 100) + "%";
        }

        void OnFlow(Flow f)
        {
            switch (f)
            {
                case Flow.Paused:
                    Show(pausePanel);
                    break;
                case Flow.Won:
                    var s = GameManager.Instance.Economy.State;
                    int minutes = (int)(s.WinTime / 60), seconds = (int)(s.WinTime % 60);
                    winStats.text = $"You rewound the tape to the end of the multiverse\nin {minutes}m {seconds:00}s with {s.Clicks.ToString("N0", NumberFormatter.Display)} clicks.\n\nSomewhere, a Walkman clicks off.";
                    Show(winPanel);
                    break;
                default:
                    Show(null);
                    break;
            }
        }

        void Show(GameObject panel)
        {
            foreach (var p in new[] { pausePanel, winPanel, confirmPanel })
                p.SetActive(p == panel);
            var any = panel != null;
            backdrop.alpha = any ? 1 : 0;
            backdrop.blocksRaycasts = backdrop.interactable = any;
        }
    }
}
