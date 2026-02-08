using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class MasterBrain : MonoBehaviour
{
    // =========================
    // PERFORMANCE & SAFETY
    // =========================
    const int TARGET_FPS = 120;
    const float MAX_ANGULAR_VEL = 7f;

    // =========================
    // MOVEMENT
    // =========================
    [Header("Car Movement")]
    public float acceleration = 1600f;
    public float steering = 75f;
    public float maxSpeed = 42f;

    // =========================
    // DRIFT
    // =========================
    [Header("Drift")]
    [Range(0.85f, 0.99f)] public float driftGrip = 0.93f;
    public float minDriftSpeed = 6f;

    // =========================
    // RESET / FAILSAFE
    // =========================
    [Header("Failsafe")]
    public float resetHeight = 3.5f;
    public float stuckSpeedThreshold = 0.2f;
    public float stuckTimeLimit = 3f;

    // =========================
    // CAMERA
    // =========================
    [Header("Camera")]
    public Vector3 camOffset = new Vector3(0, 7, -12);
    public float camSmooth = 7f;

    // =========================
    // AUDIO
    // =========================
    [Header("Audio")]
    public AudioSource music;
    [Range(0f, 1f)] public float musicVolume = 0.9f;

    // =========================
    // INTERNAL STATE
    // =========================
    Rigidbody rb;
    Camera cam;

    float throttle;
    float steer;
    bool drift;
    float stuckTimer;

    // =========================
    // INIT (PASS 1 DEBUG)
    // =========================
    void Awake()
    {
        // Performance lock
        Application.targetFrameRate = TARGET_FPS;
        QualitySettings.vSyncCount = 0;

        // Core refs
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;

        // Rigidbody hardening
        rb.centerOfMass = new Vector3(0, -0.6f, 0);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.maxAngularVelocity = MAX_ANGULAR_VEL;

        // Audio hardening
        if (music)
        {
            music.loop = true;
            music.volume = musicVolume;
            if (!music.isPlaying) music.Play();
        }
    }

    // =========================
    // INPUT (PASS 2 DEBUG)
    // =========================
    void Update()
    {
        throttle = Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);
        steer = Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
        drift = Input.GetKey(KeyCode.Space);

        if (Input.GetKeyDown(KeyCode.R))
            ForceReset();

        MonitorStuckState();
    }

    // =========================
    // PHYSICS (PASS 3 DEBUG)
    // =========================
    void FixedUpdate()
    {
        HandleMovement();
        HandleDrift();
    }

    // =========================
    // CAMERA (PASS 4 DEBUG)
    // =========================
    void LateUpdate()
    {
        if (!cam) return;

        Vector3 desired = transform.position + camOffset;
        cam.transform.position = Vector3.Lerp(
            cam.transform.position,
            desired,
            camSmooth * Time.deltaTime
        );

        cam.transform.LookAt(transform.position + Vector3.up * 1.5f);
    }

    // =========================
    // SYSTEMS
    // =========================
    void HandleMovement()
    {
        if (rb.velocity.magnitude < maxSpeed)
            rb.AddForce(transform.forward * throttle * acceleration * Time.fixedDeltaTime, ForceMode.Acceleration);

        Quaternion turn = Quaternion.Euler(0f, steer * steering * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);
    }

    void HandleDrift()
    {
        if (!drift || rb.velocity.magnitude < minDriftSpeed) return;

        Vector3 v = rb.velocity;
        v.x *= driftGrip;
        v.z *= driftGrip;
        rb.velocity = v;
    }

    // =========================
    // FAILSAFE SYSTEM (PASS 5 DEBUG)
    // =========================
    void MonitorStuckState()
    {
        if (rb.velocity.magnitude < stuckSpeedThreshold)
            stuckTimer += Time.deltaTime;
        else
            stuckTimer = 0f;

        if (stuckTimer > stuckTimeLimit)
            ForceReset();
    }

    void ForceReset()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position += Vector3.up * resetHeight;
        transform.rotation = Quaternion.identity;
        stuckTimer = 0f;
    }

    // =========================
    // FINAL SAFETY (PASS 6 DEBUG)
    // =========================
    void OnValidate()
    {
        acceleration = Mathf.Max(500f, acceleration);
        maxSpeed = Mathf.Max(10f, maxSpeed);
        camSmooth = Mathf.Clamp(camSmooth, 1f, 15f);
    }
}
