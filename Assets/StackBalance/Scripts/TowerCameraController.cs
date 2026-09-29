using UnityEngine;

namespace StackBalance
{
    public sealed class TowerCameraController : MonoBehaviour
    {
        [SerializeField] private float initialY;
        [SerializeField] private float followThresholdY = 2f;
        [SerializeField] private float verticalOffset = 2.5f;
        [SerializeField] private float smoothTime = 0.25f;

        private float targetY;
        private float velocity;
        private Camera attachedCamera;

        public float GetWorldYBelowScreenTop(float distance)
        {
            if (attachedCamera == null)
            {
                attachedCamera = GetComponent<Camera>();
            }

            float halfHeight = attachedCamera != null && attachedCamera.orthographic
                ? attachedCamera.orthographicSize
                : 5f;
            return transform.position.y + halfHeight - Mathf.Max(0f, distance);
        }

        public void Configure(float startingY, float threshold, float offset)
        {
            initialY = startingY;
            followThresholdY = threshold;
            verticalOffset = offset;
            ResetCamera();
        }

        public void FollowHeight(float towerTopY)
        {
            if (towerTopY > followThresholdY)
            {
                targetY = Mathf.Max(targetY, towerTopY - verticalOffset);
            }
        }

        public void ResetCamera()
        {
            targetY = initialY;
            velocity = 0f;
            Vector3 position = transform.position;
            position.y = initialY;
            transform.position = position;
        }

        private void LateUpdate()
        {
            Vector3 position = transform.position;
            position.y = Mathf.SmoothDamp(position.y, targetY, ref velocity, smoothTime);
            transform.position = position;
        }
    }
}
