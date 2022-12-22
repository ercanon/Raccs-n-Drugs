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
    public bool haschanged;
    private RacoonState rState;
    public float walkSpeed = 5;
    public float buffSpeed = 8;
    public float rotateSpeed = 3.5f;
    public bool quiet = true;
    public int charges = 4;
    [HideInInspector]
    public bool owned = false;

    private Rigidbody rBody;
    private Animator anim;

    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        ChangeState((int)RacoonState.onPause);
        
        rBody = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        if (owned && rState != RacoonState.dead && rState != RacoonState.onPause)
        {
            if (rState != RacoonState.charging)
            {
                if (rState != RacoonState.buffed)
                {
                    
                    quiet = true;
                    charges = 4;
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
                }
                else
                {
                    transform.Rotate(0, Input.GetAxis("Horizontal") * rotateSpeed, 0);
                    if (Input.GetKeyDown("space"))
                    {                        
                        ChangeState((int)RacoonState.charging);
                        
                    }
                    if (charges == 0)
                    {
                        rState = RacoonState.walking;
                    }
                }
            }
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

                rState = RacoonState.buffed;
                anim.Play("Idle Buff");
                break;

            case 4: //Charging
                if (rState == RacoonState.charging)
                    return;

                rBody.velocity = transform.forward * buffSpeed;
                charges = charges - 1;

                rState = RacoonState.buffed;
                break;

            case 5: //Dead
                if (rState == RacoonState.dead)
                    return;

                rState = RacoonState.dead;
                break;

            default:
                break;
        }
        anim.SetInteger("rRacoonAnim", (int)rState);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && rState == RacoonState.buffed)
            collision.gameObject.GetComponent<RacoonBehaviour>().ChangeState((int)RacoonState.dead);
    }
}