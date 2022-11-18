using System.Collections.Generic;
using UnityEngine;

public class RacoonBehaviour : MonoBehaviour
{
    enum RacoonState
    {
        idle,
        walking,
        buffed,
    }

    private RacoonState rState;
    public float walkSpeed = 5;

    [Header("Running")]
    public bool canRun = true;
    public float buffSpeed = 9;
    public KeyCode runningKey = KeyCode.LeftShift;

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
        /*
        if (canRun && Input.GetKey(runningKey))
            ChangeState((int)RacoonState.buffed);
        */

        float targetMovingSpeed = rState==RacoonState.buffed ? buffSpeed : walkSpeed;

        if (speedOverrides.Count > 0)
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();

        // Get targetVelocity from input.
        Vector2 targetVelocity = new Vector2( Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

        // Apply movement.
        rigidbody.velocity = new Vector3(targetVelocity.x, 0, targetVelocity.y);

        //Apply rotation.
        if (rigidbody.velocity.magnitude > 0)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(rigidbody.velocity), Time.deltaTime * 10f);
            if (rState != RacoonState.buffed) ChangeState((int)RacoonState.walking);
        }
        else ChangeState((int)RacoonState.idle);
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
                anim.Play("Running");
                break;
        }
        anim.SetInteger("rRacoonAnim", (int)rState);
    }
}