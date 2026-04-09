using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BunkerNpcStatusListEntryUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Slider _hungerFill;
    [SerializeField] private Slider _thirstFill;
    [SerializeField] private Slider _fatigueFill;

    private NPCBunkerWorker _worker;

    public void Bind(NPCBunkerWorker worker)
    {
        _worker = worker;
        Refresh();
    }

    public NPCBunkerWorker GetBoundWorker()
    {
        return _worker;
    }

    public void Refresh()
    {
        if (_worker == null)
            return;

        string displayName = _worker.gameObject.name;
        if (_nameText != null)
            _nameText.text = displayName;

        float hunger = Mathf.Clamp01(_worker.GetHunger() / 100f);
        float thirst = Mathf.Clamp01(_worker.GetThirst() / 100f);
        float fatigue = Mathf.Clamp01(_worker.GetFatigue() / 100f);

        SetFill(_hungerFill, hunger);
        SetFill(_thirstFill, thirst);
        SetFill(_fatigueFill, fatigue);
    }

    private void SetFill(Slider targetImage, float value)
    {
        if (targetImage == null)
            return;

        targetImage.value = value;
    }
}
