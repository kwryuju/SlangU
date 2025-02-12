using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class CircleImage : MonoBehaviour
{
    public ComputeShader computeShader;
    public int textureSize = 256;
    public string kernelName = "";

    private RenderTexture renderTexture;
    void Start()
    {
        renderTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
    }

    private void Update()
    {
        if (computeShader == null) return;
        if (string.IsNullOrEmpty(kernelName)) return;

        computeShader.SetVector("U_ScreenSize_0", new Vector4(renderTexture.width, renderTexture.height, 1.0f / (float)renderTexture.width, 1.0f / (float)renderTexture.height));
        computeShader.SetVector("_Time_0", new Vector4(Time.time / 20.0f, Time.time, Time.time * 2, Time.time * 3));

        var kernelHandle = computeShader.FindKernel(kernelName);
        computeShader.SetTexture(kernelHandle, "result_0", renderTexture);

        int threadGroupsX = textureSize / 8;
        int threadGroupsY = textureSize / 8;
        computeShader.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.mainTexture = renderTexture;
        }
        else
        {
            Debug.LogWarning("Renderer componet is not found.");
        }
    }
}
