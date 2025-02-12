using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;

public class Differential2DSplatter : MonoBehaviour
{
    public ComputeShader cs_clearDerivativesMain;
    private string kernel_clearDerivativesMain = "clearDerivativesMain";

    public ComputeShader cs_computeDerivativesMain;
    private string kernel_computeDerivativesMain = "computeDerivativesMain";

    public ComputeShader cs_updateBlobsMain;
    private string kernel_updateBlobsMain = "updateBlobsMain";

    public ComputeShader cs_imageMain;
    private string kernel_imageMain = "imageMain";

    private GraphicsBuffer blobsBuffer;
    private GraphicsBuffer derivBuffer;
    private GraphicsBuffer adamFirstMoment;
    private GraphicsBuffer adamSecondMoment;

    public Texture2D targetTexture;

    const int BLOB_BUFFER_SIZE = 184320;

    public int textureSize = 256;
    private RenderTexture renderTexture;

    void Start()
    {
        renderTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        var initData_rnd = Enumerable.Range(0, BLOB_BUFFER_SIZE)
                                     .Select(_ => UnityEngine.Random.Range(0.0f, 1.0f))
                                     .ToArray();
        var initData_zeroU = new uint[BLOB_BUFFER_SIZE]; // initialize 0 by default
        var initData_zeroF = new float[BLOB_BUFFER_SIZE]; // initialize 0 by default

        //[playground::RAND(BLOB_BUFFER_SIZE)]
        blobsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, BLOB_BUFFER_SIZE, sizeof(float));
        blobsBuffer.SetData(initData_rnd);

        //[playground::ZEROS(BLOB_BUFFER_SIZE)]
        derivBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, BLOB_BUFFER_SIZE, sizeof(uint));
        derivBuffer.SetData(initData_zeroU);

        //[playground::ZEROS(BLOB_BUFFER_SIZE)]
        adamFirstMoment = new GraphicsBuffer(GraphicsBuffer.Target.Structured, BLOB_BUFFER_SIZE, sizeof(float));
        adamFirstMoment.SetData(initData_zeroF);

        //[playground::ZEROS(BLOB_BUFFER_SIZE)]
        adamSecondMoment = new GraphicsBuffer(GraphicsBuffer.Target.Structured, BLOB_BUFFER_SIZE, sizeof(float));
        adamSecondMoment.SetData(initData_zeroF);
    }

    private void Update()
    {
        if (cs_clearDerivativesMain == null) return;
        if (cs_computeDerivativesMain == null) return;
        if (cs_updateBlobsMain == null) return;
        if (cs_imageMain == null) return;
        if (targetTexture == null) return;

        //! CALL(clearDerivativesMain, SIZE_OF(blobsBuffer))
        {
            var kernelHandle = cs_clearDerivativesMain.FindKernel(kernel_clearDerivativesMain);
            cs_clearDerivativesMain.SetBuffer(kernelHandle, "derivBuffer_0", derivBuffer);

            uint x, y, z;
            cs_clearDerivativesMain.GetKernelThreadGroupSizes(kernelHandle, out x, out y, out z);

            int threadGroupsX = (int)Mathf.Ceil(BLOB_BUFFER_SIZE / (float)x);
            cs_clearDerivativesMain.Dispatch(kernelHandle, threadGroupsX, 1, 1);
        }

        //! CALL(computeDerivativesMain, SIZE_OF(targetTexture))
        {
            var kernelHandle = cs_computeDerivativesMain.FindKernel(kernel_computeDerivativesMain);
            cs_computeDerivativesMain.SetBuffer(kernelHandle, "derivBuffer_0", derivBuffer);
            cs_computeDerivativesMain.SetBuffer(kernelHandle, "blobsBuffer_0", blobsBuffer);
            cs_computeDerivativesMain.SetTexture(kernelHandle, "targetTexture_0", targetTexture);

            uint x, y, z;
            cs_computeDerivativesMain.GetKernelThreadGroupSizes(kernelHandle, out x, out y, out z);

            int threadGroupsX = (int)Mathf.Ceil(targetTexture.width / (float)x);
            int threadGroupsY = (int)Mathf.Ceil(targetTexture.height / (float)y);
            cs_computeDerivativesMain.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);
        }

        //! CALL(updateBlobsMain, SIZE_OF(blobsBuffer))
        {
            var kernelHandle = cs_updateBlobsMain.FindKernel(kernel_updateBlobsMain);
            cs_updateBlobsMain.SetBuffer(kernelHandle, "derivBuffer_0", derivBuffer);
            cs_updateBlobsMain.SetBuffer(kernelHandle, "adamFirstMoment_0", adamFirstMoment);
            cs_updateBlobsMain.SetBuffer(kernelHandle, "adamSecondMoment_0", adamSecondMoment);
            cs_updateBlobsMain.SetBuffer(kernelHandle, "blobsBuffer_0", blobsBuffer);

            uint x, y, z;
            cs_updateBlobsMain.GetKernelThreadGroupSizes(kernelHandle, out x, out y, out z);

            int threadGroupsX = (int)Mathf.Ceil(BLOB_BUFFER_SIZE / (float)x);
            cs_updateBlobsMain.Dispatch(kernelHandle, threadGroupsX, 1, 1);
        }

        // imageMain
        {
            cs_imageMain.SetVector("U_ScreenSize_0", new Vector4(renderTexture.width, renderTexture.height, 1.0f / (float)renderTexture.width, 1.0f / (float)renderTexture.height));

            var kernelHandle = cs_imageMain.FindKernel(kernel_imageMain);
            cs_imageMain.SetBuffer(kernelHandle, "blobsBuffer_0", blobsBuffer);
            cs_imageMain.SetTexture(kernelHandle, "targetTexture_0", targetTexture);
            cs_imageMain.SetTexture(kernelHandle, "result_0", renderTexture);

            uint x, y, z;
            cs_imageMain.GetKernelThreadGroupSizes(kernelHandle, out x, out y, out z);

            int threadGroupsX = (int)Mathf.Ceil(textureSize / (float)x);
            int threadGroupsY = (int)Mathf.Ceil(textureSize / (float)y);
            cs_imageMain.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);
        }

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
        blobsBuffer?.Release();
        derivBuffer?.Release();
        adamFirstMoment?.Release();
        adamSecondMoment?.Release();

        renderTexture?.Release();
    }
}
