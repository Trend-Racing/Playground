using UnityEngine;

[System.Serializable]
public class WheelFriction
{
    [Header("Forward Friction")]
    [Space(5)]
    
    public float forwardFrictionExtremumSlip;
    public float forwardFrictionExtremumValue;
    
    [Space(5)]
    
    public float forwardFrictionAsymptoteSlip;
    public float forwardFrictionAsymptoteValue;
    
    [Space(5)]
    
    public float forwardFrictionStiffness;
    
    [Space(10)]
    
    [Header("Sideways Friction")]
    [Space(5)]
    
    public float sidewaysFrictionExtremumSlip;
    public float sidewaysFrictionExtremumValue;
    
    [Space(5)]
    
    public float sidewaysFrictionAsymptoteSlip;
    public float sidewaysFrictionAsymptoteValue;
    
    [Space(5)]
    
    public float sidewaysFrictionStiffness;
}
