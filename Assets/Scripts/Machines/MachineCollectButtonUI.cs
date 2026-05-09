using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MachineCollectButtonUI : MonoBehaviour
{
    [SerializeField] private MachineManager _machine;
    [SerializeField] private BunkerResourceManager _resourceManager;
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Slider _slider;
    [SerializeField] private string _readyText = "Collect";
    [SerializeField] private string _waitingText = "Waiting";
    [SerializeField] private string _noNPCText = "No Worker";

    private void Awake()
    {
        if (_resourceManager == null)
        {
            _resourceManager = FindFirstObjectByType<BunkerResourceManager>();
        }

        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null)
            _button.onClick.AddListener(OnCollectPressed);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnCollectPressed);
    }

    private void Update()
    {
        bool canCollect = _machine != null && _machine.CanCollectProducedResources();

        if (_button != null)
            _button.interactable = canCollect;

        if (_label != null)
            _label.text = canCollect ? _readyText : _waitingText;

        if (_machine.GetActiveWorkerCount() == 0)
        {
            _label.text = _noNPCText;
        }

        _slider.value = _machine.CurrentTimeCollectProducingResources() / _machine.MaxTimeCollectProducingResources();
    }

    public void Bind(MachineManager machine)
    {
        _machine = machine;
    }

    public void OnCollectPressed()
    {
        if (_machine == null || _resourceManager == null)
            return;

        if (GameAnalyticsManager.Instance != null)
            GameAnalyticsManager.Instance.RegisterMachineCollected(_machine.typeOfMachine.ToString());

        _machine.CollectProducedResources(_resourceManager);
    }
}
