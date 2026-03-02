using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// PersistentMusic
/// - Mantiene la música entre escenas.
/// - Provee wrappers de SFX (alert/click/back) porque otros sistemas los llaman.
/// NOTA: los clips se pueden asignar en inspector o se cargan desde Resources/Audio/SFX/...
/// </summary>
public class PersistentMusic : MonoBehaviour
{
    public static PersistentMusic instance;

    [Header("Music")]
    public AudioSource audioSource; // compat con tu proyecto (otros scripts lo usan)

    [Tooltip("Opcional: grupo del AudioMixer para música")]
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("Opcional: grupo del AudioMixer para SFX")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("SFX Clips (opcional)")]
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip uiBack;
    [SerializeField] private AudioClip alert;

    private bool clipsLoaded;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Music source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (musicMixerGroup != null)
            audioSource.outputAudioMixerGroup = musicMixerGroup;

        // SFX source
        if (sfxSource == null)
        {
            var sfxGo = new GameObject("SFX");
            sfxGo.transform.SetParent(transform);
            sfxSource = sfxGo.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        if (sfxMixerGroup != null)
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;

        // Asegurar reproducción de música si ya hay clip
        if (audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    // Compat con tu proyecto
    public void SetMusicVolume(float volume)
    {
        if (audioSource != null)
            audioSource.volume = Mathf.Clamp01(volume);
    }

    public void SetSfxVolume(float volume)
    {
        if (sfxSource != null)
            sfxSource.volume = Mathf.Clamp01(volume);
    }

    // ---- Wrappers que tu código ya llama ----

    public void PlayAlert(float volume = 1f)
    {
        EnsureClipsLoaded();
        PlayOneShotSafe(alert, volume);
    }

    public void PlayUiClick(float volume = 1f)
    {
        EnsureClipsLoaded();
        PlayOneShotSafe(uiClick, volume);
    }

    public void PlayUiBack(float volume = 1f)
    {
        EnsureClipsLoaded();
        PlayOneShotSafe(uiBack, volume);
    }

    private void PlayOneShotSafe(AudioClip clip, float volume)
    {
        if (sfxSource == null) return;
        if (clip == null) return; // si no hay clip, no spameamos errores
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void EnsureClipsLoaded()
    {
        if (clipsLoaded) return;
        clipsLoaded = true;

        // Carga por defecto desde Resources si no está asignado en inspector.
        // Rutas esperadas (sin extensión):
        // - Resources/Audio/SFX/UI_Click
        // - Resources/Audio/SFX/UI_Back
        // - Resources/Audio/SFX/Alert
        if (uiClick == null) uiClick = Resources.Load<AudioClip>("Audio/SFX/UI_Click");
        if (uiBack == null)  uiBack  = Resources.Load<AudioClip>("Audio/SFX/UI_Back");
        if (alert == null)   alert   = Resources.Load<AudioClip>("Audio/SFX/Alert");

        // Fallbacks por si tus nombres eran distintos
        if (uiClick == null) uiClick = Resources.Load<AudioClip>("Audio/SFX/Click");
        if (uiBack == null)  uiBack  = Resources.Load<AudioClip>("Audio/SFX/Back");
        if (alert == null)   alert   = Resources.Load<AudioClip>("Audio/SFX/Alarm");
    }
}
