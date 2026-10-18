using Rewind.Game;
using UnityEngine;

namespace Rewind.View
{
    public class ToolSwapper : MonoBehaviour
    {
        [Tooltip("Index 0 = no upgrades / tier 1, then one per tier 2..5")]
        [SerializeField] GameObject[] tools;

        int current = -2;

        void Update()
        {
            if (GameManager.Instance == null) return;
            var tier = GameManager.Instance.Economy.HighestOwnedTier;
            var index = Mathf.Clamp(tier, 0, tools.Length - 1);
            if (index == current) return;
            current = index;
            for (var i = 0; i < tools.Length; i++) tools[i].SetActive(i == index);
        }
    }
}
