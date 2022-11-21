using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayScript : MonoBehaviour
{
    public GameObject racoon;
    public GameObject cocaine;

    [HideInInspector]
    public List<GameObject> cocaineList;
    public List<GameObject> racoonList;
    //public connection serverConnection;

    private bool spawnCocaine = false;
    private bool cameraTransition = true;
    private Transform playableArea;

    public int maxCocaineBags;
    public float offsetCocaineSpawn;

    GameObject racoonMe;
    GameObject racoonYou;

    void Awake()
    {
        playableArea = transform.GetChild(0);
        cocaineList = new List<GameObject>();
        racoonList = new List<GameObject>();
        //serverConnection = GameObject.Find("Server").GetComponent<connection>();
    }

    private void OnEnable()
    {
        //serverConnection.gameManager = this;

        Transform[] pos = GameObject.Find("RacoonSpawn").GetComponentsInChildren<Transform>();
        //for (int i = 0; i <= serverConnection.UDPclients.Count; i++)
        //{
        //    if (i >= 4)
        //        break;
        racoonMe = Instantiate(racoon, pos[1].position, pos[1].rotation);
        racoonYou = Instantiate(racoon, pos[2].position, pos[2].rotation);
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraTransition)
        {
            Transform camera = GameObject.Find("Main Camera").transform;
            Transform gamePos = GameObject.Find("CameraGamePosition").transform;

            camera.transform.position = Vector3.Lerp(camera.transform.position, gamePos.position, 2 * Time.deltaTime);
            camera.transform.rotation = Quaternion.Lerp(camera.transform.rotation, gamePos.rotation, 2 * Time.deltaTime);

            if (Vector3.Distance(camera.transform.position, gamePos.position) < 0.15) 
                cameraTransition = false;
        }

        if (spawnCocaine)
        {
            cocaineList.Clear();

            for (int i = 0; i < maxCocaineBags; i++)
            {
                Vector3 randPosition = new Vector3(
                    playableArea.position.x + Random.Range(offsetCocaineSpawn, playableArea.GetComponent<Renderer>().bounds.size.x - offsetCocaineSpawn),
                    cocaine.transform.position.y,
                    playableArea.position.z - Random.Range(offsetCocaineSpawn, playableArea.GetComponent<Renderer>().bounds.size.z - offsetCocaineSpawn));

                GameObject obj = Instantiate(cocaine, randPosition, cocaine.transform.rotation);
                cocaineList.Add(obj);

                obj.GetComponent<CocaineBehaviour>().gameplayScript = this;
                obj.GetComponent<CocaineBehaviour>().isBuffed = i+1 >= maxCocaineBags ? true : false;
            }

            spawnCocaine = false;
        }

        if (!cameraTransition && cocaineList.Count <= 0)
            spawnCocaine = true;
    }

    public void UpdateList(GameObject obj)
    { cocaineList.Remove(obj); }

    public void DeleteList()
    {
        foreach (GameObject coc in cocaineList)
            Destroy(coc);

        cocaineList.Clear();
    }
}
