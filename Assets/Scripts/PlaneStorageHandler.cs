using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class PlaneStorageHandler : MonoBehaviour
{
    public GameEvent weaponAmmoUpdateEvent;
    public GameEvent itemAmmoUpdateEvent;
    public GameEvent updateSelectedWeapon;
    public GameEvent updateSelectedItem;

    [Header("Ammunition and Items UI")]
    public TMP_Text weaponAmmoInMagText;
    public TMP_Text weaponMaxAmmoInMagText;
    public TMP_Text weaponAmmoInHoldText;
    public TMP_Text itemCountText;
    public Image selectedWeaponImage;
    public Image selectedItemImage;

    public int maxStorageSpace = 100;
    public int storageSpaceUsed;
    public List<AmmoPack> ammoPackData = new();
    public Dictionary<AmmoClass, int> ammoInStorage = new();
    public List<Sprite> weaponImages = new();

    private Weapon selectedWeapon;
    private void OnEnable()
    {
        weaponAmmoUpdateEvent.OnStatEventRaised += UpdateWeaponAmmo;
        itemAmmoUpdateEvent.OnStatEventRaised += UpdateItemText;
        updateSelectedWeapon.OnWeaponEventRaised += UpdateSelectedWeapon;
    }
    private void OnDisable()
    {
        weaponAmmoUpdateEvent.OnStatEventRaised -= UpdateWeaponAmmo;
        itemAmmoUpdateEvent.OnStatEventRaised -= UpdateItemText;
        updateSelectedWeapon.OnWeaponEventRaised -= UpdateSelectedWeapon;
    }
    public void Initialize()
    {
        weaponImages = new(Resources.LoadAll<Sprite>("Sprites/Weapons").ToList());
        foreach (AmmoClass ammoClass in from AmmoClass ammoClass in Enum.GetValues(typeof(AmmoClass))
                                  where ammoClass != AmmoClass.Undefined
                                  select ammoClass)
        {
            ammoInStorage[ammoClass] = 0;
        }
    }
    private void UpdateAmmoInStorage(AmmoClass ammoClass, int changeAmount)
    {
        //visually, the storage box has 5 slots representing 20 storage space each
        //we need to calculate how much space is used by the ammo packs and how much change we're attempting
        //before we add or remove ammo to ensure we don't go over the limit
        if (ammoClass == AmmoClass.Undefined || changeAmount == 0)
            return;

        // Find the standard pack size for this ammo class, we do NOT want a divide by zero now, do we?
        int packSize = ammoPackData.Find(ammoPack => ammoPack.ammoClass == ammoClass).ammoAmountInPack;
        if (packSize <= 0)
            throw new UnassignedReferenceException($"Pack size for {ammoClass} is not set or invalid.");

        int currentAmmoCount = ammoInStorage[ammoClass];
        int currentPacksCount = Mathf.CeilToInt((float)currentAmmoCount / packSize);
        int newAmmoCount = Mathf.Max(currentAmmoCount + changeAmount, 0);
        int newPacksCount = Mathf.CeilToInt((float)newAmmoCount / packSize);
        int newTotalStorage = storageSpaceUsed + (currentPacksCount - newPacksCount) * 20;
        if (newTotalStorage <= maxStorageSpace)
        {
            ammoInStorage[ammoClass] = newAmmoCount;
            storageSpaceUsed = newTotalStorage;
        }
        else
            Debug.Log("Not enough storage space to add ammo.");
    }
    public void UpdateMaxStorage(int newMaxStorage) => maxStorageSpace = newMaxStorage;
    public int GetAmmoInStorageCount(AmmoClass ammoClass)
    {
        if (ammoClass == AmmoClass.Undefined)
            throw new ArgumentException("Ammo class cannot be undefined.");
        if (ammoInStorage.TryGetValue(ammoClass, out int ammoCount))
            return ammoCount;
        Debug.LogWarning($"Ammo class {ammoClass} not found in storage.");
        return 0;
    }
    public AmmoPack GetAmmoPackData(AmmoClass ammoClass)
    {
        if (ammoClass == AmmoClass.Undefined)
            throw new ArgumentException("Ammo class cannot be undefined.");
        return ammoPackData.Find(ammoPackData => ammoPackData.ammoClass == ammoClass);
    }
    public void UpdateWeaponAmmo(int ammoChangeAmount, int ammoLeftInMag)
    {
        UpdateAmmoInStorage(selectedWeapon.ammoClass, ammoChangeAmount);
        weaponAmmoInMagText.text = ammoLeftInMag > 0 ? ammoLeftInMag.ToString() : selectedWeapon.ammoInCurrentMagCount.ToString();
        weaponAmmoInHoldText.text = GetAmmoInStorageCount(selectedWeapon.ammoClass).ToString();
        Debug.Log($"{ammoChangeAmount} {ammoLeftInMag} {selectedWeapon.ammoInCurrentMagCount}");
    }

    private void UpdateItemText(int currentMagCount, int magCount) => itemCountText.text = currentMagCount.ToString();
    private void UpdateSelectedWeapon(Weapon weapon)
    {
        if (weapon == null) return;
        selectedWeapon = weapon;
        selectedWeaponImage.sprite = weaponImages.Find(s => weapon.name.Contains(s.name));
        selectedWeaponImage.gameObject.SetActive(selectedWeaponImage.sprite != null);
        weaponAmmoInMagText.gameObject.SetActive(selectedWeaponImage.sprite != null);
        weaponMaxAmmoInMagText.gameObject.SetActive(selectedWeaponImage.sprite != null);
        weaponMaxAmmoInMagText.text = weapon.maxAmmoInMag.ToString();
        weaponAmmoInHoldText.gameObject.SetActive(selectedWeaponImage.sprite != null);
        UpdateWeaponAmmo(0, weapon.ammoInCurrentMagCount);
    }
}
