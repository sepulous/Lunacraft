using UnityEngine;
using UnityEngine.SceneManagement;

class BrownMob : MonoBehaviour
{
    private Player player = null;
    private ChunkManager chunkManager = null;
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
    private int rotationDirection = 1;
    private float timeDamaged = 0;

    private float accelTime = 3;
    private float timeMoving = 0;
    private Vector3 chaseVelocity = Vector3.zero;
    private float chaseSpeedBeforeInertia = 0;
    private bool hasInertia = false;
    private bool EMERGENCY = false;

    void Awake()
    {
        collider = gameObject.AddComponent<BoxCollider>();
        rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.mass = 0.5F;
        rigidbody.angularDamping = 0;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            if (player == null)
                player = GameObject.Find("Player").GetComponent<Player>();

            if (chunkManager == null)
                chunkManager = GameObject.Find("ChunkManager").GetComponent<ChunkManager>();

            float currentTime = Time.time;
            isGrounded = Physics.OverlapBox(transform.position - new Vector3(0, 0.25F, 0), new Vector3(0.5F, 0.01F, 0.5F), Quaternion.identity, LayerMask.GetMask("Block")).Length > 0;

            if (timeDamaged != 0 && Time.time - timeDamaged > 0.15F)
            {
                GetComponent<Renderer>().material.color = Color.white;
                timeDamaged = 0;
            }

            if (!aggressive)
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
                            rotationDirection = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1;
                            rotationTimeElapsed = 0;
                            rotating = true;
                        }
                        else if (action == 1) // Hop
                        {
                            hopping = true;
                        }
                        else // Jump forward
                        {
                            if (player != null && chunkManager != null)
                            {
                                Vector3 playerDisplacement = player.transform.position - transform.position;
                                float distanceFromPlayer = playerDisplacement.magnitude;
                                bool cannotJumpOutOfWorld = Vector3.Angle(playerDisplacement, -transform.forward) < 40;
                                if (cannotJumpOutOfWorld || distanceFromPlayer < Mathf.Sqrt(2) * (32 * (chunkManager.GetRenderDistance() - 1) + 16))
                                {
                                    lastJumpTime = currentTime;
                                    jumpingForward = true;
                                    rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
                                }
                            }
                        }
                        lastActionTime = currentTime;
                        aboutToAct = false;
                    }
                }
                else
                {
                    lastActionTime += Time.deltaTime; // This ensures that the delay only starts after the mob is done acting
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
        if (aggressive)
        {
            Vector3 displacement = (player.transform.position - transform.position);
            Vector3 horizontalDisplacement = new Vector3(displacement.x, 0, displacement.z);
            if (horizontalDisplacement.magnitude > 0.1F && !hasInertia)
            {
                Quaternion rotation = Quaternion.Euler(0, Mathf.Rad2Deg*Mathf.Atan2(displacement.x, displacement.z) + 180, 0);
                rigidbody.MoveRotation(rotation);

                timeMoving += Time.fixedDeltaTime;
                float t = timeMoving / accelTime;
                float chaseSpeed = Mathf.Clamp(Mathf.Lerp(0, 12, t), 0, 12);
                chaseSpeedBeforeInertia = chaseSpeed;
                float tangent = 0.2F*Mathf.Sin(0.5F * Time.time)*Mathf.Clamp(horizontalDisplacement.magnitude, 0, 6);
                Vector3 rightVector = Vector3.Cross(horizontalDisplacement, Vector3.up);
                chaseVelocity = (horizontalDisplacement - tangent*rightVector).normalized * chaseSpeed;
                rigidbody.MovePosition(transform.position + chaseVelocity * Time.fixedDeltaTime);
            }
            else if (timeMoving > 0 && chaseVelocity.magnitude > 0.5F) // Inertia
            {
                if (horizontalDisplacement.magnitude > 0.1F)
                {
                    Quaternion rotation = Quaternion.Euler(0, Mathf.Rad2Deg*Mathf.Atan2(displacement.x, displacement.z) + 180, 0);
                    rigidbody.MoveRotation(rotation);
                }

                timeMoving -= Time.fixedDeltaTime;
                float t = timeMoving / accelTime;
                float chaseSpeed = Mathf.Clamp(Mathf.Lerp(0, chaseSpeedBeforeInertia, t), 0, chaseSpeedBeforeInertia);
                chaseVelocity = chaseVelocity.normalized * chaseSpeed;
                rigidbody.MovePosition(transform.position + chaseVelocity * Time.fixedDeltaTime);

                hasInertia = chaseSpeed > 0.5F;
                if (!hasInertia)
                    timeMoving = 0;
            }
            else
            {
                hasInertia = false;
            }
        }
        else
        {
            if (rotating)
            {
                rotationTimeElapsed += Time.fixedDeltaTime;
                float t = rotationTimeElapsed / rotationDuration;
                Quaternion newRotation = Quaternion.Euler(0, rigidbody.rotation.eulerAngles.y + rotationDirection*(90 * Time.fixedDeltaTime), 0);
                rigidbody.MoveRotation(newRotation);
                if (t > 0.99F)
                    rotating = false;
            }

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
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Block"))
        {
            float heightDiff = Mathf.Abs(transform.position.y - collision.transform.position.y);
            if (heightDiff < 0.5F)
                timeMoving = 0;
        }
    }
}
