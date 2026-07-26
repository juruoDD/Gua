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
            if (SessionState.GetBool("FrogCamp.PrototypeSmokeV5", false)) return;
            SessionState.SetBool("FrogCamp.PrototypeSmokeV5", true);
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

            if (CadenceBeatTable.Points.Count != 739 ||
                Mathf.Abs(CadenceBeatTable.Points[0].time - 22.967244f) > .001f ||
                Mathf.Abs(CadenceBeatTable.Points[1].time -
                    CadenceBeatTable.Points[0].time - .6974564f) > .001f ||
                CadenceBeatTable.Points[0].beat != 1 ||
                CadenceBeatTable.Points[3].beat != 4 ||
                CadenceBeatTable.Points[4].beat != 1 ||
                CadenceBeatTable.Points[738].time > 538.00635f)
                throw new System.Exception("项目跑操等差时间轴读取或生成失败。");

            GameStateData cadenceGame = GameSimulation.Create(room, 3f);
            GameSimulation.Tick(cadenceGame,
                CadenceBeatTable.Points[0].time + .001f, 3.05f);
            if (cadenceGame.nextCadenceBeat != 1 ||
                cadenceGame.npcs.Exists(item => item.action != "armRight"))
                throw new System.Exception("跑操第一拍未让全部 NPC 同步执行数字键 1 动作。");

            Debug.Log("原型冒烟测试通过：联机角色、吐舌判定、739 拍项目时间轴、首拍全体 NPC 动作均正常。");
        }
    }
}
