using UnityEngine;
using System.Text;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;

public class NetworkClient : MonoBehaviour
{
    public string serverAddress = "127.0.0.1";
    public ushort serverPort = 12666;

    public UdpNetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public bool m_Done;
    private GameObject m_RotatingCubePrefab;

    private Dictionary<string, GameObject> m_NetworkedCubes = new Dictionary<string, GameObject>();
    void Start ()
    {
        m_RotatingCubePrefab = Resources.Load("MyRotatingCube", typeof(GameObject)) as GameObject;
        m_Driver = new UdpNetworkDriver(new INetworkParameter[0]);
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverAddress, serverPort);
        // endpoint.Port = 9000;
        m_Connection = m_Driver.Connect(endpoint);
        Debug.Log("[CLIENT] Connection ID is: "  + m_Connection.InternalId);
    }

    public void OnDestroy()
    {
        m_Done = true;
        m_Driver.Dispose();
        //m_Connection.Dispose();
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            if (!m_Done)
                Debug.Log("[CLIENT] Something went wrong during connect");
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) !=
               NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                var readerCtx = default(DataStreamReader.Context);
                var infoBuffer = new byte[stream.Length];
                stream.ReadBytesIntoArray(ref readerCtx, ref infoBuffer, stream.Length);
                var resultString = Encoding.ASCII.GetString(infoBuffer);
                Debug.Log("[CLIENT] Got " + resultString + " from the Server");
                var message = Decoder.Decode(resultString);
                Debug.Log("[CLIENT] After decoding the message");
                if(message.cmd == Commands.OTHERS){
                    foreach(Player pl in message.players) {
                        GameObject newCube = Instantiate(
                            m_RotatingCubePrefab,
                            new Vector3(
                                pl.position.x,
                                pl.position.y,
                                pl.position.z
                            ), 
                            Quaternion.Euler(0, 0, 0)) as GameObject;
                        NetworkCube thisCubeHere = newCube.GetComponent<NetworkCube>();
                        thisCubeHere.id = pl.id;
                        thisCubeHere.ChangeColor(pl.color.R, pl.color.G, pl.color.B);
                        m_NetworkedCubes.Add(pl.id, newCube);
                    }
                } else if (message.cmd == Commands.NEW_CLIENT){
                    foreach(Player pl in message.players) {
                        GameObject newCube = Instantiate(
                            m_RotatingCubePrefab,
                            new Vector3(
                                pl.position.x,
                                pl.position.y,
                                pl.position.z
                            ), 
                            Quaternion.Euler(0, 0, 0)) as GameObject;
                        NetworkCube thisCubeHere = newCube.GetComponent<NetworkCube>();
                        thisCubeHere.id = pl.id;
                        thisCubeHere.ChangeColor(pl.color.R, pl.color.G, pl.color.B);
                        m_NetworkedCubes.Add(pl.id, newCube);
                    }
                } else if (message.cmd == Commands.UPDATE) {
                    foreach(Player pl in message.players) {
                        GameObject cube = m_NetworkedCubes[pl.id];
                        cube.transform.position = 
                            new Vector3(
                                pl.position.x,
                                pl.position.y,
                                pl.position.z
                            );
                    }
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("[CLIENT] Client got disconnected from server");
                m_Connection = default(NetworkConnection);
                m_Done = true;
            }
        }
        //Debug.Log("End of it all");
    }
}