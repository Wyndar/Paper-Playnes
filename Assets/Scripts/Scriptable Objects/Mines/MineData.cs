using UnityEngine;

[CreateAssetMenu(fileName = "NewMineType", menuName = "Scriptable/Mine Type")]
public class MineData : ScriptableObject
{
    public string mineName;
    public float explosionRadius;
    public int damage;
    public GameObject explosionEffect;
    public AudioClip explosionSound;
    public float knockbackForce = 50f;
    public float shockwaveDuration = 0.5f;
    public float detonationTime;
#pragma warning disable IDE0044
    private static Collider[] hitObjects = new Collider[10];
#pragma warning restore IDE0044
}
