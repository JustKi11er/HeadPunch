using UnityEngine;

public class JiggleBone : MonoBehaviour
{
    // physics constants
    private float stiffness = 500f;
    private float mass = 1f;
    private float damping = 15f;
    private float distance = 3f;
    private float intensivity = 10f;
    // input
    [SerializeField] private Transform JiggleParrent;
    // limits
    private float CMR = 150;
    // display debug lines
    [SerializeField] private bool debugRender = false;

    // physics variables
    Vector3 vel = new Vector3();
    Vector3 dynamicPos = new Vector3();
    Vector3 LerpClampedDS;

    // bind rotation of the bone
    Quaternion bindRot = new Quaternion();
    public float Stiffness
    {
        get
        {
            return stiffness;
        }
        set
        {
            stiffness = value;
        }
    }
    public float ClampMagnitudeRot
    {
        get
        {
            return CMR;
        }
        set
        {
            CMR = value;
        }
    }
    public float Damping
    {
        get
        {
            return damping;
        }
        set
        {
            damping = value;
        }
    }
    public void ApplyImpulse(Vector3 impulse)
    {
        vel += impulse * 10 / mass;
    }
    void Awake()
    {
        bindRot = JiggleParrent.localRotation;
        dynamicPos = JiggleParrent.position + JiggleParrent.forward;
    }
    void LateUpdate()
    {
        if (damping < 0f) damping = 0f;
        if (mass <= 0f) mass = 0.01f;
        JiggleParrent.localRotation = bindRot;

        Vector3 forwardVector = JiggleParrent.forward * distance;
        Vector3 targetPos = JiggleParrent.position + forwardVector;
        Vector3 displacement = (targetPos - dynamicPos);

        vel /= 1 + damping * Time.deltaTime;

        Vector3 force = displacement * stiffness;

        Vector3 acc = force / mass;

        vel += acc * Time.deltaTime;

        dynamicPos += vel * Time.deltaTime;
        JiggleParrent.LookAt(dynamicPos, JiggleParrent.up);
        JiggleParrent.localPosition = JiggleParrent.localPosition;
            //JiggleParrent.localRotation = Quaternion.Inverse(JiggleParrent.localRotation);
        JiggleParrent.localRotation = ClampRotation(JiggleParrent.localRotation, new Vector3(CMR, CMR, CMR));
        JiggleParrent.localRotation = JiggleParrent.localRotation * JiggleParrent.localRotation;
        if (debugRender)
        {
            Debug.DrawRay(JiggleParrent.position, forwardVector, Color.blue);
            Debug.DrawRay(JiggleParrent.position, JiggleParrent.forward, Color.magenta);
            Debug.DrawRay(dynamicPos, Vector3.up * 0.2f, Color.red);
            Debug.DrawRay(JiggleParrent.position + JiggleParrent.forward * displacement.magnitude,
                Vector3.up * 0.2f, Color.green);
        }
    }
    public static Quaternion ClampRotation(Quaternion q, Vector3 bounds)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
        angleX = Mathf.Clamp(angleX, -bounds.x, bounds.x);
        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        float angleY = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.y);
        angleY = Mathf.Clamp(angleY, -bounds.y, bounds.y);
        q.y = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleY);

        float angleZ = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.z);
        angleZ = Mathf.Clamp(angleZ, -bounds.z, bounds.z);
        q.z = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleZ);

        return q;
    }

}