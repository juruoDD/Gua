using System;
using System.Collections.Generic;

namespace FrogCamp.Networking
{
    [Serializable]
    public class RoomPlayerData
    {
        public string id;
        public string name;
        public string role;
        public bool ready;
        public bool host;
        public bool online = true;
    }

    [Serializable]
    public class RoomStateData
    {
        public string code;
        public bool inGame;
        public List<RoomPlayerData> players = new List<RoomPlayerData>();
    }

    [Serializable]
    public class LanMessage
    {
        public string type;
        public string playerId;
        public string name;
        public string role;
        public bool ready;
        public string error;
        public RoomStateData room;
    }

    public class DiscoveredRoom
    {
        public string code;
        public string hostName;
        public string address;
        public int playerCount;
        public float lastSeen;
    }
}
