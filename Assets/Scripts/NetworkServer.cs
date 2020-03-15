using System.Text;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;


public class NetworkServer : MonoBehaviour
{
    public UdpNetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections;
    private Dictionary<int, Player> m_Players = new Dictionary<int, Player>();

    void Start ()
    {
        m_Driver = new UdpNetworkDriver(new INetworkParameter[0]);
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 12666;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port 12666");
        else
            m_Driver.Listen();
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void CleanUpConnections()
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Players.Remove(m_Connections[i].InternalId);
                Debug.Log("Connection lost with " + m_Connections[i].InternalId);
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }
    }

    Player NewPlayer(int connId){
        // This new player will be always created at 0,0,0 - I'm feeling lazy
        var finalPlayer = new Player();
        finalPlayer.id = connId.ToString();
        finalPlayer.color = new ReceivedColor() {
            R=Random.Range(0f,1f),
            G=Random.Range(0f,1f),
            B=Random.Range(0f,1f)
        };
        finalPlayer.position = new ServerPosition(){x=0,y=0,z=0};
        return finalPlayer;
    }

    string NewPlayerMessage(Player player){
        var MessageObj = new Message();
        MessageObj.cmd = Commands.NEW_CLIENT;
        MessageObj.players = new Player[]{ player };
        return JsonUtility.ToJson(MessageObj);
    }

    void SendNewChallenger(Player newPlayerData)
    {
        string message = NewPlayerMessage(newPlayerData);
        for(int i = 0; i < m_Connections.Length; i++) {
            using (var writer = new DataStreamWriter(1024, Allocator.Temp)) {
                writer.WriteString(message);
                m_Connections[i].Send(m_Driver, writer);
            }
        }
    }

    string FirstUpdateMessage()
    {
        return "";
    }

    void AcceptNewConnections (){
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            Debug.Log("Accepted a connection with the following data: ");
            Debug.Log("InternalId: " + c.InternalId);
            m_Connections.Add(c);

            var currentPlayer = NewPlayer(c.InternalId);
            Debug.Log("Tell other players of this new player");
            //SendNewChallenger(currentPlayer);

            Debug.Log("About to send info about exisitng players to the guy");

            m_Players.Add(c.InternalId, currentPlayer);

        }
    }

    void ReceiveData(int connIdx){
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        var conn = m_Connections[connIdx];

        while ((cmd = m_Driver.PopEventForConnection(conn, out stream)) !=
                NetworkEvent.Type.Empty)
        {
            Debug.Log("In the while loop to get messages");
            if (cmd == NetworkEvent.Type.Data)
            {
                Debug.Log("If we've received data then");
                var readerCtx = default(DataStreamReader.Context);
                var number = stream.ReadString(ref readerCtx);
                Debug.Log("Got " + number + " from the Client");
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client disconnected from server with id: " + conn.InternalId);
                conn = default(NetworkConnection);
            }
        }
    }
    void RoundRobinConnections()
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Debug.Log("Checking Connection " + i);
            if (!m_Connections[i].IsCreated){
                Debug.Log("The connection " + i + " somehow has not been created yet - wtf? ");
                Assert.IsTrue(true);
            }
            ReceiveData(i);
        }
    }

    void Update ()
    {
        Debug.Log("Server is active!");
        m_Driver.ScheduleUpdate().Complete();
        CleanUpConnections();
        AcceptNewConnections();
        RoundRobinConnections();
    }
}