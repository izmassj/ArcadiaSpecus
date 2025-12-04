using UnityEngine;

public class SceneEntryManager : MonoBehaviour
{
    void Start()
    {
        // Restaurar control del ratón
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Asegurar que la UI puede recibir eventos
        var es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
            Debug.LogWarning("No hay EventSystem en la escena. Añádelo para que funcionen los clickables.");
    }
}
