using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityToggle : MonoBehaviour
{
    private Rigidbody[] childRigidbodies;
    private CharacterControl CC;

    private void Start()
    {
        childRigidbodies = GetComponentsInChildren<Rigidbody>();
        CC = GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterControl>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            for (int i = 0; i < childRigidbodies.Length; i++)
            {
                childRigidbodies[i].useGravity = true;
            }
            CC.LevelWon();
        }
    }
}