using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class SimpleCarController : MonoBehaviour
{
    #region Input Variables

    private float _throttleInput;
    private float _brakingInput;
    private float _steeringInput;
    private bool _handBrakeInput;

    #endregion

    #region Debug References

    [SerializeField] private TextMeshProUGUI _debugInputThrottle, _debugInputBrake, _debugWheelTorque, _debugBrakeTorque, _debugWheelRpm, _debugGear;

    #endregion

    [SerializeField] float turningRadius = 6f;

    [SerializeField] private bool _isInReverse;

    public WheelCollider frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel;

    public Transform frontLeftTransform, frontRightTransform, rearLeftTransform, rearRightTransform;

    public float maxSteeringAngle = 30f;

    public float motorForce = 250;
    [SerializeField] private float brakingForce = 500f;
    [SerializeField] private float handBrakeForce = 1000f;

    [SerializeField] private float KPH;
    
    private WheelCollider[] _wheelColliders;

    private Rigidbody rb;

    #region Input Functions

    public void SetThrottleInput(InputAction.CallbackContext context)
    {
        _throttleInput = context.ReadValue<float>();
    }

    public void SetBrakingInput(InputAction.CallbackContext context)
    {
        _brakingInput = context.ReadValue<float>();
    }

    public void SetSteeringInput(InputAction.CallbackContext context)
    {
        _steeringInput = context.ReadValue<float>();
    }

    public void SetHandBrakeInput(InputAction.CallbackContext context)
    {
        _handBrakeInput = context.ReadValueAsButton();
    }

    #endregion

    private void Start()
    {
        _wheelColliders = new[] { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel };
        rb = GetComponent<Rigidbody>();
    }

    private void Steer()
    {
        if (_steeringInput > 0)
        {
            frontLeftWheel.steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (turningRadius + (1.5f / 2)) * _steeringInput);
            frontRightWheel.steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (turningRadius - (1.5f / 2)) * _steeringInput);
        }
        else if (_steeringInput < 0)
        {
            frontLeftWheel.steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (turningRadius - (1.5f / 2)) * _steeringInput);
            frontRightWheel.steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (turningRadius + (1.5f / 2)) * _steeringInput);
        }
        else
        {
            frontLeftWheel.steerAngle = 0f;
            frontRightWheel.steerAngle = 0f;
        }
    }

    private void Accelerate()
    {
        float wheelDirection = _isInReverse ? -1f : 1f;

        rearLeftWheel.motorTorque = _throttleInput * (motorForce / 2) * wheelDirection;
        rearRightWheel.motorTorque = _throttleInput * (motorForce / 2) * wheelDirection;

        KPH = rb.velocity.magnitude * 3.6f;
    }

    private void Brake()
    {
        foreach (var wheelCollider in _wheelColliders)
        {
            wheelCollider.brakeTorque = _brakingInput * brakingForce;
        }
    }

    private void Handbrake()
    {
        if (_handBrakeInput)
        {
            _wheelColliders[2].brakeTorque = _wheelColliders[3].brakeTorque = handBrakeForce;
        }
    }

    private void UpdateWheelPoses()
    {
        UpdateWheelPose(frontLeftWheel, frontLeftTransform);
        UpdateWheelPose(frontRightWheel, frontRightTransform);
        UpdateWheelPose(rearLeftWheel, rearLeftTransform);
        UpdateWheelPose(rearRightWheel, rearRightTransform);
    }

    private void UpdateWheelPose(WheelCollider wheelCollider, Transform wheelTransform)
    {
        wheelCollider.GetWorldPose(out Vector3 targetWheelPosition, out Quaternion targetWheelRotation);

        wheelTransform.position = targetWheelPosition;
        wheelTransform.rotation = targetWheelRotation;
    }

    private void SelectGear()
    {
        if(_brakingInput > 0.1f)
        {
            _isInReverse = true;
        }
        else
        {
            _isInReverse = false;
        }
    }

    private void FixedUpdate()
    {
        Steer();
        Accelerate();
        Brake();
        Handbrake();
        UpdateWheelPoses();
    }

    private void Update()
    {
        SelectGear();
        PrintDebugValues();
    }

    private void PrintDebugValues()
    {
        float motorTorque = rearRightWheel.motorTorque;
        float brakeTorque = rearRightWheel.brakeTorque;
        float wheelRpm = rearRightWheel.rpm;

        _debugInputThrottle.text = _throttleInput.ToString(".00");
        _debugInputBrake.text = _brakingInput.ToString(".00");

        _debugWheelTorque.text = motorTorque.ToString("0000");
        _debugBrakeTorque.text = brakeTorque.ToString("0000");
        _debugWheelRpm.text = wheelRpm.ToString("0000");

        _debugGear.text = _isInReverse.ToString();
    }
}
