using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CocaineBehaviour : MonoBehaviour
{
    [HideInInspector]
    public GameplayScript gameplayScript;
    public bool isBuffed = false;

    // Buffed effect
    MeshRenderer render;
    [Range(0f, 1f)] public float transitionTime;
    public Color[] colors;

    int ColorIndex, len;
    float t;

    private void Start()
    {
        if (isBuffed)
        {
            render = this.gameObject.GetComponent<MeshRenderer>();
            len = colors.Length;
        }
    }

    private void Update()
    {
        if (isBuffed)
        {
            render.material.color = Color.Lerp(render.material.color, colors[ColorIndex], transitionTime * Time.deltaTime * 10);

            t = Mathf.Lerp(t, 1f, transitionTime * Time.deltaTime * 10);

            if (t > 0.9f)
            {
                t = 0;
                ColorIndex++;
                ColorIndex = (ColorIndex >= len) ? 0 : ColorIndex;
            }
        }
    }

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
                gameplayScript.UpdateList(this);
                Destroy(gameObject);
            }
        }
    }
}
