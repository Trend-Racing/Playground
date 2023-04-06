using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class AdvancedCarController : MonoBehaviour
{
    #region Reference Variables
    
    [Space(10)]
    
    [Header("References")]
    [SerializeField] private WheelCollider[] wheelColliders = new WheelCollider[4];
    [SerializeField] private Transform[] wheelMeshes = new Transform[4];
    [SerializeField] private ParticleSystem[] wheelParticles = new ParticleSystem[2];

    [SerializeField] private Rigidbody carRigidbody;
    
    [Space(10)]

    #endregion

    #region Input Variables
    [Space(10)]
    
    [Header("Input Debug")]
    
    [SerializeField] private float throttleInput;
    [SerializeField] private float brakeInput;
    [SerializeField] private float turnInput;

    [SerializeField] private bool handBrakeInput;

    [Space(10)]

    #endregion

    [SerializeField] private DriveTrain driveTrain = DriveTrain.RearWheelDrive;

    [SerializeField, Range(0f, 1f)] private float brakeBias = 0.7f;
    
    [SerializeField] private float motorPower = 200f;
    [SerializeField] private float brakePower = 500f;

    [SerializeField] private float slipAngle;

    [SerializeField] private float speed;

    [SerializeField] private AnimationCurve steeringCurve;

    [SerializeField] private bool shouldReverse = false;

    [SerializeField] private float slipAllowance = 0.4f;
    
    //helper variables
    private const int NumberOfWheels = 4;

    #region Input Functions

    public void ThrottleInput(InputAction.CallbackContext context)
    {
        throttleInput = context.ReadValue<float>();
    }
    public void BrakeInput(InputAction.CallbackContext context)
    {
        brakeInput = context.ReadValue<float>();
    }
    public void HandBrakeInput(InputAction.CallbackContext context)
    {
        handBrakeInput = context.ReadValueAsButton();
    }
    public void TurnInput(InputAction.CallbackContext context)
    {
        turnInput = context.ReadValue<float>();
    }

    #endregion

    private void Update()
    {
        speed = carRigidbody.velocity.magnitude * 3.6f;
    }

    private void FixedUpdate()
    {
        //CheckShouldReverse();
        
        ApplySteering();
        ApplyThrottle();
        ApplyBrake();
        ApplyWheelColliderPosition();
        
        PlayTireSmoke();
    }

    private void CheckShouldReverse()
    {
        if (shouldReverse)
        {
            shouldReverse = !(slipAngle > 120f);
        }
        
        if(brakeInput <= 0f) return;

        shouldReverse = (slipAngle > 120f);
    }

    private void ApplySteering()
    {
        var steeringAngle = turnInput * steeringCurve.Evaluate(speed);
        steeringAngle += Vector3.SignedAngle(transform.forward, carRigidbody.velocity + transform.forward, Vector3.up);
        steeringAngle = Mathf.Clamp(steeringAngle, -90f, 90f);

        for (int i = 0; i < 2; i++)
        {
            wheelColliders[i].steerAngle = steeringAngle;
        }
    }

    private void ApplyBrake()
    {
        //front wheel braking
        for (int i = 0; i < 2; i++)
        {
            wheelColliders[i].brakeTorque = brakeInput * brakePower * brakeBias;
        }
        
        //rear wheel braking
        for (int i = 2; i < NumberOfWheels; i++)
        {
            wheelColliders[i].brakeTorque = brakeInput * brakePower * (1 - brakeBias);
        }
    }
    private void ApplyThrottle()
    {
        slipAngle = Vector3.Angle(transform.forward, carRigidbody.velocity - transform.forward);

        var targetMotorTorque = motorPower * throttleInput;

        switch (driveTrain)
        {
            case DriveTrain.AllWheelDrive:
                for (int i = 0; i < NumberOfWheels; i++)
                {
                    wheelColliders[i].motorTorque = targetMotorTorque / NumberOfWheels;
                }
                break;
            
            case DriveTrain.FrontWheelDrive:
                for (int i = 0; i < 2; i++)
                {
                    wheelColliders[i].motorTorque = targetMotorTorque / 2;
                }
                break;
            
            case DriveTrain.RearWheelDrive:
                for (int i = 2; i < NumberOfWheels; i++)
                {
                    wheelColliders[i].motorTorque = targetMotorTorque / 2;
                }
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void PlayTireSmoke()
    {
        WheelHit[] wheelHits = new WheelHit[4];

        for (int i = 2; i < NumberOfWheels; i++)
        {
            wheelColliders[i].GetGroundHit(out wheelHits[i]);
            
            print(wheelHits[i].sidewaysSlip);

            if (Mathf.Abs(wheelHits[i].sidewaysSlip) + Mathf.Abs(wheelHits[i].forwardSlip) > slipAllowance)
            {
                wheelParticles[i - 2].Play();
            }
            else
            {
                wheelParticles[i - 2].Stop();
            }
        }

        
        
    }
    
    private void ApplyWheelColliderPosition()
    {
        for (var i = 0; i < NumberOfWheels; i++)
        {
            UpdateWheelTransform(wheelColliders[i], wheelMeshes[i]);
        }
    }
    private void UpdateWheelTransform(WheelCollider wheelCollider, Transform wheelTransform)
    {
        wheelCollider.GetWorldPose(out Vector3 targetWheelPosition, out Quaternion targetWheelRotation);

        wheelTransform.position = targetWheelPosition;
        wheelTransform.rotation = targetWheelRotation;
    }
}
