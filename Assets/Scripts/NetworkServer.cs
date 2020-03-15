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
    private List<Player> m_DisconnectedPlayers = new List<Player>();
    private bool dirty = false;

    void Start ()
    {
        m_Driver = new UdpNetworkDriver(new INetworkParameter[0]);
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 12666;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("[SERVER] Failed to bind to port 12666");
        else
            m_Driver.Listen();
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    void SendDisconnectedPlayers()
    {
        string message = Messager.DisconnectedPlayers(m_DisconnectedPlayers);
        for(int i = 0; i < m_Connections.Length; i++) {
            Sender.SendData(message, m_Driver, m_Connections[i]);
        }
    }
    public void OnDestroy()
    {
        for (int i = 0; i < m_Connections.Length; i++){
            m_DisconnectedPlayers.Add(m_Players[m_Connections[i].InternalId]);
            m_Players.Remove(m_Connections[i].InternalId);
        }
        SendDisconnectedPlayers(); // Sends final message to all clients
        m_Driver.Dispose();
        m_Connections.Dispose();
    }
    void CleanUpConnections()
    {
        bool oneDown = false;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_DisconnectedPlayers.Add(m_Players[m_Connections[i].InternalId]);
                m_Players.Remove(m_Connections[i].InternalId);
                Debug.Log("[SERVER] Connection lost with " + m_Connections[i].InternalId);
                //var connTemp = m_Connections[i];
                //connTemp.Dispose();
                m_Connections.RemoveAtSwapBack(i);
                --i;
                oneDown = true;
            }
        }
        if(oneDown){
            SendDisconnectedPlayers();
        }
    }
    void SendNewChallenger(Player newPlayerData)
    {
        string message = Messager.NewPlayer(newPlayerData);
        for(int i = 0; i < m_Connections.Length; i++) {
            Sender.SendData(message, m_Driver, m_Connections[i]);
        }
    }
    void SendFirstUpdateMessage(NetworkConnection c)
    {
        string message = Messager.UpdateOthers(m_Players);
        Sender.SendData(message, m_Driver, c);
    }
    void SendUpdateMessage()
    {
        string message = Messager.Update(m_Players);
        for(int i = 0; i < m_Connections.Length; i++) {
            Sender.SendData(message, m_Driver, m_Connections[i]);
        }
    }
    void AcceptNewConnections (){
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            Debug.Log("[SERVER] Accepted a connection with the following data: ");
            Debug.Log("[SERVER] InternalId: " + c.InternalId);

            var currentPlayer = Player.NewPlayer(c.InternalId);
            Debug.Log("Tell other players of this new player");
            if(m_Connections.Length > 0){
                SendNewChallenger(currentPlayer);
            }
            Debug.Log("[SERVER] Before adding new client to the list of connections and dictionary of game obejcts");
            m_Connections.Add(c);
            m_Players.Add(c.InternalId, currentPlayer);
            Debug.Log("[SERVER] About to send info about exisitng players to the guy");
            SendFirstUpdateMessage(c);
        }
    }

    void ReceiveData(int connIdx){
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        var conn = m_Connections[connIdx];

        while ((cmd = m_Driver.PopEventForConnection(conn, out stream)) !=
                NetworkEvent.Type.Empty)
        {
            Debug.Log("[SERVER] In the while loop to get messages");
            if (cmd == NetworkEvent.Type.Data)
            {
                Debug.Log("[SERVER] If we've received data then");
                var readerCtx = default(DataStreamReader.Context);
                var infoBuffer = new byte[stream.Length];
                stream.ReadBytesIntoArray(ref readerCtx, ref infoBuffer, stream.Length);
                var resultString = Encoding.ASCII.GetString(infoBuffer);
                Debug.Log("[SERVER] Got " + resultString + " from the Client: " + conn.InternalId);
                var message = Decoder.Decode(resultString);
                if (message != null && message.cmd == Commands.MOVEMENT){
                    dirty = true;
                    if(m_Players.ContainsKey(conn.InternalId))
                    {
                        m_Players[conn.InternalId].position.x += message.movePlayer.x;
                        m_Players[conn.InternalId].position.y += message.movePlayer.y;
                    }
                    else{
                        Debug.Log("Cound not find player with Id: " + conn.InternalId);
                    }
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("[SERVER] Client disconnected from server with id: " + conn.InternalId);
                // Let's hope this will be later processed correctly at the disconnect thingie
                conn = default(NetworkConnection);
            }
        }
    }
    void RoundRobinConnections()
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Debug.Log("[SERVER] Checking Connection " + i);
            if (!m_Connections[i].IsCreated){
                Debug.Log("[SERVER] The connection " + i + " somehow has not been created yet - wtf? ");
                Assert.IsTrue(true);
            }
            ReceiveData(i);
        }
        if(dirty){
            dirty = false;
            SendUpdateMessage();
        }
    }

    void Update ()
    {
        //Debug.Log("Server is active!");
        m_Driver.ScheduleUpdate().Complete();
        CleanUpConnections();
        AcceptNewConnections();
        RoundRobinConnections();
    }
}