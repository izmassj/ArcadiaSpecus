using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Utilidad para guardar/cargar remapeos del New Input System.
/// En este lote se deja la base lista; la UI de remapping la haremos en otro lote.
/// </summary>
public static class InputBindingSaveManager
{
    private const string BindingOverridesKey = "InputBindingOverrides";

    public static void SaveBindingOverrides(PlayerInput playerInput)
    {
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning("InputBindingSaveManager: PlayerInput no válido al guardar remapeos.");
            return;
        }

        string json = playerInput.actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(BindingOverridesKey, json);
        PlayerPrefs.Save();
    }

    public static void LoadBindingOverrides(PlayerInput playerInput)
    {
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning("InputBindingSaveManager: PlayerInput no válido al cargar remapeos.");
            return;
        }

        if (!PlayerPrefs.HasKey(BindingOverridesKey))
            return;

        string json = PlayerPrefs.GetString(BindingOverridesKey, string.Empty);

        if (string.IsNullOrWhiteSpace(json))
            return;

        playerInput.actions.LoadBindingOverridesFromJson(json);
    }

    public static void ResetBindingOverrides(PlayerInput playerInput)
    {
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning("InputBindingSaveManager: PlayerInput no válido al resetear remapeos.");
            return;
        }

        playerInput.actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(BindingOverridesKey);
        PlayerPrefs.Save();
    }
}