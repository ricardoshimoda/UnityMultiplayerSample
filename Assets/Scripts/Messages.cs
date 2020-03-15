using UnityEngine;
using System.Collections.Generic;
public class Messager{
    public static string NewPlayer(Player player){
        var MessageObj = new Message();
        MessageObj.cmd = Commands.NEW_CLIENT;
        MessageObj.players = new Player[]{ player };
        return JsonUtility.ToJson(MessageObj);
    }

    public static string DisconnectedPlayers(List<Player> dConnP)
    {
        var MessageObj = new Message();
        MessageObj.cmd = Commands.DELETE;
        MessageObj.players = dConnP.ToArray();
        return JsonUtility.ToJson(MessageObj);
    }
    public static string UpdateOthers(Dictionary<int, Player> activePlayers){
        var MessageObj = new Message();
        MessageObj.cmd = Commands.OTHERS;
        activePlayers.Values.CopyTo(MessageObj.players, 0);
        return JsonUtility.ToJson(MessageObj);
    }
    public static string Update(Dictionary<int, Player> activePlayers){
        var MessageObj = new Message();
        MessageObj.cmd = Commands.UPDATE;
        activePlayers.Values.CopyTo(MessageObj.players, 0);
        return JsonUtility.ToJson(MessageObj);
    }

    public static string UpdatePosition(Movement movePlayer){
        var MessageObj = new Message();
        MessageObj.cmd = Commands.MOVEMENT;
        MessageObj.movePlayer = movePlayer;
        return JsonUtility.ToJson(MessageObj);
    }
}