using UnityEngine;
using UnityEngine.SceneManagement;

public class SpaceGiraffe : MonoBehaviour
{
    private Animator[] animators;
    private Rigidbody rigidbody;

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
                            // TODO: Sometimes they jump right before walking
                            walking = true;
                            timeToWalk = UnityEngine.Random.Range(4, 9);
                            timeStartedWalking = Time.time;
                        }
                        else // Rotate
                        {
                            int direction = UnityEngine.Random.Range(0, 2);
                            if (direction == 0)
                                targetRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + 45, 0);
                            else
                                targetRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y - 45, 0);

                            // Reset state for rotation calculations
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

                if (walking)
                {
                    float timeWalked = Time.time - timeStartedWalking;
                    if (timeWalked >= timeToWalk)
                    {
                        foreach (var animator in animators)
                            animator.SetBool("Walking", false);

                        walking = false;
                        rigidbody.linearVelocity = Vector3.zero;
                    }
                    else
                    {
                        foreach (var animator in animators)
                            animator.SetBool("Walking", true);

                        rigidbody.linearVelocity = new Vector3(-transform.forward.x, rigidbody.linearVelocity.y, -transform.forward.z);

                        // Jump over obstacles
                        Collider[] obstacles = Physics.OverlapSphere(transform.position - (new Vector3(0, -0.2F, 1.6F*transform.forward.z)), 0.9F, LayerMask.GetMask("Block"));
                        if (obstacles.Length > 0)
                        {
                            Vector3 force = 2F * (-transform.forward + transform.up);
                            rigidbody.AddForce(force, ForceMode.Impulse);
                        }
                    }
                }

                if (rotating)
                {
                    rotationTimeElapsed += Time.deltaTime;
                    float t = rotationTimeElapsed / rotationDuration;
                    transform.rotation = Quaternion.Lerp(initialRotation, targetRotation, t);
                    if (t > 0.99F)
                        rotating = false;
                }
            }
            else
            {
                if (!deathAnimationFinished)
                {
                    // Rotate
                    rigidbody.constraints = RigidbodyConstraints.None;
                    if (transform.eulerAngles.z - 90 < 0.5F)
                    {
                        transform.RotateAround(transform.position + 1.5F*Vector3.down, transform.forward, 90 * Time.deltaTime);
                    }
                    else
                    {
                        deathAnimationFinished = true;
                        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                    }
                }
                else if (Time.time - timeDied > 3)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (shouldJump)
        {
            Vector3 force = 2F * (-transform.forward + 2*transform.up);
            rigidbody.AddForce(force, ForceMode.Impulse);
            shouldJump = false;
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
                rigidbody.linearVelocity = Vector3.zero;
                walking = false;
                rotating = false;
                foreach (var animator in animators)
                    animator.SetBool("Walking", false);
                timeDied = Time.time;
            }

            timeDamaged = Time.time;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                renderer.material.color = new Color(1, 0.6F, 0.6F, 1);
        }
    }
}
