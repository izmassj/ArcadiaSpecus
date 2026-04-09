using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BunkerResourceSlidersUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BunkerResourceManager _resourceManager;

    [Header("Sliders")]
    [SerializeField] private Slider _scrapSlider;
    [SerializeField] private Slider _electricitySlider;
    [SerializeField] private Slider _waterSlider;
    [SerializeField] private Slider _foodSlider;

    [Header("Display Max Values")]
    [SerializeField] private int _scrapMax = 100;
    [SerializeField] private int _electricityMax = 100;
    [SerializeField] private int _waterMax = 100;
    [SerializeField] private int _foodMax = 100;

    [Header("Behaviour")]
    [SerializeField] private bool _setSlidersAsNonInteractable = true;
    [SerializeField] private bool _autoFindResourceManager = true;

    private int _lastScrap = int.MinValue;
    private int _lastElectricity = int.MinValue;
    private int _lastWater = int.MinValue;
    private int _lastFood = int.MinValue;

    private void Awake()
    {
        if (_resourceManager == null && _autoFindResourceManager)
            _resourceManager = FindFirstObjectByType<BunkerResourceManager>();

        SetupSlider(_scrapSlider, _scrapMax);
        SetupSlider(_electricitySlider, _electricityMax);
        SetupSlider(_waterSlider, _waterMax);
        SetupSlider(_foodSlider, _foodMax);
    }

    private void OnEnable()
    {
        ForceRefresh();
    }

    private void Update()
    {
        RefreshIfChanged();
    }

    [ContextMenu("Force Refresh")]
    public void ForceRefresh()
    {
        if (_resourceManager == null)
            return;

        ApplyScrap(_resourceManager.Scrap);
        ApplyElectricity(_resourceManager.Electricity);
        ApplyWater(_resourceManager.Water);
        ApplyFood(_resourceManager.Food);
    }

    private void RefreshIfChanged()
    {
        if (_resourceManager == null)
            return;

        int scrap = _resourceManager.Scrap;
        int electricity = _resourceManager.Electricity;
        int water = _resourceManager.Water;
        int food = _resourceManager.Food;

        if (scrap != _lastScrap)
            ApplyScrap(scrap);

        if (electricity != _lastElectricity)
            ApplyElectricity(electricity);

        if (water != _lastWater)
            ApplyWater(water);

        if (food != _lastFood)
            ApplyFood(food);
    }

    private void ApplyScrap(int amount)
    {
        _lastScrap = amount;
        SetSliderValue(_scrapSlider, amount, ref _scrapMax);
    }

    private void ApplyElectricity(int amount)
    {
        _lastElectricity = amount;
        SetSliderValue(_electricitySlider, amount, ref _electricityMax);
    }

    private void ApplyWater(int amount)
    {
        _lastWater = amount;
        SetSliderValue(_waterSlider, amount, ref _waterMax);
    }

    private void ApplyFood(int amount)
    {
        _lastFood = amount;
        SetSliderValue(_foodSlider, amount, ref _foodMax);
    }

    private void SetupSlider(Slider slider, int maxValue)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = Mathf.Max(1, maxValue);

        if (_setSlidersAsNonInteractable)
            slider.interactable = false;
    }

    private void SetSliderValue(Slider slider, int amount, ref int displayMax)
    {
        if (slider == null)
            return;

        if (amount > displayMax)
        {
            displayMax = amount;
            slider.maxValue = displayMax;
        }

        slider.value = Mathf.Clamp(amount, 0, Mathf.RoundToInt(slider.maxValue));
    }
}
