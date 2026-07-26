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
            if (SessionState.GetBool("FrogCamp.PrototypeSmokeV7", false)) return;
            SessionState.SetBool("FrogCamp.PrototypeSmokeV7", true);
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
            if (game.npcs.Count != 19 || !officer.stunned ||
                officer.soundEvent != "tongueHit" || officer.soundEventId != 1)
                throw new System.Exception("军官吐舌命中 AI 的消灭或眩晕逻辑失败。");

            GameStateData soundGame = GameSimulation.Create(room, 8f);
            GameActorData croakingPlayer = soundGame.players[1];
            GameSimulation.StartAction(soundGame, croakingPlayer.id, "croak", 8.1f);
            if (croakingPlayer.soundEvent != "frog" ||
                croakingPlayer.soundEventId != 1)
                throw new System.Exception("真人或 AI 呱叫声音事件没有同步生成。");

            GameActorData missingOfficer = soundGame.players[0];
            missingOfficer.x = missingOfficer.y = 30f;
            foreach (GameActorData target in soundGame.npcs)
            {
                target.x = 850f;
                target.y = 450f;
            }
            croakingPlayer.x = 900f;
            croakingPlayer.y = 480f;
            GameSimulation.StartAction(soundGame, missingOfficer.id, "tongue", 9f);
            GameSimulation.Tick(soundGame, .05f,
                9f + GameSimulation.ActionDuration("tongue") + .01f);
            if (missingOfficer.soundEvent != "tongueMiss" ||
                missingOfficer.soundEventId != 1)
                throw new System.Exception("军官吐舌落空声音事件没有同步生成。");

            if (CadenceBeatTable.Points.Count != 100 ||
                Mathf.Abs(CadenceBeatTable.Points[0].time - 22.927202f) > .001f ||
                Mathf.Abs(CadenceBeatTable.Points[4].time - 39.669921f) > .001f ||
                Mathf.Abs(CadenceBeatTable.Points[20].time - 129.585202f) > .001f ||
                CadenceBeatTable.Points[0].beat != 1 ||
                CadenceBeatTable.Points[3].beat != 4 ||
                CadenceBeatTable.Points[4].beat != 1)
                throw new System.Exception("项目跑操重复段时间轴读取或生成失败。");

            GameStateData directionGame = GameSimulation.Create(room, 4f);
            GameActorData directionActor = directionGame.players[1];
            directionActor.facing = "right";
            GameSimulation.StartAction(directionGame, directionActor.id, "armRight", 4.1f);
            GameSimulation.SetInput(directionGame, directionActor.id, -1f, 0f);
            if (directionActor.facing != "right" || directionActor.actionFacing != "right")
                throw new System.Exception("动作播放期间角色朝向没有正确锁定。");

            GameStateData cadenceGame = GameSimulation.Create(room, 3f);
            GameSimulation.Tick(cadenceGame,
                CadenceBeatTable.Points[0].time + .001f, 3.05f);
            if (cadenceGame.nextCadenceBeat != 1 ||
                cadenceGame.npcs.Exists(item => item.action != "armRight"))
                throw new System.Exception("跑操第一拍未让全部 NPC 同步执行数字键 1 动作。");

            Debug.Log("原型冒烟测试通过：联机角色、呱叫与吐舌命中/落空声音事件、100 个重复段拍点、动作朝向锁定、首拍全体 NPC 动作均正常。");
        }
    }
}
