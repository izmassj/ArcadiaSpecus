using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class CameraBunkerManager : MonoBehaviour
{
    public static CameraBunkerManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("Camera")]
    [SerializeField] private CinemachineCamera _mainVirtualCamera;

    private Transform _originalPos;

    private bool _displaced;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _originalPos = transform;
        _displaced = false;
    }

    //// Update is called once per frame
    //void Update()
    //{
        
    //}

    public void MoveCameraTo(Transform newPos)
    {
        _displaced = true;
        _mainVirtualCamera.transform.DOMove(newPos.position, 1f);
    }

    public void MoveCameraToOriginalPos()
    {
        _displaced = false;
        _mainVirtualCamera.transform.DOMove(_originalPos.position, 1f);
    }

    public bool IsCameraDisplaced()
    {
        return _displaced;
    }
}
