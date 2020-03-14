using System;

[Serializable]
public class receivedColor{
    public float R;
    public float G;
    public float B;
}

[Serializable]
public class serverPosition{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class Player{
    public string id;
    public receivedColor color;    
    public serverPosition position;    
}

[Serializable]
public class Message{
    public commands cmd;
    public Player[] players;
}

public enum commands{
    NEW_CLIENT,
    UPDATE,
    OTHERS,
    DELETE
};
