using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessRunner
{
    [RequireComponent(typeof(CharacterController))]
    public class RunnerPlayerController : MonoBehaviour
    {
        [SerializeField] private RunnerGameManager gameManager;
        [SerializeField] private float laneDistance = 2f;
        [SerializeField] private float laneChangeSpeed = 14f;
        [SerializeField] private float jumpHeight = 2.25f;
        [SerializeField] private float gravity = -30f;
        [SerializeField] private Animator animator;
        [SerializeField] private float runningAnimationSpeed = 5.5f;

        private CharacterController characterController;
        private RunnerAnimationEventReceiver animationEventReceiver;
        private int targetLane;
        private float verticalVelocity;
        private bool isJumping;
        private bool wasGrounded;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int FreeFallHash = Animator.StringToHash("FreeFall");

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            ResolveAnimationEventReceiver();
            wasGrounded = characterController.isGrounded;
        }

        private void Update()
        {
            if (gameManager == null || gameManager.IsGameOver)
            {
                UpdateAnimator(false);
                return;
            }

            bool jumpPressed = ReadLaneInput();
            MovePlayer();
            UpdateAnimator(jumpPressed);
        }

        public void Configure(RunnerGameManager manager, Animator runnerAnimator = null)
        {
            gameManager = manager;

            if (runnerAnimator != null)
            {
                animator = runnerAnimator;
            }

            ResolveAnimationEventReceiver();
        }

        private void ResolveAnimationEventReceiver()
        {
            animationEventReceiver = animator != null
                ? animator.GetComponent<RunnerAnimationEventReceiver>()
                : GetComponentInChildren<RunnerAnimationEventReceiver>();
        }

        private bool ReadLaneInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                targetLane = Mathf.Max(-1, targetLane - 1);
            }
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                targetLane = Mathf.Min(1, targetLane + 1);
            }

            if ((keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                && characterController.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                isJumping = true;
                return true;
            }

            return false;
        }

        private void MovePlayer()
        {
            bool groundedBeforeMove = characterController.isGrounded;

            if (groundedBeforeMove && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
                isJumping = false;
            }

            verticalVelocity += gravity * Time.deltaTime;

            float targetX = targetLane * laneDistance;
            float xDelta = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime) - transform.position.x;
            Vector3 movement = new Vector3(xDelta, verticalVelocity * Time.deltaTime, 0f);
            characterController.Move(movement);

            bool groundedAfterMove = characterController.isGrounded;
            if (!wasGrounded && groundedAfterMove)
            {
                animationEventReceiver?.PlayLanding();
            }

            wasGrounded = groundedAfterMove;
        }

        private void UpdateAnimator(bool jumpPressed)
        {
            if (animator == null)
            {
                return;
            }

            bool running = gameManager != null && !gameManager.IsGameOver;
            bool grounded = characterController != null && characterController.isGrounded;

            animator.SetFloat(SpeedHash, running ? runningAnimationSpeed : 0f);
            animator.SetFloat(MotionSpeedHash, running ? 1f : 0f);
            animator.SetBool(GroundedHash, grounded);
            animator.SetBool(JumpHash, jumpPressed || isJumping);
            animator.SetBool(FreeFallHash, !grounded && verticalVelocity < 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out RunnerObstacle _))
            {
                gameManager.EndRun();
            }
        }
    }
}
