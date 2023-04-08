using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineAudio : MonoBehaviour
{
    [SerializeField] private AudioSource runningSound;
    [SerializeField] private float runningMaxVolume;
    [SerializeField] private float runningMaxPitch;
    [SerializeField] private AudioSource reverseSound;
    [SerializeField] private float reverseMaxVolume;
    [SerializeField] private float reverseMaxPitch;
    [SerializeField] private AudioSource idleSound;
    [SerializeField] private float idleMaxVolume;
    [SerializeField] private float speedRatio;
    [SerializeField] private float revLimiter;
    [SerializeField] private float LimiterSound = 1f;
    [SerializeField] private float LimiterFrequency = 3f;
    [SerializeField] private float LimiterEngage = 0.8f;
    [SerializeField] private bool isEngineRunning = false;

    [SerializeField] private AudioSource startingSound;


    private AdvancedCarController carController;
    // Start is called before the first frame update
    void Start()
    {
        carController = GetComponent<AdvancedCarController>();
        idleSound.volume = 0;
        runningSound.volume = 0;
        reverseSound.volume = 0;
    }

    // Update is called once per frame
    void Update()
    {
        float speedSign=0;
        if (carController)
        {
            speedSign = Mathf.Sign(carController.GetSpeedRatio());
            speedRatio = Mathf.Abs(carController.GetSpeedRatio());
        }
        if (speedRatio > LimiterEngage)
        {
            revLimiter = (Mathf.Sin(Time.time * LimiterFrequency) + 1f) * LimiterSound * (speedRatio - LimiterEngage);
        }
        if (isEngineRunning)
        {
            idleSound.volume = Mathf.Lerp(0.1f, idleMaxVolume, speedRatio);
            if (speedSign > 0)
            {
                reverseSound.volume = 0;
                runningSound.volume = Mathf.Lerp(0.3f, runningMaxVolume, speedRatio);
                runningSound.pitch = Mathf.Lerp(0.3f, runningMaxPitch, speedRatio);
            }
            else
            {
                runningSound.volume = 0;
                reverseSound.volume = Mathf.Lerp(0f, reverseMaxVolume, speedRatio);
                reverseSound.pitch = Mathf.Lerp(0.2f, reverseMaxPitch, speedRatio);
            }
        }
        else {
            idleSound.volume = 0;
            runningSound.volume = 0;
        }
    }
}