using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EndlessRunner
{
    public readonly struct RunnerInputFrame
    {
        public RunnerInputFrame(int laneChange, bool jumpPressed)
        {
            LaneChange = Mathf.Clamp(laneChange, -1, 1);
            JumpPressed = jumpPressed;
        }

        public int LaneChange { get; }
        public bool JumpPressed { get; }
    }

    public class RunnerInputReader : MonoBehaviour
    {
        [Header("Touch / WebGL")]
        [SerializeField] private bool enableTouchControls = true;
        [SerializeField] private bool enableMouseGestures = true;
        [SerializeField] private bool tapSidesToMove = true;
        [SerializeField, Range(0.02f, 0.3f)] private float minimumSwipeDistance = 0.08f;
        [SerializeField, Range(0.01f, 0.15f)] private float maximumTapMovement = 0.035f;
        [SerializeField, Min(0.05f)] private float maximumTapDuration = 0.35f;
        [SerializeField, Min(1f)] private float verticalSwipeBias = 1.15f;

        private readonly List<RaycastResult> uiRaycastResults = new();
        private GestureSource activeGestureSource;
        private Vector2 gestureStartPosition;
        private float gestureStartTime;
        private bool gestureResolved;
        private bool gestureBlockedByUi;

        private enum GestureSource
        {
            None,
            Touch,
            Mouse
        }

        public RunnerInputFrame ReadFrameInput()
        {
            int laneChange = 0;
            bool jumpPressed = false;

            ReadKeyboard(ref laneChange, ref jumpPressed);

            if (!enableTouchControls)
            {
                return new RunnerInputFrame(laneChange, jumpPressed);
            }

            bool touchReportedThisFrame = ReadTouch(ref laneChange, ref jumpPressed);
            if (enableMouseGestures && !touchReportedThisFrame)
            {
                ReadMouse(ref laneChange, ref jumpPressed);
            }

            return new RunnerInputFrame(laneChange, jumpPressed);
        }

        private static void ReadKeyboard(ref int laneChange, ref bool jumpPressed)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                laneChange = -1;
            }
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                laneChange = 1;
            }

            jumpPressed |= keyboard.spaceKey.wasPressedThisFrame
                || keyboard.wKey.wasPressedThisFrame
                || keyboard.upArrowKey.wasPressedThisFrame;
        }

        private bool ReadTouch(ref int laneChange, ref bool jumpPressed)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            UnityEngine.InputSystem.TouchPhase phase = touchscreen.primaryTouch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.None)
            {
                return false;
            }

            Vector2 position = touchscreen.primaryTouch.position.ReadValue();
            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    BeginGesture(GestureSource.Touch, position);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    ResolveSwipeIfReady(GestureSource.Touch, position, ref laneChange, ref jumpPressed);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                    EndGesture(GestureSource.Touch, position, ref laneChange, ref jumpPressed);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    CancelGesture(GestureSource.Touch);
                    break;
            }

            return true;
        }

        private void ReadMouse(ref int laneChange, ref bool jumpPressed)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                BeginGesture(GestureSource.Mouse, position);
            }

            if (mouse.leftButton.isPressed)
            {
                ResolveSwipeIfReady(GestureSource.Mouse, position, ref laneChange, ref jumpPressed);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                EndGesture(GestureSource.Mouse, position, ref laneChange, ref jumpPressed);
            }
        }

        private void BeginGesture(GestureSource source, Vector2 position)
        {
            activeGestureSource = source;
            gestureStartPosition = position;
            gestureStartTime = Time.unscaledTime;
            gestureResolved = false;
            gestureBlockedByUi = IsPointerOverUi(position);
        }

        private void ResolveSwipeIfReady(
            GestureSource source,
            Vector2 position,
            ref int laneChange,
            ref bool jumpPressed)
        {
            if (activeGestureSource != source || gestureResolved || gestureBlockedByUi)
            {
                return;
            }

            RunnerInputFrame gesture = ClassifyGesture(
                gestureStartPosition,
                position,
                Time.unscaledTime - gestureStartTime,
                false);
            if (gesture.LaneChange == 0 && !gesture.JumpPressed)
            {
                return;
            }

            MergeGesture(gesture, ref laneChange, ref jumpPressed);
            gestureResolved = true;
        }

        private void EndGesture(
            GestureSource source,
            Vector2 position,
            ref int laneChange,
            ref bool jumpPressed)
        {
            if (activeGestureSource != source)
            {
                return;
            }

            if (!gestureResolved && !gestureBlockedByUi)
            {
                RunnerInputFrame gesture = ClassifyGesture(
                    gestureStartPosition,
                    position,
                    Time.unscaledTime - gestureStartTime,
                    true);
                MergeGesture(gesture, ref laneChange, ref jumpPressed);
            }

            ResetGesture();
        }

        private void CancelGesture(GestureSource source)
        {
            if (activeGestureSource == source)
            {
                ResetGesture();
            }
        }

        private RunnerInputFrame ClassifyGesture(
            Vector2 startPosition,
            Vector2 endPosition,
            float duration,
            bool allowTap)
        {
            float screenReference = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height));
            float swipeDistance = minimumSwipeDistance * screenReference;
            float tapDistance = maximumTapMovement * screenReference;
            Vector2 delta = endPosition - startPosition;
            float absoluteX = Mathf.Abs(delta.x);
            float absoluteY = Mathf.Abs(delta.y);

            if (delta.y >= swipeDistance && absoluteY >= absoluteX * verticalSwipeBias)
            {
                return new RunnerInputFrame(0, true);
            }

            if (absoluteX >= swipeDistance && absoluteX > absoluteY)
            {
                return new RunnerInputFrame(delta.x < 0f ? -1 : 1, false);
            }

            if (allowTap
                && tapSidesToMove
                && duration <= maximumTapDuration
                && delta.sqrMagnitude <= tapDistance * tapDistance)
            {
                return new RunnerInputFrame(startPosition.x < Screen.width * 0.5f ? -1 : 1, false);
            }

            return default;
        }

        private static void MergeGesture(
            RunnerInputFrame gesture,
            ref int laneChange,
            ref bool jumpPressed)
        {
            if (laneChange == 0)
            {
                laneChange = gesture.LaneChange;
            }

            jumpPressed |= gesture.JumpPressed;
        }

        private bool IsPointerOverUi(Vector2 position)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            PointerEventData pointerEventData = new(eventSystem)
            {
                position = position
            };

            uiRaycastResults.Clear();
            eventSystem.RaycastAll(pointerEventData, uiRaycastResults);
            return uiRaycastResults.Count > 0;
        }

        private void ResetGesture()
        {
            activeGestureSource = GestureSource.None;
            gestureResolved = false;
            gestureBlockedByUi = false;
        }
    }
}
