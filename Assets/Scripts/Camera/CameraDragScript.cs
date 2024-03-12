using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDragScript : MonoBehaviour
{

    private Transform transform;

    private float dist;
    private Vector3 MouseStart;

    private void Awake()
    {
        transform = GetComponent<Transform>();
    }

    private void Start()
    {
        dist = transform.position.z;  // Distance camera is above map
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            MouseStart = new Vector3(Input.mousePosition.x, Input.mousePosition.y, dist);
            MouseStart = Camera.main.ScreenToWorldPoint(MouseStart);
            MouseStart.z = transform.position.z;
        }
        else if (Input.GetMouseButton(2))
        {
            var MouseMove = new Vector3(Input.mousePosition.x, Input.mousePosition.y, dist);
            MouseMove = Camera.main.ScreenToWorldPoint(MouseMove);
            MouseMove.z = transform.position.z;
            transform.position = transform.position - (MouseMove - MouseStart);
        }

    }
}
