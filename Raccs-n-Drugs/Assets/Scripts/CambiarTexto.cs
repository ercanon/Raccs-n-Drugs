using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CambiarTexto : MonoBehaviour
{
    //public TMPro changingText;
    public GameObject changingTextTwo;
    // Start is called before the first frame update
    void Start()
    {
        TextChange();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TextChange()
    {
        //changingText.text = "2";
        changingTextTwo.GetComponent<TMPro.TextMeshProUGUI>().text = "PLAYER X";
    }
}
