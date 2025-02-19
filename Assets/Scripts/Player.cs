using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour //даем системе понять, что это сетевой объект
{
    [SyncVar(hook = nameof(SyncHealth))] //задаем метод, который будет выполняться при синхронизации переменной
    [SerializeField] private int _SyncHealth;

    public int Health;
    public GameObject BulletPrefab; //сюда вешаем префаб пули
    public GameObject playerCamera; // Ссылка на камеру игрока
    public float jumpForce = 20f; // Сила прыжка
    public LayerMask groundLayer; // Слой, который считается землей
    private GameObject ui, aim;
    private Button clientBtn, hostBtn;
    private bool isCursorVisible = false; // Флаг для отслеживания состояния курсора
    private AudioListener myListener;
    private Transform cameraTransform; // Ссылка на трансформ камеры
    private CharacterController controller;
    private readonly float speed = 3f;  // Настройка скорости
    private readonly float gravity = -9.81f; // Сила тяжести
    private Vector3 velocity; // Вектор скорости (включая вертикальную скорость)
    private float rotationX = 0f; // Угол поворота по оси X
    private readonly float maxJumpHeight = 3f; // Максимальная высота прыжка
    private float yaw = 0f;
    private readonly float smoothFactor = 15f;
    private float timeSinceLastUpdate = 0.0f;
    private int frameCount = 0;
    private TextMeshProUGUI fpsText;

    private void Start()
    {
        if (isLocalPlayer) // Проверяем, если этот игрок локальный
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
            SetupCamera(); // Настройка камеры
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
        // Убедитесь, что камера активна только для локального игрока
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
            Cursor.visible = true; // Скрыть курсор
            Cursor.lockState = CursorLockMode.None; // Зафиксировать курсор в центре экрана
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
            Cursor.visible = false; // Скрыть курсор
            Cursor.lockState = CursorLockMode.Locked; // Зафиксировать курсор в центре экрана
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
                    // Если есть препятствие над игроком, останавливаем гравитацию
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
        // Позиция сферы для проверки земли
        Vector3 spherePosition = transform.position + Vector3.down * 0.5f;
        // Проверяем, есть ли коллайдеры в пределах сферы
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
        if (IsGrounded() && Input.GetKeyDown(KeyCode.Space) && isLocalPlayer) // Проверка нажатия пробела
        {
            float jumpHeight = Mathf.Sqrt(jumpForce * -2f * gravity);
            if (jumpHeight > maxJumpHeight)
            {
                jumpHeight = maxJumpHeight; // Ограничиваем прыжок
            }
            velocity.y = jumpHeight; // Логика прыжка
        }
    }

    private void Update()
    {
        if (isLocalPlayer) //проверяем, есть ли у нас права изменять этот объект
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
            // Получаем направление камеры
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            // Убираем вертикальную составляющую, чтобы не двигаться вверх или вниз
            forward.y = 0;
            right.y = 0;

            // Нормализуем векторы
            forward.Normalize();
            right.Normalize();

            // Вычисляем движение
            Vector3 movement = (right * moveHorizontal + forward * moveVertical).normalized;

            // Обработка гравитации
            if (!controller.isGrounded)
            {
                // Если не на земле, применяем гравитацию
                movement.y = gravity * Time.deltaTime; // Применяем гравитацию
            }
            else
            {
                // Обнуляем вертикальную скорость, если на земле
                movement.y = 0;
            }

            // Устанавливаем скорость движения
            controller.Move(speed * Time.deltaTime * movement);
        }
    }

    private void LateUpdate()
    {
        if (isLocalPlayer && !isCursorVisible)
        {
            RotateCamera(); // Поворот камеры
        }
    }

    private void RotateCamera()
    {
        // Получаем движение мыши
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Обновляем горизонтальный угол
        yaw += mouseX;

        // Обновляем вертикальный угол и ограничиваем его
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        // Создаем кватернионы для углов
        Quaternion targetRotation = Quaternion.Euler(rotationX, yaw, 0f);

        // Плавный переход к новой локальной ротации
        cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetRotation, smoothFactor);
    }

    private void FireBullet()
    {
        // Создаем луч из центра экрана
        Ray ray = playerCamera.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Центр экрана
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
        // Рассчитываем направление от точки выстрела до цели
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

    // Метод для обработки уведомлений о новых игроках
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

    private void SyncHealth(int oldValue, int newValue) //обязательно делаем два значения - старое и новое.
    {
        Health = newValue;
    }

    [Server] //обозначаем, что этот метод будет вызываться и выполняться только на сервере
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

    [Command] //обозначаем, что этот метод должен будет выполняться на сервере по запросу клиента
    public void CmdChangeHealth(int newValue) //обязательно ставим Cmd в начале названия метода
    {
        ChangeHealthValue(newValue); //переходим к непосредственному изменению переменной
    }

    [Server]
    public void SpawnBullet(uint owner, Vector3 target, Quaternion quaternion)
    {
        var tr = transform.position;
        tr.y += 0.5f;
        tr += cameraTransform.forward * 0.5f;
        GameObject bulletGo = Instantiate(BulletPrefab, tr, quaternion);
        NetworkServer.Spawn(bulletGo); //отправляем информацию о сетевом объекте всем игрокам.
        bulletGo.GetComponent<Bullet>().Init(owner, target); //инифиализируем поведение пули
    }

    [Command]
    public void CmdSpawnBullet(uint owner, Vector3 target, Quaternion quaternion)
    {
        SpawnBullet(owner, target, quaternion);
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        GetComponent<MeshRenderer>().materials[0].color = newColor; // Обновление цвета на клиенте
    }

    [SyncVar(hook = nameof(OnColorChanged))]
    private Color playerColor;

    [Server]
    public void SetPlayerColor(Color color)
    {
        playerColor = color; // Устанавливаем значение SyncVar
        GetComponent<MeshRenderer>().materials[0].color = color; // Устанавливаем цвет на сервере
    }
}