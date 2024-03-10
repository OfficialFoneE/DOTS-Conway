using UnityEngine;

public class Runner : MonoBehaviour
{
    public Conway Conway;

    void Start()
    {
        Conway = new Conway(5, Unity.Collections.Allocator.Persistent);

        //UnityEngine.Debug.Log(Conway.ToString());

        //UnityEngine.Debug.Log(Conway.PrintGrid());
    }

    void Update()
    {
        Conway.Update();

        //Debug.Break();
    }

    private void OnDrawGizmosSelected()
    {
        Conway.DrawPreviousGrid();
        Conway.DrawCurrentGrid();
    }

    private void OnDestroy()
    {
        Conway.Dispose();
    }
}
