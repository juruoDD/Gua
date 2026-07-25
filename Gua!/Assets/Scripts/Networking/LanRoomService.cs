using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.Networking
{
    public sealed class LanRoomService : MonoBehaviour
    {
        private const int RoomPort = 7777;
        private const int DiscoveryPort = 7778;
        private const string DiscoveryMagic = "FROG_CAMP_V1";
        private const int MaxPlayers = 4;
        private const int MinPlayers = 2;

        private static LanRoomService instance;
        private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private readonly List<HostPeer> hostPeers = new List<HostPeer>();
        private readonly List<DiscoveredRoom> discoveredRooms = new List<DiscoveredRoom>();
        private readonly object clientSendLock = new object();

        private TcpListener hostListener;
        private Thread acceptThread;
        private Thread broadcastThread;
        private UdpClient discoveryReceiver;
        private Thread discoveryThread;
        private TcpClient clientConnection;
        private StreamWriter clientWriter;
        private Thread clientReadThread;
        private volatile bool hostRunning;
        private volatile bool discoveryRunning;
        private float gameTickAccumulator;

        public static LanRoomService Instance
        {
            get
            {
                EnsureExists();
                return instance;
            }
        }

        public RoomStateData CurrentRoom { get; private set; }
        public string LocalPlayerId { get; private set; }
        public bool IsHost { get; private set; }
        public string Status { get; private set; } = "正在搜索局域网房间…";
        public IReadOnlyList<DiscoveredRoom> DiscoveredRooms { get { return discoveredRooms; } }

        public event Action StateChanged;
        public event Action DiscoveriesChanged;
        public event Action StatusChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureExists()
        {
            if (instance != null) return;
            GameObject serviceObject = new GameObject("LanRoomService");
            instance = serviceObject.AddComponent<LanRoomService>();
            DontDestroyOnLoad(serviceObject);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            StartDiscovery();
        }

        private void Update()
        {
            Action action;
            while (mainThreadActions.TryDequeue(out action)) action();

            bool removed = discoveredRooms.RemoveAll(
                room => Time.realtimeSinceStartup - room.lastSeen > 4f) > 0;
            if (removed && DiscoveriesChanged != null) DiscoveriesChanged();

            if (IsHost && CurrentRoom != null && CurrentRoom.inGame &&
                CurrentRoom.game != null)
            {
                gameTickAccumulator += Time.unscaledDeltaTime;
                while (gameTickAccumulator >= 0.05f)
                {
                    gameTickAccumulator -= 0.05f;
                    GameSimulation.Tick(CurrentRoom.game, 0.05f, Time.realtimeSinceStartup);
                    BroadcastState();
                }
            }
        }

        public void HostRoom(string playerName)
        {
            LeaveRoom();
            string safeName = CleanName(playerName);
            LocalPlayerId = Guid.NewGuid().ToString("N");
            IsHost = true;
            CurrentRoom = new RoomStateData { code = CreateRoomCode() };
            CurrentRoom.players.Add(new RoomPlayerData
            {
                id = LocalPlayerId,
                name = safeName,
                host = true,
                online = true
            });

            try
            {
                hostListener = new TcpListener(IPAddress.Any, RoomPort);
                hostListener.Start();
                hostRunning = true;
                acceptThread = new Thread(AcceptClients) { IsBackground = true };
                acceptThread.Start();
                broadcastThread = new Thread(BroadcastDiscovery) { IsBackground = true };
                broadcastThread.Start();
                SetStatus("房间已创建，等待其他玩家加入");
                RaiseStateChanged();
            }
            catch (Exception exception)
            {
                SetStatus("创建房间失败：" + exception.Message);
                LeaveRoom();
            }
        }

        public void JoinByCode(string playerName, string roomCode)
        {
            string code = CleanCode(roomCode);
            DiscoveredRoom room = discoveredRooms.FirstOrDefault(
                item => string.Equals(item.code, code, StringComparison.OrdinalIgnoreCase));
            if (room == null)
            {
                SetStatus("没有发现房间 " + code + "，请确认设备在同一局域网");
                return;
            }
            JoinAddress(playerName, room.address);
        }

        public void JoinAddress(string playerName, string address)
        {
            LeaveRoom();
            IsHost = false;
            string safeName = CleanName(playerName);
            SetStatus("正在连接 " + address + "…");
            Thread connectThread = new Thread(() =>
            {
                try
                {
                    TcpClient connection = new TcpClient();
                    IAsyncResult pending = connection.BeginConnect(address, RoomPort, null, null);
                    if (!pending.AsyncWaitHandle.WaitOne(3500))
                    {
                        connection.Close();
                        throw new TimeoutException("连接超时");
                    }
                    connection.EndConnect(pending);
                    StreamWriter writer = new StreamWriter(
                        connection.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
                    mainThreadActions.Enqueue(() =>
                    {
                        clientConnection = connection;
                        clientWriter = writer;
                        SetStatus("已连接，正在加入房间");
                        SendClient(new LanMessage { type = "join", name = safeName });
                    });
                    clientReadThread = new Thread(() => ReadClient(connection)) { IsBackground = true };
                    clientReadThread.Start();
                }
                catch (Exception exception)
                {
                    mainThreadActions.Enqueue(() => SetStatus("加入失败：" + exception.Message));
                }
            }) { IsBackground = true };
            connectThread.Start();
        }

        public void SelectRole(string role)
        {
            if (CurrentRoom == null) return;
            if (IsHost) ApplyRole(LocalPlayerId, role);
            else SendClient(new LanMessage { type = "role", role = role });
        }

        public void SetReady(bool ready)
        {
            if (CurrentRoom == null) return;
            if (IsHost) ApplyReady(LocalPlayerId, ready);
            else SendClient(new LanMessage { type = "ready", ready = ready });
        }

        public void RequestStart()
        {
            if (!IsHost || CurrentRoom == null) return;
            string reason;
            if (!CanStart(out reason))
            {
                SetStatus(reason);
                return;
            }
            CurrentRoom.game = GameSimulation.Create(CurrentRoom, Time.realtimeSinceStartup);
            CurrentRoom.inGame = true;
            gameTickAccumulator = 0f;
            BroadcastState();
            SendToAll(new LanMessage { type = "start" });
            SceneManager.LoadScene(CampScenes.Game);
        }

        public bool CanStart(out string reason)
        {
            if (CurrentRoom == null)
            {
                reason = "尚未加入房间";
                return false;
            }
            if (CurrentRoom.players.Count < MinPlayers)
            {
                reason = "至少需要 2 名玩家";
                return false;
            }
            if (CurrentRoom.players.Any(player => string.IsNullOrEmpty(player.role)))
            {
                reason = "所有玩家都需要选择身份";
                return false;
            }
            if (CurrentRoom.players.Count(player => player.role == "officer") != 1)
            {
                reason = "房间需要且只能有 1 名军官";
                return false;
            }
            if (CurrentRoom.players.Any(player => !player.ready))
            {
                reason = "等待所有玩家准备";
                return false;
            }
            reason = "全员准备完成";
            return true;
        }

        public RoomPlayerData GetLocalPlayer()
        {
            if (CurrentRoom == null) return null;
            return CurrentRoom.players.FirstOrDefault(player => player.id == LocalPlayerId);
        }

        public void SetGameInput(float x, float y)
        {
            if (CurrentRoom == null || !CurrentRoom.inGame) return;
            if (IsHost)
                GameSimulation.SetInput(CurrentRoom.game, LocalPlayerId, x, y);
            else
                SendClient(new LanMessage { type = "input", inputX = x, inputY = y });
        }

        public void TriggerGameAction(string action)
        {
            if (CurrentRoom == null || !CurrentRoom.inGame) return;
            if (IsHost)
                GameSimulation.StartAction(CurrentRoom.game, LocalPlayerId, action,
                    Time.realtimeSinceStartup);
            else
                SendClient(new LanMessage { type = "action", action = action });
        }

        public void LeaveRoom()
        {
            if (clientWriter != null)
            {
                try { SendClient(new LanMessage { type = "leave" }); }
                catch { }
            }

            hostRunning = false;
            try { hostListener?.Stop(); } catch { }
            hostListener = null;
            lock (hostPeers)
            {
                foreach (HostPeer peer in hostPeers) peer.Close();
                hostPeers.Clear();
            }

            try { clientConnection?.Close(); } catch { }
            clientConnection = null;
            clientWriter = null;
            clientReadThread = null;
            CurrentRoom = null;
            LocalPlayerId = null;
            IsHost = false;
            RaiseStateChanged();
        }

        public static string GetLocalAddressText()
        {
            List<string> addresses = new List<string>();
            foreach (NetworkInterface network in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus != OperationalStatus.Up) continue;
                foreach (UnicastIPAddressInformation info in network.GetIPProperties().UnicastAddresses)
                {
                    if (info.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(info.Address))
                    {
                        addresses.Add(info.Address.ToString());
                    }
                }
            }
            return addresses.Count > 0 ? string.Join(" / ", addresses.Distinct()) : "未检测到局域网地址";
        }

        private void StartDiscovery()
        {
            if (discoveryRunning) return;
            try
            {
                discoveryReceiver = new UdpClient();
                discoveryReceiver.Client.SetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                discoveryReceiver.Client.ExclusiveAddressUse = false;
                discoveryReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
                discoveryReceiver.Client.ReceiveTimeout = 1000;
                discoveryRunning = true;
                discoveryThread = new Thread(ReceiveDiscovery) { IsBackground = true };
                discoveryThread.Start();
            }
            catch (Exception exception)
            {
                SetStatus("局域网搜索不可用：" + exception.Message);
            }
        }

        private void ReceiveDiscovery()
        {
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            while (discoveryRunning)
            {
                try
                {
                    byte[] data = discoveryReceiver.Receive(ref sender);
                    string packet = Encoding.UTF8.GetString(data);
                    string[] parts = packet.Split('|');
                    if (parts.Length < 4 || parts[0] != DiscoveryMagic) continue;
                    string address = sender.Address.ToString();
                    string code = parts[1];
                    string hostName = parts[2];
                    int playerCount;
                    int.TryParse(parts[3], out playerCount);
                    mainThreadActions.Enqueue(() => UpdateDiscovery(
                        code, hostName, address, playerCount));
                }
                catch (SocketException) { }
                catch { }
            }
        }

        private void UpdateDiscovery(string code, string hostName, string address, int playerCount)
        {
            DiscoveredRoom room = discoveredRooms.FirstOrDefault(
                item => item.code == code && item.address == address);
            if (room == null)
            {
                room = new DiscoveredRoom { code = code, address = address };
                discoveredRooms.Add(room);
            }
            room.hostName = hostName;
            room.playerCount = playerCount;
            room.lastSeen = Time.realtimeSinceStartup;
            if (DiscoveriesChanged != null) DiscoveriesChanged();
        }

        private void BroadcastDiscovery()
        {
            UdpClient sender = new UdpClient();
            sender.EnableBroadcast = true;
            while (hostRunning)
            {
                try
                {
                    RoomPlayerData host = CurrentRoom?.players.FirstOrDefault(player => player.host);
                    string packet = string.Join("|", DiscoveryMagic, CurrentRoom?.code ?? "",
                        host?.name ?? "房主", CurrentRoom?.players.Count.ToString() ?? "0");
                    byte[] data = Encoding.UTF8.GetBytes(packet);
                    sender.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
                    sender.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, DiscoveryPort));
                }
                catch { }
                Thread.Sleep(900);
            }
            sender.Close();
        }

        private void AcceptClients()
        {
            while (hostRunning)
            {
                try
                {
                    TcpClient connection = hostListener.AcceptTcpClient();
                    HostPeer peer = new HostPeer(connection);
                    Thread reader = new Thread(() => ReadHostPeer(peer)) { IsBackground = true };
                    reader.Start();
                }
                catch { if (!hostRunning) return; }
            }
        }

        private void ReadHostPeer(HostPeer peer)
        {
            try
            {
                string line;
                while (hostRunning && (line = peer.Reader.ReadLine()) != null)
                {
                    string captured = line;
                    mainThreadActions.Enqueue(() => HandleHostMessage(peer, captured));
                }
            }
            catch { }
            finally
            {
                mainThreadActions.Enqueue(() => RemovePeer(peer));
            }
        }

        private void ReadClient(TcpClient connection)
        {
            try
            {
                StreamReader reader = new StreamReader(connection.GetStream(), Encoding.UTF8);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string captured = line;
                    mainThreadActions.Enqueue(() => HandleClientMessage(captured));
                }
            }
            catch { }
            finally
            {
                mainThreadActions.Enqueue(() =>
                {
                    if (clientConnection == connection && CurrentRoom != null)
                    {
                        SetStatus("与房主的连接已断开");
                        CurrentRoom = null;
                        RaiseStateChanged();
                    }
                });
            }
        }

        private void HandleHostMessage(HostPeer peer, string json)
        {
            LanMessage message;
            try { message = JsonUtility.FromJson<LanMessage>(json); }
            catch { return; }
            if (message == null) return;

            if (message.type == "join")
            {
                if (CurrentRoom.inGame)
                {
                    peer.Send(new LanMessage { type = "error", error = "游戏已经开始" });
                    peer.Close();
                    return;
                }
                if (CurrentRoom.players.Count >= MaxPlayers)
                {
                    peer.Send(new LanMessage { type = "error", error = "房间已满" });
                    peer.Close();
                    return;
                }
                peer.PlayerId = Guid.NewGuid().ToString("N");
                CurrentRoom.players.Add(new RoomPlayerData
                {
                    id = peer.PlayerId,
                    name = CleanName(message.name),
                    online = true
                });
                lock (hostPeers) hostPeers.Add(peer);
                peer.Send(new LanMessage { type = "joined", playerId = peer.PlayerId });
                BroadcastState();
                return;
            }
            if (string.IsNullOrEmpty(peer.PlayerId)) return;
            if (message.type == "role") ApplyRole(peer.PlayerId, message.role);
            else if (message.type == "ready") ApplyReady(peer.PlayerId, message.ready);
            else if (message.type == "input")
                GameSimulation.SetInput(CurrentRoom.game, peer.PlayerId,
                    message.inputX, message.inputY);
            else if (message.type == "action")
                GameSimulation.StartAction(CurrentRoom.game, peer.PlayerId,
                    message.action, Time.realtimeSinceStartup);
            else if (message.type == "leave") RemovePeer(peer);
        }

        private void HandleClientMessage(string json)
        {
            LanMessage message;
            try { message = JsonUtility.FromJson<LanMessage>(json); }
            catch { return; }
            if (message == null) return;

            if (message.type == "joined")
            {
                LocalPlayerId = message.playerId;
                SetStatus("成功加入房间");
            }
            else if (message.type == "state")
            {
                CurrentRoom = message.room;
                RaiseStateChanged();
            }
            else if (message.type == "start")
            {
                SceneManager.LoadScene(CampScenes.Game);
            }
            else if (message.type == "error")
            {
                SetStatus(message.error);
            }
        }

        private void ApplyRole(string playerId, string role)
        {
            if (role != "officer" && role != "disguiser") return;
            RoomPlayerData player = CurrentRoom.players.FirstOrDefault(item => item.id == playerId);
            if (player == null) return;
            int limit = role == "officer" ? 1 : 3;
            int used = CurrentRoom.players.Count(item => item.id != playerId && item.role == role);
            if (used >= limit)
            {
                SetStatus(role == "officer" ? "军官已有人选择" : "伪装者名额已满");
                return;
            }
            player.role = role;
            player.ready = false;
            BroadcastState();
        }

        private void ApplyReady(string playerId, bool ready)
        {
            RoomPlayerData player = CurrentRoom.players.FirstOrDefault(item => item.id == playerId);
            if (player == null || string.IsNullOrEmpty(player.role)) return;
            player.ready = ready;
            BroadcastState();
        }

        private void BroadcastState()
        {
            RaiseStateChanged();
            SendToAll(new LanMessage { type = "state", room = CurrentRoom });
        }

        private void SendToAll(LanMessage message)
        {
            lock (hostPeers)
            {
                foreach (HostPeer peer in hostPeers.ToArray())
                {
                    if (!peer.Send(message)) RemovePeer(peer);
                }
            }
        }

        private void SendClient(LanMessage message)
        {
            if (clientWriter == null) return;
            lock (clientSendLock)
            {
                clientWriter.WriteLine(JsonUtility.ToJson(message));
            }
        }

        private void RemovePeer(HostPeer peer)
        {
            bool removed;
            lock (hostPeers) removed = hostPeers.Remove(peer);
            if (!removed) return;
            peer.Close();
            if (!string.IsNullOrEmpty(peer.PlayerId))
            {
                GameSimulation.SetPlayerOffline(CurrentRoom?.game, peer.PlayerId);
                CurrentRoom?.players.RemoveAll(player => player.id == peer.PlayerId);
                BroadcastState();
            }
        }

        private void RaiseStateChanged()
        {
            if (StateChanged != null) StateChanged();
        }

        private void SetStatus(string status)
        {
            Status = status;
            if (StatusChanged != null) StatusChanged();
        }

        private static string CleanName(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "青蛙玩家" : value.Trim();
            return result.Length > 12 ? result.Substring(0, 12) : result;
        }

        private static string CleanCode(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            string result = new string(value.ToUpperInvariant()
                .Where(character => char.IsLetterOrDigit(character)).ToArray());
            return result.Length > 4 ? result.Substring(0, 4) : result;
        }

        private static string CreateRoomCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            System.Random random = new System.Random();
            return new string(Enumerable.Range(0, 4)
                .Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
        }

        private void OnApplicationQuit()
        {
            discoveryRunning = false;
            try { discoveryReceiver?.Close(); } catch { }
            LeaveRoom();
        }

        private sealed class HostPeer
        {
            private readonly object sendLock = new object();
            private readonly TcpClient connection;
            public readonly StreamReader Reader;
            public readonly StreamWriter Writer;
            public string PlayerId;

            public HostPeer(TcpClient connection)
            {
                this.connection = connection;
                NetworkStream stream = connection.GetStream();
                Reader = new StreamReader(stream, Encoding.UTF8);
                Writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            }

            public bool Send(LanMessage message)
            {
                try
                {
                    lock (sendLock) Writer.WriteLine(JsonUtility.ToJson(message));
                    return true;
                }
                catch { return false; }
            }

            public void Close()
            {
                try { connection.Close(); } catch { }
            }
        }
    }
}
