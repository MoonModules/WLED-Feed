using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float Speed = 10;
    public bool Random;

    void Update()
    {
        if (Random)
        {
            float t = Time.time * Speed;
            Vector3 rotation = new Vector3(Mathf.Sin(t * 13), Mathf.Sin(t * 7), Mathf.Sin(t * 29));
            rotation *= 180;
            transform.rotation = Quaternion.Euler(rotation);
        }
        else
            transform.Rotate(Vector3.up, Speed * Time.deltaTime);
    }
}
