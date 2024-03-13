using UnityEngine;

public class Runner : MonoBehaviour
{
    public int Resolution = 2;

    private Conway Conway;
    private ConwayRenderer ConwayRenderer;
    public ConwayRenderer2 ConwayRenderer2;
    void Start()
    {
        Application.targetFrameRate = -1;

        Conway = new Conway(Resolution, Unity.Collections.Allocator.Persistent);
        ConwayRenderer = new ConwayRenderer(Conway);
        ConwayRenderer2 = new ConwayRenderer2(Conway);

        UnityEngine.Debug.Log(Conway.ToString());
    }

    void Update()
    {
        //ConwayRenderer.Draw2(Conway);
        //ConwayRenderer.Dispatch2(Conway);
        ConwayRenderer2.Dispatch2(Conway);

        Conway.Update();
    }

    private void LateUpdate()
    {
        //ConwayRenderer.Draw(Conway);
        //ConwayRenderer2.Dispatch2(Conway);

        //ConwayRenderer2.Draw(Conway);
    }

    private void OnDrawGizmosSelected()
    {
        //Conway.DrawPreviousGridSquare();
        //Conway.DrawCurrentGridSquare();
        //Conway.DrawGridBoundriesSquare();
    }

    private void OnDestroy()
    {
        Conway.Dispose();
        ConwayRenderer.Dispose();
        ConwayRenderer2.Dispose();
    }
}
