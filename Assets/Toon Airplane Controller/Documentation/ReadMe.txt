Overview
The Toon Airplane Controller is a fully-featured system for creating smooth and exciting aerial gameplay. It includes flight mechanics, combat systems, checkpoint rings, a score tracker, and customizable settings, making it ideal for air races, dogfights, and objective-based missions.

---

 Setup Guide

 1. Importing the Asset
    - Import the Arcade Airplane Controller package into your Unity project.
    - Drag the `AirplaneController` prefab into your scene.

 2. Airplane Setup
    - Attach the `AirplaneController` script to your airplane GameObject.
    - Assign required references in the Inspector:
       - Left and Right Gun Spawn Points: Transform positions for the machine gun firing.
       - Missile Spawn Point: Transform position for missile launching.
       - Crosshairs Canvas: UI element for dynamic targeting feedback.

 3. Checkpoint Rings
    - Use the included CheckpointRing prefab to create race or mission paths.

 4. Scoring System
    - Add the `ScoreManager` to your scene.
    - Use the provided API to update the score when players complete objectives or hit targets.

---

 Features

 Flight Controls
- Intuitive controls for pitch, yaw, and roll.
- Auto-Leveling Mode: Smoothly stabilizes the airplane when enabled.
- Inverted Flight Mode: Toggle inverted controls for vertical input.

 Weapon Systems
- Missiles:
  - Lock onto targets or fire straight ahead if no target is locked.
  - Customizable cooldown, speed, and explosion radius.
  
- Machine Guns:
  - Dual firing positions with alternating fire.
  - Configurable fire rate, bullet speed, and lifetime.

 Boost and Air Brake
    - Boost: Temporary speed increase with a cooldown.
    - Air Brake: Gradual reduction of speed for tighter turns.

 Crosshairs and Targeting
    - Dynamic crosshairs toggle visibility when firing.
    - Color feedback when locking onto targets.

 Checkpoint Rings
    - Easily create race paths with CheckpointRing prefabs.
    - Tracks player progress through rings.

 Scoring System
    - Integrated scoring for checkpoint completion, combat, and objectives.

---

 Customization

 AirplaneController Settings
| Property               | Description                                           |
|----------------------------|-----------------------------------------------------------|
| Speed                  | Base movement speed of the airplane.                      |
| Rotation Speed         | Speed of pitch, yaw, and roll rotation.                   |
| Auto Level Mode        | Enum to enable/disable auto-leveling.                     |
| Boost Multiplier       | Multiplier for speed during boost.                        |
| Boost Duration         | Time boost is active.                                     |
| Brake Multiplier       | Reduction in speed when braking.                          |

 Weapon Settings
| Property               | Description                                           |
|----------------------------|-----------------------------------------------------------|
| Missile Prefab         | Prefab for missiles fired by the airplane.                |
| Missile Spawn Point    | Transform for missile firing position.                    |
| Bullet Prefab          | Prefab for machine gun bullets.                           |
| Gun Spawn Points       | Left and right gun spawn transforms for alternating fire. |
| Fire Rate              | Time between consecutive bullet shots.                    |
| Bullet Speed           | Speed of fired bullets.                                   |

---

 APIs and Event Hooks

 ScoreManager
- Public Methods:
  ```csharp
  public void AddScore(int amount); // Add points to the player's score
  public void ResetScore();         // Reset the score to zero
  public int GetScore();            // Get the current score
  ```

 CheckpointManager
- Events:
  ```csharp
  public Action OnCheckpointPassed; // Triggered when a checkpoint is passed
  ```

---

 Known Issues
1. Bullet Not Moving Forward:
   - Ensure the bullet prefab has a `Rigidbody` with `Use Gravity` disabled.
   - Verify the `Gun Spawn Points` are correctly oriented (Z-axis forward).

2. Missile Lock-On Not Working:
   - Ensure targets have the correct layer assigned (`Target Layer` in the Inspector).

3. Crosshairs Not Visible:
   - Confirm the `Crosshairs Canvas` is assigned and set to active.

---

 Support
For any issues, feature requests, or customization help, contact us at:

Author Information:
- Author: Golem Kin
- Version: 1.0.0
- Date: October 2024
- Contact: support@golemkin.com
