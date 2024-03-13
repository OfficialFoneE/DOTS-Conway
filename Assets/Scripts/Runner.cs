using UnityEngine;

public class Runner : MonoBehaviour
{
    public int Resolution = 2;

    private Conway Conway;
    private ConwayRenderer ConwayRenderer;

    void Start()
    {
        Application.targetFrameRate = -1;

        Conway = new Conway(Resolution, Unity.Collections.Allocator.Persistent);
        ConwayRenderer = new ConwayRenderer(Conway);

        UnityEngine.Debug.Log(Conway.ToString());
    }

    void Update()
    {
        //ConwayRenderer.Draw2(Conway);
        //ConwayRenderer.Dispatch2(Conway);
        Conway.Update();
    }

    private void LateUpdate()
    {
        //ConwayRenderer.Draw(Conway);
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
    }
}
