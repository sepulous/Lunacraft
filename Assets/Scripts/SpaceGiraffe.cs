using UnityEngine;

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
    private float distanceToWalk = 0;
    private Vector3 initialPosition;

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
        float currentTime = Time.realtimeSinceStartup;

        if (timeDamaged != 0 && Time.time - timeDamaged > 0.15F)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                renderer.material.color = new Color(1, 1, 1, 1);
            timeDamaged = 0;
        }

        if (health > 0)
        {
            if (!(walking || rotating))
            {
                if (currentTime - lastActionTime > actionDelay)
                {
                    int action = UnityEngine.Random.Range(1, 5);
                    if (action < 4) // Walk
                    {
                        // TODO: Sometimes they jump right before walking
                        walking = true;
                        distanceToWalk = UnityEngine.Random.Range(3, 7);
                        initialPosition = transform.position;
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
                float distanceWalked = (transform.position - initialPosition).magnitude;
                if (distanceWalked >= distanceToWalk)
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

                    rigidbody.linearVelocity = -1F * (new Vector3(transform.forward.x, 0, transform.forward.z));
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
                    transform.RotateAround(transform.position + 1.8F*Vector3.down, transform.forward, 90 * Time.deltaTime);
                else
                    deathAnimationFinished = true;
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
                rigidbody.linearVelocity = Vector3.zero;
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
