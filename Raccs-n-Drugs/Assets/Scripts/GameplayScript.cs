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
    [HideInInspector] public List<CocaineBehaviour> cocaineList;
    [HideInInspector] public int posRaccList;
    [HideInInspector] public bool cocaineCanSpawn = false;
    [HideInInspector] public ConnectionScript connect;
    private float timerSpawn = 0f;

    [Space]
    [Header("Game Config")]
    public int maxCocaineBags = 6;
    public float offsetCocaineSpawn = 2f;
    public float timerCocaineSpawn = 0f;


    /*---------------------MAIN-------------------*/
    public void Reset()
    {
        if (cocaineList != null)
            DeleteCocaineList();
        cocaineList = new List<CocaineBehaviour>();

        if (raccsList != null)
            DeleteRaccsList();
        raccsList = new List<RaccBehaviour>();
        posRaccList = -1;
    }

    private void FixedUpdate()
    {
        if (posRaccList == 0 && cocaineCanSpawn)
        {
            if (timerSpawn < 0f)
                SpawnCocaine();
            else
                timerSpawn -= Time.deltaTime;
        }

        if (raccsList.Count > 0)
            connect.SendClientData(4);
    }

    public void LaunchGame(int size = 0)
    {
        mainCamera.SetInteger("UIState", 5);
        ui.SetActive(false);

        for (int i = 0; i < size; i++)
        {
            if (i > 4)
                break;

            GameObject racc = Instantiate(racoon, raccsPositions[i], raccsYRototation[i]);

            RaccBehaviour raccScript = racc.GetComponent<RaccBehaviour>();
            raccScript.gameplayScript = this;

            SkinnedMeshRenderer render = racc.transform.GetChild(1).GetComponent<SkinnedMeshRenderer>();
            Color[] list = { racoonColors[i], render.material.GetColor("_EmissionColor") };
            raccScript.colors = list;

            render.material.SetColor("_EmissionColor",list[0]);
            render.material.EnableKeyword("_EMISSION");

            if (posRaccList == i)
                raccScript.owned = true;
            raccsList.Add(raccScript);
        }
    }

    public void CheckEndGame()
    {
        int aliveRaccs = 0;
        int pos = -1;
        foreach (RaccBehaviour raccScript in raccsList)
        {
            if (raccScript.GetState() != 5)
                aliveRaccs++;

            if (aliveRaccs > 1)
                return;

            pos++;
        }

        win.SetActive(true);
        ui.SetActive(true);
        raccsList[pos].transform.SetPositionAndRotation(new Vector3(-0.11f, 0.021f, -0.24f), Quaternion.Euler(new Vector3(0, 180, 0)));
        mainCamera.SetInteger("UIState", 6);
    }



    /*---------------------RACCS-------------------*/
    public void UpdateRacoon(Vector3 position, Vector3 rotation, int posRacoon)
    {
        RaccBehaviour racc = raccsList[posRacoon];

        if (racc.GetState() == 0 || racc.GetState() == 1)
        {
            if (racc.transform.position != position)
                racc.ChangeState(1);
            else
                racc.ChangeState(0);
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
        if (raccsList.Count <= pos)
            return raccsList[pos].transform;

        return null;
    }

    /*---------------------COCAINE-------------------*/
    public void SpawnCocaine()
    {
        for (int i = 0; i < maxCocaineBags; i++)
        {
            Vector3 bounds = GetComponent<Renderer>().bounds.size;
            Vector3 randPosition = new Vector3(
                (transform.position.x - bounds.x / 2) + Random.Range(offsetCocaineSpawn, bounds.x - offsetCocaineSpawn),
                cocaine.transform.position.y,
                (transform.position.z - bounds.z / 2) + Random.Range(offsetCocaineSpawn, bounds.z - offsetCocaineSpawn));

            GameObject obj = Instantiate(cocaine, randPosition, cocaine.transform.rotation);
            CocaineBehaviour cocaScript = obj.GetComponent<CocaineBehaviour>();
            cocaScript.gameplayScript = this;
            cocaScript.isBuffed = i == 0 ? true : false;
            cocaineList.Add(cocaScript);
        }

        cocaineCanSpawn = false;
        timerSpawn = timerCocaineSpawn;
        connect.SendClientData(3);
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
