using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private uint owner;
    private bool inited;
    private Vector3 target; // ���� ������ ����
    private Rigidbody rb;
    public float gravity = -9.81f; // ���� ����������
    private Vector3 velocity; // ������� ��������
    private bool hasReachedTarget = false; // ���� ��� �������� ���������� ����
    private readonly float speed = 30f; // ��������� �������� ����

    [Server]
    public void Init(uint owner, Vector3 target)
    {
        this.owner = owner; // ��� ������ �������
        this.target = target; // ���� ������ ������ ����
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        inited = true;

        // ������������ ����������� � ����
        Vector3 direction = (target - transform.position).normalized;
        velocity = direction * speed; // ������������� ��������� ��������
    }

    private void Update()
    {
        if (inited && isServer)
        {
            // ���� ���� ��� �� �������� ����
            if (!hasReachedTarget)
            {
                // ��������� ����� � ����
                rb.MovePosition(transform.position + velocity * Time.deltaTime);

                // ���������, �������� �� ���� ����
                if (Vector3.Distance(transform.position, target) < 0.1f)
                {
                    hasReachedTarget = true; // ��������, ��� ���� ����������
                }
            }
            else
            {
                // ���� ���� ����������, �������� ��������� ����������
                velocity.y += gravity * Time.deltaTime; // ��������� ����������
                rb.MovePosition(transform.position + velocity * Time.deltaTime);
                float angleX = Mathf.Clamp(Mathf.Atan2(velocity.y, velocity.magnitude) * Mathf.Rad2Deg, -30f, 30f);
                Quaternion rotation = Quaternion.Euler(-angleX + 90f, transform.eulerAngles.y, 0f);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * 10f); // ������ ���������
            }

            // �������� �� ������������
            Collider[] hitColliders = new Collider[10];
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 0.1f, hitColliders);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = hitColliders[i];
                Player player = hit.GetComponent<Player>();
                if (player && player.netId != owner)
                {
                    player.ChangeHealthValue(player.Health - 1); // ��������� ��������
                    NetworkServer.Destroy(gameObject); // ���������� ����
                    break;
                }
                else if (hit.CompareTag("Ground"))
                {
                    NetworkServer.Destroy(gameObject); // ���������� ����
                    break;
                }
            }
        }
    }
}