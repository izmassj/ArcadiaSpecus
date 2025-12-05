using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AdvancedSceneCameraManager : MonoBehaviour
{
    public static AdvancedSceneCameraManager instance;

    void Awake()
    {
        // Singleton pattern - evitar duplicados
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si ya existe una instancia, destruir esta
            Destroy(gameObject);
        }
    }

    public CinemachineBrain cinemachineBrain;
    public GameObject mainCanvas;
    public GameObject cursorCanvas;

    private Dictionary<string, CinemachineVirtualCamera> sceneCameras = new Dictionary<string, CinemachineVirtualCamera>();
    private string currentActiveScene;
    private string previousScene;

    void Start()
    {
        // Store initial scene
        currentActiveScene = SceneManager.GetActiveScene().name;
        RegisterCurrentSceneCamera();
    }

    void RegisterCurrentSceneCamera()
    {
        CinemachineVirtualCamera currentVCam = FindVCamInScene(SceneManager.GetSceneByName(currentActiveScene));
        if (currentVCam != null)
        {
            sceneCameras[currentActiveScene] = currentVCam;
            // Set high priority for initial camera
            currentVCam.Priority = 100;
        }
    }

    public void LoadAdditiveScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    IEnumerator LoadSceneRoutine(string sceneName)
    {
        GameObject.Find("Canvas").gameObject.SetActive(false);
        GameObject.Find("CanvasCursor").gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Store previous scene
        previousScene = currentActiveScene;

        // Load new scene additively
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Get the newly loaded scene
        Scene newScene = SceneManager.GetSceneByName(sceneName);

        // Wait one frame for initialization
        yield return null;

        // Find and register new scene's camera
        CinemachineVirtualCamera newVCam = FindVCamInScene(newScene);
        if (newVCam != null)
        {
            sceneCameras[sceneName] = newVCam;
            SwitchToSceneCamera(sceneName);
        }

        // Set as active scene
        SceneManager.SetActiveScene(newScene);
        currentActiveScene = sceneName;
    }

    public void ReturnToBaseScene(string baseSceneName, bool win)
    {
        StartCoroutine(ReturnToBaseRoutine(baseSceneName, win));
    }

    IEnumerator ReturnToBaseRoutine(string baseSceneName, bool win)
    {
        mainCanvas.SetActive(true);
        cursorCanvas.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string sceneToUnload = currentActiveScene;

        // Switch back to base scene's camera BEFORE unloading
        SwitchToSceneCamera(baseSceneName);

        // Set base scene as active
        Scene baseScene = SceneManager.GetSceneByName(baseSceneName);
        SceneManager.SetActiveScene(baseScene);
        if (win)
            ResourceManager.Instance.AddResource(ResourceType.Materials, 50);
        currentActiveScene = baseSceneName;

        // Unload the scene we're leaving
        if (sceneToUnload != baseSceneName)
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneToUnload);

            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            // Remove from dictionary
            if (sceneCameras.ContainsKey(sceneToUnload))
            {
                sceneCameras.Remove(sceneToUnload);
            }

            // Clean up any remaining objects (optional safety)
            CleanupDanglingObjects();
        }

        // Reset camera priorities
        ResetCameraPriorities(baseSceneName);
    }

    void SwitchToSceneCamera(string sceneName)
    {
        if (sceneCameras.ContainsKey(sceneName))
        {
            CinemachineVirtualCamera targetVCam = sceneCameras[sceneName];

            // Set target camera priority to highest
            targetVCam.Priority = 100;

            // Lower priority of all other cameras
            foreach (var kvp in sceneCameras)
            {
                if (kvp.Key != sceneName && kvp.Value != null)
                {
                    kvp.Value.Priority = 0;
                }
            }

            // Also handle regular cameras if they exist
            HandleRegularCameras(sceneName);
        }
        else
        {
            Debug.LogWarning($"No registered camera for scene: {sceneName}");
        }
    }

    void HandleRegularCameras(string activeSceneName)
    {
        Camera[] allCameras = FindObjectsOfType<Camera>();

        foreach (Camera cam in allCameras)
        {
            // Check which scene this camera belongs to
            Scene cameraScene = cam.gameObject.scene;

            if (cameraScene.name == activeSceneName)
            {
                // This is the active scene's camera
                cam.tag = "MainCamera";
                cam.enabled = true;

                // Enable audio listener
                AudioListener listener = cam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }
            else
            {
                // This is from another scene's camera
                cam.tag = "Untagged";
                cam.enabled = false;

                // Disable audio listener
                AudioListener listener = cam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }
    }

    void ResetCameraPriorities(string baseSceneName)
    {
        // Reset all cameras to base priority except base scene
        foreach (var kvp in sceneCameras)
        {
            if (kvp.Value != null)
            {
                if (kvp.Key == baseSceneName)
                {
                    kvp.Value.Priority = 100;
                }
                else
                {
                    kvp.Value.Priority = 10; // Or whatever default you want
                }
            }
        }
    }

    CinemachineVirtualCamera FindVCamInScene(Scene scene)
    {
        if (!scene.IsValid()) return null;

        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            // Check in root object
            CinemachineVirtualCamera vcam = obj.GetComponent<CinemachineVirtualCamera>();
            if (vcam != null) return vcam;

            // Check in children
            vcam = obj.GetComponentInChildren<CinemachineVirtualCamera>(true);
            if (vcam != null) return vcam;
        }

        return null;
    }

    void CleanupDanglingObjects()
    {
        // Find and destroy any objects that might have been left behind
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            // Check if object is in a scene that doesn't exist anymore
            if (obj.scene.name == null || !SceneManager.GetSceneByName(obj.scene.name).IsValid())
            {
                // This object is orphaned, destroy it
                if (obj != gameObject) // Don't destroy this manager
                {
                    Destroy(obj);
                }
            }
        }
    }

    // Public method to get current scene
    public string GetCurrentScene()
    {
        return currentActiveScene;
    }

    // Clean up on destroy
    void OnDestroy()
    {
        sceneCameras.Clear();
    }
}