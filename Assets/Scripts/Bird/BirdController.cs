using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdController : MonoBehaviour
{
    public BirdAI birdAI;
    public BirdFlocking birdFlocking;
    public float flockingActivationRadius = 10f;
    public static List<BirdFlocking> allBirds = new();

    private void Start()
    {
        if (allBirds.Count == 0)
            allBirds = new List<BirdFlocking>(FindObjectsByType<BirdFlocking>(FindObjectsSortMode.None));
        birdAI = GetComponent<BirdAI>();
        birdFlocking=GetComponent<BirdFlocking>();
        birdAI.enabled = true;
        birdFlocking.enabled = false;

        StartCoroutine(CheckForFlockmates());
    }

    private IEnumerator CheckForFlockmates()
    {
        while (true)
        {
            bool shouldFlock = ShouldEnableFlocking();
            birdFlocking.enabled = shouldFlock;
            birdAI.enabled = !shouldFlock;

            yield return new WaitForSeconds(1f);
        }
    }

    private bool ShouldEnableFlocking()
    {
        foreach (var bird in allBirds)
        {
            if (bird == this) continue;

            float distance = Vector3.Distance(transform.position, bird.transform.position);
            if (distance < flockingActivationRadius)
            {
                return true;
            }
        }
        return false;
    }
}
