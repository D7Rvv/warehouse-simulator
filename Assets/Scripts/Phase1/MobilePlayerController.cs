using UnityEngine;

namespace WarehouseSimulator.Phase1
{
    /// <summary>
    /// No physics Rigidbody or per-frame allocations. Touch HUD feeds move/look input.
    /// All movement takes place in Update (CharacterController handles walls).
    /// </summary>
    public sealed class MobilePlayerController : MonoBehaviour
    {
        private CharacterController _controller;
        private Camera _camera;
        private float _yaw;
        private float _pitch;
        private float _verticalSpeed;
        private float _overheadHeight = 19f;
        private Vector2 _touchMovement;
        private Vector2 _mouseLast;
        private bool _mouseDragging;

        public bool IsOverhead { get; private set; }
        public float MoveSpeed = 4f;
        public float LookSensitivity = 0.12f;

        public void Initialize(CharacterController controller, Camera targetCamera)
        {
            _controller = controller;
            _camera = targetCamera;
            _yaw = transform.eulerAngles.y;
        }

        public void SetMove(Vector2 move) => _touchMovement = Vector2.ClampMagnitude(move, 1f);

        public void LookDelta(Vector2 pixels)
        {
            if (IsOverhead)
            {
                // Overhead dragging pans the camera's anchor position.
                var anchor = transform.position;
                anchor.x = Mathf.Clamp(anchor.x - pixels.x * .025f, -14f, 14f);
                anchor.z = Mathf.Clamp(anchor.z - pixels.y * .025f, -11f, 11f);
                transform.position = anchor;
                return;
            }
            _yaw += pixels.x * LookSensitivity;
            _pitch = Mathf.Clamp(_pitch - pixels.y * LookSensitivity, -78f, 78f);
        }

        public void ToggleView() => IsOverhead = !IsOverhead;

        public void Zoom(float delta) => _overheadHeight = Mathf.Clamp(_overheadHeight + delta, 11f, 29f);

        private void Update()
        {
            if (_controller == null || _camera == null) return;
            var dt = Mathf.Min(Time.deltaTime, .05f);

#if ENABLE_LEGACY_INPUT_MANAGER
            var desktopMove = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (Input.GetKeyDown(KeyCode.V)) ToggleView();
            if (Input.GetMouseButtonDown(0) && !MobileHud.IsPointerOnHud(Input.mousePosition))
            {
                _mouseDragging = true;
                _mouseLast = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0)) _mouseDragging = false;
            if (_mouseDragging && Input.GetMouseButton(0))
            {
                var current = (Vector2)Input.mousePosition;
                LookDelta((current - _mouseLast) * new Vector2(1f, -1f));
                _mouseLast = current;
            }
            if (IsOverhead) Zoom(Input.mouseScrollDelta.y * -1.25f);
#else
            var desktopMove = Vector2.zero;
#endif
            if (!IsOverhead)
            {
                var move = Vector2.ClampMagnitude(_touchMovement + desktopMove, 1f);
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                _verticalSpeed = _controller.isGrounded ? -1f : Mathf.Max(_verticalSpeed - 19.6f * dt, -20f);
                var velocity = transform.right * (move.x * MoveSpeed) + transform.forward * (move.y * MoveSpeed);
                velocity.y = _verticalSpeed;
                _controller.Move(velocity * dt);
                // Keep the character inside the playable warehouse in this blockout.
                var p = transform.position;
                p.x = Mathf.Clamp(p.x, -16.25f, 16.25f);
                p.z = Mathf.Clamp(p.z, -12.7f, 12.7f);
                transform.position = p;
                _camera.transform.SetPositionAndRotation(transform.position + Vector3.up * 1.62f, Quaternion.Euler(_pitch, _yaw, 0f));
            }
            else
            {
                var origin = transform.position;
                var position = origin + new Vector3(0f, _overheadHeight, -_overheadHeight * .48f);
                _camera.transform.position = position;
                _camera.transform.LookAt(new Vector3(origin.x, 0f, origin.z));
            }
        }
    }
}
