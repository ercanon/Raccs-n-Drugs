﻿using System.Collections.Generic;
using UnityEngine;

public class RaccBehaviour : MonoBehaviour
{
    enum RacoonState { idle, walking, buffed, charging, dead }; private RacoonState raccState;
    
    public float walkSpeed = 5f;
    public float buffSpeed = 8f;
    public float rotateSpeed = 1.5f;
    public int maxCharges = 3;

    private int charges;
    private float timerCharge = 1f;

    [HideInInspector] public bool owned = false;
    [HideInInspector] public Color[] colors;
    private int colorIndex = 0;

    [HideInInspector] public GameplayScript gameplay;
    private Rigidbody rBody;
    private Animator anim;
    private GameObject buffed;
    private Material mat;

    void Awake()
    {
        rBody = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        charges = maxCharges;
        buffed = transform.GetChild(0).gameObject;
        mat = transform.GetChild(1).GetComponent<SkinnedMeshRenderer>().material;
    }

    void Update()
    {
        switch (raccState)
        {
            case RacoonState.idle:
            case RacoonState.walking:
                if (owned)
                {
                    // Get targetVelocity from input.
                    Vector2 targetVelocity = new Vector2(Input.GetAxis("Horizontal") * walkSpeed, Input.GetAxis("Vertical") * walkSpeed);

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
                break;

            case RacoonState.buffed:
                if (owned)
                {
                    transform.Rotate(0f, Input.GetAxis("Horizontal") * rotateSpeed, 0f);
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        gameplay.SendData(5);
                        ChangeState((int)RacoonState.charging);
                    }
                }

                ChangingColors();
                break;

            case RacoonState.charging:
                if (timerCharge < 0f)
                    ChargedTransitions();
                else
                    timerCharge -= Time.deltaTime;
                break;

            case RacoonState.dead:
                break;
        }
    }

    public void ChangeState(int state)
    {
        switch (state)
        {
            case 0: //Idle
                if (raccState == RacoonState.idle)
                    return;
                
                raccState = RacoonState.idle;
                break;
            
            case 1: // Walking
                if (raccState == RacoonState.walking)
                    return;

                raccState = RacoonState.walking;
                break;
            
            case 2: //Buffed
                if (raccState == RacoonState.buffed)
                    return;

                rBody.velocity = Vector3.zero;
                buffed.SetActive(true);
                timerCharge = 1f;
                if (charges <= 0)
                    charges = maxCharges;

                raccState = RacoonState.buffed;
                break;

            case 3: //Charging
                if (raccState == RacoonState.charging)
                    return;

                rBody.velocity = transform.forward * buffSpeed;
                charges--;

                mat.SetColor("_EmissionColor", colors[0]);

                raccState = RacoonState.charging;
                break;

            case 4: //Dead
                if (raccState == RacoonState.dead)
                    return;

                raccState = RacoonState.dead;

                gameplay.CheckEndGame();
                break;

            default:
                break;
        }

        anim.SetInteger("rRacoonAnim", (int)raccState);
    }

    public int GetState()
    {
        return (int)raccState;
    }

    public void IdleEndGame()
    {
        mat.SetColor("_EmissionColor", colors[0]);
        rBody.velocity = Vector3.zero;
        buffed.SetActive(false);
        ChangeState((int)RacoonState.idle);
        transform.SetPositionAndRotation(new Vector3(-0.11f, 0.021f, -0.24f), Quaternion.Euler(new Vector3(0, 180, 0)));
    }

    private void ChargedTransitions()
    {
        if (charges == 0)
        {
            rBody.velocity = Vector3.zero;
            ChangeState((int)RacoonState.idle);
            buffed.SetActive(false);
            gameplay.Invoke("SpawnCocaine", gameplay.timerCocaineSpawn);
        }
        else
            ChangeState((int)RacoonState.buffed);
    }

    private void ChangingColors()
    {
        Color colEmission = mat.GetColor("_EmissionColor");
        if (CompareColors(colEmission, colors[colorIndex]))
        {
            if (colorIndex == 0) colorIndex = 1;
            else colorIndex = 0;
        }
        mat.SetColor("_EmissionColor", Color.Lerp(colEmission, colors[colorIndex], 2f * Time.deltaTime));
    }

    private bool CompareColors(Color colorOne, Color colorTwo)
    {

        if (Mathf.Abs(colorOne.r - colorTwo.r) < 0.02f &&
            Mathf.Abs(colorOne.g - colorTwo.g) < 0.02f &&
            Mathf.Abs(colorOne.b - colorTwo.b) < 0.02f) 
            return true;

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (raccState == RacoonState.charging)
        {
            if (collision.gameObject.CompareTag("Player"))
                collision.gameObject.GetComponent<RaccBehaviour>().ChangeState((int)RacoonState.dead);

            if (collision.gameObject.CompareTag("Bounds"))
                ChargedTransitions();
        }
    }
}