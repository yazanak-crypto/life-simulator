# Prototype V0.1: third-person movement

Open `Assets/_Game/Scenes/PrototypeMovement.unity` in Unity 6000.0.84f1,
press Play, and click the Game view to capture the mouse.

- WASD: move relative to camera yaw; diagonal movement has the same maximum speed.
- Mouse: orbit the camera; vertical orbit is clamped.
- Escape: release the cursor and stop movement input. Click the Game view to resume.
- Space: no action; jumping is intentionally absent.

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
