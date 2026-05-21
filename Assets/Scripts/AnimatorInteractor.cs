using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorInteractor : MonoBehaviour
{
    [SerializeField] private Animator animator1;
    [SerializeField] private Animator animator2;

    [SerializeField] private Transform capsule1Transform;
    [SerializeField] private Transform capsule2Transform;

    [SerializeField] private Collider collider1;
    [SerializeField] private Collider collider2;
    
    [SerializeField] private float interactionDistance = 0.3f;
    
    private void Update()
    {
        if (collider1.isTrigger && collider2.isTrigger)
        {
            Debug.Log("Collision!2");
            Interact();
        }
        // if (Vector3.Distance(capsule1Transform.position, capsule2Transform.position) <= interactionDistance)
        // {
        //     Interact();
        // }
    }
    private void Interact()
    {
        animator1.SetTrigger("Interact");
        animator2.SetTrigger("Interact");
    }
}
