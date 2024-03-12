using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovementScript : MonoBehaviour
{

    private Transform transform;

    [SerializeField] private float speed = 10.0f;

    private void Awake()
    {
        transform = GetComponent<Transform>();
    }

    private void Update()
    {
        Vector3 position = transform.position;
        Vector3 velocity = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            velocity.y += speed;
        }
        if (Input.GetKey(KeyCode.A))
        {
            velocity.x -= speed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            velocity.y -= speed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            velocity.x += speed;
        }

        transform.position = position+ velocity * Time.deltaTime;
    }
}
