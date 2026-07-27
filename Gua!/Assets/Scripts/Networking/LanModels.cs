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
        public GameStateData game;
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
        public float inputX;
        public float inputY;
        public int taskProgress;
        public string action;
        public string taskId;
    }

    [Serializable]
    public class GameStateData
    {
        public List<GameActorData> players = new List<GameActorData>();
        public List<GameActorData> npcs = new List<GameActorData>();
        public string announcement;
        public int announcementId;
        public float musicTime;
        public int nextCadenceBeat;
        public List<string> cadenceCommands = new List<string>();
        public string specialMusicPhase;
        public float specialMusicTime;
        public int nextDanceBeat;
        public List<string> danceCommands = new List<string>();
        public string phase = "rules";
        public int phaseVersion;
        public float phaseRemaining;
        public bool tutorialStarted;
        public float countdownRemaining;
        public bool ended;
        public string winnerRole;
        public TaskStateData tasks;
    }

    [Serializable]
    public class TaskStateData
    {
        public List<string> activeTaskIds = new List<string>();
        public List<string> completedTaskIds = new List<string>();
        public int progressPercent;
        public bool finished;
        public int version;
    }

    [Serializable]
    public class GameActorData
    {
        public string id;
        public string name;
        public string role;
        public bool npc;
        public bool online = true;
        public bool eliminated;
        public bool moving;
        public bool stunned;
        public int taskProgress;
        public float x;
        public float y;
        public float inputX;
        public float inputY;
        public string facing = "up";
        public string actionFacing;
        public string action;
        public int actionId;
        public string pendingSequenceAction;
        public float pendingSequenceActionAt;
        public bool pendingSequenceActionEmitSound;
        public string soundEvent;
        public int soundEventId;
        public float actionStartedAt;
        public float actionUntil;
        public bool actionResolved;
        public float jumpX;
        public float jumpY;
        public float stunnedUntil;
        public float nextWhistleAt;
        public int officerNpcMistakeCount;
        public float nextDecisionAt;
        public float trialRespawnAt;
        public int assemblySlot = -1;
        public float assemblyBlockedTime;
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
