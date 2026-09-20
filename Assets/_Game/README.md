# Prototype V0.1: movement, interaction, and warehouse delivery

Open `Assets/_Game/Scenes/PrototypeMovement.unity` in Unity 6000.0.84f1,
press Play, and click the Game view to capture the mouse.

- WASD: move relative to camera yaw; diagonal movement has the same maximum speed.
- Mouse: orbit the camera; vertical orbit is clamped.
- Escape: release the cursor and stop movement input. Click the Game view to resume.
- Space: no action; jumping is intentionally absent.
- E: interact with the object under the center-screen aim marker when in reach.

`ThirdPersonPlayer` owns only movement, facing, and vertical velocity. It uses a
CharacterController and a small Input System WASD action. `ThirdPersonCamera`
owns mouse look, cursor capture, follow positioning, and basic obstacle avoidance.
Both scripts dispose their input actions when destroyed. References are assigned
in the scene; no runtime scene creation or managers are required.

The capsule and its forward marker are on Unity's built-in Ignore Raycast layer
so the camera's Default-layer obstruction cast does not hit the player. Physical
collisions still use the existing project collision matrix.

Manual checks:

1. Move with each key, then diagonally. Orbit 90 degrees and verify W follows
   the new camera heading. Check that the yellow forward marker turns smoothly.
2. Release movement keys: the player should stop and stay grounded.
3. Walk into the buildings and wall: the player should collide and slide along them.
4. Walk up the low step and ramp, then off them: gravity should return the player
   to the ground without jumping. The taller block should stop the player.
5. Orbit beside a wall: the camera should move inward to avoid the obstruction
   and return to its normal distance when clear. Check pitch limits.
6. Press Escape, click to resume, switch application focus, and stop/restart Play.
   Check cursor release and that held movement does not persist after focus loss.
7. Check the Unity Console for errors.

The ground is a finite 40 x 40 metre plane. Walking off the outer edge causes a
fall; stop and restart Play to reset. There is no respawn system in this prototype.
The existing SampleScene, build scene list, packages, and project settings are
unchanged. Open this test scene directly to run it.

## Interaction

The yellow terminal directly ahead of the starting position starts a warehouse shift.
Approach it and aim the center-screen marker at it. Within 2.5 metres (measured
from the player's chest to the aimed surface), the lower-center prompt reads
`E — Start Shift`. Starting makes one box available on the blue pickup pad to the
left. Pick up that box and carry it to the green delivery post across the test
area. Payment happens only when the box is delivered; waiting never pays.

`PlayerInteraction` handles its own Input System action and resolves the target
after the camera updates. The first solid collider under the aim marker wins;
an ordinary obstacle blocks selection. A second visibility check from the player
prevents reaching through walls even when the third-person camera can see over
them. The raycast mask includes ordinary obstacles and excludes the player's
existing Ignore Raycast layer. Trigger colliders are intentionally ignored.

`IInteractable` exposes `Prompt` and `Interact(PlayerInteraction player)`. To add a
new type, implement that interface on an enabled MonoBehaviour attached to a solid
collider's object or parent. The explicit player argument provides the initiating
player without a global singleton. Warehouse interactables check the initiating
player's ownership before advancing the task. `InteractionUI` owns the prompt, aim marker, and timed
feedback through the existing Unity UI package. No EventSystem is needed for
these non-clickable labels. Money and work do not change movement, camera, or
generic interaction behavior. The old `TestInteractable` script remains available
but is no longer attached to the scene's terminal.

Select Player in the scene to configure PlayerInteraction's Reach, origin offset,
camera/UI references, and Detection Mask. Keep both obstacles and interactables
in that mask. The scene's Interaction UI uses a scaling screen-space Canvas.

## Money and warehouse work

`PlayerWallet` stores whole dollars in a nonnegative `long`, starts at $0, and
exposes read-only `Balance`, `TryAddMoney`, `CanAfford`, and `TrySpendMoney`.
Negative amounts, overspending, and overflow are rejected without changing the
balance. Zero-value operations succeed without raising a change event.
`BalanceChanged` fires only when money actually changes.

`BalanceUI` references the specific player's wallet, subscribes to its change
event, and refreshes immediately when enabled. It displays the top-left balance
without polling each frame or knowing anything about workplaces.

`WarehouseTask` is attached to the player and owns four explicit states:
`NoShift -> PickUpBox -> DeliverBox -> Complete`. It records the originating
station, the specific box, the delivery point, and the agreed wage. A player with
an active task cannot start another, even at a different warehouse station.
Each station/box/delivery point serves one worker at a time in this prototype.

`Workplace` now only starts a shift. `WarehouseBox` implements pickup and attaches
the actual box to the player's Box Carry Point. The placeholder is a stable prop
without a Rigidbody; its collider is disabled while carried so it cannot block
movement, camera casts, or delivery targeting. This is not an inventory system.

`WarehouseDelivery` is enabled for interaction only after pickup. It checks the
worker and asks their task to deliver. The task verifies the correct destination
and that the actual assigned box is still attached. Delivery is locked through
the wallet's change callbacks, preventing repeated or reentrant payments. A
successful delivery hides the box, releases the station, pays the owning player
once, and completes the objective. Wallet overflow leaves the task undelivered
with the box still carried; no reward is silently discarded.

`ObjectiveUI` only displays text passed to it. The task supplies `No active job`,
`Objective: Pick up the box`, `Objective: Deliver the box`, or `Job complete`.
It is separate from the balance HUD and transient interaction feedback.

Walking away does not cancel a shift. Disabling the station or the player's task,
or losing the worker/wallet/assigned objects, cancels without payment and clears
the carried box. There is no timer, automatic payout, saving, movement lock,
generic quest framework, or networking. Stop and restart Play to start with $0.

Manual warehouse checks:

1. Open PrototypeMovement, press Play, and click the Game view. Verify
   `Balance: $0` and `No active job`. The blue pickup pad has no available box.
2. Walk to the yellow START SHIFT terminal ahead of spawn, aim at it, and press E
   when `E — Start Shift` appears. Verify `Objective: Pick up the box` and $0.
   Repeated E presses must not spawn extra boxes or restart the task. Wait at
   least five seconds: no money should be awarded.
3. Walk to the blue PICKUP pad to the left (world position -6, 0, -1.5). Aim at
   the brown box and press E for `E — Pick Up Box`. Verify it is visibly attached
   in front of the player and the objective becomes `Objective: Deliver the box`.
4. Walk, steer, and orbit the camera while carrying. The box should follow the
   character without interfering with collisions or the camera. Balance stays $0.
5. Walk to the green DELIVERY area behind/right of spawn (world position 6, 0,
   -10). Aim at its raised green post from close range and press E for
   `E — Deliver Box`. Verify the box disappears, `Job complete` appears, feedback
   says `Delivery complete! +$100`, and balance becomes $100.
6. Keep pressing E at delivery: balance must stay $100. Return to the start
   terminal and complete another whole pickup/delivery to earn $200.
7. In a fresh shift, visit delivery before pickup: it must offer no delivery
   prompt and pay nothing. Out-of-range or obstructed objects must not interact.
8. In Play mode, disable the Workplace or WarehouseTask while carrying. The box
   and task should clear without payment. Re-enable and start a fresh shift.
9. Verify Escape/focus switching, movement, gravity, camera obstruction, and
   interaction prompts still work. Check Console for errors. Restart Play: $0.
