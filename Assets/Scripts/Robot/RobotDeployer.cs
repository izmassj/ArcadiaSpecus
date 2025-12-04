using UnityEngine;
using UnityEngine.SceneManagement;

public class RobotDeployer : MonoBehaviour
{
    public static RobotDeployer Instance;

    public string[] sceneOptions = new string[3];

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadRandomScene()
    {
        if (sceneOptions.Length == 0)
        {
            Debug.LogError("No scene names in the array! Add scene names in the Inspector.");
            return;
        }

        int randomIndex = Random.Range(0, sceneOptions.Length);
        string sceneToLoad = sceneOptions[randomIndex];

        SceneManager.LoadScene(sceneToLoad);
    }

    private void Update()
    {
        //if (TryGetComponent<>) { }
    }
}