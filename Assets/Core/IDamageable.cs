using UnityEngine;

// Интерфейс для объектов, которые могут получать урон
public interface IDamageable
{
    void TakeDamage(float damage);
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitForce);
}
