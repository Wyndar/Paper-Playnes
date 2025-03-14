public enum LoadingMode 
{
    Local,
    Network
}
public enum HUDMarkerType 
{
    Undefined, 
    Damageable, 
    PickUp
}
public enum Team
{
    Undefined,
    RedTeam,
    BlueTeam
}
public enum PickUpType
{
    Undefined,
    Ammo,
    HP,
    Overshield
}
public enum HealthModificationType
{
    Damage,
    Heal,
    MaxHPIncrease,
    MaxHPDecrease
}
public enum GameEventType
{
    Undefined,
    NoParams,
    GameObject,
    StatUpdate,
    TeamStatUpdate,
    HealthModified
}