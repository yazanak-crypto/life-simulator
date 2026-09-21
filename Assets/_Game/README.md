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

The ground is a 120 x 100 metre plane with boundary walls and a marked driving
apron south of the original warehouse/dealership area. There is no respawn system.
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
The green YOUR CAR parking bay is at (13, 0, -13). The cube-built starter car now
supports owner-only entry, arcade driving, and exit, as described below.

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

## First vehicle prototype: enter, drive, exit

Buy the starter car, walk to it in YOUR CAR parking, aim at the body, and press
E — Enter Vehicle. The same player switches to vehicle mode. W accelerates, S
brakes then reverses, A/D steer, mouse orbits the chase camera, and E exits. Escape
releases the mouse and suspends driving input; click Game view to resume. The car
coasts when input is released. Forward speed is capped at 14 m/s, reverse at 5 m/s.
Steering fades out near zero speed. There is no speedometer.

Driver state is control only and never alters physical state: leaving a moving car
keeps its linear velocity, angular velocity and momentum, and the unoccupied car
rolls on until rolling resistance and drag stop it. The Rigidbody is never made
kinematic. Because there is no player injury or ragdoll system yet, exiting is
temporarily refused above `VehicleSeat > Safe Exit Speed` (default 2 m/s) with
"Too fast to exit". That gate is one field plus one check in `PlayerVehicleMode.TryExit`;
delete both when bailing out of a moving car becomes a survivable, injurious action.
The internal `RestoreOnFoot` cleanup path deliberately ignores the gate so a disabled
vehicle can never strand the player.

The headlights of the original primitive face local -Z; vehicle forward and the
chase camera respect that model orientation. No visual assets were imported.
The road apron spans x=-45..55 and z=-44..-10, with dashes along z=-27. The rest
of the expanded ground is also drivable. Boundary walls are at x=+/-58 and z=+/-48.
Existing warehouse, obstacles, dealership, purchase terminal, and parking positions
are preserved. Nine visual-part colliders were replaced by one root BoxCollider
and Rigidbody; car visuals move together under the independent car root.
Flat painted decals (nine road centre dashes and the two parking bay lines) are
render-only: their box colliders were removed because their surfaces sat above the
car's underside and caught the hull. The car's own collider keeps a runtime
zero-friction PhysicsMaterial: the single hull stands in for four tyres, whose grip
and rolling resistance are modelled in script, so PhysX box friction would both
double-count that model and, at 900 kg, statically pin the car against its own
drive forces. Consequently the car has no parking brake and will roll on a slope.

Focused components and state:

- `VehicleSeat` implements the existing `IInteractable` and references the existing
  `CarPurchase`. It checks `IsOwnedBy` on entry and records one explicit `Driver`.
  Unoccupied -> Driven -> Unoccupied is represented by that driver reference.
  The display car reports Vehicle not purchased and refuses entry; a different
  player receives Only the owner can drive this car. No second ownership system.
- `PlayerVehicleMode` lives on the existing player. It saves each renderer/collider
  enabled state, hides the character, disables all its colliders (including the
  CharacterController), and suspends ThirdPersonPlayer and PlayerInteraction.
  It owns driving/exit input and follows the vehicle while hidden. It never creates
  or destroys a player. Entry during an active warehouse shift is refused with
  feedback, preserving that shift and any carried box; finish the delivery first.
- `ArcadeVehicle` owns Rigidbody motion in FixedUpdate. It applies accelerations
  rather than assigning velocity, so PhysX keeps ownership of momentum, collision
  response and slopes. Per grounded step it applies capped lateral tyre grip,
  rolling resistance plus a quadratic drag term, and — only while driven — engine
  or braking force and a torque towards a speed-scaled yaw rate. A ground probe
  returns the surface normal, so drive forces follow slopes and nothing is applied
  while airborne. Pitch and roll stay constrained for stability in place of
  suspension; yaw is free, so a collision can still spin the car. Collision
  detection is ContinuousDynamic rather than speculative, because a speculative
  solver slows the body before contact and understates impact severity. This is
  not a suspension, drivetrain or tyre-slip model.
- `ThirdPersonCamera` has a temporary follow override. While driving it uses a
  higher pivot, a 7m chase distance, heading-follow plus mouse orbit, and obstruction
  checks that ignore the driven car itself. Exit restores the prior on-foot yaw,
  pitch, target, distance and pivot settings. Cursor behavior stays with this camera.

E exit checks ground, slope, full character-capsule clearance, and a clear path
beside both doors, then rear-side alternatives. It never selects a point directly
in front of the car. If all candidates are blocked, it keeps driving mode and asks
the player to move to a clear area. A successful exit stops the car, restores all
player render/collider states, walking and interaction, and the on-foot camera.
Exit runs after generic interaction, ignores the entry frame, and has a 0.25s
re-entry cooldown so the same E press cannot switch back. Duplicate claims/exits
are rejected. Disabling either participant restores the player; if no nearby exit
exists during this cleanup, it uses the last on-foot entry position. There is no
networking, saving, damage, fuel, vehicle inventory or generic vehicle framework.

Manual validation:

1. Open PrototypeMovement. Keep PlayerWallet > Development Starting Balance at 0.
   Enter Play, capture the mouse, approach the display car at (13, 0, 4), and aim
   at its body. E must not enter, move it, or alter money; it is still unpurchased.
2. Buy via three $100 warehouse deliveries, or stop Play, set Development Starting
   Balance to 300, and restart. The terminal at (13, 0.8, 0.7) still spends exactly
   $300 once and moves the car to (13, 0, -13). Confirm SOLD and OWNED indicators.
3. Walk beside the owned car, aim at its body within interaction reach, and press
   E — Enter Vehicle. Confirm the player disappears, the camera changes to a
   higher/longer view, and E — Exit Vehicle appears. In Play-mode Inspector verify
   VehicleSeat.Driver references PlayerVehicleMode on Player, ThirdPersonPlayer
   and PlayerInteraction are disabled, and player colliders/renderers are disabled.
4. Hold W: acceleration should build gradually. Drive out of the bay into the open
   apron to the south, keeping clear of the delivery station at (6, 0, -10).
   Hold S while moving: brake, stop, then reverse more slowly. Release W/S: coast
   to a stop. Hold A/D at rest: no spin. While moving, A/D should steer in both
   directions; backing up reverses the steering response. Use a long straight to
   assess maximum speed. Walls around the outer ground should contain the car.
5. Drive into an existing building or a boundary wall: the hull must stop instead
   of passing through. Steer/reverse away. Confirm the car remains upright and
   grounded. Mouse-orbit near obstacles to check chase-camera obstruction. Escape
   releases the cursor and removes throttle; click resumes input without a jump.
6. In an open area, press E. The car must stop. The same Player must appear beside
   it, walking/collision/visibility restored, with the previous on-foot camera
   orbit. WASD must move only the character. Press E once while driving and hold:
   no immediate re-entry. Release, wait over 0.25s, aim at the car, and press again.
   Repeat entry/exit several times; there must still be one player and one driver.
7. Park tight to a wall and exit: choose the clear side. For a fully blocked test,
   place temporary cubes along both sides of the car during Play, covering door
   and rear-side points (local x=+/-2.25, z=0..1.5). E must report Exit blocked and
   preserve vehicle mode. Remove the cubes or move to open space, then exit.
8. Non-owner check: before entering, duplicate Player during Play and name it Other
   Player. Disable ThirdPersonPlayer and PlayerInteraction on the original, move
   Other Player beside the car, and set Main Camera's Target to Other Player.
   Keep Other Player's interaction/movement enabled, aim at the car and press E.
   It must refuse entry and leave VehicleSeat.Driver empty. Use a fresh Play run
   afterwards to restore the original references. The automated check below also
   verifies this gate directly with a distinct player object.
9. Finish another warehouse shift after driving/exiting: pick up, carry, deliver,
   objective updates and $100 payment should still work. Try entering during an
   active shift: it should ask you to finish the shift and leave the job intact.
   The sold dealership must still reject another purchase without spending money.
10. While driving in Play, disable VehicleSeat, ArcadeVehicle, or PlayerVehicleMode
    one at a time; control/visibility should return to the player and the vehicle
    should stop. Re-enable before continuing. Check the Unity Console throughout.
11. Stop Play, restore Development Starting Balance to 0, and restart: the car is
    unpurchased, the player starts on foot with $0, and the original job loop works.

Repeatable validation: outside Play choose Tools > Life Simulator > Validate vehicle
(reopens scene), or run Unity in batch mode with -executeMethod
LifeSimulator.Editor.VehiclePrototypeValidation.Validate. This first runs dealership
regressions, then checks entry gates, driver state, disabled walking/collisions,
input bindings, camera override/restoration, deterministic Rigidbody simulation
(acceleration, forward/reverse limits, coasting, steering, wall collision), blocked
exit, restored player state, repeated calls, and warehouse payment after driving.
It reopens the saved scene to discard every test mutation. Save other scene work
first and keep the scene's development starting balance at 0. These component and
physics checks do not replace the manual Game-view input and camera-feel checks.

This suite now runs clean: 47 of 47 checks pass under Unity 6000.0.84f1 batch mode
(exit code 0), covering the dealership/economy regressions and the vehicle checks.
It also covers momentum after control removal, unoccupied rolling and coasting to
rest, the temporary safe-exit gate, cornering slip, and that impact severity scales
with speed. A future damage component needs no plumbing from these scripts: Unity
delivers OnCollisionEnter to every MonoBehaviour on the car, so it can read
Collision.relativeVelocity, impulse, contact point and normal directly. That is why
no impact event was added here.

## Gameplay slice v0.1: time, commute and a scheduled warehouse shift

The loop is: wake at home at 6:00 AM, walk the street east past the used-car lot,
clock in at the warehouse, work a series of physical orders, clock out and get paid
for the hours worked. The starter car is a long-term goal, not a first-shift reward.

### Time

`GameClock` is the only source of in-game time. It is a plain scene component, not a
singleton or static, because multiplayer will make time server-authoritative. Consumers
hold a serialized reference or subscribe to `MinuteTick`; nothing looks time up globally.
Compression defaults to 60 (one real second is one in-game minute, so an in-game day
takes 24 real minutes) and is a serialized field. Nothing assumes that value: every
system works in in-game minutes. `MinuteTick` is raised at most once per frame, so a
large jump raises it once and consumers compare absolute times rather than count ticks.
`ClockUI` mirrors `BalanceUI`: a serialized source plus a Text.

### Work

`WorkSchedule` is serialized data only (title, start/end hour, early window, hourly wage)
so a second job is simply a second `JobSite`. `JobSite` owns the schedule and the wage
rules and is the only thing that pays money. `TimeClockTerminal` is the physical punch
clock and is deliberately thin. `PlayerEmployment` holds per-player state and no rules.

Wages accrue from the later of clock-in and the scheduled start, to the earliest of
clock-out, the scheduled end, and the moment the shift's orders are finished. Arriving
early earns nothing extra; arriving late costs the missed minutes; finishing the work
stops the wage clock, so there is no reason to idle and no busywork was invented to fill
the remaining hours. The reward for working efficiently is the rest of the day.

### Orders

`ShiftAssignments` composes scene-tagged `WarehouseRack` and `WarehouseDropOff` objects at
runtime: adding a rack or a destination is all that is needed for it to join the pool, so
orders vary without hand-authoring each one. 16 racks across aisles A-D and 4 destinations
give 64 distinct orders from zero authored content. `Assignment` is a plain C# object, not
a quest framework: one order, one box, one destination, one state.

Each order spawns its own `WarehouseBox` prefab instance and destroys it on delivery. A
single shared scene box would break the moment two players worked the same warehouse.
`PlayerCarry` and `CarryableItem` are job-agnostic, so boxes never generate money and the
carry mechanic is reusable. Orders per shift default to 6 and are configurable; tune by
playing rather than by filling a timer.

### District

One street runs the length of the district with home at one end and the warehouse at the
other. Measured distances (`Tools > Life Simulator > Report district layout`):

| Route | Metres | At 5 m/s | At a future 2 m/s walk |
|---|---|---|---|
| Home to dealership | 133 | 27 s | 67 s |
| Dealership to time clock | 86 | 17 s | 43 s |
| Home to time clock | 219 | 44 s | 110 s |

The distances are deliberately sized against a future realistic walking speed rather than
the current 5 m/s, so slowing the player later does not make the commute painful. The
used-car lot sits on the commute: the player passes the $2,000 car twice a day.

### Temporary balancing values

`$14/hour`, 6 orders per shift, a `$2,000` starter car and 60x compression are all
prototype numbers. The car price is a serialized field rather than a const so it can be
tuned per vehicle. The player still starts at `$0`.

### Development overrides

`DevelopmentOverrides` is editor and development-build only and drives the same public
entry points the player uses, so no shortcut can reach a state normal play cannot.
Start-up: starting money, grant car (through the real purchase), skip to shift start,
compression override. Hotkeys: F1 advance one hour, F2 skip to shift start, F3 force
clock out, F4 complete the current order.

### Validation

`Tools > Life Simulator > Validate everything` runs clock, job and vehicle suites:
83 checks pass under Unity 6000.0.84f1 batch mode (exit code 0) via
`-executeMethod LifeSimulator.Editor.PrototypeValidation.ValidateAll`.
The world is rebuilt from `Tools > Life Simulator > Build district`, which is reproducible
and keeps hand-edited scene YAML out of the milestone.
