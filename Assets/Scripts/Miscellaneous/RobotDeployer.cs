using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// RobotDeployer (fix cámara):
/// - Elige un minijuego aleatorio de una lista.
/// - Lo carga en aditivo.
/// - Tras cargar, fuerza que se use la cámara de la escena del minijuego (desactiva cámaras de otras escenas).
///
/// Esto evita el caso que comentas: "se queda la cámara del escenario".
/// </summary>
public class RobotDeployer : MonoBehaviour
{
    [Header("Minijuegos (nombres de escena)")]
    [SerializeField] private List<string> _miniGameSceneNames = new List<string>
    {
        "MiniGameDesign1",
        "MiniGameDesign2",
        "MiniGameDesign3"
    };

    [Header("Activación")]
    [SerializeField] private bool _activateOnTrigger = false;
    [SerializeField] private string _playerTag = "Player";

    [Header("Cámara")]
    [Tooltip("Tiempo máximo (segundos) esperando a que la escena se cargue.")]
    [SerializeField] private float _loadTimeoutSeconds = 8f;

    public void Deploy()
    {
        if (_miniGameSceneNames == null || _miniGameSceneNames.Count == 0)
        {
            Debug.LogWarning("RobotDeployer: no hay escenas de minijuego en la lista.");
            return;
        }

        int idx = Random.Range(0, _miniGameSceneNames.Count);
        string sceneName = _miniGameSceneNames[idx];

        // Aquí iría SFX/animación de deploy.
        SceneFlowManager.EnsureInstance().LoadMiniGameAdditive(sceneName);

        // Forzamos cámara del minijuego cuando termine de cargar.
        StartCoroutine(ForceMinigameCameraAfterLoad(sceneName));
    }

    private void OnMouseDown()
    {
        if (_activateOnTrigger) return;
        Deploy();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_activateOnTrigger) return;
        if (!other.CompareTag(_playerTag)) return;
        Deploy();
    }

    private IEnumerator ForceMinigameCameraAfterLoad(string sceneName)
    {
        float t = 0f;

        // Esperar a que la escena exista y esté cargada
        while (t < _loadTimeoutSeconds)
        {
            Scene s = SceneManager.GetSceneByName(sceneName);
            if (s.IsValid() && s.isLoaded)
                break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning($"RobotDeployer: no se pudo forzar cámara porque '{sceneName}' no terminó de cargar.");
            yield break;
        }

        // Importante: escena activa = minijuego (muchos scripts usan SceneManager.GetActiveScene)
        SceneManager.SetActiveScene(scene);

        // Forzar cámaras: habilitar solo cámaras del minijuego
        ForceCamerasForScene(scene);
    }

    private void ForceCamerasForScene(Scene activeScene)
    {
        Camera[] allCameras = FindObjectsOfType<Camera>(true);

        Camera candidateMain = null;
        Camera existingMainInScene = null;

        for (int i = 0; i < allCameras.Length; i++)
        {
            Camera cam = allCameras[i];
            if (cam == null)
                continue;

            bool isInTargetScene = cam.gameObject.scene == activeScene;

            // Activamos cámaras del minijuego, desactivamos las demás (incluye la del bunker).
            cam.enabled = isInTargetScene;

            if (isInTargetScene)
            {
                candidateMain ??= cam;

                if (cam.CompareTag("MainCamera"))
                    existingMainInScene = cam;
            }

            // Evitar múltiples AudioListeners
            AudioListener al = cam.GetComponent<AudioListener>();
            if (al != null)
                al.enabled = isInTargetScene;
        }

        // Asegurar que haya una MainCamera real en el minijuego (por si scripts usan Camera.main)
        Camera finalMain = existingMainInScene != null ? existingMainInScene : candidateMain;

        if (finalMain != null && !finalMain.CompareTag("MainCamera"))
        {
            // Solo cambia el tag del minijuego (se destruye al salir).
            finalMain.tag = "MainCamera";
        }
    }
}
