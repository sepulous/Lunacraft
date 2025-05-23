using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    public GameObject playerCamera;
    public float walkSpeed = 4F;
    public float lookSpeed = 1.0F;
    public float jumpSpeed = 1.0F;
    public float gravity = 1.62F;

    public GameObject inventoryUI;
    private Player player;
    private PauseMenu pauseMenu;
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float cameraRotationX = 0;
    private float playerRotationY = 0;
    private float fuelDepletedTime = 0;
    private float timeStartedFlying = -1;
    private float fallTime = 0;
    private bool isGrounded = true;
    private bool bobbing = false;
    public Animator bobAnimator;

    // Audio
    private AudioSource jetpackSource;
    public AudioClip jetpackClip;
    private AudioSource jumpSource;
    public AudioClip jumpClip;
    private AudioSource landSource;
    public AudioClip landClip;

    void Awake()
    {
        float sfxVolume = OptionsManager.GetCurrentOptions().sfxVolume;
        jetpackSource = gameObject.AddComponent<AudioSource>();
        jetpackSource.volume = sfxVolume;
        jetpackSource.clip = jetpackClip;
        jetpackSource.loop = true;
        jumpSource = gameObject.AddComponent<AudioSource>();
        jumpSource.volume = sfxVolume;
        jumpSource.clip = jumpClip;
        landSource = gameObject.AddComponent<AudioSource>();
        landSource.volume = sfxVolume;
        landSource.clip = landClip;
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        pauseMenu = GameObject.Find("Pause Menu").GetComponent<PauseMenu>();
        player = GetComponent<Player>();

        lookSpeed = OptionsManager.GetCurrentOptions().sensitivity;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!player.IsDead())
        {
            // Update SFX volume
            float sfxVolume = OptionsManager.GetCurrentOptions().sfxVolume;
            jetpackSource.volume = sfxVolume;
            jumpSource.volume = sfxVolume;
            landSource.volume = sfxVolume;

            // Update mouse sensitivity
            Options options = OptionsManager.GetCurrentOptions();
            lookSpeed = options.sensitivity;

            // Ice stuff
            /*

            startedMovingOnIce += Time.deltaTime;
            walkSpeed = f(walkSpeed, startedMovingOnIce)

            determine inertia vector and add it to moveDirection

            update inertia vector by dividing it each frame until it's ~0, then clamp

            if not on ice, cancel inertia and reset walk speed

            */

            // Update movement direction
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            float mouseSpeedFB = walkSpeed * Input.GetAxis("Vertical");
            float mouseSpeedLR = walkSpeed * Input.GetAxis("Horizontal");
            float movementDirectionY = moveDirection.y;
            moveDirection = (forward * mouseSpeedFB) + (right * mouseSpeedLR) + moveDirection.y * Vector3.up;

            bool isPaused = pauseMenu.IsPaused();
            bool inventoryOpen = inventoryUI.activeSelf;
            bool isNowGrounded = (Physics.OverlapBox(transform.position - new Vector3(0, 1F, 0), new Vector3(0.4F, 0.1F, 0.4F), Quaternion.identity, LayerMask.GetMask("Block"))).Length > 0;
            if (!isGrounded && isNowGrounded)
            {
                if (fallTime >= 0.5F) // 0.5 seconds ~ 3 blocks
                    landSource.Play();
                fallTime = 0;
            }
            else if (!isNowGrounded)
            {
                fallTime += Time.deltaTime;
            }
            isGrounded = isNowGrounded;

            // Jump/fly
            float jetpackFuel = player.GetJetpackFuel();
            int jetpackLevel = player.GetJetpackLevel();
            bool flying = false;
            if (!(isPaused || inventoryOpen))
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (isGrounded)
                    {
                        moveDirection.y = jumpSpeed;
                        jumpSource.Play();
                    }
                }
                else if (Input.GetKey(KeyCode.Space))
                {
                    if (timeStartedFlying == -1)
                        timeStartedFlying = Time.time;

                    if (jetpackLevel > 0 && jetpackFuel > 0 && Time.time - timeStartedFlying > 0.2F)
                    {
                        flying = true;
                        moveDirection.y = 0.5F * jetpackLevel * jumpSpeed;
                        float fuelLossRate = 0.8F / jetpackLevel;
                        player.SetJetpackFuel(jetpackFuel - fuelLossRate * Time.deltaTime);
                        if (!jetpackSource.isPlaying)
                            jetpackSource.Play();
                    }
                    else
                    {
                        jetpackSource.Stop();
                    }

                    if (jetpackFuel < 0.01F)
                        fuelDepletedTime = Time.time;
                }
                else
                {
                    jetpackSource.Stop();

                    timeStartedFlying = -1;
                    if (Time.time - fuelDepletedTime > 0.5F)
                    {
                        float fuelGainRate = 0.1F * jetpackLevel;
                        player.SetJetpackFuel(jetpackFuel + fuelGainRate * Time.deltaTime);
                    }
                }
            }

            // Apply movement
            if (!isPaused)
            {
                // Gravity
                if (!isGrounded && !flying)
                {
                    moveDirection.y -= gravity * Time.deltaTime;
                }

                // Allow player to fall, even if in inventory
                Vector3 verticalMoveDirection = new Vector3(0, moveDirection.y, 0);
                characterController.Move(Time.deltaTime * verticalMoveDirection);

                if (!inventoryOpen)
                {
                    // Move
                    if (moveDirection != Vector3.zero)
                    {
                        Vector3 horizontalMoveDirection = moveDirection - verticalMoveDirection;
                        characterController.Move(Time.deltaTime * horizontalMoveDirection);
                        if (isGrounded && horizontalMoveDirection.magnitude > 0)
                        {
                            if (!bobbing)
                            {
                                bobbing = true;
                                bobAnimator.SetBool("IsMoving", true);
                            }
                        }
                        else if (bobbing)
                        {
                            bobbing = false;
                            bobAnimator.SetBool("IsMoving", false);
                        }
                    }

                    // Rotate
                    cameraRotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
                    cameraRotationX = Mathf.Clamp(cameraRotationX, -90, 90);
                    playerRotationY += Input.GetAxis("Mouse X") * lookSpeed;
                    playerCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0, 0);
                    transform.localRotation = Quaternion.Euler(0, playerRotationY, 0);
                }
                else
                {
                    bobbing = false;
                    bobAnimator.SetBool("IsMoving", false);
                }
            }
            else if (bobbing)
            {
                bobbing = false;
                bobAnimator.SetBool("IsMoving", false);
            }
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
