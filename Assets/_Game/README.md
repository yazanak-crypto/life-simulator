# Prototype V0.1: third-person movement and interaction

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

The yellow cube directly ahead of the starting position is the test terminal.
Approach it and aim the center-screen marker at it. Within 2.5 metres (measured
from the player's chest to the aimed surface), the lower-center prompt reads
`E — Test Interaction`. Press E to show `Interaction successful` for two seconds.

`PlayerInteraction` handles its own Input System action and resolves the target
after the camera updates. The first solid collider under the aim marker wins;
an ordinary obstacle blocks selection. A second visibility check from the player
prevents reaching through walls even when the third-person camera can see over
them. The raycast mask includes ordinary obstacles and excludes the player's
existing Ignore Raycast layer. Trigger colliders are intentionally ignored.

`IInteractable` exposes `Prompt` and `Interact(PlayerInteraction player)`. To add a
new type, implement that interface on an enabled MonoBehaviour attached to a solid
collider's object or parent. The explicit player argument provides the initiating
player without a global singleton. `TestInteractable` only requests success
feedback from that player. `InteractionUI` owns the prompt, aim marker, and timed
feedback through the existing Unity UI package. No EventSystem is needed for
these non-clickable labels. The movement and camera scripts are unchanged.

Select Player in the scene to configure PlayerInteraction's Reach, origin offset,
camera/UI references, and Detection Mask. Keep both obstacles and interactables
in that mask. The scene's Interaction UI uses a scaling screen-space Canvas.

Manual interaction checks:

1. Open PrototypeMovement, press Play, and click the Game view. The prompt starts
   hidden because the terminal is beyond reach. Pressing E should do nothing.
2. Walk toward the yellow terminal and aim at it. Check the exact prompt above.
   Press E: the green success message should appear and disappear after two seconds.
3. Hold E: feedback should not keep restarting. Release E and press again to retry.
4. Look away or walk backward out of reach: the prompt should hide immediately.
   E must do nothing. Existing success feedback may finish its two-second duration.
5. Press Escape or switch application focus: the prompt, marker, and feedback
   should hide and E should do nothing. Click the Game view to resume.
6. In Play mode, temporarily place a cube between the terminal and camera/player:
   it should block interaction. Disable TestInteractable: its prompt should hide.
7. Optionally duplicate the terminal in Play mode: aim at each in turn and check
   that only the nearest visible object under the marker is selected. Stop Play
   to discard these test changes.
8. Confirm WASD, camera orbit, collision, and gravity still work; check Console
   for errors. Reopen the scene if Unity was already showing the older scene.
