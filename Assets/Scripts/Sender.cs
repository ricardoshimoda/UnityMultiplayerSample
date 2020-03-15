using System;
using System.Text;
using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;

public class Sender{
    public static int CAPACITY = 1024;

    public static void SendData(string data, UdpNetworkDriver driver, NetworkConnection conn)
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes(data);
        using (var writer = new DataStreamWriter(CAPACITY, Allocator.Temp)) {
            Debug.Log("I'm trying to send " + sendBytes.Length + " bytes of data.");
            writer.Write(sendBytes, sendBytes.Length);
            conn.Send(driver, writer);
        }
    }

}