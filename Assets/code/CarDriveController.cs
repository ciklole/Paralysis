using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arcade wheel-collider car controller for the driving scene.
/// W / S = throttle and reverse. A / D = steering only. E = get out.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarDriveController : MonoBehaviour
{
    [Header("Wheels")]
    [SerializeField] private WheelCollider[] driveWheels;
    [SerializeField] private WheelCollider[] steerWheels;
    [SerializeField] private Transform[] visualWheels;
    [SerializeField] private Transform steeringWheel;
    [SerializeField] private float steeringWheelMaxAngle = 180f;

    [Header("Motor")]
    [SerializeField] private float motorTorque = 1800f;
    [SerializeField] private float reverseTorque = 1200f;
    [SerializeField] private float idleBrake = 1000f;
    [SerializeField] private float maxSpeed = 25f;
    [SerializeField] private float maxReverseSpeed = 8f;

    [Header("Steering")]
    [SerializeField] private float maxSteerAngle = 32f;
    [SerializeField] private float steerSpeed = 120f;
    [SerializeField] private float steerReturnSpeed = 180f;

    [Header("Stability")]
    [SerializeField] private Vector3 centerOfMass = new Vector3(0f, -0.35f, 0f);
    [SerializeField, Range(0f, 1f)] private float lateralGrip = 0.85f;
    [SerializeField] private float downforce = 0.05f;

    [Header("Input")]
    [SerializeField] private KeyCode exitKey = KeyCode.E;
    [SerializeField] private string returnFallbackScene = "City";

    private Rigidbody body;
    private WheelCollider[] allWheels;
    private float throttle;
    private float steer;
    private float currentSteer;
    private bool exiting;

    public Rigidbody Body => body;

    public float SpeedKph => body != null
        ? body.linearVelocity.magnitude * 3.6f
        : 0f;

    public void SetReturnFallbackScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            returnFallbackScene = sceneName;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (body != null)
        {
            body.centerOfMass = centerOfMass;
            body.isKinematic = false;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 10f;
        }

        allWheels = GetComponentsInChildren<WheelCollider>(true);

        if (driveWheels == null || driveWheels.Length == 0)
            driveWheels = FindDriveWheels(allWheels);

        if (steerWheels == null || steerWheels.Length == 0)
            steerWheels = FindSteerWheels(allWheels);

        if (visualWheels == null || visualWheels.Length == 0)
            visualWheels = FindVisualWheels(allWheels);
    }

    private void Start()
    {
        if (body != null)
            body.WakeUp();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private WheelCollider[] FindDriveWheels(WheelCollider[] wheels)
    {
        List<WheelCollider> rear = new List<WheelCollider>();

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] != null &&
                wheels[i].name.IndexOf(
                    "Rear",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                rear.Add(wheels[i]);
            }
        }

        if (rear.Count > 0)
            return rear.ToArray();

        // Generic wheel names such as "Wheel FL" have no "Rear" in them.
        // Fall back to local position (the car's forward is +Z).
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null)
                continue;

            Vector3 local = transform.InverseTransformPoint(
                wheels[i].transform.position
            );

            if (local.z < -0.01f)
                rear.Add(wheels[i]);
        }

        return rear.Count > 0 ? rear.ToArray() : wheels;
    }

    private WheelCollider[] FindSteerWheels(WheelCollider[] wheels)
    {
        List<WheelCollider> front = new List<WheelCollider>();

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] != null &&
                wheels[i].name.IndexOf(
                    "Front",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                front.Add(wheels[i]);
            }
        }

        if (front.Count > 0)
            return front.ToArray();

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null)
                continue;

            Vector3 local = transform.InverseTransformPoint(
                wheels[i].transform.position
            );

            if (local.z > 0.01f)
                front.Add(wheels[i]);
        }

        return front.Count > 0 ? front.ToArray() : wheels;
    }

    private Transform[] FindVisualWheels(WheelCollider[] wheels)
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        Transform[] result = new Transform[wheels.Length];

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null)
                continue;

            // Most car prefabs keep the visible tyre as a child of the
            // invisible WheelCollider object.
            MeshRenderer childMesh =
                wheels[i].GetComponentInChildren<MeshRenderer>(true);

            if (childMesh != null &&
                childMesh.transform != wheels[i].transform)
            {
                result[i] = childMesh.transform;
                continue;
            }

            for (int j = 0; j < allTransforms.Length; j++)
            {
                Transform candidate = allTransforms[j];

                if (candidate == wheels[i].transform)
                    continue;

                if (!candidate.name.Equals(
                        wheels[i].name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (candidate.GetComponent<MeshRenderer>() == null)
                    continue;

                result[i] = candidate;
                break;
            }
        }

        return result;
    }

    private void Update()
    {
        if (UIState.BlocksGameplay)
        {
            throttle = 0f;
            steer = 0f;
            return;
        }

        throttle = Input.GetAxis("Vertical");
        steer = Input.GetAxis("Horizontal");

        if (Input.GetKeyDown(exitKey))
            ExitVehicle();
    }

    /// <summary>
    /// Leaves the car and returns to the scene it came from. Returns false when
    /// the return scene is missing, so the player can keep driving.
    /// </summary>
    public bool ExitVehicle()
    {
        if (exiting)
            return false;

        exiting = true;
        CarDriveSession.SetExitPose(transform);

        if (CarDriveSession.ReturnToOriginalScene(returnFallbackScene))
            return true;

        exiting = false;
        return false;
    }

    private void FixedUpdate()
    {
        if (body == null)
            return;

        float speed = Vector3.Dot(body.linearVelocity, transform.forward);

        float targetSteer = steer * maxSteerAngle;
        float steerRate = Mathf.Abs(targetSteer) > 0.01f
            ? steerSpeed
            : steerReturnSpeed;

        currentSteer = Mathf.MoveTowards(
            currentSteer,
            targetSteer,
            steerRate * Time.fixedDeltaTime
        );

        if (allWheels != null)
        {
            for (int i = 0; i < allWheels.Length; i++)
            {
                if (allWheels[i] != null)
                    allWheels[i].steerAngle = 0f;
            }
        }

        for (int i = 0; i < steerWheels.Length; i++)
        {
            if (steerWheels[i] != null)
                steerWheels[i].steerAngle = currentSteer;
        }

        float torque = 0f;

        if (throttle > 0.01f && speed < maxSpeed)
            torque = throttle * motorTorque;
        else if (throttle < -0.01f && speed > -maxReverseSpeed)
            torque = throttle * reverseTorque;

        bool braking = Mathf.Abs(throttle) < 0.01f;

        for (int i = 0; i < driveWheels.Length; i++)
        {
            WheelCollider wheel = driveWheels[i];

            if (wheel == null)
                continue;

            wheel.motorTorque = torque;
            wheel.brakeTorque = braking ? idleBrake : 0f;
        }

        // Front wheels need the same idle brake even though they are not
        // driven wheels.
        for (int i = 0; i < steerWheels.Length; i++)
        {
            WheelCollider wheel = steerWheels[i];

            if (wheel == null)
                continue;

            bool isDriveWheel = false;

            for (int j = 0; j < driveWheels.Length; j++)
            {
                if (driveWheels[j] == wheel)
                {
                    isDriveWheel = true;
                    break;
                }
            }

            if (!isDriveWheel)
                wheel.brakeTorque = braking ? idleBrake : 0f;
        }

        // Reduce sideways sliding without removing forward momentum.
        Vector3 localVelocity = transform.InverseTransformDirection(
            body.linearVelocity
        );

        localVelocity.x *= lateralGrip;
        body.linearVelocity = transform.TransformDirection(localVelocity);

        if (downforce > 0f)
        {
            body.AddForce(
                -transform.up * downforce * body.linearVelocity.magnitude,
                ForceMode.Acceleration
            );
        }

        UpdateVisualWheels();

        if (steeringWheel != null && maxSteerAngle > 0.01f)
        {
            steeringWheel.localRotation = Quaternion.Euler(
                0f,
                0f,
                -currentSteer / maxSteerAngle * steeringWheelMaxAngle
            );
        }
    }

    private void UpdateVisualWheels()
    {
        if (visualWheels == null)
            return;

        for (int i = 0; i < visualWheels.Length; i++)
        {
            Transform visual = visualWheels[i];

            if (visual == null ||
                i >= allWheels.Length ||
                allWheels[i] == null)
            {
                continue;
            }

            allWheels[i].GetWorldPose(
                out Vector3 position,
                out Quaternion rotation
            );

            visual.position = position;
            visual.rotation = rotation;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (driveWheels == null)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < driveWheels.Length; i++)
        {
            if (driveWheels[i] != null)
            {
                Gizmos.DrawWireSphere(
                    driveWheels[i].transform.position,
                    0.2f
                );
            }
        }
    }
}
