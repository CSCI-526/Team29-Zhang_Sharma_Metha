using UnityEngine;

namespace StackBalance
{
    public sealed class BlockMover : MonoBehaviour
    {
        private float leftBoundary;
        private float rightBoundary;
        private float speed;
        private float direction;
        private bool isMoving;

        public void Begin(float moveSpeed, float left, float right, float initialDirection)
        {
            speed = moveSpeed;
            leftBoundary = left;
            rightBoundary = right;
            direction = Mathf.Sign(initialDirection);
            isMoving = true;
        }

        public void StopMoving()
        {
            isMoving = false;
        }

        private void Update()
        {
            if (!isMoving)
            {
                return;
            }

            Vector3 position = transform.position;
            position.x += direction * speed * Time.deltaTime;

            if (position.x >= rightBoundary)
            {
                position.x = rightBoundary;
                direction = -1f;
            }
            else if (position.x <= leftBoundary)
            {
                position.x = leftBoundary;
                direction = 1f;
            }

            transform.position = position;
        }
    }
}
