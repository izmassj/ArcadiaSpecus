using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private TMP_Text collectibleText;
    [SerializeField] private string textFormat = "{0} / {1}";
    
    [Header("Scene Changer")]
    [SerializeField] private GameObject sceneChangeCollider; // The collider that enables at 3 collectibles
    [SerializeField] private int requiredCollectibles = 3;
    [SerializeField] private string nextSceneName; // Name of the scene to load
    
    private int currentCollectibles = 0;
    
    void Awake()
    {
        // Singleton pattern
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
    
    void Start()
    {
        // Initialize UI
        UpdateCollectibleText();
        
        // Disable scene change collider initially
        if (sceneChangeCollider != null)
        {
            sceneChangeCollider.SetActive(false);
        }
    }
    
    public void AddCollectible(int value)
    {
        currentCollectibles += value;
        UpdateCollectibleText();
        
        // Check if we have enough collectibles to activate the scene changer
        CheckForSceneChange();
        
        Debug.Log($"Collectible collected! Total: {currentCollectibles}");
    }
    
    private void UpdateCollectibleText()
    {
        if (collectibleText != null)
        {
            collectibleText.text = string.Format(textFormat, currentCollectibles, requiredCollectibles);
        }
    }
    
    private void CheckForSceneChange()
    {
        if (currentCollectibles >= requiredCollectibles)
        {
            // Enable the collider that allows scene change
            if (sceneChangeCollider != null)
            {
                sceneChangeCollider.SetActive(true);
                Debug.Log("Scene change collider activated!");
            }
        }
    }
    
    // Call this method from the scene change collider
    public void ChangeScene()
    {
        if (currentCollectibles >= requiredCollectibles && !string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Not enough collectibles to change scene!");
        }
    }
    
    // Get current collectible count (optional)
    public int GetCurrentCollectibles()
    {
        return currentCollectibles;
    }
    
    // Reset collectibles (optional)
    public void ResetCollectibles()
    {
        currentCollectibles = 0;
        UpdateCollectibleText();
        
        if (sceneChangeCollider != null)
        {
            sceneChangeCollider.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("sads");
        if (other.CompareTag("Player")) 
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}