using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Experimental.Rendering.Universal;

public class CameraZoomScript : MonoBehaviour
{
    //private PixelPerfectCamera pixelPerfectCamera;

    //[SerializeField] private float pixelScrollMultipler = 10.0f;

    //private void Awake()
    //{
    //    pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
    //}


    //private void Update()
    //{
    //    float scrollWheel = Input.GetAxisRaw("Mouse ScrollWheel");

    //    if (Mathf.Approximately(scrollWheel, 0.0f)) return;

    //    pixelPerfectCamera.assetsPPU += (int)(scrollWheel * pixelScrollMultipler);
    //}

    private new Camera camera;

    [SerializeField] private float scrollMultipler = 10000;
    [SerializeField] private float minimumOrthographicSize = 30.0f;
    [SerializeField] private float maximumOrthographicSize = 300.0f;

    private float perferedOrthographicSize;
    [SerializeField] private float smoothMultipler = 20.0f;

    private void Awake()
    {
        camera = GetComponent<Camera>();

        perferedOrthographicSize = camera.orthographicSize;
    }

    private void Update()
    {
        float scrollWheel = Input.GetAxisRaw("Mouse ScrollWheel");

        perferedOrthographicSize -= scrollWheel * scrollMultipler * Time.deltaTime;

        perferedOrthographicSize = Mathf.Clamp(perferedOrthographicSize, minimumOrthographicSize, maximumOrthographicSize);

        camera.orthographicSize = Mathf.SmoothStep(camera.orthographicSize, perferedOrthographicSize, Time.deltaTime * smoothMultipler);
    }
}
