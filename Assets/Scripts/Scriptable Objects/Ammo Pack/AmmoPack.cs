using UnityEngine;

[CreateAssetMenu(fileName = "Ammo Pack", menuName = "Scriptable/AmmoPack")]
public class AmmoPack : ScriptableObject
{
    public AmmoClass ammoClass;
    public int ammoAmountInPack;
}
