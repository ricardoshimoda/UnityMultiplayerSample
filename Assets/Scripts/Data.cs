using System;
public enum Commands{
    NEW_CLIENT,
    UPDATE,
    OTHERS,
    DELETE
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

