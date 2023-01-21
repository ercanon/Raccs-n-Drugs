using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CocaineBehaviour : MonoBehaviour
{
    [HideInInspector] public GameplayScript gameplayScript;
    public bool isBuffed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (isBuffed)
            {
                other.gameObject.GetComponent<RaccBehaviour>().ChangeState(2);
                gameplayScript.DeleteCocaineList();
            }
            else 
            {
                gameplayScript.UpdateCocaineList(this);
                Destroy(gameObject);
            }
        }
    }
}
