using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UIAudioSfxBinder
/// - Auto-enlaza todos los Buttons cargados (incluyendo inactivos) para reproducir un SFX de click.
/// - Evita tener que poner scripts en cada botón.
/// - No sustituye si quieres SFX distintos por botón; solo asegura que hay click global.
/// </summary>
public class UIAudioSfxBinder : MonoBehaviour
{
    private static UIAudioSfxBinder _instance;

    // Se limpia por escena para no crecer sin límite.
    private readonly HashSet<int> _boundInstanceIds = new HashSet<int>();

    public static void EnsureInstance()
    {
        if (_instance != null) return;

        UIAudioSfxBinder existing = FindObjectOfType<UIAudioSfxBinder>(true);
        if (existing != null)
        {
            _instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("UIAudioSfxBinder");
        _instance = go.AddComponent<UIAudioSfxBinder>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        BindAllButtonsInLoadedScenes();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-scan para escenas nuevas o additive.
        _boundInstanceIds.Clear();
        BindAllButtonsInLoadedScenes();
    }

    private void BindAllButtonsInLoadedScenes()
    {
        // Incluye inactivos. Nota: en Unity modernas esto devuelve solo objetos de escenas cargadas.
        Button[] buttons = GameObject.FindObjectsOfType<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (b == null) continue;
            if (!b.gameObject.scene.isLoaded) continue;

            int id = b.GetInstanceID();
            if (_boundInstanceIds.Contains(id)) continue;
            _boundInstanceIds.Add(id);

            // Añadimos un listener simple. Si la escena cambia inmediatamente, al menos intenta sonar.
            b.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        if (PersistentMusic.instance != null)
            PersistentMusic.instance.PlayUiClick(1f);
    }
}
