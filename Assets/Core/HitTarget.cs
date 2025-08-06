using UnityEngine;

public class HitTarget : MonoBehaviour, IDamageable
{
    [Header("Target Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool destroyOnDeath = true;

    [Header("Physics Settings")]
    public bool useGravity = true;

    [Header("Jiggle Bone Integration")]
    public JiggleBone jiggleBone; // Ссылка на JiggleBone компонент
    public float jiggleImpulseMultiplier = 1f; // Множитель силы для JiggleBone

    [Header("Visual Effects")]
    public Color normalColor = Color.white;
    public Color damagedColor = Color.red;
    public float colorFlashDuration = 0.2f;
    public DamageText damageText;
    public ParticleSystem particle;
    [Header("Audio")]
    public AudioClip[] hitSounds;

    private Renderer targetRenderer;
    private Material targetMaterial;
    private AudioSource audioSource;
    private float flashTimer = 0f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        currentHealth = maxHealth;

        // Настройка компонентов
        targetRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (targetRenderer != null)
        {
            targetMaterial = targetRenderer.material;
        }

        // Сохраняем исходное положение
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Убеждаемся, что у объекта есть коллайдер
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"{gameObject.name} doesn't have a Collider! Adding BoxCollider.");
            gameObject.AddComponent<BoxCollider>();
        }

        // Проверяем, назначен ли JiggleBone
        if (jiggleBone == null)
        {
            // Попытаемся найти JiggleBone на дочерних объектах
            jiggleBone = GetComponentInChildren<JiggleBone>();
            if (jiggleBone == null)
            {
                Debug.LogWarning($"No JiggleBone found on {gameObject.name} or its children. Jiggle effect will not work.");
            }
        }
    }

    void Update()
    {
        UpdateVisualEffects();
    }

    public void TakeDamage(float damage)
    {
        // Передаем вызов с дефолтными значениями, если не указаны
        TakeDamage(damage, transform.position, Vector3.up);
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitForce)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        // Запускаем эффект вспышки
        flashTimer = colorFlashDuration;

        // Применяем импульс к JiggleBone для эффекта "отскока/колебания"
        if (jiggleBone != null)
        {
            Vector3 jiggleImpulseDirection = (transform.position - hitPoint).normalized;
            jiggleBone.ApplyImpulse(jiggleImpulseDirection * hitForce.magnitude * jiggleImpulseMultiplier);
        }

        // Звук удара
        PlayHitSound();
        Instantiate(particle, hitPoint, Quaternion.identity);
        
        if (damageText != null)
        {
            damageText.GenerateDamageText(Mathf.RoundToInt(damage).ToString(), hitPoint);
        }

        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            //Die();
        }
    }

    void PlayHitSound()
    {
        if (hitSounds.Length > 0 && audioSource != null)
        {
            AudioClip randomHit = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.pitch = Random.Range(0.8f, 1.2f);
            audioSource.PlayOneShot(randomHit);
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} destroyed!");

        

        if (destroyOnDeath)
        {
            // Уничтожаем через небольшую задержку, чтобы показать эффект
            Destroy(gameObject, 2f);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void UpdateVisualEffects()
    {
        if (targetMaterial != null && flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;

            // Интерполируем между нормальным и поврежденным цветом
            float t = flashTimer / colorFlashDuration;
            Color currentColor = Color.Lerp(normalColor, damagedColor, t);
            targetMaterial.color = currentColor;
        }
        else if (targetMaterial != null)
        {
            targetMaterial.color = normalColor;
        }
    }

    // Метод для сброса позиции (полезно для тестирования)
    [ContextMenu("Reset Position")]
    public void ResetPosition()
    {

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        currentHealth = maxHealth;

        // Сбрасываем JiggleBone тоже
        if (jiggleBone != null)
        {
            // Доступ к private полям для сброса
            // Это может потребовать изменения JiggleBone.cs, чтобы сделать vel и dynamicPos публичными
            // или добавить метод Reset в JiggleBone.
            // Для примера, если они публичные:
            // jiggleBone.vel = Vector3.zero;
            // jiggleBone.dynamicPos = jiggleBone.transform.position + jiggleBone.transform.forward;
        }
    }

    // Показываем полоску здоровья в Scene view
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Полоска здоровья над объектом
            Vector3 healthBarPos = transform.position + Vector3.up * 2f;
            float healthPercent = currentHealth / maxHealth;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(healthBarPos - Vector3.right, healthBarPos + Vector3.right);

            Gizmos.color = Color.green;
            Vector3 healthEnd = Vector3.Lerp(healthBarPos - Vector3.right, healthBarPos + Vector3.right, healthPercent);
            Gizmos.DrawLine(healthBarPos - Vector3.right, healthEnd);
        }
    }
}
