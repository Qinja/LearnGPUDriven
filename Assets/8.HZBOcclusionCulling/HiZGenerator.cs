using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HiZGenerator : MonoBehaviour
{
    public Material DMaterialHiZ;
    [NonSerialized]
    public Action HiZBufferUpdated;
    private Camera cameraComponent;
    private RenderTexture colorBuffer;
    private RenderTexture depthBuffer;
    private int maxMips;
    private Vector2Int hzbSize = new Vector2Int(-1, -1);
    private Vector2Int screenSize = new Vector2Int(-1, -1);
    public RenderTexture HiZBuffer { get; private set; }
    void Start()
    {
        cameraComponent = GetComponent<Camera>();
        UpdateFBAndHZB();
    }
    private void Update()
    {
        UpdateFBAndHZB();
    }
    void UpdateFBAndHZB()
    {
        var currentSize = new Vector2Int(Screen.width, Screen.height);
        if (screenSize != currentSize)
        {
            colorBuffer?.Release();
            depthBuffer?.Release();
            screenSize = currentSize;
            colorBuffer = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default)
            {
                name = "ColorBuffer",
                filterMode = FilterMode.Point,
            };
            depthBuffer = new RenderTexture(Screen.width, Screen.height, 32, RenderTextureFormat.Depth)
            {
                name = "DepthBuffer",
                filterMode = FilterMode.Point,
            };
            cameraComponent.SetTargetBuffers(colorBuffer.colorBuffer, depthBuffer.depthBuffer);

            var currentHZBSize = new Vector2Int(Mathf.NextPowerOfTwo(currentSize.x) / 2, Mathf.NextPowerOfTwo(currentSize.y) / 2);
            if (hzbSize != currentHZBSize)
            {
                hzbSize = currentHZBSize;
                var max = Mathf.Max(hzbSize.x, hzbSize.y);
                maxMips = 0;
                while (max > 1)
                {
                    max /= 2;
                    maxMips++;
                }
                HiZBuffer?.Release();
                HiZBuffer = new RenderTexture(hzbSize.x, hzbSize.y, 0, RenderTextureFormat.RFloat, maxMips + 1)
                {
                    name = "HiZBuffer",
                    useMipMap = true,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Point,
                };
                HiZBufferUpdated?.Invoke();
            }
        }
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(colorBuffer, destination);
        Graphics.Blit(depthBuffer, HiZBuffer, DMaterialHiZ);
        var w = HiZBuffer.width / 2;
        var h = HiZBuffer.height / 2;
        var src = HiZBuffer;
        for (int i = 1; i <= maxMips; i++)
        {
            RenderTexture rtTemp = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear, 1);
            Graphics.Blit(src, rtTemp, DMaterialHiZ);
            if (src != HiZBuffer) RenderTexture.ReleaseTemporary(src);
            Graphics.CopyTexture(rtTemp, 0, 0, HiZBuffer, 0, i);
            src = rtTemp;
            w = Mathf.Max(w / 2, 1);
            h = Mathf.Max(h / 2, 1);
        }
        if (src != HiZBuffer) RenderTexture.ReleaseTemporary(src);
    }
}
