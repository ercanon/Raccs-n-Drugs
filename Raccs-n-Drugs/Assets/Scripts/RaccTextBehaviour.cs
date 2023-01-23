using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaccTextBehaviour : MonoBehaviour
{
    [HideInInspector] public GameObject mCamera;

    // Start is called before the first frame update
    void Start()
    {
        Invoke("Hide", 6);
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(mCamera.transform);
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}
