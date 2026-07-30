using UnityEngine;

namespace EndlessRunner
{
    public class RunnerCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 7f, -12f);
        [SerializeField] private float lookAtHeight = 1.35f;
        [SerializeField] private float followSpeed = 8f;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * lookAtHeight);
        }

        public void Configure(Transform followTarget)
        {
            target = followTarget;
        }

        public void Configure(Transform followTarget, Vector3 followOffset, float targetHeight)
        {
            target = followTarget;
            offset = followOffset;
            lookAtHeight = targetHeight;
        }
    }
}
