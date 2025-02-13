using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[RequireComponent(typeof(CharacterController))]

public class FPSController : MonoBehaviour
{
    public GameObject playerCamera;
    public float walkSpeed = 1.0F;
    public float lookSpeed = 1.0F;
    public float jumpSpeed = 1.0F;
    public float gravity = 1.62F;

    private PauseMenu pauseMenu;
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float cameraRotationX = 0;
    private float playerRotationY = 0;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        pauseMenu = GameObject.Find("Pause Menu").GetComponent<PauseMenu>();

        Options options = OptionsManager.GetCurrentOptions();
        lookSpeed = options.sensitivity;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Update mouse sensitivity
        Options options = OptionsManager.GetCurrentOptions();
        lookSpeed = options.sensitivity;

        // Update movement direction
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        float mouseSpeedFB = walkSpeed * Input.GetAxis("Vertical");
        float mouseSpeedLR = walkSpeed * Input.GetAxis("Horizontal");
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * mouseSpeedFB) + (right * mouseSpeedLR);

        bool isPaused = pauseMenu.IsPaused();

        // Jump
        if (Input.GetButton("Jump") && !isPaused && characterController.isGrounded)
            moveDirection.y = jumpSpeed;
        else
            moveDirection.y = movementDirectionY;

        if (!isPaused)
        {
            // Gravity
            if (!characterController.isGrounded)
                moveDirection.y -= gravity * Time.deltaTime;

            // Move
            if (moveDirection != Vector3.zero)
                characterController.Move(Time.deltaTime * moveDirection); // should moveDirection be normalized?

            // Rotate
            cameraRotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            cameraRotationX = Mathf.Clamp(cameraRotationX, -90, 90);
            playerRotationY += Input.GetAxis("Mouse X") * lookSpeed;
            playerCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0, 0);
            transform.localRotation = Quaternion.Euler(0, playerRotationY, 0);
        }
    }

    public float GetCameraRotationX()
    {
        return cameraRotationX;
    }

    public void SetCameraRotationX(float val)
    {
        cameraRotationX = val;
    }

    public float GetPlayerRotationY()
    {
        return playerRotationY;
    }

    public void SetPlayerRotationY(float val)
    {
        playerRotationY = val;
    }
}
