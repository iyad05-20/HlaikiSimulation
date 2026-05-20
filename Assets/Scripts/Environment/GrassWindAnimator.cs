using UnityEngine;
using System.Collections.Generic;

public class GrassWindAnimator : MonoBehaviour
{
    [Header("Wind Settings")]
    public float swaySpeed = 2f;
    public float swayAmount = 5f;
    public float phaseOffset = 0f;

    private Quaternion initialRotation;

    private void Start()
    {
        initialRotation = transform.localRotation;
        // Randomize phase so they don't all move identically
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        // Calculate a gentle sine wave based on time
        float angle = Mathf.Sin(Time.time * swaySpeed + phaseOffset) * swayAmount;
        
        // Apply the rotation (assuming the grass grows along the Y axis and sways on X/Z)
        // We sway it around the X axis as a simple wind effect
        transform.localRotation = initialRotation * Quaternion.Euler(angle, 0, angle * 0.5f);
    }
}
