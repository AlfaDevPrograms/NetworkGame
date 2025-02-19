using Mirror;
using TMPro;
using UnityEngine;

public class NetMan : NetworkManager
{
    private bool isConn = false;

    public void OnCreateCharacter(NetworkConnectionToClient conn, PosMessage message)
    {
        if (conn.identity != null)
        {
            // ¬ы можете либо уничтожить старого игрока, либо выполнить некоторые другие действи€
            NetworkServer.Destroy(conn.identity.gameObject); // ”ничтожить существующего игрока
        }
        GameObject go = Instantiate(playerPrefab, message.vector3, Quaternion.identity); //локально на сервере создаем gameObject
        Color randomColor = new(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        go.GetComponent<Player>().SetPlayerColor(randomColor); // ”станавливаем цвет игрока
        NetworkServer.AddPlayerForConnection(conn, go); //ѕрисоедин€ем gameObject к пулу сетевых объектов
        PlayerConnectedMessage playerConnectedMsg = new()
        {
            playerId = conn.connectionId.ToString() // или используйте любой уникальный идентификатор
        };
        // ќтправл€ем сообщение всем клиентам
        NetworkServer.SendToAll(playerConnectedMsg); // ”ведомл€ем всех клиентов
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<PosMessage>(OnCreateCharacter); //указываем, какой struct должен прийти на сервер, чтобы выполнилс€ свапн
    }

    public TMP_InputField addressInputField;

    public void StartHostButton()
    {
        try
        {
            if (!isNetworkActive) // ѕровер€ем, активна ли сеть
            {
                networkAddress = addressInputField.text;
                singleton.StartHost(); // ѕодключаемс€ как хост
            }
            else
            {
                Debug.LogWarning("—ервер или клиент уже запущены.");
            }
        }
        catch (System.Exception)
        {
        }
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void StartClientButton()
    {
        if (isConn)
        {
            Vector3 pos = new(13.86299f, 0.400005f, -1.514028f);
            PosMessage m = new() { vector3 = pos }; // создаем struct определенного типа, чтобы сервер пон€л, к чему относ€тс€ эти данные
            if (NetworkClient.isConnected) // или аналогичный метод проверки соединени€
            {
                NetworkClient.Send(m); // отправка сообщени€ на сервер с координатами спавна
            }
            else
            {
                networkAddress = addressInputField.text;
                singleton.StartClient();
            }
        }
        else
        {
            networkAddress = addressInputField.text;
            singleton.StartClient();
        }
    }

    public void StopClientButton()
    {
        singleton.StopClient();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Vector3 pos = new(13.86299f, 0.400005f, -1.514028f);
        PosMessage m = new() { vector3 = pos }; // создаем struct определенного типа, чтобы сервер пон€л, к чему относ€тс€ эти данные
        NetworkClient.Send(m); // отправка сообщени€ на сервер с координатами спавна
        isConn = true;
    }
}

public struct PosMessage : NetworkMessage //наследуемс€ от интерфейса NetworkMessage, чтобы система пон€ла какие данные упаковывать
{
    public Vector3 vector3; //нельз€ использовать Property
}

public struct PlayerConnectedMessage : NetworkMessage
{
    public string playerId; // можно использовать уникальный идентификатор игрока
}