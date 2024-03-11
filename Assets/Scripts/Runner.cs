using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public class Runner : MonoBehaviour
{
    public int Resolution = 2;

    private Conway Conway;

    void Start()
    {
        Application.targetFrameRate = -1;

        Conway = new Conway(Resolution, Unity.Collections.Allocator.Persistent);

        CreateDraw();

        UnityEngine.Debug.Log(Conway.ToString());
    }

    void Update()
    {
        Conway.Update();
    }

    private void LateUpdate()
    {
        DrawLate();
    }

    private void OnDrawGizmosSelected()
    {
        //Conway.DrawPreviousGrid();
        //Conway.DrawCurrentGrid();

        //for (int i = 0; i < Conway.ArrayElementWidth; i++)
        //{
        //    for (int j = 0; j < Conway.ArrayElementHeight; j++)
        //    {
        //        var startingX = i * 64;
        //        var startingY = j;


        //        Gizmos.DrawWireCube(new Vector3(startingX + 32 - 0.5f, startingY), new Vector3(64, 1.0f));
        //    }
        //}
    }

    private void OnDestroy()
    {
        Conway.Dispose();

        DrawDispose();
    }

    public ComputeShader ComputeShader;
    public Material GridCellMaterial;
    int ComputeShaderKernel;
    uint3 ComputeShaderThreadGroupSizes;

    ComputeBuffer GridCellDataBuffer;
    ComputeBuffer GridCellDrawBuffer;
    ComputeBuffer GridCellDrawCount;

    RenderParams renderParams;

    public Mesh QuadMesh;
    GraphicsBuffer CommandBuffer;
    GraphicsBuffer.IndirectDrawIndexedArgs[] IndirectDrawIndexedArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[1];

    private void CreateDraw()
    {
        GridCellDataBuffer = new ComputeBuffer(Conway.ArrayElemetCount, UnsafeUtility.SizeOf<uint2>(), ComputeBufferType.Structured);
        GridCellDrawBuffer = new ComputeBuffer(Conway.ArrayElemetCount * 64, UnsafeUtility.SizeOf<uint>(), ComputeBufferType.Append);
        GridCellDrawCount = new ComputeBuffer(1, UnsafeUtility.SizeOf<uint>(), ComputeBufferType.Raw);

        renderParams = new RenderParams(GridCellMaterial);
        renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000000);

        CommandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        IndirectDrawIndexedArgs[0] = new GraphicsBuffer.IndirectDrawIndexedArgs
        {
            startIndex = 0,
            startInstance = 0,
            baseVertexIndex = 0,
            instanceCount = 0,
            indexCountPerInstance = QuadMesh.GetIndexCount(0),
        };

        ComputeShaderKernel = ComputeShader.FindKernel("CSMain");
        ComputeShader.GetKernelThreadGroupSizes(ComputeShaderKernel, out ComputeShaderThreadGroupSizes.x, out ComputeShaderThreadGroupSizes.y, out ComputeShaderThreadGroupSizes.z);
    }

    private void DrawUpdate()
    {
        GridCellDataBuffer.SetData(Conway.CurrentGrid);
        GridCellDrawBuffer.SetCounterValue(0);

        ComputeShader.SetInt("ArrayElementWidth", Conway.ArrayElementWidth);
        ComputeShader.SetInt("ArrayElementHeight", Conway.ArrayElementHeight);

        var kernel = ComputeShader.FindKernel("CSMain");
        ComputeShader.SetBuffer(kernel, "GridCellDataBuffer", GridCellDataBuffer);
        ComputeShader.SetBuffer(kernel, "GridCellDrawBuffer", GridCellDrawBuffer);

        GridCellMaterial.SetInteger("ArrayElementWidth", Conway.ArrayElementWidth);
        GridCellMaterial.SetInteger("ArrayElemetCount", Conway.ArrayElemetCount);
        GridCellMaterial.SetBuffer("GridCellDrawBuffer", GridCellDrawBuffer);

        //int threadGroups = (int)((Conway.ArrayElemetCount + (ComputeShaderThreadGroupSizes.x - 1)) / ComputeShaderThreadGroupSizes.x);
        //ComputeShader.Dispatch(kernel, threadGroups, 1, 1);

        ComputeShader.Dispatch(kernel, Conway.ArrayElemetCount / 64, 1, 1);
    }

    private void DrawLate()
    {
        DrawUpdate();

        ComputeBuffer.CopyCount(GridCellDrawBuffer, GridCellDrawCount, 0);
        var data = new uint[1];
        GridCellDrawCount.GetData(data);

        IndirectDrawIndexedArgs[0].instanceCount = data[0];

        //UnityEngine.Debug.Log($"Alive cell count: {Conway.GetNumberOfAliveCells()}, GPU: {data[0]}");

        CommandBuffer.SetData(IndirectDrawIndexedArgs);

        Graphics.RenderMeshIndirect(renderParams, QuadMesh, CommandBuffer);
    }

    private void DrawDispose()
    {
        GridCellDataBuffer?.Dispose();
        GridCellDrawBuffer?.Dispose();
        GridCellDrawCount?.Dispose();
        CommandBuffer?.Dispose();

        GridCellDataBuffer = null;
        GridCellDrawBuffer = null;
        GridCellDrawCount = null;
        CommandBuffer = null;
    }
}
