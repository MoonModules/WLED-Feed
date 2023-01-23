using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float SpeedDegPerSecond = 10;

    void Update()
    {
        transform.Rotate(Vector3.up, SpeedDegPerSecond * Time.deltaTime);
    }
}
