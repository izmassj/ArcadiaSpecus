using UnityEngine;

public class MachineNoise : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip clip;

    [Header("Intervalo en segundos")]
    public float intervalo = 10f;

    private float timer = 0f;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (audioSource == null || clip == null)
            return;

        timer += Time.deltaTime;

        if (timer >= intervalo)
        {
            audioSource.PlayOneShot(clip);
            timer = 0f;
        }
    }

    
}
