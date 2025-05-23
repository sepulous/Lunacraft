using UnityEngine;
using UnityEngine.SceneManagement;

public class SpaceGiraffe : MonoBehaviour
{
    private Animator[] animators;
    private Rigidbody rigidbody;
    private Player player = null;
    private ChunkManager chunkManager = null;
    private int health = 4;
    private bool deathAnimationFinished = false;
    private float timeDamaged = 0;
    private float timeDied = 0;
    private float lastActionTime = 0;
    private float actionDelay = 4;

    // Walking data
    private bool walking = false;
    private float timeToWalk = 0;
    private float timeStartedWalking;
    private bool shouldJump = false;
    private int jumps = 0;
    private Vector3 lastPosition;
    private float lastPositionCheckTime = 0;
    private float lastObstructionCheckTime = 0;
    private bool animatorsAreActive = false;

    // Rotation data
    private bool rotating = false;
    private float rotationDuration = 0.5F;
    private float rotationTimeElapsed = 0;
    private Quaternion initialRotation;
    private Quaternion targetRotation;

    void Awake()
    {
        animators = GetComponentsInChildren<Animator>();
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rigidbody.centerOfMass = Vector3.zero;
        rigidbody.inertiaTensorRotation = Quaternion.identity;
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            float currentTime = Time.realtimeSinceStartup;

            if (player == null)
                player = GameObject.Find("Player").GetComponent<Player>();

            if (chunkManager == null)
                chunkManager = GameObject.Find("ChunkManager").GetComponent<ChunkManager>();

            if (timeDamaged != 0 && Time.time - timeDamaged > 0.15F)
            {
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                    renderer.material.color = new Color(1, 1, 1, 1);
                timeDamaged = 0;
            }

            if (health > 0)
            {
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

                if (!(walking || rotating))
                {
                    if (currentTime - lastActionTime > actionDelay)
                    {
                        int action = UnityEngine.Random.Range(1, 5);
                        if (action < 4) // Walk
                        {
                            if (player != null && chunkManager != null)
                            {
                                Vector3 playerDisplacement = player.transform.position - transform.position;
                                float distanceFromPlayer = playerDisplacement.magnitude;
                                bool cannotWalkOutOfWorld = Vector3.Angle(playerDisplacement, -transform.forward) < 40;
                                if (cannotWalkOutOfWorld || distanceFromPlayer < Mathf.Sqrt(2) * (32 * (chunkManager.GetRenderDistance() - 1) + 16))
                                {
                                    walking = true;
                                    animatorsAreActive = false;
                                    timeToWalk = UnityEngine.Random.Range(4, 9);
                                    timeStartedWalking = Time.time;
                                }
                            }
                        }
                        else // Rotate
                        {
                            int sign = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1;
                            targetRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + sign * 45, 0);
                            initialRotation = transform.rotation;
                            rotationTimeElapsed = 0;
                            rotating = true;
                        }

                        lastActionTime = currentTime;
                    }
                }
                else
                {
                    lastActionTime += Time.deltaTime;
                }
            }
        }
    }

    private void SetWalkingState(bool state)
    {
        foreach (var animator in animators)
            animator.SetBool("Walking", state);

        walking = state;
    }

    void FixedUpdate()
    {
        // Walking
        if (walking)
        {
            float currentTime = Time.time;
            float timeWalked = currentTime - timeStartedWalking;

            if (currentTime - lastPositionCheckTime > 0.1F && timeWalked > 0.1F)
            {
                float distance = (transform.position - lastPosition).magnitude;
                lastPosition = transform.position;
                lastPositionCheckTime = currentTime;
                if (distance < 0.05F)
                {
                    if (jumps == 0)
                    {
                        shouldJump = true;
                    }
                    else
                    {
                        SetWalkingState(false);

                        // Rotate away
                        int sign = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1;
                        targetRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + sign * 90, 0);
                        initialRotation = transform.rotation;
                        rotationTimeElapsed = 0;
                        rotating = true;

                        return;
                    }
                }
            }

            if (timeWalked >= timeToWalk)
            {
                SetWalkingState(false);
            }
            else
            {
                if (!animatorsAreActive)
                {
                    SetWalkingState(true); // Don't need to do this every call to FixedUpdate(). Ridiculous.
                    animatorsAreActive = true;
                }

                rigidbody.MovePosition(transform.position - 0.8F * transform.forward * Time.fixedDeltaTime);
            }
        }
        else
        {
            jumps = 0;
        }

        // Jumping
        if (shouldJump && jumps == 0)
        {
            Vector3 force = 18F * transform.up;
            rigidbody.AddForce(force, ForceMode.Impulse);
            jumps++;
            shouldJump = false;
        }

        // Rotating
        if (rotating)
        {
            rotationTimeElapsed += Time.fixedDeltaTime;
            float t = rotationTimeElapsed / rotationDuration;
            transform.rotation = Quaternion.Lerp(initialRotation, targetRotation, t);
            if (t > 0.99F)
                rotating = false;
        }

        // Death
        if (health == 0)
        {
            if (!deathAnimationFinished)
            {
                // Rotate
                rigidbody.constraints = RigidbodyConstraints.None;
                if (rigidbody.rotation.eulerAngles.z - 90 < 0.5F)
                {
                    Quaternion deltaRotation = Quaternion.Euler(0, 0, 90 * Time.fixedDeltaTime);
                    rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
                    Vector3 transRight = Vector3.Cross(transform.forward, Vector3.up);
                    rigidbody.MovePosition(rigidbody.position + (-0.3F*Vector3.up + 1F*transRight) * Time.fixedDeltaTime);
                }
                else
                {
                    deathAnimationFinished = true;
                    rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePosition;
                }
            }
            else if (Time.time - timeDied > 3)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Slug"))
        {
            health--;
            Destroy(collision.gameObject);

            if (health == 0)
            {
                Destroy(GetComponent<BoxCollider>());
                rigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
                rigidbody.useGravity = false;
                rotating = false;
                SetWalkingState(false);
                timeDied = Time.time;
            }

            timeDamaged = Time.time;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                renderer.material.color = new Color(1, 0.6F, 0.6F, 1);
        }
    }
}
