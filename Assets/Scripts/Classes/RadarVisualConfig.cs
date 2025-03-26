using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RadarVisualConfig : MonoBehaviour
{
    public Team team;
    public GameObject blipPrefab;
    public GameObject arrowPrefab;
    public List<GameObject> blipPool = new();
    public List<GameObject> arrowPool = new();
    public int activeBlips = 0;
    public int activeArrows = 0;
}
