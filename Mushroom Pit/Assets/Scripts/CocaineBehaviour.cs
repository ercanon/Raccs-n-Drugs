using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CocaineBehaviour : MonoBehaviour
{
    [HideInInspector]
    public GameplayScript gameplayScript;
    public bool isBuffed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (isBuffed)
            {
                other.gameObject.GetComponent<RacoonBehaviour>().ChangeState(2);
                gameplayScript.DeleteList();
            }
            else 
            {
                gameplayScript.UpdateList(gameObject);
                Destroy(gameObject);
            }
        }
    }
}
