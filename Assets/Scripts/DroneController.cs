using UnityEngine;
using UnityEngine.XR;

public class FPVDroneController : MonoBehaviour
{
    [Header("Drone Physics")]
    public float maxThrustForce = 50f;
    public float tiltForce = 8f;
    public float yawForce = 25f;
    public float movementSpeed = 5f;

    [Header("Tilt Limits")]
    public float maxForwardTilt = 10f;
    public float maxBackwardTilt = 10f;
    public float maxSideTilt = 20f;

    [Header("Auto Leveling")]
    public bool autoLevel = true;
    public float levelingStrength = 3f;
    public float levelingResponseSpeed = 2f;

    [Header("Stabilization")]
    public bool autoStabilize = true;
    public float stabilizationStrength = 4f;
    public float yawStabilizationMultiplier = 0.1f;

    [Header("Emergency Recovery")]
    public bool enableEmergencyRecovery = true;
    public float recoveryThreshold = 30f;
    public float recoveryStrength = 8f;
    public float userOverrideStrength = 2f;

    [Header("Initial Rotation")]
    public Vector3 initialRotation = new Vector3(-90f, -122f, 0f);

    private Rigidbody rb;
    private Vector2 leftStick;
    private Vector2 rightStick;
    private float currentThrust = 0f;
    private bool isFirstFrame = true;
    private Quaternion targetLevelRotation;
    private bool isUserControlling = false;
    private float timeWithoutControl = 0f;
    private bool isInEmergencyRecovery = false;
    private bool isUpsideDown = false;
    private float upsideDownTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        targetLevelRotation = Quaternion.Euler(initialRotation);
        transform.rotation = Quaternion.Euler(initialRotation);

        if (rb != null)
        {
            rb.mass = 1.2f;
            rb.drag = 0.3f;
            rb.angularDrag = 1.5f;
            rb.useGravity = true;
        }
    }

    void Update()
    {
        GetVRInput();

        bool wasControlling = isUserControlling;
        isUserControlling = Mathf.Abs(leftStick.x) > 0.1f ||
                           Mathf.Abs(rightStick.x) > 0.1f ||
                           Mathf.Abs(rightStick.y) > 0.1f;

        // Проверяем, не перевернут ли дрон
        CheckUpsideDownState();

        if (isUserControlling)
        {
            timeWithoutControl = 0f;
            isInEmergencyRecovery = false;
        }
        else
        {
            timeWithoutControl += Time.deltaTime;
        }

        if (wasControlling && !isUserControlling)
        {
            UpdateLevelDirection();
        }

        if (enableEmergencyRecovery && !isUserControlling)
        {
            CheckEmergencyRecovery();
        }

        if (isInEmergencyRecovery)
        {
            Debug.Log("?? АВАРИЙНОЕ ВЫРАВНИВАНИЕ! Угол: " + GetCurrentTiltAngle().ToString("F1") +
                     (isUpsideDown ? " (ПЕРЕВЕРНУТ!)" : ""));
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        // Перезагружаем текущую сцену
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );

        Debug.Log("🔄 ИГРА ПЕРЕЗАПУЩЕНА");
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (isFirstFrame)
        {
            isFirstFrame = false;
            return;
        }

        ApplyDronePhysics();

        if (autoStabilize)
        {
            StabilizeDrone();
        }

        if (autoLevel)
        {
            AutoLevelDrone();
        }

        if (isInEmergencyRecovery)
        {
            EmergencyRecovery();
        }
    }

    void CheckUpsideDownState()
    {
        // Проверяем, направлен ли "верх" дрона вниз
        float dot = Vector3.Dot(transform.up, Vector3.down);
        bool wasUpsideDown = isUpsideDown;

        if (dot > 0.7f) // Если дрон сильно перевернут
        {
            upsideDownTimer += Time.deltaTime;
            if (upsideDownTimer > 0.5f) // Ждем полсекунды для уверенности
            {
                isUpsideDown = true;
            }
        }
        else
        {
            upsideDownTimer = 0f;
            isUpsideDown = false;
        }

        if (wasUpsideDown != isUpsideDown)
        {
            Debug.Log(isUpsideDown ? "?? ДРОН ПЕРЕВЕРНУТ!" : "? Дрон в нормальном положении");
        }
    }

    void GetVRInput()
    {
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand)
            .TryGetFeatureValue(CommonUsages.primary2DAxis, out leftStick);
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand)
            .TryGetFeatureValue(CommonUsages.primary2DAxis, out rightStick);

        if (isFirstFrame)
        {
            leftStick = Vector2.zero;
            rightStick = Vector2.zero;
        }

        if (leftStick.magnitude < 0.1f && rightStick.magnitude < 0.1f)
        {
            leftStick.y = Input.GetKey(KeyCode.Space) ? 1f : Input.GetKey(KeyCode.LeftControl) ? -1f : 0f;
            leftStick.x = Input.GetKey(KeyCode.Q) ? -1f : Input.GetKey(KeyCode.E) ? 1f : 0f;
            rightStick.x = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
            rightStick.y = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        }

        // Инвертируем управление тягой если дрон перевернут
        float thrustMultiplier = isUpsideDown ? -1f : 1f;
        float targetThrust = leftStick.y * maxThrustForce * thrustMultiplier;
        currentThrust = Mathf.Lerp(currentThrust, targetThrust, Time.fixedDeltaTime * 5f);
    }

    void UpdateLevelDirection()
    {
        // Если дрон перевернут, выравниваемся к перевернутой версии начального поворота
        if (isUpsideDown)
        {
            // Поворачиваем начальную ориентацию на 180 градусов по Z
            Vector3 upsideDownRotation = initialRotation + new Vector3(0f, 0f, 180f);
            targetLevelRotation = Quaternion.Euler(upsideDownRotation);
        }
        else
        {
            targetLevelRotation = Quaternion.Euler(initialRotation);
        }
    }

    void ApplyDronePhysics()
    {
        // 1. ВЕРТИКАЛЬНАЯ ТЯГА
        float gravityCompensation = Physics.gravity.magnitude * rb.mass;
        Vector3 thrustVector = Vector3.up * (currentThrust + gravityCompensation);
        rb.AddForce(thrustVector);

        // 2. ПОВОРОТ (рыскание)
        float yaw = leftStick.x * yawForce;
        rb.AddTorque(Vector3.up * yaw);

        // 3. НАКЛОНЫ
        float pitch = rightStick.y * tiltForce;
        float roll = -rightStick.x * tiltForce;

        // Инвертируем управление наклонами если дрон перевернут
        if (isUpsideDown)
        {
            pitch = -pitch;
            roll = -roll;
        }

        if (GetCurrentTiltAngle() > recoveryThreshold * 0.7f)
        {
            pitch *= userOverrideStrength;
            roll *= userOverrideStrength;
        }

        if (CanTilt(pitch, roll))
        {
            rb.AddTorque(transform.right * pitch);
            rb.AddTorque(transform.forward * roll);
        }

        // 4. ГОРИЗОНТАЛЬНОЕ ДВИЖЕНИЕ
        ApplyHorizontalMovement();
    }

    bool CanTilt(float pitch, float roll)
    {
        float currentTilt = GetCurrentTiltAngle();

        if (isInEmergencyRecovery && currentTilt > recoveryThreshold * 0.5f)
        {
            return false;
        }

        return currentTilt < 80f;
    }

    float GetCurrentTiltAngle()
    {
        Quaternion currentRot = transform.rotation;
        Quaternion targetRot = targetLevelRotation;
        return Quaternion.Angle(currentRot, targetRot);
    }

    void CheckEmergencyRecovery()
    {
        float tiltAngle = GetCurrentTiltAngle();

        // Включаем аварийное выравнивание при сильном наклоне ИЛИ если дрон перевернут
        if (tiltAngle > recoveryThreshold || isUpsideDown || timeWithoutControl > 2f)
        {
            isInEmergencyRecovery = true;
        }

        // Выключаем когда почти выровнялись и не перевернуты
        if (isInEmergencyRecovery && tiltAngle < 5f && !isUpsideDown)
        {
            isInEmergencyRecovery = false;
            Debug.Log("? Дрон выровнен к начальной ориентации");
        }
    }

    void EmergencyRecovery()
    {
        // ОСОБЫЙ РЕЖИМ: если дрон перевернут, сначала пытаемся его перевернуть обратно
        if (isUpsideDown)
        {
            ApplyUpsideDownRecovery();
            return;
        }

        // Обычное аварийное выравнивание
        float tiltAngle = GetCurrentTiltAngle();

        Quaternion currentRot = transform.rotation;
        Quaternion targetRot = targetLevelRotation;

        Vector3 correctionAxis;
        float correctionAngle;
        Quaternion.FromToRotation(currentRot * Vector3.up, targetRot * Vector3.up).ToAngleAxis(out correctionAngle, out correctionAxis);

        Vector3 levelTorque = correctionAxis * correctionAngle * Mathf.Deg2Rad * recoveryStrength;
        rb.AddTorque(levelTorque);

        Vector3 angularVelocity = rb.angularVelocity;
        Vector3 damping = -angularVelocity * recoveryStrength * 0.5f;
        rb.AddTorque(damping);

        rb.angularDrag = 3f;
    }

    void ApplyUpsideDownRecovery()
    {
        // Специальная логика для выхода из перевернутого состояния

        // 1. Сначала пытаемся перевернуть дрон обратно
        Vector3 flipTorque = Vector3.zero;

        // Определяем, в какую сторону проще перевернуть
        float forwardDot = Vector3.Dot(transform.forward, Vector3.up);
        float rightDot = Vector3.Dot(transform.right, Vector3.up);

        if (Mathf.Abs(forwardDot) > Mathf.Abs(rightDot))
        {
            // Переворачиваем через pitch
            flipTorque = transform.right * Mathf.Sign(forwardDot) * recoveryStrength * 2f;
        }
        else
        {
            // Переворачиваем через roll
            flipTorque = transform.forward * Mathf.Sign(rightDot) * recoveryStrength * 2f;
        }

        rb.AddTorque(flipTorque);

        // 2. Гасим все вращения
        Vector3 angularVelocity = rb.angularVelocity;
        Vector3 damping = -angularVelocity * recoveryStrength;
        rb.AddTorque(damping);

        // 3. Уменьшаем тягу чтобы не улететь
        currentThrust = Mathf.Lerp(currentThrust, maxThrustForce * 0.3f, Time.fixedDeltaTime * 3f);

        rb.angularDrag = 4f;
    }

    void ApplyHorizontalMovement()
    {
        if ((Mathf.Abs(rightStick.y) > 0.1f || Mathf.Abs(rightStick.x) > 0.1f) && currentThrust > 5f)
        {
            Vector3 moveDirection = Vector3.zero;

            if (Mathf.Abs(rightStick.y) > 0.1f)
            {
                Vector3 forwardDir = transform.forward;
                forwardDir.y = 0;
                if (forwardDir.magnitude > 0.1f)
                {
                    forwardDir.Normalize();
                    moveDirection += forwardDir * rightStick.y;
                }
            }

            if (Mathf.Abs(rightStick.x) > 0.1f)
            {
                Vector3 rightDir = transform.right;
                rightDir.y = 0;
                if (rightDir.magnitude > 0.1f)
                {
                    rightDir.Normalize();
                    moveDirection += rightDir * rightStick.x;
                }
            }

            if (moveDirection.magnitude > 0.1f)
            {
                moveDirection.Normalize();
                float horizontalPower = movementSpeed * (currentThrust / maxThrustForce);
                rb.AddForce(moveDirection * horizontalPower * 0.5f, ForceMode.VelocityChange);
            }
        }
    }

    void AutoLevelDrone()
    {
        if (!isUserControlling && !isInEmergencyRecovery)
        {
            ApplyLeveling(levelingStrength);
        }
        else if (isUserControlling)
        {
            ApplyLeveling(levelingStrength * 0.3f);
        }
    }

    void ApplyLeveling(float strength)
    {
        rb.angularDrag = 1.5f;

        Quaternion currentRot = transform.rotation;
        Quaternion targetRot = targetLevelRotation;

        Quaternion rotDifference = targetRot * Quaternion.Inverse(currentRot);

        Vector3 levelAxis;
        float levelAngle;
        rotDifference.ToAngleAxis(out levelAngle, out levelAxis);

        if (levelAngle > 180f)
            levelAngle -= 360f;

        if (Mathf.Abs(levelAngle) > 1f)
        {
            Vector3 levelTorque = levelAxis * (levelAngle * Mathf.Deg2Rad * strength * levelingResponseSpeed * 0.01f);
            rb.AddTorque(levelTorque);
        }
    }

    void StabilizeDrone()
    {
        Vector3 angularVelocity = rb.angularVelocity;

        Vector3 stabilization = new Vector3(
            -angularVelocity.x * stabilizationStrength,
            -angularVelocity.y * stabilizationStrength * yawStabilizationMultiplier,
            -angularVelocity.z * stabilizationStrength
        );
        rb.AddTorque(stabilization);
    }

    public void ForceRecovery()
    {
        isInEmergencyRecovery = true;
        timeWithoutControl = 0f;

        // Принудительно сбрасываем перевернутое состояние
        if (isUpsideDown)
        {
            Debug.Log("?? ПРИНУДИТЕЛЬНЫЙ ПЕРЕВОРОТ ДРОНА");
            // Добавляем сильный момент для переворота
            rb.AddTorque(transform.right * recoveryStrength * 3f);
        }
        else
        {
            Debug.Log("?? ПРИНУДИТЕЛЬНЫЙ СБРОС ДРОНА");
        }
    }

    // Экстренный переворот дрона
    public void EmergencyFlip()
    {
        isUpsideDown = true;
        isInEmergencyRecovery = true;
        upsideDownTimer = 1f; // Принудительно устанавливаем таймер
        Debug.Log("?? АВАРИЙНЫЙ ПЕРЕВОРОТ АКТИВИРОВАН");
    }
}