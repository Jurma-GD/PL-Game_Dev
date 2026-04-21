# Design Document: Player Checkpoint Teleport

## Overview

A `Checkpoint_System` MonoBehaviour is added to the Player GameObject (or a dedicated manager). It records the player's position (and optionally health) either manually via key press or automatically on a timer. A separate teleport key instantly moves the player back to the saved checkpoint, with a configurable cooldown preventing spam.

## Architecture

### New File

`Assets/Scripts/Checkpoint_System.cs` — a single MonoBehaviour that owns all checkpoint/teleport logic.

### Modified Files

- `Assets/Scripts/PlayerHealth.cs` — no structural changes needed; `currentHealth` is already public.
- `Assets/Scripts/PlayerMovement.cs` — no changes needed; `Rigidbody2D rb` is already public.

### Component Relationships

```
Player GameObject
  ├── PlayerMovement      (rb: Rigidbody2D)
  ├── PlayerHealth        (currentHealth: int)
  └── Checkpoint_System   (references PlayerMovement.rb, PlayerHealth)
        └── Checkpoint_Indicator (child/separate GameObject, assigned via Inspector)
```

---

## Data Model

```csharp
// Stored inside Checkpoint_System
private struct CheckpointData
{
    public Vector2 position;
    public int health;          // only meaningful when restoreHealth == true
    public bool isManual;       // true = manual, false = auto
}
```

---

## Checkpoint_System Inspector Fields

| Field | Type | Description |
|---|---|---|
| `playerRb` | `Rigidbody2D` | Reference to the player's Rigidbody2D |
| `playerHealth` | `PlayerHealth` | Reference to the PlayerHealth component |
| `checkpointIndicator` | `GameObject` | Visual marker shown at checkpoint position |
| `checkpointKey` | `string` | Input key name for placing a checkpoint |
| `teleportKey` | `string` | Input key name for activating teleport |
| `autoCheckpointInterval` | `float` | Seconds between auto-checkpoints (N) |
| `teleportCooldown` | `float` | Seconds of cooldown after teleport (D) |
| `restoreHealth` | `bool` | Whether to restore health on teleport |

---

## Core Logic

### SaveCheckpoint(bool isManual)

```
1. Read playerRb.position → checkpointData.position
2. If restoreHealth: read playerHealth.currentHealth → checkpointData.health
3. Set checkpointData.isManual = isManual
4. Set hasCheckpoint = true
5. Move checkpointIndicator to checkpointData.position, set active = true
```

### TryTeleport()

```
1. If !hasCheckpoint → return (Req 3.5)
2. If cooldownRemaining > 0 → return (Req 3.4 / 3.6)
3. Set playerRb.position = checkpointData.position
4. Set playerRb.velocity = Vector2.zero (Req 3.2)
5. If restoreHealth: playerHealth.currentHealth = checkpointData.health (Req 4.2)
6. Set cooldownRemaining = teleportCooldown
7. Hide checkpointIndicator (Req 5.2)
```

### Update() / FixedUpdate()

```
Update():
  - Try parse checkpointKey → if invalid, log error (Req 6.3)
  - If Input.GetKeyDown(checkpointKey) → SaveCheckpoint(isManual: true)
  - Try parse teleportKey → if invalid, log error (Req 6.3)
  - If Input.GetKeyDown(teleportKey) → TryTeleport()
  - If cooldownRemaining > 0: cooldownRemaining -= Time.deltaTime

Auto-checkpoint coroutine (started in Start()):
  - Loop: WaitForSeconds(autoCheckpointInterval)
  - If !checkpointData.isManual (no recent manual): SaveCheckpoint(isManual: false) (Req 2.2)
  - Coroutine is stopped/started based on Time.timeScale or player active state (Req 2.3)
```

---

## Input Validation

`Input.GetKeyDown(string)` throws `ArgumentException` for invalid key names. Wrap in try/catch and log a descriptive error (Req 6.3).

---

## Correctness Properties

### Property 1: Checkpoint Overwrite Consistency
For any sequence of SaveCheckpoint calls, `checkpointData` always reflects the most recent call. No stale data persists.
- Validates: Requirements 1.2, 2.2

### Property 2: Teleport Position Accuracy
After TryTeleport(), `playerRb.position` equals the `checkpointData.position` that was stored at save time, regardless of how far the player moved between save and teleport.
- Validates: Requirements 3.1

### Property 3: Cooldown Monotonicity
`cooldownRemaining` only decreases over time (via `Time.deltaTime`) and never resets to a positive value except immediately after a successful teleport.
- Validates: Requirements 3.3, 3.4

### Property 4: Health Restoration Toggle Consistency
For any checkpoint/teleport cycle, if `restoreHealth == false`, `playerHealth.currentHealth` after teleport equals `playerHealth.currentHealth` before teleport (unchanged).
- Validates: Requirements 4.3

### Property 5: Indicator Visibility Invariant
`checkpointIndicator.activeSelf == true` if and only if `hasCheckpoint == true` AND no teleport has occurred since the last save.
- Validates: Requirements 5.1, 5.2, 5.3

### Property 6: No-Checkpoint Teleport Safety
If `hasCheckpoint == false`, calling TryTeleport() any number of times leaves `playerRb.position` unchanged.
- Validates: Requirements 3.5
