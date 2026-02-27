using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona carga aditiva de minijuegos y cambio de cámara/UI al entrar/salir.
/// Mantiene la API que ya usa vuestro proyecto.
/// </summary>
public class AdvancedSceneCameraManager : MonoBehaviour
{
    public static AdvancedSceneCameraManager instance;

    [Header("Referencias (opcionales, se pueden auto-buscar)")]
    public CinemachineBrain cinemachineBrain;
    public GameObject mainCanvas;
    public GameObject cursorCanvas;
    public GameObject GameOver;

    private readonly Dictionary<string, CinemachineVirtualCamera> _sceneCameras = new Dictionary<string, CinemachineVirtualCamera>();

    private string _currentActiveScene;
    private string _previousScene;
    private bool _isTransitioning;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void Start()
    {
        SceneFlowManager.EnsureInstance();

        _currentActiveScene = SceneManager.GetActiveScene().name;

        RefreshCachedReferences();
        RegisterCurrentSceneCamera();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        _sceneCameras.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            _sceneCameras.Clear();
            _currentActiveScene = scene.name;
            _previousScene = string.Empty;
        }

        RefreshCachedReferences();
        RegisterSceneCamera(scene);
        SwitchToSceneCamera(SceneManager.GetActiveScene().name);
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (_sceneCameras.ContainsKey(scene.name))
            _sceneCameras.Remove(scene.name);
    }

    private void RefreshCachedReferences()
    {
        if (cinemachineBrain == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cinemachineBrain = mainCam.GetComponent<CinemachineBrain>();
        }

        // Se buscan por nombre por compatibilidad con vuestras escenas actuales.
        // Si luego queréis, se puede refactorizar a referencias por inspector.
        if (mainCanvas == null)
        {
            GameObject found = GameObject.Find("Canvas");
            if (found != null)
                mainCanvas = found;
        }

        if (cursorCanvas == null)
        {
            GameObject found = GameObject.Find("CanvasCursor");
            if (found != null)
                cursorCanvas = found;
        }

        if (GameOver == null)
        {
            GameObject found = GameObject.Find("GameOverManager");
            if (found != null)
                GameOver = found;
        }
    }

    private void RegisterCurrentSceneCamera()
    {
        RegisterSceneCamera(SceneManager.GetSceneByName(_currentActiveScene));
    }

    private void RegisterSceneCamera(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        CinemachineVirtualCamera currentVCam = FindVCamInScene(scene);
        if (currentVCam == null)
            return;

        _sceneCameras[scene.name] = currentVCam;
    }

    public void LoadAdditiveScene(string sceneName)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("AdvancedSceneCameraManager: transición en curso, se ignora LoadAdditiveScene.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("AdvancedSceneCameraManager: nombre de escena inválido.");
            yield break;
        }

        _isTransitioning = true;
        RefreshCachedReferences();

        SetBaseUIVisible(false);
        SetCursorForMiniGame(lockedCursor: true);

        _previousScene = SceneManager.GetActiveScene().name;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (asyncLoad == null)
        {
            _isTransitioning = false;
            yield break;
        }

        while (!asyncLoad.isDone)
            yield return null;

        Scene newScene = SceneManager.GetSceneByName(sceneName);

        // Un frame para que la escena termine de inicializar objetos/cámaras.
        yield return null;

        RegisterSceneCamera(newScene);

        if (_sceneCameras.ContainsKey(sceneName))
            SwitchToSceneCamera(sceneName);

        if (newScene.IsValid() && newScene.isLoaded)
        {
            SceneManager.SetActiveScene(newScene);
            _currentActiveScene = sceneName;
        }

        _isTransitioning = false;
    }

    public void ReturnToBaseScene(string baseSceneName, bool win)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("AdvancedSceneCameraManager: transición en curso, se ignora ReturnToBaseScene.");
            return;
        }

        StartCoroutine(ReturnToBaseRoutine(baseSceneName, win));
    }

    private IEnumerator ReturnToBaseRoutine(string baseSceneName, bool win)
    {
        if (string.IsNullOrWhiteSpace(baseSceneName))
        {
            Debug.LogWarning("AdvancedSceneCameraManager: baseSceneName inválido.");
            yield break;
        }

        _isTransitioning = true;
        RefreshCachedReferences();

        SetCursorForMiniGame(lockedCursor: false);

        // Si la escena base no está cargada (fallback), cargamos en single.
        Scene baseScene = SceneManager.GetSceneByName(baseSceneName);
        if (!baseScene.IsValid() || !baseScene.isLoaded)
        {
            SceneFlowManager.EnsureInstance().LoadSceneByName(baseSceneName);
            _isTransitioning = false;
            yield break;
        }

        string sceneToUnload = SceneManager.GetActiveScene().name;

        // Volver a la escena base como activa antes de descargar el minijuego.
        SceneManager.SetActiveScene(baseScene);
        _currentActiveScene = baseSceneName;

        RegisterSceneCamera(baseScene);
        SwitchToSceneCamera(baseSceneName);

        if (win && ResourceManager.Instance != null)
        {
            // Recompensa básica de minijuego (placeholder funcional)
            // Aquí luego podéis diferenciar por minijuego/tipo de recurso.
            ResourceManager.Instance.AddResource(ResourceType.Materials, 50);

            // Aquí iría feedback visual/sonoro de recompensa.
        }

        if (!string.Equals(sceneToUnload, baseSceneName) && SceneManager.GetSceneByName(sceneToUnload).isLoaded)
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneToUnload);
            if (asyncUnload != null)
            {
                while (!asyncUnload.isDone)
                    yield return null;
            }

            if (_sceneCameras.ContainsKey(sceneToUnload))
                _sceneCameras.Remove(sceneToUnload);
        }

        RefreshCachedReferences();
        SetBaseUIVisible(true);
        ResetCameraPriorities(baseSceneName);

        _isTransitioning = false;
    }

    private void SetBaseUIVisible(bool isVisible)
    {
        if (mainCanvas != null)
            mainCanvas.SetActive(isVisible);

        if (cursorCanvas != null)
            cursorCanvas.SetActive(isVisible);

        if (GameOver != null)
            GameOver.SetActive(isVisible);
    }

    private void SetCursorForMiniGame(bool lockedCursor)
    {
        if (lockedCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void SwitchToSceneCamera(string sceneName)
    {
        if (!_sceneCameras.ContainsKey(sceneName))
        {
            Debug.LogWarning($"AdvancedSceneCameraManager: no hay cámara registrada para '{sceneName}'.");
            HandleRegularCameras(sceneName);
            return;
        }

        foreach (KeyValuePair<string, CinemachineVirtualCamera> kvp in _sceneCameras)
        {
            if (kvp.Value == null)
                continue;

            kvp.Value.Priority = kvp.Key == sceneName ? 100 : 0;
        }

        HandleRegularCameras(sceneName);
    }

    private void HandleRegularCameras(string activeSceneName)
    {
        Camera[] allCameras = FindObjectsOfType<Camera>(true);

        for (int i = 0; i < allCameras.Length; i++)
        {
            Camera cam = allCameras[i];
            if (cam == null)
                continue;

            bool belongsToActiveScene = cam.gameObject.scene.name == activeSceneName;

            // IMPORTANTE:
            // Esto mantiene la intención de vuestro código original: evitar múltiples cámaras/audio listeners activos.
            cam.enabled = belongsToActiveScene;

            AudioListener listener = cam.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = belongsToActiveScene;
        }
    }

    private void ResetCameraPriorities(string baseSceneName)
    {
        foreach (KeyValuePair<string, CinemachineVirtualCamera> kvp in _sceneCameras)
        {
            if (kvp.Value == null)
                continue;

            kvp.Value.Priority = kvp.Key == baseSceneName ? 100 : 10;
        }
    }

    private CinemachineVirtualCamera FindVCamInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] rootObjects = scene.GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            GameObject root = rootObjects[i];

            CinemachineVirtualCamera vcam = root.GetComponent<CinemachineVirtualCamera>();
            if (vcam != null)
                return vcam;

            vcam = root.GetComponentInChildren<CinemachineVirtualCamera>(true);
            if (vcam != null)
                return vcam;
        }

        return null;
    }

    public string GetCurrentScene()
    {
        return _currentActiveScene;
    }
}