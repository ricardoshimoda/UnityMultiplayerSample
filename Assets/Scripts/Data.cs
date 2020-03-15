using System;
using UnityEngine;
using Random = UnityEngine.Random;
public enum Commands{
    NEW_CLIENT,
    UPDATE,
    OTHERS,
    DELETE,
    MOVEMENT
};

[Serializable]
public class ReceivedColor{
    public float R;
    public float G;
    public float B;
}

[Serializable]
public class ServerPosition{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class Player{
    public string id;
    public ReceivedColor color;    
    public ServerPosition position;
    public static Player NewPlayer(int connId){
        // This new player will be always created at 0,0,0 - I'm feeling lazy
        var finalPlayer = new Player();
        finalPlayer.id = connId.ToString();
        finalPlayer.color = new ReceivedColor() {
            R = Random.Range(0f,1f),
            G = Random.Range(0f,1f),
            B = Random.Range(0f,1f)
        };
        finalPlayer.position = new ServerPosition(){x=0,y=0,z=0};
        return finalPlayer;
    }
}

public class Movement{
    public float x;
    public float y;
}

[Serializable]
public class Message{
    public Commands cmd;
    public Player[] players;
    public Movement movePlayer;
}

