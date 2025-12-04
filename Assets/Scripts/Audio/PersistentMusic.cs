using UnityEngine;

public class PersistentMusic : MonoBehaviour
{
    private static PersistentMusic instance;
    private AudioSource audioSource;

    void Awake()
    {
        // Singleton pattern - evitar duplicados
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Obtener AudioSource
            audioSource = GetComponent<AudioSource>();

            // Asegurar que se reproduce
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            // Si ya existe una instancia, destruir esta
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Doble verificación
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    // Método para control desde otros scripts
    public void SetMusicVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }
}