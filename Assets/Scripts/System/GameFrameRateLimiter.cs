using UnityEngine;

public class GameFrameRateLimiter : MonoBehaviour
{
    [SerializeField] private int _targetFrameRate = 60;
    [SerializeField] private bool _dontDestroyOnLoad = true;

    private static GameFrameRateLimiter _instance;

    private void Awake()
    {
        if (_dontDestroyOnLoad)
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        Apply();
    }

    private void Apply()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = _targetFrameRate;
    }
}