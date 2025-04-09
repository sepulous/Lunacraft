using UnityEngine;

class GreenMob : MonoBehaviour
{
    public Material spriteMaterial; // So it can throw out biogel upon death

    private Rigidbody rigidbody;
    private BoxCollider collider;
    private float lastActionTime = 0;
    private int nextActionDelay = 0;
    private bool aboutToAct = false;

    private int health = 3;
    private bool deathAnimationFinished = false;
    private bool jumping = false;
    private float lastJumpTime = 0;
    private bool rotating = false;
    private float rotationDuration = 0.5F;
    private float rotationTimeElapsed = 0;
    private float timeDamaged = 0;
    private float timeDied = 0;
    private Quaternion initialRotation;
    private Quaternion targetRotation;

    void Awake()
    {
        collider = gameObject.AddComponent<BoxCollider>();
        collider.material.staticFriction = 0F;
        collider.material.dynamicFriction = 0F;
        rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        float currentTime = Time.realtimeSinceStartup;

        if (timeDamaged != 0 && Time.time - timeDamaged > 0.15F)
        {
            GetComponent<Renderer>().material.color = Color.white;
            timeDamaged = 0;
        }

        if (health > 0)
        {
            if (!aboutToAct)
            {
                nextActionDelay = UnityEngine.Random.Range(1, 5); // Wait 1-4 seconds before next action
                aboutToAct = true;
            }
            
            if (!(rotating || jumping))
            {
                if (currentTime - lastActionTime > nextActionDelay)
                {
                    int action = UnityEngine.Random.Range(0, 3);
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
                    else // Jump forward
                    {
                        Vector3 force = 5F * (-transform.forward + 2*transform.up);
                        rigidbody.AddForce(force, ForceMode.Impulse);
                        lastJumpTime = currentTime;
                        jumping = true;
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

            if (jumping && currentTime - lastJumpTime > 4)
            {
                jumping = false;
                rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }
        else
        {
            if (!deathAnimationFinished)
            {
                // Rotate
                rigidbody.constraints = RigidbodyConstraints.None;
                if (transform.eulerAngles.z - 90 < 0.5F)
                    transform.RotateAround(transform.position + 0.5F*Vector3.down + 0.5F*transform.right, transform.forward, 90 * Time.deltaTime);
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
                timeDied = Time.time;

                // Shoot out biogel
                GameObject biogel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                biogel.transform.position = transform.position + 0.6F*Vector3.up;
                biogel.hideFlags = HideFlags.HideInHierarchy;
                biogel.layer = LayerMask.NameToLayer("Dropped");

                Dropped droppedScript = biogel.AddComponent<Dropped>();
                droppedScript.itemID = ItemID.biogel;
                droppedScript.amount = 1;
                droppedScript.thrown = true;
                droppedScript.readyForPickup = false;
                droppedScript.isSprite = true;
                biogel.GetComponent<Renderer>().material = spriteMaterial;
                biogel.GetComponent<Renderer>().material.SetTexture("_BaseMap", MeshData.GetItemSprite(ItemID.biogel).texture);
                
                droppedScript.Drop();
            }

            timeDamaged = Time.time;
            GetComponent<Renderer>().material.color = new Color(1, 0.5F, 0.5F, 1);
        }
    }
}
