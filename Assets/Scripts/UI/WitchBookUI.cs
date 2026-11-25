using System.Collections.Generic;
using UnityEngine;

public class WitchBookUI : MonoBehaviour
{
    [SerializeField] private GameObject body;
    [SerializeField] private GenericButton questButton;
    [SerializeField] private GenericButton inventoryButton;
    [SerializeField] private GenericButton optionButton;
    [SerializeField] private GenericButton creditsButton;
    [SerializeField] private GenericButton gameExitButton;
    [SerializeField] private GenericButton menuExitButton;
    [SerializeField] private List<GameObject> mainScreensOption;

    void Start()
    {
        questButton.Initialize(OpenMainScreen);
        inventoryButton.Initialize(OpenMainScreen);
        optionButton.Initialize(OpenMainScreen);
        creditsButton.Initialize(OpenMainScreen);
    }

    void OnEnable()
    {
        GameEvents.UI.SetBookVisibility += SetBookVisibility;
    }

    void OnDisable()
    {
        GameEvents.UI.SetBookVisibility -= SetBookVisibility;
    }

    public void SetBookVisibility(TypeBook type)
    {
        body.gameObject.SetActive(!body.gameObject.activeSelf);
        if (!body.gameObject.activeSelf)
            return;

        DesactiveAllButtons();

        switch (type)
        {
            case TypeBook.GameBook:
                questButton.SetActive(true);
                inventoryButton.SetActive(true);
                optionButton.SetActive(true);
                gameExitButton.SetActive(true);
                questButton.ClickSelectedButton(0);
            break;
           case TypeBook.MenuBook:
                optionButton.SetActive(true);
                menuExitButton.SetActive(true);
           break;

           case TypeBook.CreditsBook:
                creditsButton.SetActive(true);
                menuExitButton.SetActive(true);
           break; 
        }
    }

    private void DesactiveAllButtons()
    {
        questButton.SetActive(false);
        inventoryButton.SetActive(false);
        optionButton.SetActive(false);
        creditsButton.SetActive(false);
        gameExitButton.SetActive(false);
        menuExitButton.SetActive(false);
    }

    private void DesactiveAllMainScreen()
    {
        foreach (var screen in mainScreensOption)
        {
            screen.SetActive(false);
        }
    }

    private void OpenMainScreen(int index, GenericButton button)
    {
        if(index < 4)
        {
            DesactiveAllMainScreen();            
            mainScreensOption[index].SetActive(true);
        }
    }
}

public enum TypeBook{
    GameBook,
    MenuBook,
    CreditsBook
}
