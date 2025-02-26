using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdFlocking : MonoBehaviour
{
    public float speed = 3f;
    public float turnSpeed = 2f;
    public float neighborRadius = 5f;
    public float avoidanceRadius = 2f;
    public float detectionRange = 3f;
    public LayerMask obstacleLayer;
    public float updateInterval = 1f;

    private Vector3 targetDirection;
    private Coroutine runningRoutine;
    private void OnEnable()
    {
        if (BirdController.allBirds.Count > 0 && BirdController.allBirds[0] == this)
            runningRoutine = StartCoroutine(EnableFlockingAfterDelay());
        else
        {
            targetDirection = Random.insideUnitSphere.normalized;
            StartCoroutine(UpdateFlockingRoutine());
        }
    }

    private IEnumerator EnableFlockingAfterDelay()
    {
        targetDirection = Random.insideUnitSphere.normalized;
        yield return new WaitForSeconds(5f);
        StartCoroutine(UpdateFlockingRoutine());
    }

    private void OnDisable() => StopAllCoroutines();

    private void Update()
    {
        transform.position += speed * Time.deltaTime * targetDirection;

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    private IEnumerator UpdateFlockingRoutine()
    {
        if (runningRoutine != null)
            StopCoroutine(runningRoutine);
        while (true)
        {
            UpdateFlocking();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void UpdateFlocking()
    {
        Vector3 separation = ComputeSeparation();
        Vector3 alignment = ComputeAlignment();
        Vector3 cohesion = ComputeCohesion();
        Vector3 avoidance = ComputeAvoidance();
        Vector3 randomDrift = Random.insideUnitSphere * 0.1f;

        targetDirection = (separation + alignment + cohesion + avoidance + randomDrift).normalized;

    }

    private Vector3 ComputeSeparation()
    {
        Vector3 separationForce = Vector3.zero;
        int neighborCount = 0;

        foreach (var bird in BirdController.allBirds)
        {
            if (bird == this) continue;

            float distance = Vector3.Distance(transform.position, bird.transform.position);
            if (distance < avoidanceRadius)
            {
                separationForce += (transform.position - bird.transform.position) / distance;
                neighborCount++;
            }
        }

        return neighborCount > 0 ? separationForce / neighborCount : Vector3.zero;
    }

    private Vector3 ComputeAlignment()
    {
        Vector3 alignmentForce = Vector3.zero;
        int neighborCount = 0;

        foreach (var bird in BirdController.allBirds)
        {
            if (bird == this) continue;

            float distance = Vector3.Distance(transform.position, bird.transform.position);
            if (distance < neighborRadius)
            {
                alignmentForce += bird.targetDirection;
                neighborCount++;
            }
        }

        return neighborCount > 0 ? alignmentForce / neighborCount : Vector3.zero;
    }

    private Vector3 ComputeCohesion()
    {
        Vector3 cohesionForce = Vector3.zero;
        int neighborCount = 0;

        foreach (var bird in BirdController.allBirds)
        {
            if (bird == this) continue;

            float distance = Vector3.Distance(transform.position, bird.transform.position);
            if (distance < neighborRadius)
            {
                cohesionForce += bird.transform.position;
                neighborCount++;
            }
        }

        return neighborCount > 0 ? ((cohesionForce / neighborCount) - transform.position).normalized : Vector3.zero;
    }

    private Vector3 ComputeAvoidance()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, detectionRange, obstacleLayer))
            return Vector3.Reflect(transform.forward, hit.normal);

        return Vector3.zero;
    }
}
