using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSwitcher : MonoBehaviour
{
    [SerializeField] private MenuManager mainMenu;
    [SerializeField] private MenuManager minigamesMenu;
    [SerializeField] private MenuManager coinflipMenu;

    public void OpenMinigames()
    {
        mainMenu.CloseMenu();
        minigamesMenu.ResetToFirstButton();
        minigamesMenu.OpenMenu();
    }

    public void OpenCoinflip()
    {
        minigamesMenu.CloseMenu();
        coinflipMenu.ResetToFirstButton();
        coinflipMenu.OpenMenu();
    }


    public void BackToMainMenu()
    {
        minigamesMenu.CloseMenu();
        mainMenu.OpenMenu();
    }

    public void BackToMinigamesMenu()
    {
        coinflipMenu.CloseMenu();
        minigamesMenu.OpenMenu();
    }
}
