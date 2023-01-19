using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
    public GameplayScript gameplayScript;

    public void StartGame()
    {
        gameplayScript.cocaineCanSpawn = true;
    }
}
