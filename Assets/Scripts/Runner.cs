using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public class Runner : MonoBehaviour
{
    public Conway Conway;

    public ComputeShader ComputeShader;
    public Material GridCellMaterial;

    void Start()
    {
        Conway = new Conway(5, Unity.Collections.Allocator.Persistent);

        CreateDraw();

        UnityEngine.Debug.Log(Conway.ToString());

        //UnityEngine.Debug.Log(Conway.PrintGrid());
    }

    void Update()
    {
        Conway.Update();

        //Debug.Break();
    }

    private void LateUpdate()
    {
        Draw();
    }

    private void OnDrawGizmosSelected()
    {
        Conway.DrawPreviousGrid();
        Conway.DrawCurrentGrid();
    }

    private void OnDestroy()
    {
        Conway.Dispose();

        DrawDispose();
    }

    ComputeBuffer GridCellDataBuffer;
    ComputeBuffer GridCellDrawBuffer;

    RenderParams renderParams;

    public Mesh QuadMesh;
    GraphicsBuffer CommandBuffer;
    GraphicsBuffer.IndirectDrawIndexedArgs[] IndirectDrawIndexedArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[1];

    private void CreateDraw()
    {
        GridCellDataBuffer = new ComputeBuffer(Conway.ArrayElemetCount, UnsafeUtility.SizeOf<uint2>(), ComputeBufferType.Structured);
        GridCellDrawBuffer = new ComputeBuffer(Conway.ArrayElemetCount, UnsafeUtility.SizeOf<uint2>(), ComputeBufferType.Append);

        renderParams = new RenderParams(GridCellMaterial);
        renderParams.worldBounds = new Bounds(new Vector3(10000, 10000, 10000), new Vector3(10000000, 10000000, 10000000));

        CommandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        IndirectDrawIndexedArgs[0] = new GraphicsBuffer.IndirectDrawIndexedArgs
        {
            startIndex = 0,
            startInstance = 0,
            baseVertexIndex = 0,
            instanceCount = 0,
            indexCountPerInstance = QuadMesh.GetIndexCount(0),
        };
    }

    private void Draw()
    {
        GridCellDataBuffer.SetData(Conway.grid0);
        GridCellDrawBuffer.SetCounterValue(0);

        ComputeShader.SetInt("ArrayElementWidth", Conway.ArrayElementWidth);
        ComputeShader.SetInt("ArrayElemetCount", Conway.ArrayElemetCount);
        ComputeShader.SetBuffer(0, "GridCellDataBuffer", GridCellDataBuffer);
        ComputeShader.SetBuffer(0, "GridCellDrawBuffer", GridCellDrawBuffer);

        GridCellMaterial.SetInteger("ArrayElementWidth", Conway.ArrayElementWidth);
        GridCellMaterial.SetInteger("ArrayElemetCount", Conway.ArrayElemetCount);
        GridCellMaterial.SetBuffer("GridCellDrawBuffer", GridCellDrawBuffer);

        ComputeShader.Dispatch(0, 8, 8, 1);

        UnityEngine.Debug.Log($"RenderCount: {(uint)GridCellDrawBuffer.count}");
        IndirectDrawIndexedArgs[0].instanceCount = (uint)GridCellDrawBuffer.count;
        CommandBuffer.SetData(IndirectDrawIndexedArgs);

        Graphics.RenderMeshIndirect(renderParams, QuadMesh, CommandBuffer);
    }

    private void DrawDispose()
    {
        GridCellDataBuffer?.Dispose();
        GridCellDrawBuffer?.Dispose();
        CommandBuffer?.Dispose();

        GridCellDataBuffer = null;
        GridCellDrawBuffer = null;
        CommandBuffer = null;
    }
}
