using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    /*---------------------VARIABLES-------------------*/
    enum UIStates { Profile, ServerConfig, Lobby, Settings, GameSettings, GamePlay, Win }; private UIStates uiStates;

    private UIStates prevUIState;
    private bool isHost = false;
    [HideInInspector] public ConnectionScript connect;
    [HideInInspector] public GameplayScript gameplay;

    [Header("Animations")]
    [SerializeField] private Animator uiTransition;
    private Animator cameraTransition;

    [Header("Information UI")]
    [SerializeField] private GameObject tutorialWindow;

    [Header("Config UI")]
    [SerializeField] private InputField IPInput;
    [SerializeField] private InputField portInput;
    [SerializeField] private Text actionButton;

    [Header("Lobby UI")]
    [SerializeField] private GameObject gameSettings;
    public Toggle startButton;
    private Text startButtonText;
    private GameObject startButtonObject;

    [Header("Chat")]
    public InputField userName;
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
    }

    void Awake()
    {
        uiStates = UIStates.Profile;
        cameraTransition = GetComponent<Animator>();
        startButtonText = startButton.transform.GetChild(1).GetComponent<Text>();
        startButtonObject = startButton.transform.GetChild(0).gameObject;
        userName.text = "Player" + (int)Random.Range(1, 100);
    }

    public void StartGame()
    {
        gameplay.Invoke("SpawnCocaine", 0);
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
        prevUIState = uiStates;
        uiList[(int)uiStates].SetActive(false);

        uiStates = (UIStates)uiMenu;
        uiList[uiMenu].SetActive(true);

        uiTransition.SetInteger("UIState", uiMenu);
        cameraTransition.SetInteger("UIState", uiMenu);
    }

    public void GoToPrevious()
    {
        UIIteration((int)prevUIState);
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
            startButtonText.text = "Start Game";
            startButtonObject.SetActive(false);

            connect.CreateGame(portInput.text, userName.text);
        }
        else
        {
            if (IPInput.text == "")
            {
                IPInput.text = "Use IP to join!";
                return;
            }
            if (IPInput.text == "Use IP to join!")
                    return;

            gameSettings.SetActive(false);
            startButtonText.text = "Ready";

            connect.JoinGame(IPInput.text, portInput.text, userName.text);
        }

        UIIteration((int)UIStates.Lobby);
    }
    
    public void Exit()
    {
        Application.Quit();
    }
}
