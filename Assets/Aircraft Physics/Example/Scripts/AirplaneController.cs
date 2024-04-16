using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AirplaneController : MonoBehaviour
{
    [SerializeField]
    List<AeroSurface> controlSurfaces = null;
    [SerializeField]
    List<WheelCollider> wheels = null;
    [SerializeField]
    float rollControlSensitivity = 0.2f;
    [SerializeField]
    float pitchControlSensitivity = 0.2f;
    [SerializeField]
    float yawControlSensitivity = 0.2f;
    [SerializeField]
    float thrustControlSensitivity = 0.2f;

    [Range(-1, 1)]
    public float Pitch;
    [Range(-1, 1)]
    public float Yaw;
    [Range(-1, 1)]
    public float Roll;
    [Range(0, 1)]
    public float Flap;
    [SerializeField]
    Text displayText = null;

    float thrustPercent;
    float brakesTorque;

    AircraftPhysics aircraftPhysics;
    Rigidbody rb;

    // Unit conversion rates
    float knotsConversionFactor = 1.9438444924f;
    float feetConversionFactor = 3.28084f;

    // States of controller button presses (to make sure button click events are only triggered once in Update())
    bool isXPressed = false;
    bool isYPressed = false;
    bool isAPressed = false;

    private void Start()
    {
        aircraftPhysics = GetComponent<AircraftPhysics>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Pitch = Input.GetAxis("Vertical");
        Roll = Input.GetAxis("Horizontal");
        Yaw = Input.GetAxis("Yaw");

        // Decrease thrust progressively
        // < on keyboard; X on left controller
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            SetThrust(true);
        }
        if (!isXPressed && OVRInput.Get(OVRInput.Button.Three))
        {
            isXPressed = true;
            SetThrust(true);
        }
        if (isXPressed && !OVRInput.Get(OVRInput.Button.Three))
        {
            isXPressed = false;
        }

        // Increase thrust progressively
        // > on keyboard; Y on left controller
        if (Input.GetKeyDown(KeyCode.Period))
        {
            SetThrust();
        }
        if (!isYPressed && OVRInput.Get(OVRInput.Button.Four))
        {
            isYPressed = true;
            SetThrust();
        }
        if (isYPressed && !OVRInput.Get(OVRInput.Button.Four))
        {
            isYPressed = false;
        }

        // Flap
        // F on keyboard
        if (Input.GetKeyDown(KeyCode.F))
        {
            Flap = Flap > 0 ? 0 : 0.3f;
        }

        // Brake
        // Space on keyboard; A on right controller
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetBrake();
        }
        if (!isAPressed && OVRInput.Get(OVRInput.Button.One))
        {
            isAPressed = true;
            SetBrake();
        }
        if (isAPressed && !OVRInput.Get(OVRInput.Button.One))
        {
            isAPressed = false;
        }

        displayText.text = "V: " + ((int)ConvertSpeedUnit(rb.velocity.magnitude)).ToString("D3") + " knots\n";
        displayText.text += "A: " + ((int)ConvertLengthUnit(transform.position.y)).ToString("D4") + " feet\n";
        displayText.text += "T: " + (int)(thrustPercent * 100) + "%\n";
        displayText.text += brakesTorque > 0 ? "B: ON" : "B: OFF";
    }

    private void FixedUpdate()
    {
        SetControlSurfecesAngles(Pitch, Roll, Yaw, Flap);
        aircraftPhysics.SetThrustPercent(thrustPercent);
        foreach (var wheel in wheels)
        {
            wheel.brakeTorque = brakesTorque;
            // small torque to wake up wheel collider
            wheel.motorTorque = 0.01f;
        }
    }

    private void SetThrust(bool decrease = false)
    {
        if (decrease)
        {
            float percent = thrustPercent - thrustControlSensitivity;
            thrustPercent = Mathf.Clamp01(percent);
        }
        else
        {
            float percent = thrustPercent + thrustControlSensitivity;
            thrustPercent = Mathf.Clamp01(percent);
        }
        Debug.Log("Changing Thrust To: " + thrustPercent);
    }

    private void SetBrake()
    {
        brakesTorque = brakesTorque > 0 ? 0 : 100f;
        Debug.Log("Changing Brake To: " + brakesTorque);
    }

    public void SetControlSurfecesAngles(float pitch, float roll, float yaw, float flap)
    {
        foreach (var surface in controlSurfaces)
        {
            if (surface == null || !surface.IsControlSurface) continue;
            switch (surface.InputType)
            {
                case ControlInputType.Pitch:
                    surface.SetFlapAngle(pitch * pitchControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Roll:
                    surface.SetFlapAngle(roll * rollControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Yaw:
                    surface.SetFlapAngle(yaw * yawControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Flap:
                    surface.SetFlapAngle(Flap * surface.InputMultiplyer);
                    break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            SetControlSurfecesAngles(Pitch, Roll, Yaw, Flap);
    }

    // Convert the unit of speed from "m/s" to "knots" 
    private float ConvertSpeedUnit(float mps)
    {
        return mps * knotsConversionFactor;
    }

    // Convert the unit of length from "meters" to "feet" 
    private float ConvertLengthUnit(float m)
    {
        return m * feetConversionFactor;
    }
}
