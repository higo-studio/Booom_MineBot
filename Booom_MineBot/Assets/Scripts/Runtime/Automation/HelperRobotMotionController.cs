using UnityEngine;

namespace Minebot.Automation
{
    public sealed class HelperRobotMotionController : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 5f;

        private Vector3 targetPosition;

        public Vector3 TargetPosition => targetPosition;

        private void Awake()
        {
            targetPosition = transform.position;
        }

        private void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, Mathf.Max(0.1f, moveSpeed) * Time.deltaTime);
        }

        public void SetTarget(Vector3 target)
        {
            targetPosition = target;
        }

        public void SnapTo(Vector3 target)
        {
            targetPosition = target;
            transform.position = target;
        }
    }
}
