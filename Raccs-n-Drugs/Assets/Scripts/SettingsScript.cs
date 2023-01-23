using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsScript : MonoBehaviour
{
    /*---------------------VARAIBLES-------------------*/
    private ConnectionScript connection;
    [SerializeField] private GameplayScript gameplay;
    [SerializeField] private UIScript ui;

    [SerializeField] private Text lvlMapText;
    [SerializeField] private Text gameTypeText;
    [SerializeField] private GameObject adjust;
    private InputField[] fields;
    private bool interactableOnce;

    /*---------------------GAME SETTINGS-------------------*/
    private string[] gameTypeName = { "Casual", "Flash", "Chaos", "Personalized" };
    private string[] mapName = { "BackStreet" };
    enum TypeGame { casual, flash, chaos, personalized }; private TypeGame gameType;
    enum LevelMap { backStreet }; private LevelMap map;
    public struct GameSettings
    {
        public GameSettings(int maxPlayers, int maxCocaineBags, float offsetCocaineSpawn, float timerCocaineSpawn, float walkSpeed, float buffSpeed, float rotateSpeed, int maxCharges)
        {
            this.maxPlayers = maxPlayers;
            this.maxCocaineBags = maxCocaineBags;
            this.offsetCocaineSpawn = offsetCocaineSpawn;
            this.timerCocaineSpawn = timerCocaineSpawn;

            this.walkSpeed = walkSpeed;
            this.buffSpeed = buffSpeed;
            this.rotateSpeed = rotateSpeed;
            this.maxCharges = maxCharges;
        }

        public int maxPlayers;
        public int maxCocaineBags;
        public float offsetCocaineSpawn;
        public float timerCocaineSpawn;

        public float walkSpeed;
        public float buffSpeed;
        public float rotateSpeed;
        public int maxCharges;
    }
    public GameSettings gameSettings;

    /*---------------------SETTINGS-------------------*/
    public struct Settings
    {

    }



    /*---------------------FUNCTIONS-------------------*/
    private void Awake()
    {
        fields = adjust.GetComponentsInChildren<InputField>();
        connection = GetComponent<ConnectionScript>();
        connection.settings = this;

        lvlMapText.text = mapName[(int)map];
        gameTypeText.text = gameTypeName[(int)gameType];

        gameSettings = new GameSettings(4, 6, 2f, 2f, 5f, 8f, 1.5f, 3);
        interactableOnce = false;
        foreach (InputField input in fields)
            input.interactable = false;
        fields[0].text = 4.ToString();
        fields[1].text = 6.ToString();
        fields[2].text = 2f.ToString();
        fields[3].text = 2f.ToString();
        fields[4].text = 5f.ToString();
        fields[5].text = 8f.ToString();
        fields[6].text = 1.5f.ToString();
        fields[7].text = 3.ToString();

        gameplay.settings = gameSettings;
    }

    /*---------------------GAME SETTINGS-------------------*/
    public void SetGameSettings()
    {
        switch (gameType)
        {
            case TypeGame.casual:
                gameSettings = new GameSettings(4, 6, 2f, 2f, 5f, 8f, 1.5f, 3);
                break;
            case TypeGame.flash:
                gameSettings = new GameSettings(4, 6, 2f, 0f, 5f, 10f, 1.5f, 3);
                break;
            case TypeGame.chaos:
                gameSettings = new GameSettings(-1, -1, 2f, 2f, 5f, 8f, 1.5f, 3);
                break;
            case TypeGame.personalized:
                gameSettings = new GameSettings(
                    int.Parse(fields[0].text),
                    int.Parse(fields[1].text),
                    float.Parse(fields[2].text),
                    float.Parse(fields[3].text),
                    float.Parse(fields[4].text),
                    float.Parse(fields[5].text),
                    float.Parse(fields[6].text),
                    int.Parse(fields[7].text));
                break;
        }

        gameplay.settings = gameSettings;
        connection.SendClientData(8);
    }

    public int GameType(int newType = -1)
    {
        if (newType != -1)
            gameType = (TypeGame)newType;

        return (int)gameType;
    }

    public void LVLMap(bool isPositive)
    {
        if (isPositive)
        {
            map++;
            if (map == LevelMap.backStreet + 1)
                map = LevelMap.backStreet;
        }
        else
        {
            map--;
            if (map == LevelMap.backStreet - 1)
                map = LevelMap.backStreet;
        };

        lvlMapText.text = mapName[(int)map];
    }

    public void GameMode(bool isPositive)
    {
        if (isPositive)
        {
            gameType++;
            if (gameType == TypeGame.personalized + 1)
                gameType = TypeGame.casual;
        }
        else
        {
            gameType--;
            if (gameType == TypeGame.casual - 1)
                gameType = TypeGame.personalized;
        };

        gameTypeText.text = gameTypeName[(int)gameType];

        if (gameType == TypeGame.personalized)
        {
            foreach (InputField input in fields)
                input.interactable = true;
            interactableOnce = true;
        }
        else if (interactableOnce)
        {
            foreach (InputField input in fields)
                input.interactable = false;
            interactableOnce = false;
        }

        switch (gameType)
        {
            case TypeGame.casual:
                fields[0].text = 4.ToString();
                fields[1].text = 6.ToString();
                fields[2].text = 2f.ToString();
                fields[3].text = 2f.ToString();
                fields[5].text = 8f.ToString();
                fields[6].text = 1.5f.ToString();
                fields[7].text = 3.ToString();
                break;
            case TypeGame.flash:
                fields[0].text = 4.ToString();
                fields[1].text = 6.ToString();
                fields[2].text = 2f.ToString();
                fields[3].text = 0f.ToString();
                fields[5].text = 10f.ToString();
                fields[6].text = 1.5f.ToString();
                fields[7].text = 3.ToString();
                break;
            case TypeGame.chaos:
                fields[0].text = "-" + 1.ToString();
                fields[1].text = "-" + 1.ToString();
                fields[2].text = 2f.ToString();
                fields[3].text = 2f.ToString();
                fields[5].text = 8f.ToString();
                fields[6].text = 1.5f.ToString();
                fields[7].text = 3.ToString();
                break;
        }
    }

    /*---------------------SETTINGS-------------------*/
    public void MusicVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void FXVolume(float volume)
    {

    }

    public void FullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }
}
