using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayScript : MonoBehaviour
{
    public GameObject racoon;
    public GameObject cocaine;
    private List<GameObject> cocaineList;
    private bool spawnCocaine;
    private Transform playableArea;

    public int maxCocaineBags;
    public float offsetCocaineSapwn;

    void Awake()
    {
        playableArea = transform.GetChild(0);
        cocaineList = new List<GameObject>();

        spawnCocaine = true;

        //Aqui el spawn del racoon
        //Ej: Instantiate(racoon);
    }

    // Update is called once per frame
    void Update()
    {
        if (spawnCocaine)
        {
            cocaineList.Clear();

            for (int i = 0; i < maxCocaineBags; i++)
            {
                Vector3 randPosition = new Vector3(
                    playableArea.position.x + Random.Range(offsetCocaineSapwn, playableArea.GetComponent<Renderer>().bounds.size.x - offsetCocaineSapwn),
                    cocaine.transform.position.y,
                    playableArea.position.z - Random.Range(offsetCocaineSapwn, playableArea.GetComponent<Renderer>().bounds.size.z - offsetCocaineSapwn));

                GameObject obj = Instantiate(cocaine, randPosition, cocaine.transform.rotation);
                cocaineList.Add(obj);

                obj.GetComponent<CocaineBehaviour>().gameplayScript = this;
                obj.GetComponent<CocaineBehaviour>().isBuffed = i+1 >= maxCocaineBags ? true : false;
            }

            spawnCocaine = false;
        }

        if (cocaineList.Count <= 0)
            spawnCocaine = true;
    }

    public void UpdateList(GameObject obj)
    { cocaineList.Remove(obj); }

    public void DeleteList()
    {
        foreach (GameObject cocaine in cocaineList)
            Destroy(cocaine);

        cocaineList.Clear();
    }
}
