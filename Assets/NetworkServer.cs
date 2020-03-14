using UnityEngine;
using UnityEngine.Assertions;

using Unity.Collections;
using Unity.Networking.Transport;

public class NetworkServer : MonoBehaviour
{
    public UdpNetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections;

    void Start ()
    {
        Debug.Log("Before Start");
        m_Driver = new UdpNetworkDriver(new INetworkParameter[0]);
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 12666;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port 12666");
        else
            m_Driver.Listen();
        Debug.Log("After Start");
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void Update ()
    {
        Debug.Log("Server is active!");
        m_Driver.ScheduleUpdate().Complete();

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }
        // AcceptNewConnections
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            m_Connections.Add(c);
            Debug.Log("Accepted a connection");
        }

        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Debug.Log("Checking Connection " + i);

            if (!m_Connections[i].IsCreated){
                Debug.Log("The connection " + i + " somehow has not been created yet - wtf? ");
                Assert.IsTrue(true);
            }

            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) !=
                   NetworkEvent.Type.Empty)
            {
                Debug.Log("In the while loop to get messages");
                if (cmd == NetworkEvent.Type.Data)
                {
                    Debug.Log("If we've received data then");
                    var readerCtx = default(DataStreamReader.Context);
                    var number = stream.ReadString(ref readerCtx);
                    Debug.Log("Got " + number + " from the Client adding + 2 to it.");
                    //number +=2;
                    /*
                    using (var writer = new DataStreamWriter(4, Allocator.Temp))
                    {
                        writer.Write(number);
                        m_Driver.Send(NetworkPipeline.Null, m_Connections[i], writer);
                    }*/
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    m_Connections[i] = default(NetworkConnection);
                }
            }
        }
    }
}