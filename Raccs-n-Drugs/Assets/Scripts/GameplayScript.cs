using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayScript : MonoBehaviour
{
    /*---------------------VARIABLES-------------------*/
    [SerializeField] private GameObject racoon;
    [SerializeField] private GameObject cocaine;
    [SerializeField] private Animator mainCamera;
    [SerializeField] private GameObject ui;
    [SerializeField] private GameObject win;

    [Space]
    [SerializeField] private List<Color> racoonColors;
    [SerializeField] private List<Vector3> raccsPositions;
    [SerializeField] private List<Quaternion> raccsYRototation;

    private List<RaccBehaviour> raccsList;
    [HideInInspector] public int posRaccList;
    [HideInInspector] public List<CocaineBehaviour> cocaineList;

    [HideInInspector] public ConnectionScript connect;
    [HideInInspector] public SettingsScript.GameSettings settings;

    public GameObject songGameplay;
    public GameObject songMenu;


    /*---------------------MAIN-------------------*/
    public void Reset()
    {
        if (cocaineList != null)
            DeleteCocaineList();
        cocaineList = new List<CocaineBehaviour>();

        if (raccsList != null)
            DeleteRaccsList();
        raccsList = new List<RaccBehaviour>();

        connect.clientsReady = 0;
    }

    private void FixedUpdate()
    {
        if (!ui.activeInHierarchy && !win.activeInHierarchy)
            connect.SendClientData(4);
    }

    public void LaunchGame(int size = 0)
    {
        mainCamera.SetInteger("UIState", 5);
        ui.SetActive(false);
        songMenu.SetActive(false);
        songGameplay.SetActive(true);
        if (settings.maxCocaineBags < 0)
            settings.maxCocaineBags = size;

        for (int i = 0; i < size; i++)
        {
            if (settings.maxPlayers > 0)
            {
                if (i > settings.maxPlayers)
                    return;

                GameObject racc = Instantiate(racoon, raccsPositions[i], raccsYRototation[i]);

                RaccBehaviour raccScript = racc.GetComponent<RaccBehaviour>();
                raccScript.gameplay = this;

                SkinnedMeshRenderer render = racc.transform.GetChild(2).GetComponent<SkinnedMeshRenderer>();
                Color[] list = { racoonColors[i], render.material.GetColor("_EmissionColor") };
                raccScript.colors = list;

                render.material.SetColor("_EmissionColor", list[0]);
                render.material.EnableKeyword("_EMISSION");

                TMPro.TextMeshProUGUI headText = racc.transform.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                headText.text = connect.clientsNames[i];
                headText.gameObject.GetComponent<RaccTextBehaviour>().mCamera = mainCamera.gameObject;

                if (posRaccList == i)
                    raccScript.owned = true;
                raccsList.Add(raccScript);
            }
            else
            {
                Vector3 bounds = GetComponent<Renderer>().bounds.size;
                Vector3 randPosition = new Vector3(
                    (transform.position.x - bounds.x / 2) + Random.Range(settings.offsetCocaineSpawn, bounds.x - settings.offsetCocaineSpawn),
                    cocaine.transform.position.y,
                    (transform.position.z - bounds.z / 2) + Random.Range(settings.offsetCocaineSpawn, bounds.z - settings.offsetCocaineSpawn));

                GameObject racc = Instantiate(racoon, randPosition, Random.rotation);

                RaccBehaviour raccScript = racc.GetComponent<RaccBehaviour>();
                raccScript.gameplay = this;

                SkinnedMeshRenderer render = racc.transform.GetChild(2).GetComponent<SkinnedMeshRenderer>();
                Color[] list = { Random.ColorHSV(), render.material.GetColor("_EmissionColor") };
                raccScript.colors = list;

                render.material.SetColor("_EmissionColor", list[0]);
                render.material.EnableKeyword("_EMISSION");

                TMPro.TextMeshProUGUI headText = racc.transform.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                headText.text = connect.clientsNames[i];
                headText.gameObject.GetComponent<RaccTextBehaviour>().mCamera = mainCamera.gameObject;

                if (posRaccList == i)
                    raccScript.owned = true;
                raccsList.Add(raccScript);
            }
        }
    }

    public void CheckEndGame()
    {
        int aliveRaccs = 0;
        int pos = 0;
        for (int i = 0; i < raccsList.Count; i++)
        {
            if (raccsList[i].GetState() != 4)
            {
                aliveRaccs++;
                pos = i;
            }

            if (aliveRaccs > 1)
                return;
        }

        win.SetActive(true);
        songGameplay.SetActive(false);
        songMenu.SetActive(true);
        raccsList[pos].Invoke("IdleEndGame", 2);
        mainCamera.SetInteger("UIState", 6);
    }

    public void SendData(int typeData)
    {
        connect.SendClientData(typeData);
    }



    /*---------------------RACCS-------------------*/
    public void UpdateRacoon(Vector3 position, Vector3 rotation, int posRacoon)
    {
        RaccBehaviour racc = raccsList[posRacoon];

        if (racc.GetState() == 0 || racc.GetState() == 1)
        {
            if (Vector3.Distance(racc.transform.position, position) < 0.01f)
                racc.ChangeState(0);
            else
                racc.ChangeState(1);
        }

        racc.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
    }

    public void ChargeRacoon(int posRacoon)
    {
        raccsList[posRacoon].ChangeState(3);
    }

    public void DeleteRaccsList(RaccBehaviour exception = null)
    {
        foreach (RaccBehaviour raccScript in raccsList)
            if (exception != raccScript)
                Destroy(raccScript.gameObject);

        raccsList.Clear();
    }

    public Transform GetRaccTransform(int pos)
    {
        if (raccsList.Count > pos)
            return raccsList[pos].transform;

        return null;
    }



    /*---------------------COCAINE-------------------*/
    public void SpawnCocaine()
    {
        if (posRaccList == 0)
        {
            for (int i = 0; i < settings.maxCocaineBags; i++)
            {
                Vector3 bounds = GetComponent<Renderer>().bounds.size;
                Vector3 randPosition = new Vector3(
                    (transform.position.x - bounds.x / 2) + Random.Range(settings.offsetCocaineSpawn, bounds.x - settings.offsetCocaineSpawn),
                    cocaine.transform.position.y,
                    (transform.position.z - bounds.z / 2) + Random.Range(settings.offsetCocaineSpawn, bounds.z - settings.offsetCocaineSpawn));

                GameObject obj = Instantiate(cocaine, randPosition, cocaine.transform.rotation);
                CocaineBehaviour cocaScript = obj.GetComponent<CocaineBehaviour>();
                cocaScript.gameplayScript = this;
                cocaScript.isBuffed = i == 0 ? true : false;
                cocaineList.Add(cocaScript);
            }

            connect.SendClientData(3);
        }
    }

    public void UpdateCocaine(Vector3 position, int posRacoon, bool isBuffed = false)
    {
        GameObject obj = Instantiate(cocaine, position, cocaine.transform.rotation);
        CocaineBehaviour cocaScript = obj.GetComponent<CocaineBehaviour>();
        cocaScript.gameplayScript = this;
        cocaScript.isBuffed = isBuffed;
        cocaineList.Add(cocaScript);
    }

    public void UpdateCocaineList(CocaineBehaviour cocaScript)
    {
        cocaineList.Remove(cocaScript);
    }

    public void DeleteCocaineList()
    {
        foreach (CocaineBehaviour cocaScript in cocaineList)
            Destroy(cocaScript.gameObject);

        cocaineList.Clear();
    }
}
