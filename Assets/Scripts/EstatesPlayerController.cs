using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EstatesPlayerController : MonoBehaviour
{
    public GameObject head;
    public float acceleration = 15;
    public float deceleration = 5;
    public float maxSpeed = 5;
    public float maxSlopeAngle = 35;
    public bool canPick = false;
    public float maxPickDist = 2;
    [Range(0.01f, 1)]
    public float sensitivity;

    public bool isOverUI = false;
    public bool inHistoryMode = false;
    public bool inHazardChecklist = false;

    Vector2 moveInputVector;
    Vector2 lookInputVector;
    Vector3 cameraForward;
    Vector3 cameraRight;
    Vector3 moveVector;

    public CinemachineVirtualCamera vCam;

    float moveThresh = 0.01f;
    bool grounded;
    int magnificationSetting = 0;
    public bool inBinoc = false;
    public float defaultFL, zoomFL, zoomPower, zoomBoost;
    public float ZoomFOV;

    Rigidbody rb;

    Transform heldObject;
    Rigidbody targetRB;

  

    //UI buttons
    [SerializeField]
    GameObject MapButton;


    [SerializeField]
    PlayerInput playerInput;
    InputActionMap actionMap;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>() != null ? GetComponent<Rigidbody>() : new Rigidbody();
        defaultFL = 1 / Mathf.Tan(30 * Mathf.Deg2Rad);
        zoomFL = 1 / Mathf.Tan(15 * Mathf.Deg2Rad);
        zoomPower = zoomFL / defaultFL;
        //zoomPower *= zoomBoost;
        actionMap = playerInput.currentActionMap;
        foreach (InputAction input in actionMap.actions)
        {

            Debug.Log("action: " + input.name);
        }

    }

    private void Start()
    {
        vCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
        vCam.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_MaxSpeed = sensitivity;
        vCam.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_MaxSpeed = sensitivity;

       

        

        

    }

    public void StopCameraMovement(Vector2 cameraValue)
    {
        // Debug.Log("old camera value is: " + cameraValue);
        cameraValue = new Vector2(0f, 0f);
        // Debug.Log("new camera value is: " + cameraValue);
    }

    void OnLook(InputValue inputValue)
    {
        /*if (!Mouse.current.rightButton.isPressed)
        {

            actionMap.actions[1].ApplyBindingOverride(new InputBinding { overrideProcessors = "ScaleVector2(X=0,Y=0)" });


            //Quaternion cameraRot = GameObject.Find("Main Camera").transform.rotation;
            //cameraRot.eulerAngles = new Vector3(0f, 0f, 0f);
        }
        else
        {
            actionMap.actions[1].ApplyBindingOverride(new InputBinding { overrideProcessors = "ScaleVector2(X=0.5,Y=0.5)" });
        }*/
    }

    public void ChangeMagnification()
    {
        
    }
    void OnMove(InputValue inputValue)
    {
        

           
            moveInputVector = inputValue.Get<Vector2>();
            
        




    }

    void OnEscape()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    void OnFire()
    {
        
        



    }

    void OnBinoculars()
    {

    }

    public void ChangeZoomIntensity(float zoomValue)
    {
        vCam.m_Lens.FieldOfView = zoomValue;
        //ZoomFOV = zoomValue;
    }



    Vector3 MoveVectorCameraProjection()
    {
        
            cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            cameraRight = Camera.main.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();
            return cameraForward * moveInputVector.y + cameraRight * moveInputVector.x;
        


    }

    void HoldObject()
    {
        Ray cameraCenter = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, maxPickDist));
        RaycastHit hit;
        if (Physics.SphereCast(cameraCenter, 0.1f, out hit, maxPickDist))
        {
            if (hit.transform.gameObject.tag != "Holdable")
            {
                DropObject();
                return;
            }
            targetRB = hit.transform.gameObject.GetComponent<Rigidbody>();
            if (targetRB != null)
            {
                targetRB.velocity = Vector3.zero;
                targetRB.useGravity = false;
                targetRB.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                targetRB.freezeRotation = true;
                if (heldObject == null)
                {
                    heldObject = hit.transform;
                    return;
                }
            }
        }
        DropObject();
    }

    void DropObject()
    {
        if (heldObject != null)
        {
            targetRB.useGravity = true;
            targetRB.collisionDetectionMode = CollisionDetectionMode.Continuous;
            targetRB.freezeRotation = false;
            heldObject = null;
            targetRB = null;
        }
    }

    void UpdateHeldPosition()
    {
        Vector3 objectDistance = targetRB.position - rb.position;
        if (Mathf.Abs(objectDistance.magnitude) > 3)
        {
            HoldObject();
            return;
        }
        Ray cameraCenter = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, maxPickDist));
        Vector3 cameraRayEnd = cameraCenter.origin + (cameraCenter.direction * maxPickDist);
        Vector3 targetToHoldPoint = cameraRayEnd - targetRB.position;
        targetRB.velocity = targetToHoldPoint * 1000 * Time.deltaTime;
    }

    void MovePlayer()
    {
        rb.AddForce(Vector3.down * Time.deltaTime * 10);
        DeceleratePlayer();
        if (moveVector != Vector3.zero)
        {
            rb.AddForce(cameraForward * moveInputVector.y * acceleration * Time.deltaTime);
            rb.AddForce(cameraRight * moveInputVector.x * acceleration * Time.deltaTime);
        }
        Vector2 velocity = new Vector2(rb.velocity.x, rb.velocity.z);
        if (velocity.magnitude > maxSpeed) rb.velocity = rb.velocity.normalized * maxSpeed;
    }

    Vector2 ViewRelativeVelocity(float speed)
    {
        float viewDir = Camera.main.transform.eulerAngles.y;
        float moveDir = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;
        float u = Mathf.DeltaAngle(viewDir, moveDir);
        float v = 90 - u;
        float yVel = speed * Mathf.Cos(u * Mathf.Deg2Rad);
        float xVel = speed * Mathf.Cos(v * Mathf.Deg2Rad);
        return new Vector2(xVel, yVel);
    }

    void DeceleratePlayer()
    {
        if (!grounded) return;
        Vector3 inverseVelocity = -Camera.main.transform.InverseTransformDirection(rb.velocity);
        if (moveInputVector.x == 0)
        {
            rb.AddForce(inverseVelocity.x * cameraRight.normalized * acceleration * deceleration * Time.deltaTime);
        }
        if (moveInputVector.y == 0)
        {
            rb.AddForce(inverseVelocity.z * cameraForward.normalized * acceleration * deceleration * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
        if (heldObject != null && targetRB != null) UpdateHeldPosition();
    }

    private void Update()
    {

        moveVector = MoveVectorCameraProjection();

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        if (!Mouse.current.rightButton.isPressed)
        {
            vCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
            vCam.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_MaxSpeed = 0;
            vCam.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_MaxSpeed = 0;
        }
        else
        {
            vCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
            vCam.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_MaxSpeed = sensitivity;
            vCam.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_MaxSpeed = sensitivity;
        }



        /*if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            //Debug.Log("Pressed space bar");
            try
            {
                Debug.Log("actions: " + playerInput.actions.FindAction("Look").ReadValue<Vector2>());
            }
            catch
            {
                Debug.Log("value was null");
            }
            
        }*/
        //Debug.DrawRay(transform.position,cameraForward);
        //Debug.DrawRay(transform.position,cameraRight);
    }

    private void LateUpdate()
    {
        if (inHistoryMode || inHazardChecklist)
        {
            //Camera.main.transform.position = new Vector3(0f, 0f, 0f);
            //Quaternion camRotation = Camera.main.transform.rotation;
            // Debug.Log("camera y rotation: " + camRotation.eulerAngles.y);
            // camRotation.eulerAngles = new Vector3(0f, 0f, 0f);
            //Debug.Log("camera y rotation after: " + camRotation.eulerAngles.y);

            //Debug.Log(" vcam xy axis: "+GameObject.Find("CM vcam1").GetComponent<CinemachineInputProvider>().XYAxis.action.ToString());
            GameObject.Find("CM vcam1").GetComponent<CinemachineInputProvider>().XYAxis.action.Disable();
            //StopCameraMovement(GameObject.Find("CM vcam1").GetComponent<CinemachineInputProvider>().XYAxis.action.ReadValue<Vector2>());

        }
        else
        {
            if (!GameObject.Find("CM vcam1").GetComponent<CinemachineInputProvider>().XYAxis.action.enabled)
            {
                GameObject.Find("CM vcam1").GetComponent<CinemachineInputProvider>().XYAxis.action.Enable();
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        bool hitGround;
        bool cancelGrounded = true;
        foreach (ContactPoint contact in collision.contacts)
        {
            hitGround = Vector3.Angle(Vector3.up, contact.normal) < maxSlopeAngle ? true : false;
            if (hitGround)
            {
                grounded = true;
                cancelGrounded = false;
                CancelInvoke(nameof(StopGrounded));
            }
        }
        if (cancelGrounded) return;
        float delay = 3f;
        cancelGrounded = true;
        Invoke(nameof(StopGrounded), Time.deltaTime * delay);
    }

    private void StopGrounded()
    {
        grounded = false;
    }
}
