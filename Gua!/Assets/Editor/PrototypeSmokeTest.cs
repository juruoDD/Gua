using FrogCamp.Networking;
using UnityEditor;
using UnityEngine;

namespace FrogCamp.Editor
{
    [InitializeOnLoad]
    public static class PrototypeSmokeTest
    {
        static PrototypeSmokeTest()
        {
            if (SessionState.GetBool("FrogCamp.PrototypeSmokeV2", false)) return;
            SessionState.SetBool("FrogCamp.PrototypeSmokeV2", true);
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Tools/Frog Camp/Run Prototype Smoke Test")]
        public static void Run()
        {
            RoomStateData room = new RoomStateData();
            room.players.Add(new RoomPlayerData { id = "officer", name = "军官", role = "officer" });
            room.players.Add(new RoomPlayerData { id = "spy", name = "测试玩家", role = "disguiser" });
            GameStateData game = GameSimulation.Create(room, 1f);
            if (game.players.Count != 2 || game.npcs.Count != 20)
                throw new System.Exception("玩家或 AI 初始化数量不正确。");

            GameActorData officer = game.players[0];
            GameActorData npc = game.npcs[0];
            officer.x = 300f; officer.y = 300f; officer.facing = "right";
            npc.x = 330f; npc.y = 300f;
            GameSimulation.StartAction(game, officer.id, "tongue", 2f);
            GameSimulation.Tick(game, .05f, 2.46f);
            GameSimulation.Tick(game, .05f, 2.51f);
            if (game.npcs.Count != 19 || !officer.stunned)
                throw new System.Exception("军官吐舌命中 AI 的消灭或眩晕逻辑失败。");

            Debug.Log("原型冒烟测试通过：2 名玩家、20 个 AI、吐舌消灭 AI、军官眩晕均正常。");
        }
    }
}
