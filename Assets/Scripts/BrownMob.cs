using UnityEngine;
using UnityEngine.SceneManagement;

class BrownMob : MonoBehaviour
{
    private GameObject playerObj = null;
    private Rigidbody rigidbody;
    private BoxCollider collider;
    private float lastActionTime = 0;
    private int nextActionDelay = 0;
    private bool aboutToAct = false;
    public bool aggressive = false;
    private AudioSource explodeSource;
    private bool isGrounded = false;

    private int health = 5;
    private bool jumpingForward = false;
    private bool hopping = false;
    private float lastJumpTime = 0;
    private bool rotating = false;
    private float rotationDuration = 0.5F;
    private float rotationTimeElapsed = 0;
    private float timeDamaged = 0;
    private Quaternion initialRotation;
    private Quaternion targetRotation;

    void Awake()
    {
        collider = gameObject.AddComponent<BoxCollider>();
        collider.material.staticFriction = 0;
        collider.material.dynamicFriction = 0;
        rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.mass = 0.5F;
        rigidbody.angularDamping = 0;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            if (playerObj == null)
                playerObj = GameObject.Find("Player");

            float currentTime = Time.time;

            isGrounded = Physics.OverlapBox(transform.position - new Vector3(0, 0.25F, 0), new Vector3(0.5F, 0.01F, 0.5F), Quaternion.identity, LayerMask.GetMask("Block")).Length > 0;

            if (timeDamaged != 0 && Time.time - timeDamaged > 0.15F)
            {
                GetComponent<Renderer>().material.color = Color.white;
                timeDamaged = 0;
            }

            if (aggressive)
            {
                // transform.LookAt(playerObj.transform);
                // rigidbody.linearVelocity = new Vector3(
                //     6F * (playerObj.transform.position - transform.position).normalized.x,
                //     rigidbody.linearVelocity.y,
                //     6F * (playerObj.transform.position - transform.position).normalized.z
                // );
                Vector3 displacement = (playerObj.transform.position - transform.position);
                transform.rotation = Quaternion.Euler(0, Mathf.Rad2Deg*Mathf.Atan2(displacement.x, displacement.z) + 180, 0);
                Vector3 forceDirection = (new Vector3(displacement.x, 0, displacement.z)).normalized;
                rigidbody.AddForce(5F * forceDirection);
            }
            else
            {
                if (!aboutToAct && isGrounded)
                {
                    nextActionDelay = UnityEngine.Random.Range(1, 3); // Wait 1-2 seconds before next action
                    aboutToAct = true;
                }
                
                if (!(rotating || jumpingForward || hopping) && isGrounded)
                {
                    if (currentTime - lastActionTime > nextActionDelay)
                    {
                        int action = UnityEngine.Random.Range(0, 4);
                        if (action == 0) // Rotate
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
                        else if (action == 1) // Hop
                        {
                            hopping = true;
                        }
                        else // Jump forward
                        {
                            lastJumpTime = currentTime;
                            jumpingForward = true;
                            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
                        }
                        lastActionTime = currentTime;
                        aboutToAct = false;
                    }
                }
                else
                {
                    lastActionTime += Time.deltaTime; // This ensures that the delay only starts after the mob is done acting
                }

                if (rotating)
                {
                    rotationTimeElapsed += Time.deltaTime;
                    float t = rotationTimeElapsed / rotationDuration;
                    transform.rotation = Quaternion.Lerp(initialRotation, targetRotation, t);
                    if (t > 0.99F)
                        rotating = false;
                }

                if (jumpingForward && currentTime - lastJumpTime > 4)
                {
                    jumpingForward = false;
                    rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (hopping)
        {
            Vector3 force = 2F * transform.up;
            rigidbody.AddForce(force, ForceMode.Impulse);
            hopping = false;
        }
        else if (jumpingForward)
        {
            Vector3 force = 2F * (-transform.forward + 3*transform.up);
            rigidbody.AddForce(force, ForceMode.Impulse);
            jumpingForward = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Slug"))
        {
            aggressive = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            health--;
            Destroy(collision.gameObject);

            if (health == 0)
            {
                // TODO: Falling over animation
                Destroy(gameObject);
            }
            else
            {
                timeDamaged = Time.time;
                GetComponent<Renderer>().material.color = new Color(1, 0.5F, 0.5F, 1);
            }
        }
        else if (aggressive && collision.gameObject.name == "Player")
        {
            Player playerScript = collision.gameObject.GetComponent<Player>();
            playerScript.Damage(25);
            GameObject.Find("ChunkManager").GetComponent<ChunkManager>().BrownMobExplode(transform.position);
            Destroy(gameObject);
        }
    }
}
