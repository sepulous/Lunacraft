using UnityEngine;

public class Dropped : MonoBehaviour
{
    public ItemID itemID;
    public int amount = 1;
    public bool movingTowardPlayer = false;
    public bool thrown = false;
    public bool readyForPickup = true;
    public bool isSprite = false;
    private Player player;
    private Rigidbody rigidbody;
    private bool landed = false;
    private float groundPosY = 0;
    private float timeSinceStartedFalling = 0;

    public void Drop()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
        if (GetComponent<BlockData>()?.blockID == BlockID.light)
            Destroy(GetComponent<Light>());
        gameObject.layer = LayerMask.NameToLayer("Dropped");
        if (isSprite)
            transform.localScale = new Vector3(0.5F, 0.5F, 0);
        else
            transform.localScale = new Vector3(0.25F, 0.25F, 0.25F);
        rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        if (thrown)
        {
            Vector3 cameraForward = GameObject.Find("PlayerCamera").transform.forward;
            rigidbody.AddForce(400*cameraForward + 50*Vector3.up);
        }
        else
        {
            float forceStrengthLR = UnityEngine.Random.Range(-50, 50);
            float forceStrengthFB = UnityEngine.Random.Range(-50, 50);
            rigidbody.AddForce(200*Vector3.up + forceStrengthLR*Vector3.right + forceStrengthFB*Vector3.forward);
        }
    }

    void Update()
    {
        if (isSprite)
            transform.LookAt(player.transform);

        if (!landed)
        {
            landed = (Physics.OverlapBox(transform.position - new Vector3(0, 0.13F, 0), new Vector3(0.12F, 0.13F, 0.12F), Quaternion.identity, LayerMask.GetMask("Block"))).Length > 0;
            if (landed)
            {
                groundPosY = transform.position.y;
                rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                readyForPickup = true;
            }
        }
        else if (!movingTowardPlayer)
        {
            transform.position = new Vector3(
                transform.position.x,
                groundPosY + 0.1F*Mathf.Pow(Mathf.Sin(1.5F * Time.time), 2),
                transform.position.z
            );

            if (!isSprite)
                transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + 20F*Time.deltaTime, transform.eulerAngles.z);
            
            landed = (Physics.OverlapBox(new Vector3(transform.position.x, groundPosY - 0.13F, transform.position.z), new Vector3(0.12F, 0.13F, 0.12F), Quaternion.identity, LayerMask.GetMask("Block"))).Length > 0;
        }

        rigidbody.useGravity = !landed;
    }
}
