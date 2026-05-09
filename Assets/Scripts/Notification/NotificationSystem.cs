using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class NotificationSystem : MonoBehaviour
{
    public GameObject notificationPanel;
    public TMP_Text notificationText;
    public float displayTime = 3f;

    [Header("Animation")]
    public float moveDistance = 40f;
    public float animDuration = 0.35f;

    [TextArea]
    public List<string> messages = new List<string>();

    private Coroutine currentNotification;
    private RectTransform panelRect;

    private Vector2 originalPos;

    void Awake()
    {
        panelRect = notificationPanel.GetComponent<RectTransform>();
        originalPos = panelRect.anchoredPosition;
    }

    public void ShowRandomNotification()
    {
        string randomMessage = messages[Random.Range(0, messages.Count)];
        ShowNotification(randomMessage);
    }

    public void ShowNotification(string message)
    {
        if (currentNotification != null)
        {
            StopCoroutine(currentNotification);
        }

        currentNotification = StartCoroutine(ShowNotificationCoroutine(message));
    }

    IEnumerator ShowNotificationCoroutine(string message)
    {
        notificationPanel.SetActive(true);
        notificationText.text = message;

        // Posición inicial un poco más arriba
        panelRect.anchoredPosition = originalPos + Vector2.up * moveDistance;

        // Animación hacia abajo
        panelRect.DOAnchorPos(originalPos, animDuration)
            .SetEase(Ease.OutCubic);

        yield return new WaitForSeconds(displayTime);

        notificationPanel.SetActive(false);
    }
}