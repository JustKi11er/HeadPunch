using UnityEngine;

public class WeaponSwingController : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float damageMultiplier = 1;
    public float followSpeed = 10f;
    public float swingForce = 2000f;
    public float maxVelocity = 15f;
    public float damping = 0.95f;

    [Header("Mouse Control")]
    public float mouseSensitivity = 1f;
    public float screenDepth = 5f;
    public LayerMask hitLayers = -1;

    [Header("Pickup Settings")]
    public float pickupRange = 10f;
    public Color highlightColor = Color.yellow;
    public float highlightIntensity = 1.5f;

    [Header("Effects")]
    public float hitForceMultiplier = 500f;
    public float cameraShakeIntensity = 0.3f;
    public float cameraShakeDuration = 0.2f;

    [Header("Audio")]
    public AudioClip[] swingSounds;
    public AudioClip[] hitSounds;
    public AudioClip pickupSound;
    public AudioClip dropSound;
    public float minSwingVelocity = 2f;

    private Rigidbody rb;
    private Camera playerCamera;
    private AudioSource audioSource;
    private Renderer weaponRenderer;
    private Material weaponMaterial;
    private Color originalColor;

    // Состояния оружия
    private bool isHeld = false;
    private bool canBePickedUp = false;
    private Vector3 targetPosition;
    private Vector3 lastMouseWorldPos;
    private Vector3 weaponVelocity;
    private float lastSoundTime;
    private bool isSwinging = false;

    // Для тряски камеры
    private Vector3 originalCameraPos;
    private float shakeTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        weaponRenderer = GetComponent<Renderer>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (rb == null)
        {
            Debug.LogError("WeaponSwingController requires a Rigidbody component!");
            enabled = false;
            return;
        }

        // Сохраняем оригинальный материал и цвет
        if (weaponRenderer != null)
        {
            weaponMaterial = weaponRenderer.material;
            originalColor = weaponMaterial.color;
        }

        // Настройка физики для брошенного состояния
        SetDroppedState();

        originalCameraPos = playerCamera.transform.localPosition;
    }

    void Update()
    {
        CheckForPickup();
        HandleMouseInput();
        UpdateHighlight();
        UpdateCameraShake();
    }

    void FixedUpdate()
    {
        if (isHeld)
        {
            MoveWeaponToTarget();
            CalculateVelocity();
            PlaySwingSound();
        }
    }

    void CheckForPickup()
    {
        if (!isHeld)
        {
            // Проверяем, наведен ли курсор на оружие
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, pickupRange))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    canBePickedUp = true;
                    return;
                }
            }

            canBePickedUp = false;
        }
    }

    void HandleMouseInput()
    {
        // Подбираем оружие при клике
        if (Input.GetMouseButtonDown(0))
        {
            if (!isHeld && canBePickedUp)
            {
                PickupWeapon();
            }
        }

        // Обновляем позицию при удержании
        if (Input.GetMouseButton(0) && isHeld)
        {
            UpdateWeaponTarget();
        }

        // Бросаем оружие при отпускании кнопки
        if (Input.GetMouseButtonUp(0) && isHeld)
        {
            DropWeapon();
        }
    }

    void PickupWeapon()
    {
        isHeld = true;
        canBePickedUp = false;

        // Настройка физики для удержания
        rb.useGravity = false;
        rb.drag = 2f;
        rb.angularDrag = 5f;

        // Начальная позиция
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = screenDepth;
        targetPosition = playerCamera.ScreenToWorldPoint(mousePos);
        lastMouseWorldPos = targetPosition;

        // Звук подбора
        if (pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }

        Debug.Log("Weapon picked up!");
    }

    void DropWeapon()
    {
        isHeld = false;
        isSwinging = false;

        SetDroppedState();

        // Звук падения
        if (dropSound != null)
        {
            audioSource.PlayOneShot(dropSound);
        }

        Debug.Log("Weapon dropped!");
    }

    void SetDroppedState()
    {
        // Настройка физики для брошенного состояния
        rb.useGravity = true;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
    }

    void UpdateWeaponTarget()
    {
        // Получаем позицию мыши в мировых координатах
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = screenDepth;
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(mousePos);

        targetPosition = mouseWorldPos;

        // Определяем, машет ли игрок оружием
        float mouseMovement = Vector3.Distance(mouseWorldPos, lastMouseWorldPos);
        isSwinging = mouseMovement > 0.1f;

        lastMouseWorldPos = mouseWorldPos;
    }

    void UpdateHighlight()
    {
        if (weaponMaterial != null)
        {
            if (canBePickedUp && !isHeld)
            {
                // Подсвечиваем оружие когда можно подобрать
                Color highlightedColor = originalColor * highlightIntensity;
                highlightedColor = Color.Lerp(originalColor, highlightColor, Mathf.PingPong(Time.time * 2f, 1f));
                weaponMaterial.color = highlightedColor;
            }
            else
            {
                // Возвращаем оригинальный цвет
                weaponMaterial.color = originalColor;
            }
        }
    }

    void MoveWeaponToTarget()
    {
        // Плавно перемещаем оружие к целевой позиции
        Vector3 direction = (targetPosition - transform.position);
        float distance = direction.magnitude;

        if (distance > 0.1f)
        {
            // Применяем силу для движения к цели
            Vector3 force = direction.normalized * followSpeed * distance;
            force = Vector3.ClampMagnitude(force, maxVelocity);

            rb.AddForce(force, ForceMode.Force);

            // Поворачиваем оружие в направлении движения
            if (rb.velocity.magnitude > 0.5f)
            {
                Vector3 lookDirection = rb.velocity.normalized;
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
            }
        }

        // Применяем затухание
        rb.velocity *= damping;
        rb.angularVelocity *= damping;
    }

    void CalculateVelocity()
    {
        weaponVelocity = rb.velocity;
    }

    void PlaySwingSound()
    {
        if (isSwinging && weaponVelocity.magnitude > minSwingVelocity &&
            Time.time - lastSoundTime > 0.3f && swingSounds.Length > 0)
        {
            AudioClip randomSwing = swingSounds[Random.Range(0, swingSounds.Length)];
            audioSource.pitch = Random.Range(0.8f, 1.2f);
            audioSource.PlayOneShot(randomSwing, 0.5f);
            lastSoundTime = Time.time;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Только наносим урон если оружие в руках
        if (isHeld && ((1 << collision.gameObject.layer) & hitLayers) != 0)
        {
            HandleHit(collision);
        }
    }

    void HandleHit(Collision collision)
    {
        float hitStrength = weaponVelocity.magnitude * damageMultiplier;

        // Получаем точку контакта и направление удара
        ContactPoint contact = collision.contacts[0];
        Vector3 hitPoint = contact.point;
        Vector3 hitDirection = contact.normal * -1f;

        // Применяем силу к объекту
        Rigidbody hitRb = collision.gameObject.GetComponent<Rigidbody>();
        if (hitRb != null)
        {
            Vector3 hitForce = hitDirection * hitStrength * hitForceMultiplier;
            hitRb.AddForceAtPosition(hitForce, hitPoint, ForceMode.Impulse);

            Vector3 torqueDirection = Vector3.Cross((hitPoint - hitRb.worldCenterOfMass), hitDirection);
            hitRb.AddTorque(torqueDirection * hitStrength * 50f, ForceMode.Impulse);
        }

        // Эффекты удара
        PlayHitEffects(hitPoint, hitStrength);
        StartCameraShake(hitStrength);

        // Компонент урона
        HitTarget hitTarget = collision.gameObject.GetComponent<HitTarget>();
        if (hitTarget != null)
        {
            float damage = hitStrength * 10f;
            hitTarget.TakeDamage(damage, hitPoint, hitDirection * hitStrength);
        }

        Debug.Log($"Hit {collision.gameObject.name} with force: {hitStrength}");
    }

    void PlayHitEffects(Vector3 hitPoint, float intensity)
    {
        if (hitSounds.Length > 0)
        {
            AudioClip randomHit = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(randomHit, Mathf.Clamp01(intensity / 10f));
        }
    }

    void StartCameraShake(float intensity)
    {
        shakeTimer = cameraShakeDuration;
    }

    void UpdateCameraShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;

            Vector3 shakeOffset = Random.insideUnitSphere * cameraShakeIntensity * (shakeTimer / cameraShakeDuration);
            playerCamera.transform.localPosition = originalCameraPos + shakeOffset;
        }
        else
        {
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition,
                originalCameraPos,
                Time.deltaTime * 10f
            );
        }
    }
}
