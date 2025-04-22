using UnityEngine;

public class CarController : MonoBehaviour
{
    public float[] inputs = new float[5]; // 5 sensores
    public float[] outputs = new float[3]; // Acelerar/frenar, girar izquierda, girar derecha

    public NeuralNetwork brain;
    public float fitness = 0;
    public float totalDistance = 0;
    public float timeAlive = 0;

    public float maxSpeed = 20f;
    public float minSpeed = 2f;
    public float acceleration = 10f;
    public float steering = 180f;
    public float sensorLength = 20f;

    public float maxIdleTime = 5f;
    private float idleTime = 0f;

    public float sensorForwardOffset = 2.5f; 
    public float sensorHeight = 0.5f; 
    private Rigidbody rb;
    private Vector3 startPosition;
    private Quaternion startRotation;
    public bool isDead = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("RB no esta en el prefab de mi carro");
        }

       
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Start()
    {
        if (brain == null)
        {
            brain = new NeuralNetwork(5, 6, 4, 3); 
        }
    }

    void Update()
    {
        if (isDead) return;

        UpdateSensors();
        outputs = brain.FeedForward(inputs);
        ApplyOutputs();
        UpdateFitness();
        CheckIdleStatus();

        timeAlive += Time.deltaTime;
    }
    void UpdateSensors()
    {
        Vector3 sensorStartPos = transform.position + transform.forward * sensorForwardOffset + Vector3.up * sensorHeight;

        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;
            Vector3 sensorDirection = Quaternion.Euler(0, -90 + 45 * i, 0) * transform.forward;
            if (Physics.Raycast(sensorStartPos, sensorDirection, out hit, sensorLength))
            {
                inputs[i] = 1 - (hit.distance / sensorLength);
                Debug.DrawRay(sensorStartPos, sensorDirection * hit.distance, Color.red);
            }
            else
            {
                inputs[i] = 0f; 
                Debug.DrawRay(sensorStartPos, sensorDirection * sensorLength, Color.green);
            }
        }
    }

    void ApplyOutputs()
    {
        float forwardSpeed = Mathf.Clamp(outputs[0], 0, 1) * maxSpeed;
        float turning = (outputs[1] - outputs[2]) * steering;//intensidad de giro

        if (forwardSpeed > 0)
        {
            forwardSpeed = Mathf.Max(forwardSpeed, minSpeed);
        }

        rb.velocity = transform.forward * forwardSpeed;
        transform.Rotate(Vector3.up * turning * Time.deltaTime);

        totalDistance += rb.velocity.magnitude * Time.deltaTime;
    }

    void UpdateFitness()
    {
        fitness = totalDistance + (timeAlive / 10f);
    }

    void CheckIdleStatus()
    {
        if (rb.velocity.magnitude < minSpeed)
        {
            idleTime += Time.deltaTime;
            if (idleTime > maxIdleTime)
            {
                isDead = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            idleTime = 0f;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            isDead = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Reset()
    {
        if (rb != null)
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
            isDead = false;
            fitness = 0;
            totalDistance = 0;
            timeAlive = 0;
            idleTime = 0f;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            Debug.LogError("error");
        }
    }
}