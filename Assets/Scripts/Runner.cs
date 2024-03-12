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
        ConwayRenderer.Draw2(Conway);
        ConwayRenderer.Dispatch2(Conway);
        Conway.Update();
    }

    private void LateUpdate()
    {
        //ConwayRenderer.Draw(Conway);
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
        ConwayRenderer.Dispose();
    }
}
