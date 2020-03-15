using UnityEngine;
using System;

public class Decoder{
    public static Message Decode(string message){
        try{
            return JsonUtility.FromJson<Message>(message);
        }
        catch(Exception ex){
            Debug.Log("Error when decoding the message from the server: " + message);
            Debug.Log("Expection: " + ex.ToString());
        }
        // Returns nothing when an error has happened
        return null;
    }
}