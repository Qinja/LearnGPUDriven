using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[RequireComponent(typeof(Camera))]
public class HiZGenerator : MonoBehaviour
{
    public Material DMaterialHiZ;
    [NonSerialized]
    public Action<RenderTexture> HiZBufferUpdated;
    private RenderTexture HiZBuffer;
    private Camera cameraComponent;
    private RenderTexture renderTexture;
    private int maxMips;
    private Vector2Int hzbSize = new Vector2Int(-1, -1);
    private Vector2Int screenSize = new Vector2Int(-1, -1);
    private RenderTexture[] HiZBufferIntermediates;
    private Material[] hiZMaterialIntermediates;
    void Start()
    {
        cameraComponent = GetComponent<Camera>();
    }
    void OnPreRender()
    {
        var currentSize = new Vector2Int(Screen.width, Screen.height);
        if (screenSize != currentSize)
        {
            cameraComponent.targetTexture?.Release();
            cameraComponent.targetTexture = null;
            renderTexture?.Release();
            screenSize = currentSize;
            renderTexture = new RenderTexture(Screen.width, Screen.height, 32, RenderTextureFormat.Default)
            {
                name = "Scene RenderTexture",
            };

            var currentHZBSize = new Vector2Int(Mathf.NextPowerOfTwo(currentSize.x) / 2, Mathf.NextPowerOfTwo(currentSize.y) / 2);
            if (hzbSize != currentHZBSize)
            {
                hzbSize = currentHZBSize;
                var max = Mathf.Max(hzbSize.x, hzbSize.y);
                maxMips = 0;
                while (max > 2)
                {
                    max /= 2;
                    maxMips++;
                }
                HiZBuffer?.Release();
                HiZBuffer = new RenderTexture(hzbSize.x, hzbSize.y, 0, GraphicsFormat.R32_UInt, maxMips + 1)
                {
                    name = "HiZBuffer",
                    useMipMap = true,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Point,
                };
                HiZBufferUpdated?.Invoke(HiZBuffer);
                if (HiZBufferIntermediates != null)
                {
                    for (int i = 0; i < HiZBufferIntermediates.Length; i++)
                    {
                        RenderTexture.ReleaseTemporary(HiZBufferIntermediates[i]);
                    }
                }
                HiZBufferIntermediates = new RenderTexture[maxMips];
                hiZMaterialIntermediates = new Material[maxMips];
                for (int i = 0, w = hzbSize.x, h = hzbSize.y; i < maxMips; i++)
                {
                    w = Mathf.Max(w / 2, 1);
                    h = Mathf.Max(h / 2, 1);
                    HiZBufferIntermediates[i] = RenderTexture.GetTemporary(w, h, 0, GraphicsFormat.R32_UInt);
                    HiZBufferIntermediates[i].name = "HiZBufferIntermediate(mip" + (i + 1) + ")";
                    HiZBufferIntermediates[i].filterMode = FilterMode.Point;
                    hiZMaterialIntermediates[i] = new Material(DMaterialHiZ);
                    hiZMaterialIntermediates[i].SetTexture("_DepthTex", i == 0 ? HiZBuffer : HiZBufferIntermediates[i - 1]);
                }
            }
        }
        cameraComponent.targetTexture = renderTexture;
        DMaterialHiZ.SetTexture("_DepthTex", renderTexture, UnityEngine.Rendering.RenderTextureSubElement.Depth);
    }
    void OnPostRender()
    {
        Graphics.Blit(null, HiZBuffer, DMaterialHiZ);
        for (int i = 0; i < maxMips; i++)
        {
            RenderTexture dst = HiZBufferIntermediates[i];
            Graphics.Blit(null, dst, hiZMaterialIntermediates[i]);
            Graphics.CopyTexture(dst, 0, 0, HiZBuffer, 0, i + 1);
        }
        Graphics.Blit(renderTexture, (RenderTexture)null);
        cameraComponent.targetTexture = null;
    }
    private void OnDestroy()
    {
        if (HiZBufferIntermediates != null)
        {
            for (int i = 0; i < HiZBufferIntermediates.Length; i++)
            {
                RenderTexture.ReleaseTemporary(HiZBufferIntermediates[i]);
            }
        }
        renderTexture?.Release();
        HiZBuffer?.Release();
        HiZBufferUpdated?.Invoke(null);
    }
}
