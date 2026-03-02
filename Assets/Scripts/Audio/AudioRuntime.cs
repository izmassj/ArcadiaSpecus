using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// AudioRuntime
/// - Carga el AudioMixer desde Resources/Audio/ArcadiaMixer
/// - Aplica volúmenes (MusicVolume / SFXVolume)
/// - Auto-rutea AudioSources a grupos Music/SFX para que el mixer afecte realmente.
/// </summary>
public static class AudioRuntime
{
    // Resources path SIN extensión
    private const string MixerResourcePath = "Audio/ArcadiaMixer";

    private static AudioMixer _mixer;
    private static AudioMixerGroup _musicGroup;
    private static AudioMixerGroup _sfxGroup;
    private static bool _init;

    private static float _musicVol01 = 0.7f;
    private static float _sfxVol01 = 0.7f;

    public static AudioMixer Mixer
    {
        get
        {
            EnsureInit();
            return _mixer;
        }
    }

    public static AudioMixerGroup MusicGroup
    {
        get
        {
            EnsureInit();
            return _musicGroup;
        }
    }

    public static AudioMixerGroup SfxGroup
    {
        get
        {
            EnsureInit();
            return _sfxGroup;
        }
    }

    public static void EnsureInit()
    {
        if (_init) return;
        _init = true;

        _mixer = Resources.Load<AudioMixer>(MixerResourcePath);
        if (_mixer != null)
        {
            var mg = _mixer.FindMatchingGroups("Music");
            if (mg != null && mg.Length > 0) _musicGroup = mg[0];

            var sg = _mixer.FindMatchingGroups("SFX");
            if (sg != null && sg.Length > 0) _sfxGroup = sg[0];
        }
    }

    public static void ApplyMixerVolumes(float musicSlider01, float sfxSlider01)
    {
        EnsureInit();

        _musicVol01 = Mathf.Clamp01(musicSlider01);
        _sfxVol01 = Mathf.Clamp01(sfxSlider01);

        // 1) Ajuste por mixer (si existe)
        if (_mixer != null)
        {
            _mixer.SetFloat("MusicVolume", SliderToDb(_musicVol01));
            _mixer.SetFloat("SFXVolume", SliderToDb(_sfxVol01));
        }

        // 2) Auto-routeo para que esos parámetros afecten a algo.
        RouteAllSceneAudioSources();
    }

    public static void ApplyMusicOnly(float musicSlider01)
    {
        ApplyMixerVolumes(musicSlider01, _sfxVol01);
    }

    public static void ApplySfxOnly(float sfxSlider01)
    {
        ApplyMixerVolumes(_musicVol01, sfxSlider01);
    }

    public static float SliderToDb(float slider01)
    {
        // 0 => silencio duro
        if (slider01 <= 0.0001f) return -80f;
        slider01 = Mathf.Clamp(slider01, 0.0001f, 1f);
        return Mathf.Log10(slider01) * 20f; // 1 -> 0 dB, 0.0001 -> -80 dB
    }

    public static void RouteAllSceneAudioSources()
    {
        EnsureInit();
        if (_mixer == null) return;

        // Si no tenemos grupos, no podemos rutear.
        if (_musicGroup == null && _sfxGroup == null) return;

        PersistentMusic pm = PersistentMusic.instance;
        AudioSource pmMusic = pm != null ? pm.audioSource : null;

        // Nota: includeInactive=true para pillar cosas desactivadas en jerarquía.
        var sources = Object.FindObjectsOfType<AudioSource>(true);
        foreach (var src in sources)
        {
            if (src == null) continue;

            // Si es el AudioSource principal de música
            if (pmMusic != null && src == pmMusic)
            {
                if (_musicGroup != null) src.outputAudioMixerGroup = _musicGroup;
                continue;
            }

            // Si es cualquier AudioSource dentro del objeto PersistentMusic (click/alert/etc)
            if (pm != null && src.transform.IsChildOf(pm.transform))
            {
                if (_sfxGroup != null) src.outputAudioMixerGroup = _sfxGroup;
                continue;
            }

            // Resto: por defecto lo tratamos como SFX
            if (_sfxGroup != null) src.outputAudioMixerGroup = _sfxGroup;
        }
    }

    public static void RouteToSfx(AudioSource src)
    {
        EnsureInit();
        if (src == null) return;
        if (_sfxGroup != null) src.outputAudioMixerGroup = _sfxGroup;
    }

    public static void RouteToMusic(AudioSource src)
    {
        EnsureInit();
        if (src == null) return;
        if (_musicGroup != null) src.outputAudioMixerGroup = _musicGroup;
    }

    /// <summary>
    /// Reproduce un clip 3D en una posición, ruteado al grupo SFX del mixer.
    /// </summary>
    public static void PlayWorldSfx(AudioClip clip, Vector3 position, float volume01 = 1f, float minDistance = 1f, float maxDistance = 25f)
    {
        if (clip == null) return;
        EnsureInit();

        GameObject go = new GameObject("WorldSFX_" + clip.name);
        go.transform.position = position;

        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 1f;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.volume = Mathf.Clamp01(volume01);

        if (_sfxGroup != null)
            src.outputAudioMixerGroup = _sfxGroup;

        src.clip = clip;
        src.Play();

        Object.Destroy(go, clip.length + 0.25f);
    }
}
