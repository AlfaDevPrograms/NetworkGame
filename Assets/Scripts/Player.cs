using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour //���� ������� ������, ��� ��� ������� ������
{
    [SyncVar(hook = nameof(SyncHealth))] //������ �����, ������� ����� ����������� ��� ������������� ����������
    [SerializeField] private int _SyncHealth;

    public int Health;
    public GameObject BulletPrefab; //���� ������ ������ ����
    public GameObject playerCamera; // ������ �� ������ ������
    public float jumpForce = 20f; // ���� ������
    public LayerMask groundLayer; // ����, ������� ��������� ������
    private GameObject ui, aim;
    private Button clientBtn, hostBtn;
    private bool isCursorVisible = false; // ���� ��� ������������ ��������� �������
    private AudioListener myListener;
    private Transform cameraTransform; // ������ �� ��������� ������
    private CharacterController controller;
    private readonly float speed = 3f;  // ��������� ��������
    private readonly float gravity = -9.81f; // ���� �������
    private Vector3 velocity; // ������ �������� (������� ������������ ��������)
    private float rotationX = 0f; // ���� �������� �� ��� X
    private readonly float maxJumpHeight = 3f; // ������������ ������ ������
    private float yaw = 0f;
    private readonly float smoothFactor = 15f;
    private float timeSinceLastUpdate = 0.0f;
    private int frameCount = 0;
    private TextMeshProUGUI fpsText;

    private void Start()
    {
        if (isLocalPlayer) // ���������, ���� ���� ����� ���������
        {
            Application.targetFrameRate = 144;
            controller = GetComponent<CharacterController>();
            cameraTransform = GetComponentInChildren<Camera>().gameObject.transform;
            myListener = GetComponentInChildren<AudioListener>();
            ui = GameObject.FindWithTag("UI");
            aim = GameObject.FindWithTag("Aim");
            clientBtn = GameObject.FindWithTag("ClientButton").GetComponent<Button>();
            hostBtn = GameObject.FindWithTag("HostButton").GetComponent<Button>();
            clientBtn.enabled = false;
            hostBtn.enabled = false;
            SetupCamera(); // ��������� ������
            var meshRenderer = GetComponent<MeshRenderer>();
            if (isLocalPlayer)
            {
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = false;
                }
            }
        }
    }

    private void SetupCamera()
    {
        // ���������, ��� ������ ������� ������ ��� ���������� ������
        playerCamera.GetComponent<Camera>().enabled = true;
        DisableCursor();
    }

    private void EnableCursor()
    {
        if (isLocalPlayer)
        {
            ui.SetActive(true);
            if (aim != null)
            {
                aim.transform.GetChild(0).gameObject.SetActive(false);
            }
            Cursor.visible = true; // ������ ������
            Cursor.lockState = CursorLockMode.None; // ������������� ������ � ������ ������
        }
    }

    private void DisableCursor()
    {
        if (isLocalPlayer)
        {
            ui.SetActive(false);
            if (aim != null)
            {
                aim.transform.GetChild(0).gameObject.SetActive(true);
            }
            Cursor.visible = false; // ������ ������
            Cursor.lockState = CursorLockMode.Locked; // ������������� ������ � ������ ������
        }
    }

    private void ApplyGravity()
    {
        if (isLocalPlayer)
        {
            if (!IsGrounded())
            {
                Vector3 spherePosition = transform.position + Vector3.up * 0.5f;
                if (Physics.CheckSphere(spherePosition, 0.25f, groundLayer))
                {
                    // ���� ���� ����������� ��� �������, ������������� ����������
                    velocity.y = -2f;
                }
                else
                {
                    velocity.y += gravity * Time.deltaTime;
                }
            }
            else
            {
                if (velocity.y < 0)
                {
                    velocity.y = -2f;
                }
            }

            controller.Move(maxJumpHeight * Time.deltaTime * velocity);
        }
    }

    private bool IsGrounded()
    {
        // ������� ����� ��� �������� �����
        Vector3 spherePosition = transform.position + Vector3.down * 0.5f;
        // ���������, ���� �� ���������� � �������� �����
        if (Physics.CheckSphere(spherePosition, 0.25f, groundLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Jump()
    {
        if (IsGrounded() && Input.GetKeyDown(KeyCode.Space) && isLocalPlayer) // �������� ������� �������
        {
            float jumpHeight = Mathf.Sqrt(jumpForce * -2f * gravity);
            if (jumpHeight > maxJumpHeight)
            {
                jumpHeight = maxJumpHeight; // ������������ ������
            }
            velocity.y = jumpHeight; // ������ ������
        }
    }

    private void Update()
    {
        if (isLocalPlayer) //���������, ���� �� � ��� ����� �������� ���� ������
        {
            ApplyGravity();
            timeSinceLastUpdate += Time.deltaTime;
            frameCount++;

            if (timeSinceLastUpdate >= 1.0f)
            {
                float fps = frameCount / timeSinceLastUpdate;
                if (fpsText != null)
                {
                    fpsText.text = Mathf.Round(fps).ToString();
                }
                else
                {
                    fpsText = aim.GetComponentInChildren<TextMeshProUGUI>();
                }
                timeSinceLastUpdate -= 1.0f;
                frameCount = 0;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!isCursorVisible)
                {
                    EnableCursor();
                    isCursorVisible = true;
                }
                else
                {
                    DisableCursor();
                    isCursorVisible = false;
                }
            }
            if (!isCursorVisible)
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    FireBullet();
                }
                if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                }
            }
            Walk();
            Jump();
        }
    }

    private void Walk()
    {
        if (isLocalPlayer)
        {
            float moveHorizontal = Input.GetAxisRaw("Horizontal");
            float moveVertical = Input.GetAxisRaw("Vertical");

            if (Mathf.Approximately(moveHorizontal, 0) && Mathf.Approximately(moveVertical, 0))
            {
                return;
            }
            // �������� ����������� ������
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            // ������� ������������ ������������, ����� �� ��������� ����� ��� ����
            forward.y = 0;
            right.y = 0;

            // ����������� �������
            forward.Normalize();
            right.Normalize();

            // ��������� ��������
            Vector3 movement = (right * moveHorizontal + forward * moveVertical).normalized;

            // ��������� ����������
            if (!controller.isGrounded)
            {
                // ���� �� �� �����, ��������� ����������
                movement.y = gravity * Time.deltaTime; // ��������� ����������
            }
            else
            {
                // �������� ������������ ��������, ���� �� �����
                movement.y = 0;
            }

            // ������������� �������� ��������
            controller.Move(speed * Time.deltaTime * movement);
        }
    }

    private void LateUpdate()
    {
        if (isLocalPlayer && !isCursorVisible)
        {
            RotateCamera(); // ������� ������
        }
    }

    private void RotateCamera()
    {
        // �������� �������� ����
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // ��������� �������������� ����
        yaw += mouseX;

        // ��������� ������������ ���� � ������������ ���
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        // ������� ����������� ��� �����
        Quaternion targetRotation = Quaternion.Euler(rotationX, yaw, 0f);

        // ������� ������� � ����� ��������� �������
        cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetRotation, smoothFactor);
    }

    private void FireBullet()
    {
        // ������� ��� �� ������ ������
        Ray ray = playerCamera.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // ����� ������
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.distance < 0.5f)
            {
                return;
            }
            if (hit.distance > 10)
            {
                targetPoint = ray.GetPoint(5);
            }
            else
            {
                targetPoint = hit.point;
            }
        }
        else
        {
            targetPoint = ray.GetPoint(5);
        }
        // ������������ ����������� �� ����� �������� �� ����
        Vector3 direction = (targetPoint - transform.position).normalized;

        Quaternion quaternion = Quaternion.LookRotation(direction);

        Quaternion bulletRotation = quaternion * Quaternion.Euler(90, 0, 0);
        if (isServer)
            SpawnBullet(netId, targetPoint, bulletRotation);
        else
            CmdSpawnBullet(netId, targetPoint, bulletRotation);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.ReplaceHandler<PlayerConnectedMessage>(OnPlayerConnectedToServer);
    }

    public override void OnStopClient()
    {
        if (isLocalPlayer)
        {
            clientBtn.enabled = true;
            hostBtn.enabled = true;
            EnableCursor();
            isCursorVisible = true;
        }
        base.OnStopClient();
    }

    // ����� ��� ��������� ����������� � ����� �������
    private void OnPlayerConnectedToServer(PlayerConnectedMessage message)
    {
        Debug.Log($"Player connected: {message.playerId}");
    }

    [ClientRpc]
    private void RpcShowUI()
    {
        if (isLocalPlayer)
        {
            clientBtn.enabled = true;
            hostBtn.enabled = true;
            EnableCursor();
            isCursorVisible = true;
        }
    }

    private void SyncHealth(int oldValue, int newValue) //����������� ������ ��� �������� - ������ � �����.
    {
        Health = newValue;
    }

    [Server] //����������, ��� ���� ����� ����� ���������� � ����������� ������ �� �������
    public void ChangeHealthValue(int newValue)
    {
        _SyncHealth = newValue;

        if (_SyncHealth <= 0)
        {
            if (isLocalPlayer)
            {
                clientBtn.enabled = true;
                hostBtn.enabled = true;
                EnableCursor();
                isCursorVisible = true;
            }
            RpcShowUI();
            NetworkServer.Destroy(gameObject);
        }
    }

    [Command] //����������, ��� ���� ����� ������ ����� ����������� �� ������� �� ������� �������
    public void CmdChangeHealth(int newValue) //����������� ������ Cmd � ������ �������� ������
    {
        ChangeHealthValue(newValue); //��������� � ����������������� ��������� ����������
    }

    [Server]
    public void SpawnBullet(uint owner, Vector3 target, Quaternion quaternion)
    {
        var tr = transform.position;
        tr.y += 0.5f;
        tr += cameraTransform.forward * 0.5f;
        GameObject bulletGo = Instantiate(BulletPrefab, tr, quaternion);
        NetworkServer.Spawn(bulletGo); //���������� ���������� � ������� ������� ���� �������.
        bulletGo.GetComponent<Bullet>().Init(owner, target); //�������������� ��������� ����
    }

    [Command]
    public void CmdSpawnBullet(uint owner, Vector3 target, Quaternion quaternion)
    {
        SpawnBullet(owner, target, quaternion);
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        GetComponent<MeshRenderer>().materials[0].color = newColor; // ���������� ����� �� �������
    }

    [SyncVar(hook = nameof(OnColorChanged))]
    private Color playerColor;

    [Server]
    public void SetPlayerColor(Color color)
    {
        playerColor = color; // ������������� �������� SyncVar
        GetComponent<MeshRenderer>().materials[0].color = color; // ������������� ���� �� �������
    }
}