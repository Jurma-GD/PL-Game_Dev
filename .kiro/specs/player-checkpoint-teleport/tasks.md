# Implementation Plan: Player Checkpoint Teleport

## Overview

Implement the `Checkpoint_System` MonoBehaviour in C# for the existing Unity 2D project. The system records the player's position/health as a checkpoint (manually or on a timer) and teleports the player back on demand, with a configurable cooldown and optional health restoration.

## Tasks

- [x] 1. Create the Checkpoint_System script with Inspector fields and data model
  - Create `Assets/Scripts/Checkpoint_System.cs`
  - Define the `CheckpointData` private struct with `position`, `health`, and `isManual` fields
  - Declare all Inspector-exposed fields: `playerRb`, `playerHealth`, `checkpointIndicator`, `checkpointKey`, `teleportKey`, `autoCheckpointInterval`, `teleportCooldown`, `restoreHealth`
  - Declare private state: `hasCheckpoint`, `cooldownRemaining`, `checkpointData`
  - _Requirements: 1.1, 2.1, 3.3, 4.1, 4.4, 5.4, 6.1, 6.2_

- [x] 2. Implement SaveCheckpoint and indicator logic
  - [x] 2.1 Implement `SaveCheckpoint(bool isManual)` method
    - Read `playerRb.position` into `checkpointData.position`
    - If `restoreHealth`, read `playerHealth.currentHealth` into `checkpointData.health`
    - Set `checkpointData.isManual`, set `hasCheckpoint = true`
    - Move `checkpointIndicator` to checkpoint position and set it active
    - _Requirements: 1.1, 1.2, 1.3, 4.1, 5.1_

  - [ ]* 2.2 Write property test for checkpoint overwrite consistency (Property 1)
    - **Property 1: Checkpoint Overwrite Consistency**
    - **Validates: Requirements 1.2, 2.2**
    - Call `SaveCheckpoint` multiple times with different positions; assert `checkpointData.position` always equals the last saved value

- [x] 3. Implement TryTeleport logic
  - [x] 3.1 Implement `TryTeleport()` method
    - Guard: return if `!hasCheckpoint` (Req 3.5)
    - Guard: return if `cooldownRemaining > 0` (Req 3.4, 3.6)
    - Set `playerRb.position = checkpointData.position` and `playerRb.velocity = Vector2.zero`
    - If `restoreHealth`, restore `playerHealth.currentHealth`
    - Set `cooldownRemaining = teleportCooldown`
    - Hide `checkpointIndicator`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 4.2, 4.3, 5.2_

  - [ ]* 3.2 Write property test for teleport position accuracy (Property 2)
    - **Property 2: Teleport Position Accuracy**
    - **Validates: Requirements 3.1**
    - Save a checkpoint at an arbitrary position, move the player, teleport; assert `playerRb.position == saved position`

  - [ ]* 3.3 Write property test for cooldown monotonicity (Property 3)
    - **Property 3: Cooldown Monotonicity**
    - **Validates: Requirements 3.3, 3.4**
    - After a teleport, simulate time passing; assert `cooldownRemaining` only decreases and a second teleport is blocked while `cooldownRemaining > 0`

  - [ ]* 3.4 Write property test for no-checkpoint teleport safety (Property 6)
    - **Property 6: No-Checkpoint Teleport Safety**
    - **Validates: Requirements 3.5**
    - With `hasCheckpoint == false`, call `TryTeleport()` N times; assert player position is unchanged

- [x] 4. Checkpoint - Ensure SaveCheckpoint and TryTeleport compile and core logic is correct
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement Update loop: manual input and cooldown tick
  - [x] 5.1 Implement `Update()` with input handling and cooldown countdown
    - Wrap `Input.GetKeyDown(checkpointKey)` in try/catch; log descriptive error on `ArgumentException` (Req 6.3)
    - On valid checkpoint key press: call `SaveCheckpoint(isManual: true)` (Req 1.1, 1.4)
    - Wrap `Input.GetKeyDown(teleportKey)` in try/catch; log descriptive error on `ArgumentException` (Req 6.3)
    - On valid teleport key press: call `TryTeleport()`
    - Decrement `cooldownRemaining` by `Time.deltaTime`, clamp to 0
    - _Requirements: 1.1, 1.4, 3.1, 3.3, 6.1, 6.2, 6.3_

- [x] 6. Implement auto-checkpoint coroutine
  - [x] 6.1 Implement `AutoCheckpointCoroutine()` and wire it in `Start()`
    - Loop: `yield return new WaitForSeconds(autoCheckpointInterval)`
    - Only call `SaveCheckpoint(isManual: false)` if `!checkpointData.isManual` (Req 2.2)
    - Start coroutine in `Start()`; stop it when `!gameObject.activeInHierarchy` (Req 2.3)
    - Override `OnEnable`/`OnDisable` to start/stop the coroutine so it suspends when the player is inactive
    - _Requirements: 2.1, 2.2, 2.3_

  - [ ]* 6.2 Write property test for health restoration toggle consistency (Property 4)
    - **Property 4: Health Restoration Toggle Consistency**
    - **Validates: Requirements 4.3**
    - With `restoreHealth == false`, save checkpoint, change health, teleport; assert health is unchanged after teleport

- [x] 7. Implement indicator visibility invariant
  - [x] 7.1 Verify and enforce indicator visibility rules in SaveCheckpoint and TryTeleport
    - Confirm `checkpointIndicator` is hidden on scene load (`Start()` sets it inactive if `!hasCheckpoint`) (Req 5.3)
    - Confirm indicator is shown on save and hidden on teleport (already wired in tasks 2.1 and 3.1)
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [ ]* 7.2 Write property test for indicator visibility invariant (Property 5)
    - **Property 5: Indicator Visibility Invariant**
    - **Validates: Requirements 5.1, 5.2, 5.3**
    - Assert `checkpointIndicator.activeSelf == true` after save and `== false` after teleport or on fresh scene load

- [x] 8. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP
- All code targets Unity 2D C# (MonoBehaviour / Coroutine patterns)
- `playerRb` and `playerHealth` references are assigned via the Unity Inspector
- Property tests can be written as Unity Test Framework EditMode tests in `Assets/Tests/`
