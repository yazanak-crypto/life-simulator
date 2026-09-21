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

## Starter car dealership

The dealership is at world (13, 0, 4), east/right of the starting area. Its yellow
purchase terminal is at (13, 0.8, 0.7), facing south toward the approach from spawn.
The starter car costs exactly $300; warehouse deliveries still pay $100 each.
The green YOUR CAR parking bay is at (13, 0, -13). Cars are stationary props made
from cubes, with no Rigidbody, driving, entry, or inventory functionality.

`CarPurchase` implements the existing `IInteractable` on the purchase terminal.
Each instance represents one specific vehicle and holds a read-only runtime
`Owner` reference to the purchasing `PlayerInteraction`. `IsOwnedBy(player)`
checks that exact reference. A per-car sold flag prevents resale even after its
owner object is destroyed; it is not a global player ownership flag. A transaction
guard prevents a second charge through synchronous wallet event callbacks.
Spending uses only `PlayerWallet.TrySpendMoney(300)`, preserving the normal HUD
event flow. On success the same car moves to that player's assigned `PlayerParking`
spot, its OWNED sign activates, and the terminal changes to SOLD. Other players
cannot claim it. The parking Transform lives in the world, outside the player
hierarchy, so walking does not drag the car along.

This is deliberately runtime-only, one car and one parking spot per player in
this scene. Additional assets can have independent ownership records; persistent
player/asset IDs and allocation of additional parking spots are future work.
No shared database or inventory is introduced.

For fast development tests, select Player, set PlayerWallet > Development Starting
Balance to 300 (or 500), then enter Play. The Inspector value defaults to 0 in the
saved scene and is applied only with UNITY_EDITOR or DEVELOPMENT_BUILD. Release
builds ignore it. Change it before entering Play, and restore 0 before saving.
Restarting Play resets purchases and ownership.

Manual purchase checks (Unity license required):

1. Open PrototypeMovement, select Player, confirm Development Starting Balance is
   0, and press Play. Click Game view. Confirm Balance: $0.
2. Walk east/right to the dealership terminal at (13, 0.8, 0.7). Approach its
   south-facing yellow front, aim at it within 2.5m, and verify the prompt reads
   E — Buy Starter Car ($300). Press E. Expect Not enough money, $0 unchanged,
   the car still on display, and no OWNED indicator.
3. Complete three warehouse shifts using the earlier steps: terminal (0, 1, 1.5),
   pickup (-6, 0, -1.5), delivery (6, 0, -10). Confirm $100, $200, then $300.
   Try buying after each of the first two deliveries; the balance must not change.
4. With $300, return to the dealership and press E. Expect Starter Car purchased!,
   Balance: $0, terminal SOLD, and the display car gone. Walk south to (13, 0, -13):
   the same car should be in YOUR CAR parking, facing north with OWNED above it.
5. Return to the sold terminal and press E repeatedly. Expect You already own this
   car and no charge or additional car. Finish another warehouse shift and confirm
   the normal objective flow and a further $100 payment still work.
6. Stop Play. Set Development Starting Balance to 500, restart, and buy: expect
   exactly $200 remaining. Try again: still $200 and only one car. Stop Play and
   restore Development Starting Balance to 0.
7. For a second-player check in Play, duplicate Player before buying. Disable the
   duplicate's ThirdPersonPlayer and WarehouseTask to avoid shared input/job UI;
   keep its PlayerInteraction and PlayerWallet enabled. In Inspector, give the
   duplicate's PlayerParking a separate empty world Transform. Disable the original
   PlayerInteraction while testing the duplicate, and change the camera's target
   to the duplicate if testing its movement. Buying with either object must set
   ownership only to that object; the other object's attempts must report This car
   is already owned and leave its wallet unchanged. The automated check below
   verifies reference ownership directly without needing two controlled players.
8. Check movement, camera orbit/obstruction, Escape/click recapture, balance HUD,
   warehouse objectives, and Console errors. Stop/restart with default settings:
   balance returns to $0 and the car is for sale again.

Repeatable component checks: outside Play, use Tools > Life Simulator > Validate
dealership (reopens scene). It offers to save pending scene edits, then opens
PrototypeMovement, checks failed purchases at $0/$100/$200, three warehouse
payments, purchase event count/reentrant calls, exact charge, feedback, repeated
purchase with sufficient money, different-player ownership, parking movement,
OWNED activation, and another warehouse payment. It discards all test mutations
by reopening the saved scene. This checks component methods, not simulated input
or rendered appearance. The scene must have its normal $0 starting setting.
Batch equivalent: Unity.exe -batchmode -nographics -projectPath <project path>
-executeMethod LifeSimulator.Editor.DealershipPrototype.Validate -quit -logFile <log path>.

Validation in this implementation session: runtime and Editor C# compiled against
installed Unity 6000.0.84f1 assemblies using Roslyn, with no compile errors (Inspector-field CS0649 warnings and an expected
unused development setting CS0169 warning in the release configuration). Scene references/hierarchy were checked offline.
Unity batch execution exited 198: No valid Unity Editor license found. Therefore
the integration checks and manual Play/visual checks have NOT run successfully;
activate the local Editor license and run the checks above before accepting them.
