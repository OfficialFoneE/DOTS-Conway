using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public struct ConwayRenderer : IDisposable
{
    private const string GridCellMaterialPath = "Materials/GridMaterial";
    private const string CreateDrawCommandsComputeShaderPath = "Shaders/CreateDrawCommands";

    private Mesh GridCellMesh;
    public Material GridCellMaterial;

    private ComputeShader ComputeShader;
    private int ComputeShaderKernel;
    private uint3 ComputeShaderThreadGroupSizes;

    private ComputeBuffer GridCellDataBuffer;
    public RenderTexture GridCellDrawTexture;

    public ConwayRenderer(in Conway conway)
    {
        GridCellDrawTexture = new RenderTexture(conway.GridSize, conway.GridSize, 0, RenderTextureFormat.R8);
        GridCellDrawTexture.enableRandomWrite = true;
        GridCellDrawTexture.filterMode = FilterMode.Point;
        GridCellDrawTexture.Create();

        GridCellMesh = MeshUtility.Quad();
        GridCellMesh.bounds = new Bounds(Vector3.zero, Vector3.one * conway.GridSize);
        GridCellMaterial = Resources.Load<Material>(GridCellMaterialPath);
        GridCellMaterial.mainTexture = GridCellDrawTexture;

        ComputeShader = Resources.Load<ComputeShader>(CreateDrawCommandsComputeShaderPath);
        ComputeShaderKernel = ComputeShader.FindKernel("CSMain");
        ComputeShader.GetKernelThreadGroupSizes(ComputeShaderKernel, out ComputeShaderThreadGroupSizes.x, out ComputeShaderThreadGroupSizes.y, out ComputeShaderThreadGroupSizes.z);

        GridCellDataBuffer = new ComputeBuffer(conway.ArrayElemetCount, UnsafeUtility.SizeOf<uint2>(), ComputeBufferType.Structured);

        UpdateComputeProperites(conway);
        UpdateMaterialProperties(conway);
    }

    public void Dispatch(in Conway conway)
    {
        Profiler.BeginSample("GridCellDataBufferSet");
        GridCellDataBuffer.SetData(conway.CurrentGrid);
        Profiler.EndSample();

        // TODO: You should be able to speed things up by dispatching the compute shader on the x and y thread groups.. Not that it needs it.
        //int threadGroups = (int)((conway.ArrayElemetCount + (ComputeShaderThreadGroupSizes.x - 1)) / ComputeShaderThreadGroupSizes.x);
        //ComputeShader.Dispatch(kernel, threadGroups, 1, 1);
        Profiler.BeginSample("Dispatch");
        ComputeShader.Dispatch(ComputeShaderKernel, conway.ArrayElemetCount / 64/* / 4*/, /*conway.ArrayElementHeight / 8*/1, 1);
        Profiler.EndSample();
    }

    public void Draw(in Conway conway)
    {
        Graphics.DrawMesh(GridCellMesh, Vector3.zero, quaternion.identity, GridCellMaterial, 0);
    }

    private void UpdateComputeProperites(in Conway conway)
    {
        ComputeShader.SetInt("ArrayElementWidth", conway.ArrayElementWidth);
        ComputeShader.SetInt("ArrayElementHeight", conway.ArrayElementHeight);
        ComputeShader.SetFloat("CellSize", 1.0f);

        // Culling is not being used at the moment.
        //float2 cameraPosition = (Vector2)Camera.main.transform.position;
        //float2 cameraDimenions = new float2(Camera.main.orthographicSize * Camera.main.aspect, Camera.main.orthographicSize);
        //float2 cameraBoundsMin = cameraPosition - cameraDimenions;
        //float2 cameraBoundsMax = cameraPosition + cameraDimenions;
        //ComputeShader.SetFloats("CameraBoundsMin", cameraBoundsMin.x, cameraBoundsMin.y);
        //ComputeShader.SetFloats("CameraBoundsMax", cameraBoundsMax.x, cameraBoundsMax.y);

        ComputeShader.SetBuffer(ComputeShaderKernel, "GridCellDataBuffer", GridCellDataBuffer);
        ComputeShader.SetTexture(ComputeShaderKernel, "GridCellDrawTexture", GridCellDrawTexture);
    }

    private void UpdateMaterialProperties(in Conway conway)
    {
        GridCellMaterial.SetFloat("GridSize", conway.GridSize);
    }

    public void Dispose()
    {
        GridCellDataBuffer?.Dispose();
        GridCellDataBuffer = null;

        GridCellDrawTexture?.Release();
        GridCellDrawTexture = null;
    }
}
