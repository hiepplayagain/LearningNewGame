using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{

    #region Variables

    Vector2 _rotationInput;
    Vector2 _zoomInput;


    [Header("Camera Settings")]
    public Transform player;                // Transform của player
    [Range(1f, 100f)]
    public float mouseSensitivity = 100f;   // Độ nhạy chuột
    [SerializeField] float minVerticalAngle = -90f;   // Góc nhìn xuống tối đa
    [SerializeField] float maxVerticalAngle = 90f;    // Góc nhìn lên tối đa

    [Header("Zoom Settings")]
    public float defaultDistance = 5f;      // Khoảng cách mặc định
    [SerializeField] float minDistance = 2f;          // Khoảng cách tối thiểu
    [SerializeField] float maxDistance = 10f;         // Khoảng cách tối đa
    [SerializeField] float zoomSpeed = 2f;            // Tốc độ zoom

    [Header("Collision Settings")]
    public LayerMask collisionLayers = -1;  // Layer để check va chạm
    public float collisionRadius = 0.3f;    // Bán kính kiểm tra va chạm
    public float smoothTime = 0.1f;         // Thời gian smooth khi di chuyển camera

    [Header("Camera Offset")]
    
    private float horizontalAngle = 0f;     // Góc quay ngang
    private float verticalAngle = 0f;       // Góc quay dọc
    private float currentDistance;          // Khoảng cách hiện tại
    private float targetDistance;           // Khoảng cách mục tiêu
    private Vector3 currentPosition;        // Vị trí camera hiện tại
    private Vector3 targetPosition;         // Vị trí camera mục tiêu
    private Vector3 velocity = Vector3.zero; // Velocity cho smooth damp

    InputSystemManagement _cameraController;

    #endregion

    // Planar rotation properties
    public float PlanarAngle => horizontalAngle;  // Góc xoay ngang (Y-axis)
    public Vector3 CameraForward => GetCameraPlanarForward();
    public Vector3 CameraRight => GetCameraPlanarRight();

    private void Awake()
    {
        _cameraController = new();

    }

    void Start()
    {
        // Khởi tạo các giá trị ban đầu
        currentDistance = defaultDistance;
        targetDistance = defaultDistance;

        // Ẩn và khóa cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }


    #region Get values from Input system

    private void OnEnable()
    {
        _cameraController.Enable();
        _cameraController.Camera.Rotation.performed += ctx => _rotationInput = ctx.ReadValue<Vector2>();
        _cameraController.Camera.Rotation.canceled += ctx => _rotationInput = Vector2.zero;
        _cameraController.Camera.Zoom.performed += ctx => _zoomInput = ctx.ReadValue<Vector2>();
        _cameraController.Camera.Zoom.canceled += ctx => _zoomInput = Vector2.zero;

    }

    private void OnDisable()
    {
        _cameraController.Camera.Rotation.performed -= ctx => _rotationInput = ctx.ReadValue<Vector2>();
        _cameraController.Camera.Rotation.canceled -= ctx => _rotationInput = Vector2.zero;
        _cameraController.Camera.Zoom.performed -= ctx => _zoomInput = ctx.ReadValue<Vector2>();
        _cameraController.Camera.Zoom.canceled -= ctx => _zoomInput = Vector2.zero;
        _cameraController.Disable();

    }
    #endregion
    void Update()
    {
        if (player == null) return;

        HandleInput();
        UpdateCameraPosition();
    }

    void HandleInput()
    {
        // Xử lý input chuột để xoay camera
        float mouseX = _rotationInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = _rotationInput.y * mouseSensitivity * Time.deltaTime;

        horizontalAngle += mouseX;
        verticalAngle -= mouseY; // Trừ để đảo ngược trục Y

        // Giới hạn góc nhìn dọc
        verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);

        // Xử lý zoom với scroll wheel
        float scroll = _zoomInput.y;
        targetDistance -= scroll * zoomSpeed;
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

        // Smooth zoom
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * 5f);

        // Mở khóa cursor khi nhấn ESC
        if (_cameraController.Camera.ActiveCursor.WasPressedThisFrame())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
           
        }
        else if (_cameraController.Camera.ActiveCursor.WasReleasedThisFrame())
        {
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void UpdateCameraPosition()
    {
        // Tính toán vị trí mục tiêu của camera dựa trên góc quay
        Vector3 direction = new(0, 0, -currentDistance);
        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);

        // Vị trí mục tiêu (chưa check collision)
        Vector3 playerPosition = player.position;
        targetPosition = playerPosition + rotation * direction;

        // Kiểm tra va chạm và điều chỉnh vị trí camera
        CheckCollisionAndAdjust(playerPosition);

        // Smooth camera movement
        currentPosition = Vector3.SmoothDamp(currentPosition, targetPosition, ref velocity, smoothTime);

        // Cập nhật vị trí và hướng nhìn của camera
        transform.position = currentPosition;
        transform.LookAt(playerPosition);
    }

    void CheckCollisionAndAdjust(Vector3 playerPosition)
    {
        Vector3 directionToCamera = (targetPosition - playerPosition).normalized;
        float distanceToTarget = Vector3.Distance(playerPosition, targetPosition);

        // Raycast từ player đến vị trí camera mục tiêu
        RaycastHit hit;
        if (Physics.SphereCast(playerPosition, collisionRadius, directionToCamera,
                              out hit, distanceToTarget, collisionLayers))
        {
            // Nếu có va chạm, đặt camera trước vật cản một chút
            float safeDistance = hit.distance - collisionRadius * 1.2f;
            safeDistance = Mathf.Max(safeDistance, minDistance);
            targetPosition = playerPosition + directionToCamera * safeDistance;
        }

        // Kiểm tra thêm bằng cách raycast ngược từ camera về player
        Vector3 reverseDirection = (playerPosition - targetPosition).normalized;
        float reverseDistance = Vector3.Distance(targetPosition, playerPosition);

        if (Physics.SphereCast(targetPosition, collisionRadius, reverseDirection,
                              out hit, reverseDistance, collisionLayers))
        {
            // Điều chỉnh vị trí nếu cần thiết
            targetPosition = hit.point + hit.normal * collisionRadius;
        }
    }

    void LateUpdate()
    {
        // Đảm bảo camera luôn nhìn về phía player
        if (player != null)
        {
            transform.LookAt(player.position);
        }
    }

    // Phương thức để set target từ script khác
    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
    }

    // Phương thức để điều chỉnh khoảng cách camera
    public void SetDistance(float distance)
    {
        targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    // Phương thức để điều chỉnh độ nhạy chuột
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }

    // PLANAR ROTATION METHODS - Để player lấy góc xoay camera

    /// <summary>
    /// Lấy góc xoay ngang của camera (Y-axis rotation)
    /// </summary>
    public float GetPlanarAngle()
    {
        return horizontalAngle;
    }

    /// <summary>
    /// Lấy góc xoay ngang của camera ở dạng 0-360 độ
    /// </summary>
    public float GetPlanarAngleNormalized()
    {
        float angle = horizontalAngle % 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }

    /// <summary>
    /// Lấy vector hướng forward của camera trên mặt phẳng ngang (bỏ qua trục Y)
    /// </summary>
    public Vector3 GetCameraPlanarForward()
    {
        Vector3 forward = Quaternion.Euler(0, horizontalAngle, 0) * Vector3.forward;
        return forward.normalized;
    }

    /// <summary>
    /// Lấy vector hướng right của camera trên mặt phẳng ngang
    /// </summary>
    public Vector3 GetCameraPlanarRight()
    {
        Vector3 right = Quaternion.Euler(0, horizontalAngle, 0) * Vector3.right;
        return right.normalized;
    }

    /// <summary>
    /// Lấy Quaternion rotation chỉ cho trục Y (planar rotation)
    /// </summary>
    public Quaternion GetPlanarRotation()
    {
        return Quaternion.Euler(0, horizontalAngle, 0);
    }

    /// <summary>
    /// Set góc xoay ngang cho camera (hữu ích khi cần đồng bộ với player)
    /// </summary>
    public void SetPlanarAngle(float angle)
    {
        horizontalAngle = angle;
    }

    /// <summary>
    /// Thêm góc xoay ngang vào camera hiện tại
    /// </summary>
    public void AddPlanarAngle(float deltaAngle)
    {
        horizontalAngle += deltaAngle;
    }

    // Hiển thị gizmos trong Scene view để debug
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, 0.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(player.position, transform.position);
        }
    }
}