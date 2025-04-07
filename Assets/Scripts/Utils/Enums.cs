//always add new enums after not before, can break enum order in inspector
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
    BlueTeam,
    GreenTeam,
    YellowTeam
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
    Toggle,
    GameObject,
    StatUpdate,
    TeamStatUpdate,
    HealthModified,
    Location,
    Weapon
}
public enum AutoLevelMode { Off, On }

public enum PlaneState
{
    Flight,
    Boost,
    ADS,
    BarrelRoll
}

public enum AmmoClass
{
    Undefined,
    LowCaliber,
    MediumCaliber,
    HighCaliber,
    Shotgun,
    PureEnergy,
    Rocket
}