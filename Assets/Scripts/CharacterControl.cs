using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class CharacterControl : MonoBehaviour
{
    #region Variables

    [SerializeField]
    [Tooltip("Reference to this player")]
    private GameObject Player;

    [SerializeField]
    [Tooltip("Reference to this player")]
    private float fallDistance = -15.0f;

    [SerializeField]
    [Tooltip("Reference to characters animator(michelle)")]
    private Animator characterAnimator;

    [Header("Ground Detetcion")]
    [SerializeField]
    [Tooltip("This is the reference to the transform of the gameobject that is at the feet of the character, a sphere with a certain radius(Ground Radius) is drawn with this transform at it's center")]
    private Transform groundCheck;

    private bool isGrounded;

    [SerializeField]
    [Tooltip("A Sphere with this radius is drawn at the feet of the character, and if the sphere touches or intersects the ground layer the character is Considered to be grounded")]
    private float groundRadius = 0.3f;

    [SerializeField]
    [Tooltip("Select the layer which is supposed to be Ground")]
    private LayerMask whatIsGround;

    [Header("Swing Mechaninc")]
    [SerializeField]
    [Tooltip("Transform from which the hook is shot")]
    private Transform hookOrigin;

    [SerializeField]
    [Tooltip("This is the layer which are considered to be hooks. All hooks need to have this layer for them to work as hook points")]
    private LayerMask layerToHook;

    [SerializeField]
    [Tooltip("List of references to each Hooked script attached to each hook point in the level")]
    public List<Hooked> HookPoints;

    [Tooltip("Upper limit of the distance range over which the spring will not apply any force.")]
    [SerializeField]
    private float maxHookDistance = 0.8f;

    [Tooltip("Lower limit of the distance range over which the spring will not apply any force.")]
    [SerializeField]
    private float minHookDistance = 0.25f;

    [Tooltip("Strength of the spring.")]
    [SerializeField]
    private float spring = 4.5f;

    [Tooltip("Amount that the spring is reduced when active.")]
    [SerializeField]
    private float damper = 7f;

    [Tooltip("The scale to apply to the inverse mass and inertia tensor of the body prior to solving the constraints.")]
    [SerializeField]
    private float massScale = 4.5f;

    [SerializeField]
    private LineRenderer lr;

    private GameObject hook;

    [Header("Player Movement")]
    [Tooltip("Minimum amount character is allowed to move in x-axis")]
    [SerializeField]
    private float xMin = -6.5f;

    [SerializeField]
    [Tooltip("Maximum amount character is allowed to move in x-axis")]
    private float xMax = 6.5f;

    [Tooltip("How fast the character moves sideways (higher value makes it move faster)")]
    [SerializeField]
    private float sideMovementMultiplier = 0.08f;

    [Tooltip("The speed with which character run forward")]
    [SerializeField]
    private float runSpeed = 5f;

    [Tooltip("Reference to text on screen")]
    [SerializeField]
    private SpringJoint joint;

    [SerializeField]
    private TextMeshProUGUI text;

    [SerializeField]
    private GameObject LevelWonPanel;

    private CharacterControls characterControls;

    private bool Dragging = false, falling = false, startSwing = false, land = false;

    private Vector2 currentPos = Vector2.zero, lastPos = Vector2.zero;

    public Rigidbody rb;

    private Collider[] col;

    private Bounds bound;

    private Vector3 newPos;

    private GameObject platform;

    private Scene scene;

    private bool gameStarted = false, gameWon = false;

    #endregion Variables

    #region Unity_Events

    private void Awake()
    {
        scene = SceneManager.GetActiveScene();
        characterControls = new CharacterControls();
        if (Player == null)
        {
            Player = GameObject.FindGameObjectWithTag("Player");
        }
        rb = Player.GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (HookPoints == null || HookPoints.Count == 0)
        {
            GameObject[] hooks = GameObject.FindGameObjectsWithTag("Hook");
            for (int i = 0; i < hooks.Length; i++)
            {
                HookPoints.Add(hooks[i].GetComponent<Hooked>());
            }
        }
        gameStarted = false;
    }

    private void OnEnable()
    {
        characterControls.Character.Enable();
        characterControls.Character.PrimaryContact.performed += StartDrag;
        characterControls.Character.PrimaryContact.canceled += StopDrag;
        characterControls.Character.Hook.performed += StartSwing;
        // characterControls.Character.Hook.canceled += StopSwing;
    }

    private void OnDisable()
    {
        characterControls.Character.Disable();
        characterControls.Character.PrimaryContact.performed -= StartDrag;
        characterControls.Character.PrimaryContact.canceled -= StopDrag;
        characterControls.Character.Hook.performed -= StartSwing;
        // characterControls.Character.Hook.canceled -= StopSwing;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(groundCheck.position, groundRadius);
    }

    // Update is called once per frame
    private void Update()
    {
        if (gameStarted)
        {
            text.text = "";

            isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, (int)whatIsGround);
            if (isGrounded)
            {
                Platform();
            }
            if (rb.position.y < fallDistance)
            {
                ResetLevel();
            }
            else if (rb.position.y < -1f)
            {
                falling = true;
            }

            AnimationController();
        }
        else
        {
#if UNITY_ANDROID
            text.text = "Tap and hold to start";
#elif UNITY_EDITOR
            text.text = " Click and hold to start";
#else
            text.text = " Click and hold to start";
#endif
        }
    }

    private void FixedUpdate()
    {
        if (gameStarted)
        {
            if (Dragging)
            {
                DragMechanic();
                RigidbodySidewaysMove();
            }
            if (isGrounded)
            {
                rb.MovePosition(rb.position + transform.forward * runSpeed * Time.deltaTime);
            }
        }
    }

    private void LateUpdate()
    {
        if (gameStarted)
        {
            DrawRope();
            AvoidSidefall();
            AnimationController();
        }
    }

    #endregion Unity_Events

    #region DragMechanics

    private void StartDrag(InputAction.CallbackContext context)
    {
        if (gameStarted)
        {
            Dragging = true;
        }
        else if (!gameWon)
        {
            gameStarted = true;
        }
    }

    private void StopDrag(InputAction.CallbackContext context)
    {
        Dragging = false;
        lastPos = Vector2.zero;
    }

    private void StartSwing(InputAction.CallbackContext context)
    {
        if (!startSwing)
        {
            for (int i = 0; i < HookPoints.Count; i++)
            {
                if (HookPoints[i].inRange && !HookPoints[i].hooked)
                {
                    if (PlayerPrefs.GetInt("Played", 0) == 0)
                    {
                        PlayerPrefs.SetInt("Played", 1);
                    }
                    hook = HookPoints[i].gameObject;

                    float distance = Vector3.Distance(hookOrigin.position, hook.transform.position);

                    joint = Player.AddComponent<SpringJoint>();
                    joint.autoConfigureConnectedAnchor = false;
                    //joint.connectedBody = hook.GetComponent<Rigidbody>();
                    hook.GetComponent<Hooked>().OnHooked();
                    joint.connectedAnchor = hook.transform.position;

                    joint.maxDistance = distance * maxHookDistance;
                    joint.minDistance = distance * minHookDistance;

                    joint.spring = spring;
                    joint.damper = damper;
                    joint.massScale = massScale;

                    lr.positionCount = 2;
                    startSwing = true;
                    land = false;
                    break;
                }
            }
        }
        else
        {
            lr.positionCount = 0;
            Destroy(joint);
            startSwing = false;
            land = true;
        }
    }

    private void StopSwing(InputAction.CallbackContext context)
    {
        lr.positionCount = 0;
        Destroy(joint);
        startSwing = false;
        land = true;
    }

    private void DrawRope()
    {
        if (joint && hook)
        {
            lr.SetPosition(0, hookOrigin.position);
            lr.SetPosition(1, hook.transform.position);
        }
        else
        {
            return;
        }
    }

    private void RigidbodySidewaysMove()
    {
        rb.MovePosition(newPos);
    }

    private void DragMechanic()
    {
        if (!isGrounded)
        {
            currentPos = characterControls.Character.PrimaryPosition.ReadValue<Vector2>();
            float xDiff = 0;
            if (lastPos != Vector2.zero)
            {
                xDiff = (currentPos.x - lastPos.x) * sideMovementMultiplier;
            }
            newPos = new Vector3(Mathf.Clamp(rb.position.x + xDiff, xMin, xMax), rb.position.y, rb.position.z);

            lastPos = currentPos;
        }
        else
        {
            currentPos = characterControls.Character.PrimaryPosition.ReadValue<Vector2>();
            float xDiff = 0;
            if (lastPos != Vector2.zero)
            {
                xDiff = (currentPos.x - lastPos.x) * sideMovementMultiplier;
            }
            newPos = new Vector3(Mathf.Clamp(rb.position.x + xDiff, -bound.extents.x, bound.extents.x), rb.position.y, rb.position.z);

            lastPos = currentPos;
        }
    }

    #endregion DragMechanics

    private void Platform()
    {
        col = Physics.OverlapSphere(groundCheck.position, groundRadius, whatIsGround);
        platform = col[0].gameObject;
        bound = platform.GetComponent<Renderer>().bounds;
    }

    private void AvoidSidefall()
    {
        rb.position = new Vector3(Mathf.Clamp(rb.position.x, xMin, xMax), rb.position.y, rb.position.z);
    }

    #region Animations

    private void AnimationController()
    {
        if (!falling && gameStarted)
        {
            if (isGrounded)
            {
                characterAnimator.SetBool("Run", true);
                characterAnimator.SetBool("Land", false);
            }
            else
            {
                characterAnimator.SetBool("Run", false);
            }
            if (startSwing)
            {
                characterAnimator.SetBool("StartSwing", true);
                characterAnimator.SetBool("Land", false);
            }
            else if (land)
            {
                characterAnimator.SetBool("Land", true);
                characterAnimator.SetBool("StartSwing", false);
            }
            if (falling)
            {
                characterAnimator.SetBool("Fall", true);
            }
        }
    }

    #endregion Animations

    public void LevelWon()
    {
        runSpeed = 4;
        LevelWonPanel.SetActive(true);
        gameWon = true;
        StartCoroutine(levelWon());
    }

    public void ResetLevel()
    {
        SceneManager.LoadScene(scene.name);
    }

    private IEnumerator levelWon()
    {
        characterAnimator.SetBool("Run", false);
        yield return new WaitForSeconds(0.5f);
        gameStarted = false;
    }
}