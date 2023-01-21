using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    /*---------------------VARIABLES-------------------*/
    enum UIStates { Profile, ServerConfig, Lobby, Settings, GameSettings, NullState }; private UIStates uiStates = UIStates.Profile;

    private bool isHost = false;
    [HideInInspector] public ConnectionScript connect;

    [Header("Animations")]
    [SerializeField] private Animator uiTransition;

    [Header("Information UI")]
    [SerializeField] private GameObject tutorialWindow;

    [Header("Config UI")]
    [SerializeField] private InputField IPInput;
    [SerializeField] private InputField portInput;
    [SerializeField] private Text actionButton;

    [Header("Lobby UI")]
    [SerializeField] private GameObject gameSettings;
    [SerializeField] private Text startButton;

    [Header("Chat")]
    [SerializeField] private InputField userName;
    [SerializeField] private Text ChatBox;
    [SerializeField] private InputField enterMessage;
    private string log;

    [Space]
    [SerializeField] private List<GameObject> uiList;



    /*---------------------CONFIG-------------------*/
    public void Reset()
    {
        log = null;
        ChatBox.text = null;

        portInput.text = "";
        IPInput.text = "";
    }

    void Awake()
    {
        userName.text = "Player" + (int)Random.Range(1, 100);
    }



    /*---------------------CHAT-------------------*/
    public void customLog(string data, string sender, bool nl = true)
    {
        string message = data.TrimEnd('\0');
        log += "[" + sender + "] ";
        log += message;
        if (nl) log += '\n';
    }

    void Update()
    {
        if (log != null) { ChatBox.text += "\n"; ChatBox.text += log; log = null; } // ChatBox.text += "\n" -> Made for a jump before the message. Only happen 1 time.
    }

    public void CreateMessage()
    {
        connect.SendClientData(2);
        customLog(enterMessage.text, userName.text);
        enterMessage.text = "";
    }



    /*---------------------UI-------------------*/
    public void OpenInfo()
    {
        bool set = tutorialWindow.activeSelf;
        tutorialWindow.SetActive(set != true);
    }

    public void UIIteration(int uiMenu)
    {
        uiList[(int)uiStates].SetActive(false);

        uiStates = (UIStates)uiMenu;
        uiList[uiMenu].SetActive(true);

        uiTransition.SetInteger("UIState", uiMenu);
    }

    public void Settings()
    {

    }

    public void GoToConfig(bool option)
    {
        isHost = option;
        if (isHost)
        {
            IPInput.interactable = false;
            actionButton.text = "Create";
            connect.ChangeProfile(0);
        }
        else
        {
            IPInput.interactable = true;
            actionButton.text = "Join";
            connect.ChangeProfile(1);
        }

        UIIteration((int)UIStates.ServerConfig);
    }  
    
    public void GoToLobby()
    {
        if (isHost)
        {
            gameSettings.SetActive(true);
            startButton.text = "Start Game";

            connect.CreateGame(portInput.text, userName.text);
        }
        else
        {
            if (IPInput.text == "")
            {
                IPInput.text = "Use IP to join!";
                return;
            }

            gameSettings.SetActive(false);
            startButton.text = "Ready";

            connect.JoinGame(IPInput.text, portInput.text, userName.text);
        }

        UIIteration((int)UIStates.Lobby);
    }

    public void GameSettings()
    {

    }  

    public void Exit()
    {
        Application.Quit();
    }
}
