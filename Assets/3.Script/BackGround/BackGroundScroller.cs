using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackGroundScroller : MonoBehaviour
{
    [SerializeField] public float moveFactor = 0.5f;   
    public Transform cameraTransform; 
    private float startX;

    void Start()
    {
        startX = transform.position.x;
    }

    void LateUpdate()
    {
        float moveX = cameraTransform.position.x * moveFactor;
        transform.position = new Vector3(startX + moveX, transform.position.y, transform.position.z);
    }
}
