using UnityEngine;

public class Slug : MonoBehaviour
{
    public Vector3 initialPosition = Vector3.zero;
    public Vector3 initialVelocity = Vector3.zero;
    public float initialAngleInDegrees = -420;
    private float startTime = -1;
    private bool flying = true;
    private float timeSinceCollision = 0;

    void Awake()
    {
        GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Update()
    {
        if (flying && initialPosition != Vector3.zero && initialVelocity != Vector3.zero && initialAngleInDegrees != -420)
        {
            if (startTime == -1)
                startTime = Time.time;

            float time = Time.time - startTime;

            transform.position = new Vector3(
                initialPosition.x + initialVelocity.x * time,
                initialPosition.y + initialVelocity.y * time - (0.5F * 9.8F * time*time),
                initialPosition.z + initialVelocity.z * time
            );

            transform.rotation = Quaternion.Euler(
                Mathf.Rad2Deg * Mathf.Atan(time + Mathf.Tan(Mathf.Deg2Rad * initialAngleInDegrees)),
                transform.eulerAngles.y,
                transform.eulerAngles.z
            );
        }
        else
        {
            timeSinceCollision += Time.deltaTime;
            if (timeSinceCollision > 60)
                Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        flying = false;
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<Rigidbody>().isKinematic = true;
        transform.position = transform.position + 0.1F*transform.forward;
    }
}
