using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class AdvancedCarController : MonoBehaviour
{
    #region Reference Variables
    
    [Space(10)]
    
    [Header("References")]
    [SerializeField] private WheelCollider[] wheelColliders = new WheelCollider[4];
    [SerializeField] private Transform[] wheelMeshes = new Transform[4];
    [SerializeField] private ParticleSystem[] wheelParticles = new ParticleSystem[2];
    [SerializeField] private TrailRenderer[] wheelTrails = new TrailRenderer[2];

    [SerializeField] private Rigidbody carRigidbody;
    
    [SerializeField] private TextMeshProUGUI rpmText;
    [SerializeField] private TextMeshProUGUI gearText;
    [SerializeField] private TextMeshProUGUI speedText;
    
    [Space(10)]

    #endregion

    #region Input Variables
    [Space(10)]
    
    [Header("Input Debug")]
    
    [SerializeField] private float throttleInput;
    [SerializeField] private float brakeInput;
    [SerializeField] private float turnInput;

    [SerializeField] private bool handBrakeInput;
    [SerializeField] private bool clutchInput;

    [Space(10)]

    #endregion

    #region Car Specs
    [Space(10)]
    [Header("Car Specs")]
    
    [SerializeField] private DriveTrain driveTrain = DriveTrain.RearWheelDrive;

    [SerializeField] private float carMass = 1000f;
    [SerializeField] private Transform centerOfMass;
    
    [Space(10)]
    #endregion

    #region Speed
    [Space(10)]
    [Header("Speed")]

    [SerializeField] private float maxHorsePower = 200f;
    
    [SerializeField] private AnimationCurve powerCurve;
    
    [SerializeField] private float currentSpeed;
    [SerializeField] private float rpm;
    [SerializeField] private float wheelRpm;

    [Space(10)]
    #endregion

    #region Braking
    [Space(10)]
    [Header("Braking")]
    
    [SerializeField] private float brakePower = 500f;
    
    [SerializeField, Range(0f, 1f)] private float brakeBias = 0.7f;

    [Space(10)]
    #endregion

    #region Gearing
    [Space(10)]
    [Header("Gearing")]
    
    [SerializeField] private int currentGear;
    [SerializeField] private float[] gearRatios;
    [SerializeField] private float differentialRatio;
    
    [Space(5)]
    
    [SerializeField] private float idleRpm;
    [SerializeField] private float redLineRpm;
    
    [Space(5)]
    
    [SerializeField] private GearState gearState = GearState.Running;
    [SerializeField] private float gearChangeTime = 0.5f;
    
    [Space(5)]
    
    [SerializeField] private float upShiftRpm;
    [SerializeField] private float downShiftRpm;

    [Space(5)] 
    
    [SerializeField] private float clutchSpeed = 4f;
    [SerializeField] private float clutch;
    [SerializeField] private float currentTorque;

    
    [Space(10)]
    #endregion

    #region Steering
    [Space(10)]
    [Header("Steering")]
    
    [SerializeField] private AnimationCurve steeringCurve;

    [SerializeField] private float steeringSpeed;
    [SerializeField] private float maxSteeringAngle;

    [Space(10)]

    #endregion

    #region Drifting
    [Space(10)]
    [Header("Drifting")]
    
    [SerializeField] private float driftSpeed = 0.5f;
    [SerializeField] private AnimationCurve driftingCurve;
    [SerializeField] private AnimationCurve driftPowerModifier;
    [SerializeField] private WheelFriction[] driftWheelFriction = new WheelFriction[4];
    
    [SerializeField] private float driftingAxis;
    
    private WheelFrictionCurve[] _racingWheelFrictionCurves = new WheelFrictionCurve[8];
    private WheelFrictionCurve[] _driftingWheelFrictionCurves = new WheelFrictionCurve[8];

    [Space(10)]
    #endregion

    #region VFX
    [Space(10)]
    [Header("VFX")]
    
    [SerializeField] private float slipAllowance = 0.4f;

    [Space(10)]

    #endregion

    private Gamepad _currentGamepad;

    [SerializeField] private Vector2 motorSpeed;
    [SerializeField] private float motorSpeedMultiplier;

    [SerializeField] private float gearChangeRumbleMultiplier = 0.2f;

    [SerializeField] private float wheelSlipRumbleMultiplier = 0.1f;
    
    //[SerializeField] private float slipAngle;

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

    public void ClutchInput(InputAction.CallbackContext context)
    {
        clutchInput = context.ReadValueAsButton();
    }

    #endregion

    private void OnEnable()
    {
        carRigidbody.centerOfMass = centerOfMass.localPosition;
        carRigidbody.mass = carMass;

        _currentGamepad = GetGamepad();

        SetUpFrictionCurves();
    }

    private Gamepad GetGamepad()
    {
        return Gamepad.current;
    }

    private void SetUpFrictionCurves()
    {
        for (var i = 0; i < NumberOfWheels; i++)
        {
            //forward friction
            _racingWheelFrictionCurves[i] = new WheelFrictionCurve
            {
                extremumSlip = wheelColliders[i].forwardFriction.extremumSlip,
                extremumValue = wheelColliders[i].forwardFriction.extremumValue,
                asymptoteSlip = wheelColliders[i].forwardFriction.asymptoteSlip,
                asymptoteValue = wheelColliders[i].forwardFriction.asymptoteValue,
                stiffness = wheelColliders[i].forwardFriction.stiffness
            };
            
            //sideways friction
            _racingWheelFrictionCurves[i + 4] = new WheelFrictionCurve
            {
                extremumSlip = wheelColliders[i].sidewaysFriction.extremumSlip,
                extremumValue = wheelColliders[i].sidewaysFriction.extremumValue,
                asymptoteSlip = wheelColliders[i].sidewaysFriction.asymptoteSlip,
                asymptoteValue = wheelColliders[i].sidewaysFriction.asymptoteValue,
                stiffness = wheelColliders[i].sidewaysFriction.stiffness
            };
            
            //forward friction
            _driftingWheelFrictionCurves[i] = new WheelFrictionCurve
            {
                extremumSlip = driftWheelFriction[i].forwardFrictionExtremumSlip,
                extremumValue = driftWheelFriction[i].forwardFrictionExtremumValue,
                asymptoteSlip = driftWheelFriction[i].forwardFrictionAsymptoteSlip,
                asymptoteValue = driftWheelFriction[i].forwardFrictionAsymptoteValue,
                stiffness = driftWheelFriction[i].forwardFrictionStiffness
            };
            
            //sideways friction
            _driftingWheelFrictionCurves[i + 4] = new WheelFrictionCurve
            {
                extremumSlip = driftWheelFriction[i].sidewaysFrictionExtremumSlip,
                extremumValue = driftWheelFriction[i].sidewaysFrictionExtremumValue,
                asymptoteSlip = driftWheelFriction[i].sidewaysFrictionAsymptoteSlip,
                asymptoteValue = driftWheelFriction[i].sidewaysFrictionAsymptoteValue,
                stiffness = driftWheelFriction[i].sidewaysFrictionStiffness,
            };
        }
    }

    private void Update()
    {
        CalculateSpeed();
        
        UpdateUI();
        
        AutoClutch();
        
        ClearRumble();
        WheelSlipRumble();
        HighRpmRumble();
        GearChangeRumble();
        ApplyRumble();
    }

    private void WheelSlipRumble()
    {
        float[] wheelSlip = new float[2];
        
        WheelHit[] wheelHits = new WheelHit[4];

        for (int i = 2; i < NumberOfWheels; i++)
        {
            wheelColliders[i].GetGroundHit(out wheelHits[i]);

            wheelSlip[i - 2] = Mathf.Abs(wheelHits[i].sidewaysSlip) + Mathf.Abs(wheelHits[i].forwardSlip) - slipAllowance;
            wheelSlip[i - 2] = Mathf.Clamp01(wheelSlip[i - 2]);
            wheelSlip[i -2] *= wheelSlipRumbleMultiplier;
        }

        motorSpeed.x += wheelSlip[0];
        motorSpeed.y += wheelSlip[1];
    }
    
    private void HighRpmRumble()
    {
        var highRpmRumbleAmount = (rpm / upShiftRpm) - 1;
        highRpmRumbleAmount = Mathf.Clamp01(highRpmRumbleAmount);

        highRpmRumbleAmount *= motorSpeedMultiplier;

        motorSpeed.x += highRpmRumbleAmount;
    }

    private void GearChangeRumble()
    {
        var gearChangeRumbleAmount = 0f;
        if (gearState == GearState.ChangingGear)
        {
            gearChangeRumbleAmount = 1f * gearChangeRumbleMultiplier;
        }

        motorSpeed.y += gearChangeRumbleAmount;
    }

    private void ClearRumble()
    {
        motorSpeed = new Vector2(0f, 0f);
    }

    private void ApplyRumble()
    {
        _currentGamepad.SetMotorSpeeds(motorSpeed.x, motorSpeed.y);
    }
    
    public float GetSpeedRatio()
    {
        var gas = Mathf.Clamp(Mathf.Abs(throttleInput), 0.5f, 1f);
        return rpm * gas / redLineRpm;
    }

    private void AutoClutch()
    {
        if (gearState != GearState.ChangingGear)
        {
            clutch = clutchInput ? 0f : Mathf.Lerp(clutch, 1, Time.deltaTime * clutchSpeed);
        }
        else if (gearState == GearState.ChangingGear)
        {
            clutch = 0f;
        }
    }

    private void UpdateUI()
    {
        speedText.text = currentSpeed.ToString("000") + " KM/h";
        rpmText.text = rpm.ToString("0000");
        gearText.text = (currentGear + 1).ToString("0");
    }

    private void CalculateSpeed()
    {
        currentSpeed = carRigidbody.velocity.magnitude * 3.6f;
    }

    private void FixedUpdate()
    {
        ApplySteering();
        ApplyThrottle();
        ApplyBrake();

        if (handBrakeInput)
        {
            Drift();
        }
        else
        {
            RecoverTraction();
        }
        
        
        ApplyWheelColliderPosition();
        
        PlayTireSmoke();
    }

    private void Drift()
    {
        driftingAxis += driftSpeed * Time.deltaTime;
        
        var localVelocityX = transform.InverseTransformDirection(carRigidbody.velocity).x;

        if (driftingAxis > 1f)
        {
            driftingAxis = 1f;
        }

        if (driftingAxis > 0f)
        {
            LerpFrictionCurves(_driftingWheelFrictionCurves);
        }
    }

    private void RecoverTraction()
    {
        driftingAxis -= driftSpeed * Time.deltaTime;

        if (driftingAxis < 0f)
        {
            driftingAxis = 0f;
        }

        if (driftingAxis >= 0f)
        {
            LerpFrictionCurves(_racingWheelFrictionCurves);
        }
    }

    private void LerpFrictionCurves(WheelFrictionCurve[] targetFrictionCurves)
    {
        for (var i = 0; i < NumberOfWheels; i++)
        {
            var forwardFrictionExtremumSlip = Mathf.Lerp(
            wheelColliders[i].forwardFriction.extremumSlip,
            targetFrictionCurves[i].extremumSlip,
            driftingCurve.Evaluate(driftingAxis)
            );
            var forwardFrictionExtremumValue = Mathf.Lerp(
            wheelColliders[i].forwardFriction.extremumValue,
            targetFrictionCurves[i].extremumValue,
            driftingCurve.Evaluate(driftingAxis)
            );
            var forwardFrictionAsymptoteSlip = Mathf.Lerp(
            wheelColliders[i].forwardFriction.asymptoteSlip,
            targetFrictionCurves[i].asymptoteSlip,
            driftingCurve.Evaluate(driftingAxis)
            );
            var forwardFrictionAsymptoteValue = Mathf.Lerp(
            wheelColliders[i].forwardFriction.asymptoteValue,
            targetFrictionCurves[i].asymptoteValue,
            driftingCurve.Evaluate(driftingAxis)
            );
            var forwardFrictionStiffness = Mathf.Lerp(
            wheelColliders[i].forwardFriction.stiffness,
            targetFrictionCurves[i].stiffness,
            driftingCurve.Evaluate(driftingAxis)
            );
        
            var sidewaysFrictionExtremumSlip = Mathf.Lerp(
            wheelColliders[i].sidewaysFriction.extremumSlip,
            targetFrictionCurves[i + 4].extremumSlip,
            driftingCurve.Evaluate(driftingAxis)
            );
            var sidewaysFrictionExtremumValue = Mathf.Lerp(
            wheelColliders[i].sidewaysFriction.extremumValue,
            targetFrictionCurves[i + 4].extremumValue,
            driftingCurve.Evaluate(driftingAxis)
            );
            var sidewaysFrictionAsymptoteSlip = Mathf.Lerp(
            wheelColliders[i].sidewaysFriction.asymptoteSlip,
            targetFrictionCurves[i + 4].asymptoteSlip,
            driftingCurve.Evaluate(driftingAxis)
            );
            var sidewaysFrictionAsymptoteValue = Mathf.Lerp(
            wheelColliders[i].sidewaysFriction.asymptoteValue,
            targetFrictionCurves[i + 4].asymptoteValue,
            driftingCurve.Evaluate(driftingAxis)
            );
            var sidewaysFrictionStiffness = Mathf.Lerp(
            wheelColliders[i].sidewaysFriction.stiffness,
            targetFrictionCurves[i + 4].stiffness,
            driftingCurve.Evaluate(driftingAxis)
            );

            WheelFrictionCurve lerpForwardFrictionCurve = new WheelFrictionCurve
            {
                extremumSlip = forwardFrictionExtremumSlip,
                extremumValue = forwardFrictionExtremumValue,
                asymptoteSlip = forwardFrictionAsymptoteSlip,
                asymptoteValue = forwardFrictionAsymptoteValue,
                stiffness = forwardFrictionStiffness
            };
            
            WheelFrictionCurve lerpSidewaysFrictionCurve = new WheelFrictionCurve
            {
                extremumSlip = sidewaysFrictionExtremumSlip,
                extremumValue = sidewaysFrictionExtremumValue,
                asymptoteSlip = sidewaysFrictionAsymptoteSlip,
                asymptoteValue = sidewaysFrictionAsymptoteValue,
                stiffness = sidewaysFrictionStiffness
            };

            wheelColliders[i].forwardFriction = lerpForwardFrictionCurve;
            wheelColliders[i].sidewaysFriction = lerpSidewaysFrictionCurve;
        }
    }

    private void ApplySteering()
    {
        var targetSteeringAngle = turnInput * maxSteeringAngle * steeringCurve.Evaluate(currentSpeed);
        
        //drift steering code
        Vector3 forwardDirection = transform.forward;
        targetSteeringAngle += driftingAxis * (Vector3.SignedAngle(forwardDirection, carRigidbody.velocity + forwardDirection, Vector3.up));
        targetSteeringAngle = Mathf.Clamp(targetSteeringAngle, -70f, 70f);


        for (var i = 0; i < 2; i++)
        {
            wheelColliders[i].steerAngle = Mathf.Lerp(wheelColliders[i].steerAngle, targetSteeringAngle, steeringSpeed);
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
        //slipAngle = Vector3.Angle(transform.forward, carRigidbody.velocity - transform.forward);

        CheckGearChange();
        
        currentTorque = CalculateTorque();
        
        var targetMotorTorque = (currentTorque * throttleInput) * driftPowerModifier.Evaluate(driftingAxis);

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

    private float CalculateTorque()
    {
        var torque = 0f;
        if (clutch < 0.1f)
        {
            rpm = Mathf.Lerp(rpm, Mathf.Max(idleRpm, redLineRpm * throttleInput) + Random.Range(-50, 50), Time.deltaTime);
        }
        else
        {
            wheelRpm = Mathf.Abs(GetDriveWheelRpm()) * gearRatios[currentGear] * differentialRatio;
            rpm = Mathf.Lerp(rpm, Mathf.Max(idleRpm, wheelRpm), Time.deltaTime * 3f);
            torque = (powerCurve.Evaluate(rpm / redLineRpm) * maxHorsePower / rpm) * gearRatios[currentGear] * differentialRatio * 5252f * clutch;
        }

        return torque;
    }

    private void CheckGearChange()
    {
        if (gearState == GearState.Running && clutch > 0)
        {
            if (rpm > upShiftRpm)
            {
                StartCoroutine(ChangeGear(1));
            }
            else if (rpm < downShiftRpm)
            {
                StartCoroutine(ChangeGear(-1));
            }
        }
    }

    private float GetDriveWheelRpm()
    {
        var totalWheelRpm = 0f;
        var driveWheels = 0f;
        switch (driveTrain)
        {
            case DriveTrain.AllWheelDrive:
                for (int i = 0; i < NumberOfWheels; i++)
                {
                    totalWheelRpm += wheelColliders[i].rpm;
                }
                driveWheels = 4;
                break;
            case DriveTrain.FrontWheelDrive:
                for (int i = 0; i < 2; i++)
                {
                    totalWheelRpm += wheelColliders[i].rpm;
                }
                driveWheels = 2;
                break;
            case DriveTrain.RearWheelDrive:
                for (int i = 2; i < NumberOfWheels; i++)
                {
                    totalWheelRpm += wheelColliders[i].rpm;
                }
                driveWheels = 2;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return totalWheelRpm / driveWheels;
    }

    IEnumerator ChangeGear(int gearChange)
    {
        gearState = GearState.CheckingChange;
        if (currentGear + gearChange >= 0)
        {
            if (gearChange > 0)
            {
                yield return new WaitForSeconds(0.2f);
                if (rpm < upShiftRpm || currentGear >= gearRatios.Length - 1)
                {
                    gearState = GearState.Running;
                    yield break;
                }
            }
            if (gearChange < 0)
            {
                yield return new WaitForSeconds(0.2f);
                if (rpm > downShiftRpm || currentGear <= 0)
                {
                    gearState = GearState.Running;
                    yield break;
                }
            }
            gearState = GearState.ChangingGear;
            yield return new WaitForSeconds(gearChangeTime);
            currentGear += gearChange;
        }
        gearState = GearState.Running;
    }

    private void PlayTireSmoke()
    {
        WheelHit[] wheelHits = new WheelHit[4];

        for (int i = 2; i < NumberOfWheels; i++)
        {
            wheelColliders[i].GetGroundHit(out wheelHits[i]);

            if (Mathf.Abs(wheelHits[i].sidewaysSlip) + Mathf.Abs(wheelHits[i].forwardSlip) > slipAllowance)
            {
                wheelParticles[i - 2].Play();
                wheelTrails[i - 2].emitting = true;
            }
            else
            {
                wheelParticles[i - 2].Stop();
                wheelTrails[i - 2].emitting = false;
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
