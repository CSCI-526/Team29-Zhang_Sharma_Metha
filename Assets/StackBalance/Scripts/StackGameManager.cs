using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StackBalance
{
    public sealed class StackGameManager : MonoBehaviour
    {
        private enum GameState { Ready, Moving, Placing, GameOver }

        [Header("Scene references")]
        [SerializeField] private Transform towerRoot;
        [SerializeField] private TowerBalanceSystem balanceSystem;
        [SerializeField] private BalanceMeterUI balanceMeter;
        [SerializeField] private GameUIController gameUI;
        [SerializeField] private TowerCameraController cameraController;

        [Header("Block geometry")]
        [SerializeField, Min(0.25f)] private float originalBlockWidth = 3f;
        [SerializeField, Min(0.1f)] private float blockHeight = 0.55f;
        [SerializeField] private float baseTopY = -3f;
        [SerializeField] private float leftBoundary = -3.5f;
        [SerializeField] private float rightBoundary = 3.5f;
        [SerializeField, Min(0f)] private float perfectTolerance = 0.05f;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float initialMoveSpeed = 2.5f;
        [SerializeField, Min(0f)] private float speedIncreasePerBlock = 0.12f;
        [SerializeField, Min(0.1f)] private float maximumMoveSpeed = 7f;
        [SerializeField, Range(0f, 1f)] private float nextBlockDelay = 0.2f;
        [SerializeField, Min(0f)] private float spawnDistanceFromScreenTop = 1.1f;
        [SerializeField, Min(0.1f)] private float dropSpeed = 12f;

        [Header("Balance")]
        [SerializeField] private float pivotX;
        [SerializeField, Min(0.1f)] private float maximumSafeTorque = 8f;

        [Header("Weight probabilities")]
        [SerializeField, Min(0f)] private float lightProbability = 0.3f;
        [SerializeField, Min(0f)] private float normalProbability = 0.5f;
        [SerializeField, Min(0f)] private float heavyProbability = 0.2f;

        [Header("Visuals")]
        [SerializeField] private Color lightColor = new Color(1f, 0.78f, 0.2f);
        [SerializeField] private Color normalColor = new Color(0.1f, 0.7f, 0.75f);
        [SerializeField] private Color heavyColor = new Color(1f, 0.3f, 0.25f);

        private GameState state;
        private StackBlock activeBlock;
        private Sprite squareSprite;
        private int score;
        private int placedBlockCount;
        private int completedLevelCount;
        private float nextDirection = 1f;
        private readonly List<TowerLayer> towerLayers = new List<TowerLayer>();

        public void Configure(Transform newTowerRoot, TowerBalanceSystem newBalanceSystem,
            BalanceMeterUI newBalanceMeter, GameUIController newGameUI,
            TowerCameraController newCameraController)
        {
            towerRoot = newTowerRoot;
            balanceSystem = newBalanceSystem;
            balanceMeter = newBalanceMeter;
            gameUI = newGameUI;
            cameraController = newCameraController;
        }

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            squareSprite = CreateSquareSprite();
            gameUI.BindRestart(RestartGame);
            gameUI.BindEndGame(ExitGame);
            RestartGame();
        }

        private void Update()
        {
            if (state != GameState.Moving)
            {
                return;
            }

            KeepActiveBlockBelowScreenTop();

            bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool placePressed = Input.GetKeyDown(KeyCode.Space) ||
                                (Input.GetMouseButtonDown(0) && !pointerOverUI);
            if (placePressed)
            {
                PlaceActiveBlock();
            }
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            state = GameState.Ready;

            for (int i = towerRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(towerRoot.GetChild(i).gameObject);
            }

            score = 0;
            placedBlockCount = 0;
            completedLevelCount = 0;
            nextDirection = 1f;
            activeBlock = null;
            towerLayers.Clear();
            balanceSystem.Configure(pivotX, maximumSafeTorque);
            balanceSystem.ResetBalance();
            balanceMeter.SetBalance(0f, 0f);
            gameUI.UpdateScore(score, completedLevelCount);
            gameUI.HideGameOver();
            cameraController.ResetCamera();

            CreateBaseBlock();
            SpawnNextBlock();
        }

        private void CreateBaseBlock()
        {
            float baseCenterY = baseTopY - blockHeight * 0.5f;
            StackBlock baseBlock = CreateBlock("Base Block", pivotX,
                baseCenterY,
                originalBlockWidth, BlockWeightType.Normal, 0f, new Color(0.3f, 0.34f, 0.42f));
            TowerLayer baseLayer = new TowerLayer(baseCenterY);
            baseLayer.Blocks.Add(baseBlock);
            towerLayers.Add(baseLayer);
        }

        private void SpawnNextBlock()
        {
            float movementDirection = nextDirection;
            float spawnX = movementDirection > 0f ? leftBoundary : rightBoundary;
            nextDirection *= -1f;
            float spawnY = cameraController.GetWorldYBelowScreenTop(spawnDistanceFromScreenTop);

            BlockWeightType type = ChooseWeightType();
            float mass = MassFor(type);
            Color color = ColorFor(type);

            activeBlock = CreateBlock("Moving Block", spawnX, spawnY,
                originalBlockWidth, type, mass, color);
            BlockMover mover = activeBlock.gameObject.AddComponent<BlockMover>();
            float speed = Mathf.Min(initialMoveSpeed + placedBlockCount * speedIncreasePerBlock,
                maximumMoveSpeed);
            mover.Begin(speed, leftBoundary, rightBoundary, movementDirection);
            gameUI.SetCurrentWeight(type);
            state = GameState.Moving;
        }

        private void PlaceActiveBlock()
        {
            state = GameState.Placing;
            BlockMover mover = activeBlock.GetComponent<BlockMover>();
            mover.StopMoving();
            Destroy(mover);

            StartCoroutine(DropAndResolvePlacement());
        }

        private IEnumerator DropAndResolvePlacement()
        {
            float movingLeft = activeBlock.CenterX - activeBlock.Width * 0.5f;
            float movingRight = activeBlock.CenterX + activeBlock.Width * 0.5f;
            int supportLayerIndex = FindHighestOverlappingLayer(movingLeft, movingRight);
            bool foundSupport = supportLayerIndex >= 0;
            float landingY = foundSupport
                ? towerLayers[supportLayerIndex].CenterY + blockHeight
                : baseTopY - blockHeight * 2f;

            while (activeBlock != null && activeBlock.transform.position.y > landingY)
            {
                Vector3 position = activeBlock.transform.position;
                position.y = Mathf.MoveTowards(position.y, landingY, dropSpeed * Time.deltaTime);
                activeBlock.transform.position = position;
                yield return null;
            }

            if (activeBlock == null || state == GameState.GameOver)
            {
                yield break;
            }

            Vector3 landedPosition = activeBlock.transform.position;
            landedPosition.y = landingY;
            activeBlock.transform.position = landedPosition;

            if (!foundSupport)
            {
                MakeFalling(activeBlock.gameObject, activeBlock.CenterX < pivotX ? -1f : 1f);
                EndGame("Missed every layer!", false);
                yield break;
            }

            ResolvePlacement(supportLayerIndex, landingY);
        }

        private void ResolvePlacement(int supportLayerIndex, float landingY)
        {
            TowerLayer supportLayer = towerLayers[supportLayerIndex];
            supportLayer.GetBounds(out float stationaryLeft, out float stationaryRight);
            float stationaryCenter = (stationaryLeft + stationaryRight) * 0.5f;
            float stationaryWidth = stationaryRight - stationaryLeft;
            float movingLeft = activeBlock.CenterX - activeBlock.Width * 0.5f;
            float movingRight = activeBlock.CenterX + activeBlock.Width * 0.5f;

            bool perfect = Mathf.Abs(activeBlock.CenterX - stationaryCenter) <= perfectTolerance;
            if (perfect)
            {
                float snappedCenter = stationaryCenter;
                float keptWidth = Mathf.Min(activeBlock.Width, stationaryWidth);
                SpawnTrimPieces(movingLeft, movingRight,
                    snappedCenter - keptWidth * 0.5f, snappedCenter + keptWidth * 0.5f);
                activeBlock.SetGeometry(snappedCenter, keptWidth);
                CompletePlacement(true, supportLayerIndex, landingY);
                return;
            }

            float overlapLeft = Mathf.Max(stationaryLeft, movingLeft);
            float overlapRight = Mathf.Min(stationaryRight, movingRight);
            float overlapWidth = overlapRight - overlapLeft;
            if (overlapWidth <= 0.0001f)
            {
                Debug.LogError("Landing support no longer overlaps the active block.", this);
                MakeFalling(activeBlock.gameObject,
                    activeBlock.CenterX < stationaryCenter ? -1f : 1f);
                EndGame("Missed every layer!", false);
                return;
            }

            SpawnTrimPieces(movingLeft, movingRight, overlapLeft, overlapRight);
            float overlapCenter = (overlapLeft + overlapRight) * 0.5f;
            activeBlock.SetGeometry(overlapCenter, overlapWidth);
            CompletePlacement(false, supportLayerIndex, landingY);
        }

        private void SpawnTrimPieces(float movingLeft, float movingRight,
            float keptLeft, float keptRight)
        {
            float leftTrimWidth = keptLeft - movingLeft;
            if (leftTrimWidth > 0.0001f)
            {
                CreateFallingPiece(movingLeft + leftTrimWidth * 0.5f, leftTrimWidth, -1f);
            }

            float rightTrimWidth = movingRight - keptRight;
            if (rightTrimWidth > 0.0001f)
            {
                CreateFallingPiece(keptRight + rightTrimWidth * 0.5f, rightTrimWidth, 1f);
            }
        }

        private void CreateFallingPiece(float centerX, float width, float pushDirection)
        {
            StackBlock piece = CreateBlock("Trimmed Piece", centerX,
                activeBlock.transform.position.y, width, activeBlock.WeightType,
                activeBlock.Mass, ColorFor(activeBlock.WeightType));
            MakeFalling(piece.gameObject, pushDirection);
        }

        private void CompletePlacement(bool perfect, int supportLayerIndex, float landingY)
        {
            StackBlock placedBlock = activeBlock;
            activeBlock = null;
            placedBlockCount++;
            score += perfect ? 25 : 10;
            balanceSystem.AddBlock(placedBlock);
            balanceMeter.SetBalance(balanceSystem.NormalizedBalance, balanceSystem.TotalTorque);
            gameUI.UpdateScore(score, completedLevelCount);
            if (perfect)
            {
                gameUI.ShowFeedback("Perfect! +15");
            }

            int landingLayerIndex = supportLayerIndex + 1;
            if (landingLayerIndex == towerLayers.Count)
            {
                towerLayers.Add(new TowerLayer(landingY));
            }

            towerLayers[landingLayerIndex].Blocks.Add(placedBlock);
            completedLevelCount = towerLayers.Count - 1;
            gameUI.UpdateScore(score, completedLevelCount);
            cameraController.FollowHeight(landingY + blockHeight * 0.5f);

            if (landingLayerIndex < completedLevelCount)
            {
                gameUI.ShowFeedback($"Landed on level {landingLayerIndex}");
            }

            if (balanceSystem.IsUnsafe)
            {
                EndGame("Tower became unbalanced!", true);
                return;
            }

            StartCoroutine(SpawnAfterDelay());
        }

        private int FindHighestOverlappingLayer(float movingLeft, float movingRight)
        {
            for (int i = towerLayers.Count - 1; i >= 0; i--)
            {
                towerLayers[i].GetBounds(out float layerLeft, out float layerRight);
                float overlapWidth = Mathf.Min(layerRight, movingRight) -
                                     Mathf.Max(layerLeft, movingLeft);
                if (overlapWidth > 0.0001f)
                {
                    return i;
                }
            }

            return -1;
        }

        private void KeepActiveBlockBelowScreenTop()
        {
            if (activeBlock == null)
            {
                return;
            }

            Vector3 position = activeBlock.transform.position;
            position.y = cameraController.GetWorldYBelowScreenTop(spawnDistanceFromScreenTop);
            activeBlock.transform.position = position;
        }

        private IEnumerator SpawnAfterDelay()
        {
            yield return new WaitForSeconds(nextBlockDelay);
            if (state != GameState.GameOver)
            {
                SpawnNextBlock();
            }
        }

        private void EndGame(string reason, bool collapseTower)
        {
            state = GameState.GameOver;
            StopAllCoroutines();
            if (collapseTower)
            {
                float direction = Mathf.Sign(balanceSystem.TotalTorque);
                foreach (StackBlock block in balanceSystem.PlacedBlocks)
                {
                    if (block != null)
                    {
                        MakeFalling(block.gameObject, direction);
                    }
                }
            }
            gameUI.ShowGameOver(reason, score);
        }

        private void ExitGame()
        {
            state = GameState.GameOver;
            StopAllCoroutines();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void MakeFalling(GameObject blockObject, float pushDirection)
        {
            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = blockObject.AddComponent<Rigidbody2D>();
            }
            body.gravityScale = 1.5f;
            body.AddForce(new Vector2(pushDirection * 1.5f, 0.5f), ForceMode2D.Impulse);
            body.AddTorque(-pushDirection * 1.5f, ForceMode2D.Impulse);
            Destroy(blockObject, 5f);
        }

        private StackBlock CreateBlock(string objectName, float centerX, float centerY,
            float width, BlockWeightType type, float mass, Color color)
        {
            GameObject blockObject = new GameObject(objectName);
            blockObject.transform.SetParent(towerRoot);
            blockObject.transform.position = new Vector3(centerX, centerY, 0f);
            blockObject.transform.localScale = new Vector3(width, blockHeight, 1f);

            SpriteRenderer renderer = blockObject.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = 5;

            BoxCollider2D collider = blockObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            StackBlock block = blockObject.AddComponent<StackBlock>();
            block.Configure(type, mass, color);
            return block;
        }

        private BlockWeightType ChooseWeightType()
        {
            float total = Mathf.Max(0.0001f, lightProbability + normalProbability + heavyProbability);
            float roll = Random.value * total;
            if (roll < lightProbability) return BlockWeightType.Light;
            if (roll < lightProbability + normalProbability) return BlockWeightType.Normal;
            return BlockWeightType.Heavy;
        }

        private static float MassFor(BlockWeightType type)
        {
            switch (type)
            {
                case BlockWeightType.Light: return 1f;
                case BlockWeightType.Heavy: return 4f;
                default: return 2f;
            }
        }

        private Color ColorFor(BlockWeightType type)
        {
            switch (type)
            {
                case BlockWeightType.Light: return lightColor;
                case BlockWeightType.Heavy: return heavyColor;
                default: return normalColor;
            }
        }

        private static Sprite CreateSquareSprite()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "Runtime Square Texture";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
        }

        private bool ValidateReferences()
        {
            bool valid = towerRoot != null && balanceSystem != null && balanceMeter != null &&
                         gameUI != null && cameraController != null;
            if (!valid)
            {
                Debug.LogError("StackGameManager is missing scene references. Run Tools > Stack Balance > Create Prototype Scene.", this);
            }
            return valid;
        }

        private sealed class TowerLayer
        {
            public readonly List<StackBlock> Blocks = new List<StackBlock>();
            public float CenterY { get; }

            public TowerLayer(float centerY)
            {
                CenterY = centerY;
            }

            public void GetBounds(out float left, out float right)
            {
                left = float.PositiveInfinity;
                right = float.NegativeInfinity;

                foreach (StackBlock block in Blocks)
                {
                    if (block == null)
                    {
                        continue;
                    }

                    left = Mathf.Min(left, block.CenterX - block.Width * 0.5f);
                    right = Mathf.Max(right, block.CenterX + block.Width * 0.5f);
                }
            }
        }
    }
}
