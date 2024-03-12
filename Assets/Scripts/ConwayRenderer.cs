using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public struct ConwayRenderer : IDisposable
{
    private const string GridCellMaterialPath = "Materials/GridCellMaterial";
    private const string CreateDrawCommandsComputeShaderPath = "Shaders/CreateDrawCommands";

    private Mesh GridCellMesh;
    private Material GridCellMaterial;

    private ComputeShader ComputeShader;
    private int ComputeShaderKernel;
    private uint3 ComputeShaderThreadGroupSizes;

    private RenderParams RenderParams;

    private ComputeBuffer GridCellDataBuffer;
    private ComputeBuffer GridCellDrawBuffer;
    private ComputeBuffer GridCellDrawCount;

    private GraphicsBuffer IndirectArgsCommandBuffer;
    private NativeArray<GraphicsBuffer.IndirectDrawIndexedArgs> IndirectDrawIndexedArgs;

    public ConwayRenderer(in Conway conway)
    {
        GridCellMesh = MeshUtility.Quad();
        GridCellMaterial = Resources.Load<Material>(GridCellMaterialPath);

        ComputeShader = Resources.Load<ComputeShader>(CreateDrawCommandsComputeShaderPath);
        ComputeShaderKernel = ComputeShader.FindKernel("CSMain");
        ComputeShader.GetKernelThreadGroupSizes(ComputeShaderKernel, out ComputeShaderThreadGroupSizes.x, out ComputeShaderThreadGroupSizes.y, out ComputeShaderThreadGroupSizes.z);

        RenderParams = new RenderParams(GridCellMaterial);
        RenderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000000);

        GridCellDataBuffer = new ComputeBuffer(conway.ArrayElemetCount, UnsafeUtility.SizeOf<uint2>(), ComputeBufferType.Structured);
        GridCellDrawBuffer = new ComputeBuffer(conway.GridCellCount, UnsafeUtility.SizeOf<uint>(), ComputeBufferType.Append);
        GridCellDrawCount = new ComputeBuffer(1, UnsafeUtility.SizeOf<uint>(), ComputeBufferType.Raw);

        IndirectArgsCommandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);

        IndirectDrawIndexedArgs = new NativeArray<GraphicsBuffer.IndirectDrawIndexedArgs>(1, Allocator.Persistent);
        IndirectDrawIndexedArgs[0] = new GraphicsBuffer.IndirectDrawIndexedArgs
        {
            startIndex = 0,
            startInstance = 0,
            baseVertexIndex = 0,
            instanceCount = 0,
            indexCountPerInstance = GridCellMesh.GetIndexCount(0),
        };
    }

    public void Dispatch2(in Conway conway)
    {
        Profiler.BeginSample("GridCellDataBufferSet");
        GridCellDataBuffer.SetData(conway.CurrentGrid);
        GridCellDrawBuffer.SetCounterValue(0);
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
        ComputeShader.SetBuffer(kernel, "GridCellDrawBuffer", GridCellDrawBuffer);
        Profiler.EndSample();

        // TODO: You should be able to speed things up by dispatching the compute shader on the x and y thread groups.. Not that it needs it.
        //int threadGroups = (int)((Conway.ArrayElemetCount + (ComputeShaderThreadGroupSizes.x - 1)) / ComputeShaderThreadGroupSizes.x);
        //ComputeShader.Dispatch(kernel, threadGroups, 1, 1);
        Profiler.BeginSample("Dispatch");
        ComputeShader.Dispatch(kernel, conway.ArrayElemetCount / 64, /*conway.ArrayElementHeight / 8*/1, 1);
        Profiler.EndSample();
    }

    public void Draw2(in Conway conway)
    {
        GridCellMaterial.SetInteger("ArrayElementWidth", conway.ArrayElementWidth);
        GridCellMaterial.SetInteger("ArrayElemetCount", conway.ArrayElemetCount);
        GridCellMaterial.SetBuffer("GridCellDrawBuffer", GridCellDrawBuffer);

        Profiler.BeginSample("Copy");
        // Fetch the number of instances in the append buffer.
        ComputeBuffer.CopyCount(GridCellDrawBuffer, GridCellDrawCount, 0);
        var instanceCount = new uint[1];
        GridCellDrawCount.GetData(instanceCount);
        Profiler.EndSample();

        // Update the number of instances to draw.
        var indirectDrawIndexedArgs = IndirectDrawIndexedArgs[0];
        indirectDrawIndexedArgs.instanceCount = instanceCount[0];
        IndirectDrawIndexedArgs[0] = indirectDrawIndexedArgs;

        // Update the new arguments on the GPU.
        IndirectArgsCommandBuffer.SetData(IndirectDrawIndexedArgs);

        // Dispatch the draw mesh indirect.
        Graphics.RenderMeshIndirect(RenderParams, GridCellMesh, IndirectArgsCommandBuffer);
    }

    private void DrawUpdate(in Conway conway)
    {
        GridCellDataBuffer.SetData(conway.CurrentGrid);
        GridCellDrawBuffer.SetCounterValue(0);

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
        ComputeShader.SetBuffer(kernel, "GridCellDrawBuffer", GridCellDrawBuffer);

        GridCellMaterial.SetInteger("ArrayElementWidth", conway.ArrayElementWidth);
        GridCellMaterial.SetInteger("ArrayElemetCount", conway.ArrayElemetCount);
        GridCellMaterial.SetBuffer("GridCellDrawBuffer", GridCellDrawBuffer);

        // TODO: You should be able to speed things up by dispatching the compute shader on the x and y thread groups.. Not that it needs it.
        //int threadGroups = (int)((Conway.ArrayElemetCount + (ComputeShaderThreadGroupSizes.x - 1)) / ComputeShaderThreadGroupSizes.x);
        //ComputeShader.Dispatch(kernel, threadGroups, 1, 1);

        ComputeShader.Dispatch(kernel, conway.ArrayElemetCount / 64, /*conway.ArrayElementHeight / 8*/1, 1);
    }

    public void Draw(in Conway conway)
    {
        DrawUpdate(conway);

        // Fetch the number of instances in the append buffer.
        ComputeBuffer.CopyCount(GridCellDrawBuffer, GridCellDrawCount, 0);
        var instanceCount = new uint[1];
        GridCellDrawCount.GetData(instanceCount);

        // Update the number of instances to draw.
        var indirectDrawIndexedArgs = IndirectDrawIndexedArgs[0];
        indirectDrawIndexedArgs.instanceCount = instanceCount[0];
        IndirectDrawIndexedArgs[0] = indirectDrawIndexedArgs;

        // Update the new arguments on the GPU.
        IndirectArgsCommandBuffer.SetData(IndirectDrawIndexedArgs);

        // Dispatch the draw mesh indirect.
        Graphics.RenderMeshIndirect(RenderParams, GridCellMesh, IndirectArgsCommandBuffer);
    }

    public void Dispose()
    {
        GridCellDataBuffer?.Dispose();
        GridCellDrawBuffer?.Dispose();
        GridCellDrawCount?.Dispose();
        IndirectArgsCommandBuffer?.Dispose();

        GridCellDataBuffer = null;
        GridCellDrawBuffer = null;
        GridCellDrawCount = null;
        IndirectArgsCommandBuffer = null;

        if (IndirectDrawIndexedArgs.IsCreated) IndirectDrawIndexedArgs.Dispose();
    }
}
