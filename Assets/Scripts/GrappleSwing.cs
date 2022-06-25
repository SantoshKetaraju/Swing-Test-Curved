using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrappleSwing : MonoBehaviour
{
    private CharacterControls characterControls;

    [SerializeField]
    private LineRenderer lr;

    [SerializeField]
    private Vector3 swingPoint;

    [SerializeField]
    private LayerMask whereToHook;

    // Start is called before the first frame update
    private void Awake()
    {
        characterControls = new CharacterControls();
        lr = GetComponent<LineRenderer>();
    }

    private void OnEnable()
    {
        characterControls.Character.Enable();
        characterControls.Character.Hook.performed += StartSwing;
    }

    private void OnDisable()
    {
        characterControls.Character.Disable();
        characterControls.Character.Hook.performed -= StopSwing;
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void StartSwing(InputAction.CallbackContext context)
    {
    }

    private void StopSwing(InputAction.CallbackContext context)
    {
    }
}