using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField] private QuestBookUI questBookUI;

    List<GameQuest> quests;

    void Start()
    {
        questButton.Initialize(OpenQuestBookMainScreen);
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

    public void SetBookVisibility(TypeBook type, List<GameQuest> playerQuests)
    {
        quests = playerQuests;
        body.gameObject.SetActive(!body.gameObject.activeSelf);
        if (!body.gameObject.activeSelf)
        {
            CloseQuestUI();
            return;
        }
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
        questBookUI.ResetQuestUI();
    }

    private void OpenMainScreen(int index, GenericButton button)
    {
        if(index < 4)
        {
            DesactiveAllMainScreen();            
            mainScreensOption[index].SetActive(true);
        }
    }

    private void OpenQuestBookMainScreen(int index, GenericButton button)
    {
        OpenMainScreen(index, button);
        SetupQuest();
    }

    private void SetupQuest()
    {
        questBookUI.CreateQuestUI(quests);
    }

    private void CloseQuestUI()
    {
        questBookUI.DesactiveAllQuestUI();
    }
}

public enum TypeBook{
    GameBook,
    MenuBook,
    CreditsBook
}
