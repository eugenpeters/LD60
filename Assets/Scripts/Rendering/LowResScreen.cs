using UnityEngine;
using UnityEngine.UI;

namespace Rewind.Rendering
{
    public class LowResScreen : MonoBehaviour
    {
        [SerializeField] Camera sceneCamera;
        [SerializeField] RawImage screen;
        [SerializeField] Material crtTemplate;
        [SerializeField] int verticalResolution = 360;

        public Material Material { get; private set; }

        RenderTexture rt;
        int lastW, lastH;

        void Awake()
        {
            Material = new Material(crtTemplate);
            screen.material = Material;
            Rebuild();
        }

        void Update()
        {
            if (Screen.width != lastW || Screen.height != lastH) Rebuild();
        }

        void Rebuild()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            var aspect = lastH > 0 ? lastW / (float)lastH : 16f / 9f;
            var h = verticalResolution;
            var w = Mathf.Max(16, Mathf.RoundToInt(h * aspect));

            if (rt != null)
            {
                sceneCamera.targetTexture = null;
                rt.Release();
                Destroy(rt);
            }
            rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "LowResScene",
            };
            rt.Create();
            sceneCamera.targetTexture = rt;
            screen.texture = rt;
            Material.SetFloat("_ScanlineCount", h);
        }

        void OnDestroy()
        {
            if (rt != null) rt.Release();
        }
    }
}
