using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[RequireComponent(typeof(Camera))]
public class HiZGenerator : MonoBehaviour
{
    public Material DMaterialHiZ;
    [NonSerialized]
    public Action<RenderTexture> HiZBufferUpdated;
    private RenderTexture hiZBuffer;
    private bool hiZBufferReady = false;
    private Camera cameraComponent;
    private RenderTexture renderTexture;
    private int maxMips;
    private Vector2Int hzbSize = new Vector2Int(-1, -1);
    private Vector2Int screenSize = new Vector2Int(-1, -1);
    private RenderTexture[] hiZBufferIntermediates;
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
                hiZBuffer?.Release();
                HiZBufferUpdated?.Invoke(null);
                hiZBuffer = new RenderTexture(hzbSize.x, hzbSize.y, 0, GraphicsFormat.R32_UInt, maxMips + 1)
                {
                    name = "HiZBuffer",
                    useMipMap = true,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Point,
                };
                hiZBufferReady = false;
                if (hiZBufferIntermediates != null)
                {
                    for (int i = 0; i < hiZBufferIntermediates.Length; i++)
                    {
                        RenderTexture.ReleaseTemporary(hiZBufferIntermediates[i]);
                    }
                }
                hiZBufferIntermediates = new RenderTexture[maxMips];
                hiZMaterialIntermediates = new Material[maxMips];
                for (int i = 0, w = hzbSize.x, h = hzbSize.y; i < maxMips; i++)
                {
                    w = Mathf.Max(w / 2, 1);
                    h = Mathf.Max(h / 2, 1);
                    hiZBufferIntermediates[i] = RenderTexture.GetTemporary(w, h, 0, GraphicsFormat.R32_UInt);
                    hiZBufferIntermediates[i].name = "HiZBufferIntermediate(mip" + (i + 1) + ")";
                    hiZBufferIntermediates[i].filterMode = FilterMode.Point;
                    hiZMaterialIntermediates[i] = new Material(DMaterialHiZ);
                    hiZMaterialIntermediates[i].SetTexture("_DepthTex", i == 0 ? hiZBuffer : hiZBufferIntermediates[i - 1]);
                }
            }
        }
        cameraComponent.targetTexture = renderTexture;
        DMaterialHiZ.SetTexture("_DepthTex", renderTexture, UnityEngine.Rendering.RenderTextureSubElement.Depth);
    }
    void OnPostRender()
    {
        Graphics.Blit(null, hiZBuffer, DMaterialHiZ);
        for (int i = 0; i < maxMips; i++)
        {
            RenderTexture dst = hiZBufferIntermediates[i];
            Graphics.Blit(null, dst, hiZMaterialIntermediates[i]);
            Graphics.CopyTexture(dst, 0, 0, hiZBuffer, 0, i + 1);
        }
        if(!hiZBufferReady)
        {
            hiZBufferReady = true;
            HiZBufferUpdated?.Invoke(hiZBuffer);
        }
        Graphics.Blit(renderTexture, (RenderTexture)null);
        cameraComponent.targetTexture = null;
    }
    private void OnDestroy()
    {
        if (hiZBufferIntermediates != null)
        {
            for (int i = 0; i < hiZBufferIntermediates.Length; i++)
            {
                RenderTexture.ReleaseTemporary(hiZBufferIntermediates[i]);
            }
        }
        renderTexture?.Release();
        hiZBuffer?.Release();
        HiZBufferUpdated?.Invoke(null);
    }
}
