using System.Collections.Generic;
using UnityEngine;

public class RacoonBehaviour : MonoBehaviour
{
    enum RacoonState
    {
        onPause,
        idle,
        walking,
        buffed,
        charging,
        dead
    }
    private RacoonState rState;
    public float walkSpeed = 5;
    public float buffSpeed = 8;
    public float rotateSpeed = 3.5f;
    public int charges = 3;
    private float timerCharge = 0f;

    // Buffed effect / Buffed feedback
    // Rainbow
    SkinnedMeshRenderer render;
    [Range(0f, 1f)] public float transitionTime;
    public Color originalColor;
    public Color[] colors;

    int ColorIndex, len;
    float t;

    // Charges count
    public GameObject[] chargesBox;

    [HideInInspector]
    public bool owned = false;

    public GameplayScript gameplayScript;
    private Rigidbody rBody;
    private Animator anim;

    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        ChangeState((int)RacoonState.onPause);

        rBody = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        render = transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
        len = colors.Length;
    }

    void FixedUpdate()
    {
        if (rState != RacoonState.dead && rState != RacoonState.onPause)
        {
            if (owned && rState != RacoonState.charging)
            {
                if (rState != RacoonState.buffed)
                {
                    float targetMovingSpeed = walkSpeed;

                    if (speedOverrides.Count > 0)
                        targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();

                    // Get targetVelocity from input.
                    Vector2 targetVelocity = new Vector2(Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

                    // Apply movement.
                    rBody.velocity = new Vector3(targetVelocity.x, 0, targetVelocity.y);

                    //Apply rotation.
                    if (rBody.velocity.magnitude > 0)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(rBody.velocity), Time.deltaTime * 10f);
                        ChangeState((int)RacoonState.walking);
                    }
                    else
                        ChangeState((int)RacoonState.idle);

                    render.material.color = originalColor;
                    chargesBox[0].SetActive(false);
                    chargesBox[1].SetActive(false);
                    chargesBox[2].SetActive(false);
                }
                else
                {
                    transform.Rotate(0, Input.GetAxis("Horizontal") * rotateSpeed, 0);
                    if (Input.GetKeyDown("space"))                     
                        ChangeState((int)RacoonState.charging);
                    
                    RainbowBuffed();

                    switch (charges)
                    {
                        case 3:
                            chargesBox[0].SetActive(true);
                            chargesBox[1].SetActive(true);
                            chargesBox[2].SetActive(true);
                            break;
                        case 2:
                            chargesBox[0].SetActive(false);
                            break;
                        case 1:
                            chargesBox[1].SetActive(false);
                            break;
                        case 0:
                            chargesBox[2].SetActive(false);
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (rState == RacoonState.charging)
            {
                timerCharge += Time.deltaTime;
                if (timerCharge > 1)
                    ChargedTransitions();
            }
        }
    }

    private void ChargedTransitions()
    {
        if (charges == 0)
        {
            ChangeState((int)RacoonState.idle);
            gameplayScript.cocaineCanSpawn = true;
        }
        else
        {
            ChangeState((int)RacoonState.buffed);
        }
    }

    public void ChangeState(int state)
    {
        switch (state)
        {
            case 0: //On Pause
                if (rState == RacoonState.onPause)
                    return;

                rState = RacoonState.onPause;
                anim.Play("Idle");
                break;
            case 1: //Idle
                if (rState == RacoonState.idle)
                    return;

                rState = RacoonState.idle;
                anim.Play("Idle");
                break;
            
            case 2: // Walking
                if (rState == RacoonState.walking)
                    return;

                rState = RacoonState.walking;
                anim.Play("Walking");
                break;
            
            case 3: //Buffed
                if (rState == RacoonState.buffed)
                    return;

                rBody.velocity = Vector3.zero;
                timerCharge = 0f;
                if (charges <= 0)
                    charges = 3;

                rState = RacoonState.buffed;
                anim.Play("Idle Buff");
                break;

            case 4: //Charging
                if (rState == RacoonState.charging)
                    return;

                rBody.velocity = transform.forward * buffSpeed;
                charges = charges - 1;

                rState = RacoonState.charging;
                //animation
                break;

            case 5: //Dead
                if (rState == RacoonState.dead)
                    return;

                rState = RacoonState.dead;
                //animation
                break;

            default:
                break;
        }
        anim.SetInteger("rRacoonAnim", (int)rState);
    }

    void RainbowBuffed()
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

    private void OnCollisionEnter(Collision collision)
    {
        if (rState == RacoonState.charging)
        {
            if (collision.gameObject.CompareTag("Player"))
                collision.gameObject.GetComponent<RacoonBehaviour>().ChangeState((int)RacoonState.dead);

            if (collision.gameObject.CompareTag("Bounds"))
                ChargedTransitions();
        }
    }
}