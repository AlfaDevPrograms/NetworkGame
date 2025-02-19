using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private uint owner;
    private bool inited;
    private Vector3 target; // Цель полета пули
    private Rigidbody rb;
    public float gravity = -9.81f; // Сила гравитации
    private Vector3 velocity; // Текущая скорость
    private bool hasReachedTarget = false; // Флаг для проверки достижения цели
    private readonly float speed = 30f; // Начальная скорость пули

    [Server]
    public void Init(uint owner, Vector3 target)
    {
        this.owner = owner; // Кто сделал выстрел
        this.target = target; // Куда должна лететь пуля
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        inited = true;

        // Рассчитываем направление к цели
        Vector3 direction = (target - transform.position).normalized;
        velocity = direction * speed; // Устанавливаем начальную скорость
    }

    private void Update()
    {
        if (inited && isServer)
        {
            // Если пуля еще не достигла цели
            if (!hasReachedTarget)
            {
                // Двигаемся прямо к цели
                rb.MovePosition(transform.position + velocity * Time.deltaTime);

                // Проверяем, достигла ли пуля цели
                if (Vector3.Distance(transform.position, target) < 0.1f)
                {
                    hasReachedTarget = true; // Отмечаем, что цель достигнута
                }
            }
            else
            {
                // Если цель достигнута, начинаем применять гравитацию
                velocity.y += gravity * Time.deltaTime; // Применяем гравитацию
                rb.MovePosition(transform.position + velocity * Time.deltaTime);
                float angleX = Mathf.Clamp(Mathf.Atan2(velocity.y, velocity.magnitude) * Mathf.Rad2Deg, -30f, 30f);
                Quaternion rotation = Quaternion.Euler(-angleX + 90f, transform.eulerAngles.y, 0f);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * 10f); // Плавно наклоняем
            }

            // Проверка на столкновения
            Collider[] hitColliders = new Collider[10];
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 0.1f, hitColliders);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = hitColliders[i];
                Player player = hit.GetComponent<Player>();
                if (player && player.netId != owner)
                {
                    player.ChangeHealthValue(player.Health - 1); // Уменьшаем здоровье
                    NetworkServer.Destroy(gameObject); // Уничтожаем пулю
                    break;
                }
                else if (hit.CompareTag("Ground"))
                {
                    NetworkServer.Destroy(gameObject); // Уничтожаем пулю
                    break;
                }
            }
        }
    }
}