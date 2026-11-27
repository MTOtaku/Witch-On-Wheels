using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{

    [Header("Gerenciador das telas")]
    [Tooltip("Linkar os Objetos de quais estes menus pertencem")]

    [Header("Paineis")]
    [SerializeField] private GameObject[] menuPanels;
    [SerializeField] private GameObject titleScreenPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [SerializeField] private GenericButton2D startButton;
    [SerializeField] private GenericButton2D optionsButton;
    [SerializeField] private GenericButton2D creditsButton;
    [SerializeField] private GenericButton2D quitButton;
    //[SerializeField] private GenericButton2D languageButton;

    //[SerializeField] private List<string> keyReferences;
    [SerializeField] private List<GenericButton2D> backsButtons;
    //[SerializeField] private int currentLanguageIndex;
    [SerializeField] private TextMeshProUGUI gameVersionText;

    [Header("Level Start")]
    [Tooltip("Nome do Level ou da Scene que vai iniciar ao apertar o Play")]
    [SerializeField] private string GAME_SCENE_NAME = "LD_2";


    private GameObject currentPanel;

    void Start()
    {
        startButton.SetupButton(LoadGameLevel);
        creditsButton.SetupButton(OpenCreditsPanel);
        optionsButton.SetupButton(OpenSettingsPanel);
        quitButton.SetupButton(QuitGame);

        foreach (var backButton in backsButtons)
        {
            backButton.SetupButton(BackToTitleScreen);
        }

        //gameVersionText.text = $"Game Version: {Application.version}";
    }


    //Ativar somente o painel escolhido
    public void OpenPanel(GameObject selectedPanel)
    {
        if (selectedPanel == null)
            return;
        selectedPanel.SetActive(true);
        currentPanel = selectedPanel;
    }

    public void BackToTitleScreen()
    {
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }
        currentPanel = null;
    }

    /*public void OpenLevelSelectPanel()
    {
        GameEvents.UI.OnFadeAnimation?.Invoke(false, LoadGameLevel);
    }*/

    private void LoadGameLevel()
    {
        SceneManager.LoadScene(GAME_SCENE_NAME);
    }

    public void OpenSettingsPanel()
    {
        OpenPanel(settingsPanel);
    }

    public void OpenCreditsPanel()
    {
        OpenPanel(creditsPanel);
    }
    public void QuitGame()
    {
        Debug.Log("Saindo do jogo");
        Application.Quit();
    }

    /*public void ChangeCurrentLanguage()
    {
        currentLanguageIndex++;
        if (currentLanguageIndex == LocalizationSettings.AvailableLocales.Locales.Count)
            currentLanguageIndex = 0;
        languageButton.ChangeKeyReference(keyReferences[currentLanguageIndex]);
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[currentLanguageIndex];
        GameEvents.Localization.OnChangeLocalization?.Invoke();
    }*/


}
