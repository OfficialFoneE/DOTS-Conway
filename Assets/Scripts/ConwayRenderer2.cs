using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;


public struct ConwayRenderer2 : IDisposable
{
    private const string GridCellMaterialPath = "Materials/GridCellMaterialSquare";
    private const string CreateDrawCommandsComputeShaderPath = "Shaders/CreateDrawCommandsSquare";

    private Mesh GridCellMesh;
    public Material GridCellMaterial;

    private ComputeShader ComputeShader;
    private int ComputeShaderKernel;
    private uint3 ComputeShaderThreadGroupSizes;

    private ComputeBuffer GridCellDataBuffer;
    public RenderTexture GridCellDrawTexture;

    public ConwayRenderer2(in Conway conway)
    {
        GridCellDrawTexture = new RenderTexture(conway.GridSize, conway.GridSize, 0, RenderTextureFormat.R8);
        GridCellDrawTexture.enableRandomWrite = true;
        GridCellDrawTexture.filterMode = FilterMode.Point;
        GridCellDrawTexture.Create();

        GridCellMesh = MeshUtility.Quad();
        GridCellMaterial = Resources.Load<Material>(GridCellMaterialPath);
        GridCellMaterial.mainTexture = GridCellDrawTexture;

        ComputeShader = Resources.Load<ComputeShader>(CreateDrawCommandsComputeShaderPath);
        ComputeShaderKernel = ComputeShader.FindKernel("CSMain");
        ComputeShader.GetKernelThreadGroupSizes(ComputeShaderKernel, out ComputeShaderThreadGroupSizes.x, out ComputeShaderThreadGroupSizes.y, out ComputeShaderThreadGroupSizes.z);

        GridCellDataBuffer = new ComputeBuffer(conway.ArrayElemetCount, UnsafeUtility.SizeOf<uint2>(), ComputeBufferType.Structured);
    }

    public void Dispatch2(in Conway conway)
    {
        Profiler.BeginSample("GridCellDataBufferSet");
        GridCellDataBuffer.SetData(conway.CurrentGrid);
        Profiler.EndSample();

        Profiler.BeginSample("GridCellDataBufferSetParams");
        ComputeShader.SetInt("ArrayElementWidth", conway.ArrayElementWidth);
        ComputeShader.SetInt("ArrayElementHeight", conway.ArrayElementHeight);
        ComputeShader.SetFloat("CellSize", 1.0f);

        float2 cameraPosition = (Vector2)Camera.main.transform.position;
        float2 cameraDimenions = new float2(Camera.main.orthographicSize * Camera.main.aspect, Camera.main.orthographicSize);
        float2 cameraBoundsMin = cameraPosition - cameraDimenions;
        float2 cameraBoundsMax = cameraPosition + cameraDimenions;
        ComputeShader.SetFloats("CameraBoundsMin", cameraBoundsMin.x, cameraBoundsMin.y);
        ComputeShader.SetFloats("CameraBoundsMax", cameraBoundsMax.x, cameraBoundsMax.y);

        var kernel = ComputeShader.FindKernel("CSMain");
        ComputeShader.SetBuffer(kernel, "GridCellDataBuffer", GridCellDataBuffer);
        ComputeShader.SetTexture(kernel, "GridCellDrawTexture", GridCellDrawTexture);
        Profiler.EndSample();

        // TODO: You should be able to speed things up by dispatching the compute shader on the x and y thread groups.. Not that it needs it.
        //int threadGroups = (int)((Conway.ArrayElemetCount + (ComputeShaderThreadGroupSizes.x - 1)) / ComputeShaderThreadGroupSizes.x);
        //ComputeShader.Dispatch(kernel, threadGroups, 1, 1);
        Profiler.BeginSample("Dispatch");
        ComputeShader.Dispatch(kernel, conway.ArrayElemetCount / 64, /*conway.ArrayElementHeight / 8*/1, 1);
        Profiler.EndSample();
    }

    public void Draw(in Conway conway)
    {
        var matrix = Matrix4x4.TRS(Vector3.zero, quaternion.identity, new Vector3(1000, 1000));

        Graphics.DrawMesh(GridCellMesh, matrix, GridCellMaterial, 0);

        //GridCellMaterial.SetInteger("ArrayElementWidth", conway.ArrayElementWidth);
        //GridCellMaterial.SetInteger("ArrayElemetCount", conway.ArrayElemetCount);
        //GridCellMaterial.SetBuffer("GridCellDrawBuffer", GridCellDrawBuffer);
    }

    public void Dispose()
    {
        GridCellDataBuffer?.Dispose();
        GridCellDataBuffer = null;

        GridCellDrawTexture?.Release();
        GridCellDrawTexture = null;
    }
}
