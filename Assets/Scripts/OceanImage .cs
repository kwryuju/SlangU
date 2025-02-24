using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class OceanImage : MonoBehaviour
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
        computeShader.SetVector("_MousePosition_0", Vector4.zero); // TODO

        var kernelHandle = computeShader.FindKernel(kernelName);
        computeShader.SetTexture(kernelHandle, "result_0", renderTexture);

        uint x, y, z;
        computeShader.GetKernelThreadGroupSizes(kernelHandle, out x, out y, out z);

        int threadGroupsX = (int)Mathf.Ceil(textureSize / (float)x);
        int threadGroupsY = (int)Mathf.Ceil(textureSize / (float)y);
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

    private void OnDestroy()
    {
        renderTexture?.Release();
    }
}
