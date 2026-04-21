# Requirements Document

## Introduction

This feature adds a checkpoint and teleport (rewind) system to the 2D Unity game. The player can manually place a checkpoint at their current position, or the system can automatically record one at set intervals. At any time, the player can activate a teleport to instantly return to the last saved checkpoint — restoring their position and optionally their health state. This creates a "go back in time" mechanic that adds strategic depth and recovery options during gameplay.

## Glossary

- **Checkpoint_System**: The MonoBehaviour component responsible for recording, storing, and restoring player checkpoint data.
- **Player**: The GameObject controlled by the user, using `PlayerMovement`, `PlayerHealth`, and `Player_Combat` components.
- **Checkpoint**: A saved snapshot of the Player's position (and optionally health) at a specific moment in time.
- **Manual_Checkpoint**: A Checkpoint explicitly placed by the player via a key press.
- **Auto_Checkpoint**: A Checkpoint recorded automatically by the Checkpoint_System at a fixed time interval.
- **Teleport**: The act of instantly moving the Player back to the most recently saved Checkpoint position.
- **Cooldown**: A mandatory wait period after a Teleport before another Teleport can be activated.
- **Checkpoint_Indicator**: A visual marker rendered in the game world at the saved Checkpoint position.

---

## Requirements

### Requirement 1: Manual Checkpoint Placement

**User Story:** As a player, I want to manually place a checkpoint at my current position, so that I can mark a safe spot to return to during dangerous situations.

#### Acceptance Criteria

1. WHEN the player presses the designated checkpoint key, THE Checkpoint_System SHALL save the Player's current `Transform.position` as the active Checkpoint.
2. WHEN a Manual_Checkpoint is saved, THE Checkpoint_System SHALL overwrite any previously stored Checkpoint with the new position.
3. WHEN a Manual_Checkpoint is saved, THE Checkpoint_System SHALL display a Checkpoint_Indicator at the saved position.
4. IF the player presses the checkpoint key while a Teleport Cooldown is active, THEN THE Checkpoint_System SHALL still save the new Checkpoint position.

---

### Requirement 2: Automatic Checkpoint Recording

**User Story:** As a player, I want the game to automatically record my position at regular intervals, so that I always have a recent fallback point even if I forget to place one manually.

#### Acceptance Criteria

1. THE Checkpoint_System SHALL record an Auto_Checkpoint of the Player's `Transform.position` every N seconds, where N is a designer-configurable value exposed in the Unity Inspector.
2. WHEN an Auto_Checkpoint is recorded, THE Checkpoint_System SHALL overwrite the previously stored Checkpoint only if no Manual_Checkpoint was placed more recently.
3. WHILE the Player is inactive or the game is paused, THE Checkpoint_System SHALL suspend the Auto_Checkpoint recording interval.

---

### Requirement 3: Teleport to Checkpoint

**User Story:** As a player, I want to teleport back to my last checkpoint, so that I can recover from a bad situation or rewind a mistake.

#### Acceptance Criteria

1. WHEN the player presses the designated teleport key and a Checkpoint exists, THE Checkpoint_System SHALL set the Player's `Rigidbody2D.position` to the stored Checkpoint position.
2. WHEN a Teleport is activated, THE Checkpoint_System SHALL set the Player's `Rigidbody2D.velocity` to `Vector2.zero`.
3. WHEN a Teleport is activated, THE Checkpoint_System SHALL begin a Cooldown period of D seconds, where D is a designer-configurable value exposed in the Unity Inspector.
4. WHILE a Cooldown is active, THE Checkpoint_System SHALL prevent the player from activating another Teleport.
5. IF the player presses the teleport key and no Checkpoint has been saved, THEN THE Checkpoint_System SHALL take no action.
6. IF the player presses the teleport key while a Cooldown is active, THEN THE Checkpoint_System SHALL take no action.

---

### Requirement 4: Health Restoration on Teleport

**User Story:** As a player, I want the option to restore my health to what it was when I set the checkpoint, so that teleporting feels like a true rewind.

#### Acceptance Criteria

1. WHERE health restoration is enabled, THE Checkpoint_System SHALL save the Player's `PlayerHealth.currentHealth` value alongside the Checkpoint position.
2. WHERE health restoration is enabled, WHEN a Teleport is activated, THE Checkpoint_System SHALL restore the Player's `PlayerHealth.currentHealth` to the value saved at the Checkpoint.
3. WHERE health restoration is disabled, WHEN a Teleport is activated, THE Checkpoint_System SHALL leave the Player's `PlayerHealth.currentHealth` unchanged.
4. THE Checkpoint_System SHALL expose a boolean toggle for health restoration in the Unity Inspector.

---

### Requirement 5: Checkpoint Indicator Visibility

**User Story:** As a player, I want to see where my checkpoint is placed in the world, so that I know where I will be teleported to.

#### Acceptance Criteria

1. WHEN a Checkpoint is saved, THE Checkpoint_System SHALL move the Checkpoint_Indicator to the saved Checkpoint position and make it visible.
2. WHEN a Teleport is activated, THE Checkpoint_System SHALL hide the Checkpoint_Indicator.
3. IF no Checkpoint has been saved since the scene loaded, THE Checkpoint_System SHALL keep the Checkpoint_Indicator hidden.
4. THE Checkpoint_System SHALL accept a designer-assigned Checkpoint_Indicator GameObject reference via the Unity Inspector.

---

### Requirement 6: Input Configuration

**User Story:** As a developer, I want checkpoint and teleport inputs to be configurable, so that they can be remapped without modifying code.

#### Acceptance Criteria

1. THE Checkpoint_System SHALL read the checkpoint placement input from a string key name exposed in the Unity Inspector.
2. THE Checkpoint_System SHALL read the teleport activation input from a string key name exposed in the Unity Inspector.
3. WHEN the configured key name does not correspond to a valid Unity Input axis or key, THE Checkpoint_System SHALL log a descriptive error message to the Unity Console and take no action.
