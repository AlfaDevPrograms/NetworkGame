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
            // �� ������ ���� ���������� ������� ������, ���� ��������� ��������� ������ ��������
            NetworkServer.Destroy(conn.identity.gameObject); // ���������� ������������� ������
        }
        GameObject go = Instantiate(playerPrefab, message.vector3, Quaternion.identity); //�������� �� ������� ������� gameObject
        Color randomColor = new(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        go.GetComponent<Player>().SetPlayerColor(randomColor); // ������������� ���� ������
        NetworkServer.AddPlayerForConnection(conn, go); //������������ gameObject � ���� ������� ��������
        PlayerConnectedMessage playerConnectedMsg = new()
        {
            playerId = conn.connectionId.ToString() // ��� ����������� ����� ���������� �������������
        };
        // ���������� ��������� ���� ��������
        NetworkServer.SendToAll(playerConnectedMsg); // ���������� ���� ��������
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<PosMessage>(OnCreateCharacter); //���������, ����� struct ������ ������ �� ������, ����� ���������� �����
    }

    public TMP_InputField addressInputField;

    public void StartHostButton()
    {
        try
        {
            if (!isNetworkActive) // ���������, ������� �� ����
            {
                networkAddress = addressInputField.text;
                singleton.StartHost(); // ������������ ��� ����
            }
            else
            {
                Debug.LogWarning("������ ��� ������ ��� ��������.");
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
            PosMessage m = new() { vector3 = pos }; // ������� struct ������������� ����, ����� ������ �����, � ���� ��������� ��� ������
            if (NetworkClient.isConnected) // ��� ����������� ����� �������� ����������
            {
                NetworkClient.Send(m); // �������� ��������� �� ������ � ������������ ������
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
        PosMessage m = new() { vector3 = pos }; // ������� struct ������������� ����, ����� ������ �����, � ���� ��������� ��� ������
        NetworkClient.Send(m); // �������� ��������� �� ������ � ������������ ������
        isConn = true;
    }
}

public struct PosMessage : NetworkMessage //����������� �� ���������� NetworkMessage, ����� ������� ������ ����� ������ �����������
{
    public Vector3 vector3; //������ ������������ Property
}

public struct PlayerConnectedMessage : NetworkMessage
{
    public string playerId; // ����� ������������ ���������� ������������� ������
}