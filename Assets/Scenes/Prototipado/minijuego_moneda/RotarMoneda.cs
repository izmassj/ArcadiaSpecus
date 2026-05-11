using UnityEngine;

public class RotarMoneda : MonoBehaviour
{
    public float velocidad = 100f;

    void Update()
    {
        transform.Rotate(velocidad * Time.deltaTime, 0f, 0f);
    }
}