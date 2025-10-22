using UnityEngine;
using UnityEngine.XR;

public class FPVDroneController : MonoBehaviour
{
    [Header("Drone Physics")]
    public float thrustForce = 60f;
    public float pitchForce = 50f;
    public float rollForce = 50f;
    public float yawForce = 30f;
    public float movementForce = 280f; // НОВЫЙ ПАРАМЕТР - сила движения

    [Header("Stabilization")]
    public bool autoStabilize = true;
    public float stabilizationStrength = 1.5f;

    private Rigidbody rb;
    private Vector2 leftStick;
    private Vector2 rightStick;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.mass = 1f;
            rb.drag = 0.8f;
            rb.angularDrag = 0.8f;
            rb.useGravity = true;
        }
    }

    void Update()
    {
        GetVRInput();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        ApplyDronePhysics();

        if (autoStabilize)
        {
            StabilizeDrone();
        }
    }

    void GetVRInput()
    {
        // Получаем ввод с VR контроллеров
        bool leftFound = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand)
            .TryGetFeatureValue(CommonUsages.primary2DAxis, out leftStick);
        bool rightFound = InputDevices.GetDeviceAtXRNode(XRNode.RightHand)
            .TryGetFeatureValue(CommonUsages.primary2DAxis, out rightStick);

        // Резервное управление с клавиатуры для теста
        if (!leftFound || !rightFound || (leftStick.magnitude < 0.1f && rightStick.magnitude < 0.1f))
        {
            // ЛЕВЫЙ СТИК
            leftStick.y = Input.GetKey(KeyCode.Space) ? 1f : Input.GetKey(KeyCode.LeftControl) ? -1f : 0f;
            leftStick.x = Input.GetKey(KeyCode.Q) ? -1f : Input.GetKey(KeyCode.E) ? 1f : 0f;

            // ПРАВЫЙ СТИК  
            rightStick.x = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
            rightStick.y = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        }
    }

    void ApplyDronePhysics()
    {
        // 1. ВЗЛЕТ/ПОСАДКА - Левый стик ВВЕРХ/ВНИЗ
        float thrust = leftStick.y * thrustForce;
        rb.AddForce(Vector3.up * thrust);

        // 2. ПОВОРОТ - Левый стик ВЛЕВО/ВПРАВО
        float yaw = leftStick.x * yawForce;
        rb.AddTorque(Vector3.up * yaw);

        // 3. НАКЛОНЫ ВПЕРЕД/НАЗАД И ВЛЕВО/ВПРАВО
        float pitch = rightStick.y * pitchForce;
        float roll = -rightStick.x * rollForce;
        rb.AddTorque(transform.right * pitch + transform.forward * roll);

        // 4. ДВИЖЕНИЕ ВПЕРЕД/НАЗАД/ВБОК - ОСНОВНОЕ ИЗМЕНЕНИЕ!
        // Когда дрон наклонен - он двигается в этом направлении
        Vector3 moveDirection = Vector3.zero;

        // Движение вперед/назад (ось Z) - зависит от наклона вперед/назад
        moveDirection += transform.forward * rightStick.y * movementForce;

        // Движение влево/вправо (ось X) - зависит от наклона вбок  
        moveDirection += transform.right * rightStick.x * movementForce;

        // Применяем движение
        rb.AddForce(moveDirection);
    }

    void StabilizeDrone()
    {
        Vector3 currentAngularVelocity = rb.angularVelocity;
        rb.AddTorque(-currentAngularVelocity * stabilizationStrength);
    }
}