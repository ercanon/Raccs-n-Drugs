using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayScript : MonoBehaviour
{
    public GameObject cocaine;

    [HideInInspector]
    public List<GameObject> cocaineList;

    private bool spawnCocaine = false;
    private bool cameraTransition = true;

    public int maxCocaineBags;
    public float offsetCocaineSpawn;

    private Transform playableArea;
    private Transform camera;
    private Transform gamePos;

    void Awake()
    {
        playableArea = transform.GetChild(0);
        cocaineList = new List<GameObject>();

        camera = GameObject.Find("Main Camera").transform;
        gamePos = GameObject.Find("CameraGamePosition").transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraTransition)
        {
            camera.transform.position = Vector3.Lerp(camera.transform.position, gamePos.position, 2 * Time.deltaTime);
            camera.transform.rotation = Quaternion.Lerp(camera.transform.rotation, gamePos.rotation, 2 * Time.deltaTime);

            if (Vector3.Distance(camera.transform.position, gamePos.position) < 0.15) 
                cameraTransition = false;
        }

        if (spawnCocaine)
            SpawnCocaine();

        if (!cameraTransition && cocaineList.Count <= 0)
            spawnCocaine = true;
    }

    public void SpawnCocaine()
    {
        cocaineList.Clear();

        for (int i = 0; i < maxCocaineBags; i++)
        {
            Vector3 randPosition = new Vector3(
                playableArea.position.x + Random.Range(offsetCocaineSpawn, playableArea.GetComponent<Renderer>().bounds.size.x - offsetCocaineSpawn),
                cocaine.transform.position.y,
                playableArea.position.z - Random.Range(offsetCocaineSpawn, playableArea.GetComponent<Renderer>().bounds.size.z - offsetCocaineSpawn));

            GameObject obj = Instantiate(cocaine, randPosition, cocaine.transform.rotation);
            obj.GetComponent<CocaineBehaviour>().gameplayScript = this;
            obj.GetComponent<CocaineBehaviour>().isBuffed = i + 1 >= maxCocaineBags ? true : false;
            cocaineList.Add(obj);
        }


        spawnCocaine = false;
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
