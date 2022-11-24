using System.Collections.Generic;
using UnityEngine;

public class RacoonBehaviour : MonoBehaviour
{
    enum RacoonState
    {
        idle,
        walking,
        buffed,
        dead
    }

    private RacoonState rState;
    public float walkSpeed = 5;
    public float buffSpeed = 8;

    private Rigidbody rigidbody;
    private Animator anim;

    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        ChangeState((int)RacoonState.idle);

        rigidbody = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        if (rState != RacoonState.dead)
        {
            /*
            if (canRun && Input.GetKey(runningKey))
                ChangeState((int)RacoonState.buffed);
            */

            float targetMovingSpeed = rState == RacoonState.buffed ? buffSpeed : walkSpeed;

            if (speedOverrides.Count > 0)
                targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();

            // Get targetVelocity from input.
            Vector2 targetVelocity = new Vector2(Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

            // Apply movement.
            rigidbody.velocity = new Vector3(targetVelocity.x, 0, targetVelocity.y);

            //Apply rotation.
            if (rigidbody.velocity.magnitude > 0)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(rigidbody.velocity), Time.deltaTime * 10f);
                if (rState != RacoonState.buffed)
                    ChangeState((int)RacoonState.walking);
            }
            else if (rState != RacoonState.buffed)
                ChangeState((int)RacoonState.idle);
        }
    }

    public void ChangeState(int state)
    {
        switch (state)
        {
            case 0: //Idle
                if (rState == RacoonState.idle)
                    return;

                rState = RacoonState.idle;
                anim.Play("Idle");
                break;
            
            case 1: // Walking
                if (rState == RacoonState.walking)
                    return;

                rState = RacoonState.walking;
                anim.Play("Walking");
                break;
            
            case 2: //Buffed
                if (rState == RacoonState.buffed)
                    return;

                rState = RacoonState.buffed;
                anim.Play("Idle Buff");
                break;

            case 3: //Dead
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
        //if (collision.gameObject.CompareTag("Player"))
            //collision.gameObject.GetComponent<RacoonBehaviour>().ChangeState((int)RacoonState.dead);
    }
}