using System.Collections.Generic;
using UnityEngine;

namespace StackBalance
{
    public sealed class TowerBalanceSystem : MonoBehaviour
    {
        [SerializeField] private float pivotX;
        [SerializeField, Min(0.1f)] private float maximumSafeTorque = 8f;

        private readonly List<StackBlock> placedBlocks = new List<StackBlock>();

        public float TotalTorque { get; private set; }
        public float NormalizedBalance => TotalTorque / maximumSafeTorque;
        public bool IsUnsafe => Mathf.Abs(TotalTorque) >= maximumSafeTorque;
        public IReadOnlyList<StackBlock> PlacedBlocks => placedBlocks;

        public void Configure(float newPivotX, float safeTorque)
        {
            pivotX = newPivotX;
            maximumSafeTorque = Mathf.Max(0.1f, safeTorque);
        }

        public float AddBlock(StackBlock block)
        {
            placedBlocks.Add(block);
            Recalculate();
            return TotalTorque;
        }

        public void ResetBalance()
        {
            placedBlocks.Clear();
            TotalTorque = 0f;
        }

        public void Recalculate()
        {
            float torque = 0f;
            foreach (StackBlock block in placedBlocks)
            {
                if (block != null)
                {
                    torque += block.Mass * (block.CenterX - pivotX);
                }
            }

            TotalTorque = torque;
        }
    }
}
