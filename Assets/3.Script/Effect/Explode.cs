using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explode : MonoBehaviour
{
    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayexplodeSFX();
        }
    }
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
