using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CocaineBehaviour : MonoBehaviour
{
    [HideInInspector]
    public GameplayScript gameplayScript;
    public bool isBuffed;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (isBuffed)
            {
                other.gameObject.GetComponent<RacoonBehaviour>().ChangeState(3);
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
