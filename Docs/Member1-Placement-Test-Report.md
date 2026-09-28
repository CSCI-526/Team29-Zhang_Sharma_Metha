# Member 1 Placement Test

Date: September 28, 2026
Unity version: 6000.3.22f1
Branch: feature/member1-placement

## Modified and Tested 

- Horizontal block movement
- Vertical block dropping
- Support-layer detection
- Overlap trimming
- Perfect placement
- Missed and partial-placement behavior

## Test results

| Test | Result |
|---|---|
| Block moves between both horizontal boundaries | Pass |
| Block reverses direction at each boundary | Pass |
| Mouse click drops exactly one block | Pass |
| Space drops exactly one block | Pass |
| Additional input while dropping is ignored | Pass |
| Partial overlap from the left trims correctly | Pass |
| Partial overlap from the right trims correctly | Pass |
| Unsupported offcuts fall independently | Pass |
| Perfect placement preserves block width | Pass |
| Perfect placement awards bonus feedback | Pass |
| Block can miss a narrow upper layer and land lower | Pass |
| Separated blocks use the layer's outer support bounds | Pass |
| Lower-level recovery does not incorrectly increase height | Pass |
| Missing every layer and the base ends the game | Pass |

## Conclusion

The current placement mechanics passed manual Play Mode testing. No
gameplay code changes were necessary for this iteration.

## Known limitations

- Placement behavior currently has manual rather than automated tests.
- Camera presentation for blocks falling several levels may need future polish.
- WebGL behavior will be verified as part of Member 3's responsibilities.