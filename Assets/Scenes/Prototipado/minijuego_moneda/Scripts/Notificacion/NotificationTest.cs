using UnityEngine;

public class NotificationTester : MonoBehaviour
{
    public NotificationSystem notificationSystem;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            notificationSystem.ShowRandomNotification();
        }
    }
}