# Stack Balance — Unity 6000.3.22f1

Stack Balance is a complete 2D arcade prototype. Every moving block starts at the same original width and moves horizontally near the top of the screen. Click or press Space to drop it onto the tower. The block checks the tower from the highest layer downward and lands on the first layer whose support span overlaps it. After landing, only the overlapping interval remains and every unsupported piece falls away independently.

## Open and run

1. In Unity Hub, choose **Add > Add project from disk** and select this `StackBalance` folder.
2. Open it with Unity **6000.3.22f1**.
3. Wait for package import and script compilation to finish.
4. Select **Tools > Stack Balance > Create Prototype Scene**.
5. Open `Assets/StackBalance/Scenes/StackBalance.unity` if it is not already open.
6. Press Play. Click or press Space to place a block.

The scene generator deliberately asks before replacing an existing scene. It also adds the generated scene to Build Settings.

## Game rules and mathematics

For an interval with center `x` and width `w`:

```text
left  = x - w / 2
right = x + w / 2
```

For the moving and stationary blocks:

```text
overlapLeft  = max(stationaryLeft, movingLeft)
overlapRight = min(stationaryRight, movingRight)
overlapWidth = overlapRight - overlapLeft
overlapCenter = (overlapLeft + overlapRight) / 2
```

An overlap width at or below zero is a complete miss. Otherwise the moving block is resized and recentered to the overlap. Since incoming blocks always have the original width, a narrow target can produce both a left and a right offcut; the implementation supports both.

Each successfully placed block keeps its assigned arcade mass even after trimming:

- Light: 1
- Normal: 2
- Heavy: 4

Torque is deterministic:

```text
blockTorque = blockMass * (blockCenterX - pivotX)
totalTorque = sum(blockTorque)
```

The tower fails when `abs(totalTorque) >= maximumSafeTorque`. Negative torque collapses left and positive torque collapses right. Balance failure is evaluated after every successful placement. Rigidbody2D is added only to discarded pieces, missed blocks, and the final visual collapse.

## Layer support rule

A block that misses the highest layer continues falling until it overlaps a lower layer. It fails only when it also misses the base. Multiple independently placed blocks can therefore occupy the same height. When a layer contains multiple blocks, its support span runs from the leftmost block edge to the rightmost block edge, including any gaps between those blocks. Trimmed pieces fall independently and never snap back into the tower or combine with another block.

## Main tuning values

Select **Game Systems** in the generated scene to tune these serialized fields:

| Setting | Initial value |
|---|---:|
| Original block width | 3.0 |
| Block height | 0.55 |
| Perfect tolerance | 0.05 |
| Initial speed | 2.5 |
| Speed increase per block | 0.12 |
| Maximum speed | 7.0 |
| Distance below screen top | 1.1 |
| Drop speed | 12.0 |
| Maximum safe torque | 8.0 |
| Light / Normal / Heavy probability | 0.30 / 0.50 / 0.20 |

Raise maximum safe torque if games end too quickly; lower it if weight choice feels unimportant.

## WebGL build

1. Open **File > Build Profiles**.
2. Add or select the Web platform and switch to it.
3. Confirm `StackBalance` is the enabled scene.
4. Make a Development Build first and test click, Space, restart, and UI scaling in a browser.

No external art, audio, native plugins, threads, or filesystem access are used.
The project manifest explicitly includes Unity's built-in 2D Physics module, which provides
`Rigidbody2D`, `BoxCollider2D`, and `ForceMode2D`.

## Script responsibilities

- `StackGameManager`: state machine, spawning, placement, trimming, score, failure and restart.
- `BlockMover`: deterministic horizontal movement while a block waits near the top of the screen.
- `StackBlock`: width, position, weight type and mass data.
- `TowerBalanceSystem`: placed-block list and torque calculation.
- `BalanceMeterUI`: normalized pointer and direction label.
- `GameUIController`: score, height, feedback and game-over UI.
- `TowerCameraController`: upward-only camera following.
- `StackBalanceSceneBuilder`: safe one-click scene creation and wiring.

## Verification still required in Unity

The source should be compiled and play-tested inside Unity 6000.3.22f1. Verify partial overlaps from both directions, a wide block overhanging both sides, perfect placement, complete misses, left/right torque failure, repeated restarts, tall-tower camera movement, and a browser Development Build.
