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
            if (SessionState.GetBool("FrogCamp.PrototypeSmokeV10", false)) return;
            SessionState.SetBool("FrogCamp.PrototypeSmokeV10", true);
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
            if (game.npcs.Count != 20 || !npc.eliminated ||
                npc.action != "death" || !officer.stunned ||
                officer.soundEvent != "tongueWrong" || officer.soundEventId != 2)
                throw new System.Exception("军官吐舌命中 AI 后保留尸体或眩晕逻辑失败。");
            GameSimulation.Tick(game, 4f, 7f);
            if (game.npcs.Count != 20 || !npc.eliminated || npc.action != "death")
                throw new System.Exception("绿色 AI 尸体没有保持死亡最终状态。");

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
            if (missingOfficer.soundEvent != "tongueCast" ||
                missingOfficer.soundEventId != 1)
                throw new System.Exception("吐舌动作声音事件没有同步生成。");
            missingOfficer.x = GameSimulation.AssemblyCenterX;
            missingOfficer.y = GameSimulation.AssemblyCenterY;
            GameSimulation.StartAction(soundGame, missingOfficer.id, "whistle", 11f);
            if (missingOfficer.soundEvent != "whistle" ||
                missingOfficer.soundEventId != 2)
                throw new System.Exception("军官吹哨声音事件没有同步生成。");
            if (FrogCamp.Gameplay.FrogAnimationSet.GetFrameCount("whistle") != 7)
                throw new System.Exception("粉色军官吹哨动画没有按 7 帧播放。");

            if (CadenceBeatTable.Points.Count != 48 ||
                Mathf.Abs(CadenceBeatTable.Points[0].time - 22.927202f) > .001f ||
                Mathf.Abs(CadenceBeatTable.Points[4].time - 25.819710f) > .001f ||
                Mathf.Abs(CadenceBeatTable.LoopStartTime - 39.669921f) > .001f ||
                Mathf.Abs(CadenceBeatTable.LoopEndTime - 106.658f) > .001f ||
                CadenceBeatTable.LoopStartIndex != 8 ||
                CadenceBeatTable.Points[0].beat != 1 ||
                CadenceBeatTable.Points[3].beat != 4 ||
                CadenceBeatTable.Points[4].beat != 1 ||
                game.cadenceCommands.Count != 48)
                throw new System.Exception("项目跑操循环段时间轴读取失败。");

            for (int index = 0; index < CadenceBeatTable.Points.Count; index++)
            {
                bool sequenceStart = index == 0 ||
                    CadenceBeatTable.Points[index].time -
                    CadenceBeatTable.Points[index - 1].time > 1.5f;
                if (sequenceStart &&
                    !game.cadenceCommands[index].StartsWith("move"))
                    throw new System.Exception("跑操每轮第一个命令不是方向移动。");
            }

            GameStateData directionGame = GameSimulation.Create(room, 4f);
            GameActorData directionActor = directionGame.players[1];
            directionActor.facing = "right";
            GameSimulation.StartAction(directionGame, directionActor.id, "armRight", 4.1f);
            GameSimulation.SetInput(directionGame, directionActor.id, -1f, 0f);
            if (directionActor.facing != "right" || directionActor.actionFacing != "right")
                throw new System.Exception("动作播放期间角色朝向没有正确锁定。");

            GameStateData cadenceGame = GameSimulation.Create(room, 3f);
            cadenceGame.cadenceCommands[0] = "armLeft";
            foreach (GameActorData cadenceNpc in cadenceGame.npcs)
                cadenceNpc.facing = "right";
            GameSimulation.Tick(cadenceGame,
                CadenceBeatTable.Points[0].time + .001f, 3.05f);
            if (cadenceGame.nextCadenceBeat != 1 ||
                cadenceGame.npcs.Exists(item =>
                    item.action != "armLeft" || item.actionFacing != "right"))
                throw new System.Exception("跑操第一拍未让全部 NPC 同步执行预生成命令。");

            float firstActionEnd = 3.05f +
                GameSimulation.ActionDuration("armLeft") + .01f;
            GameSimulation.Tick(cadenceGame,
                GameSimulation.ActionDuration("armLeft") + .01f, firstActionEnd);
            if (cadenceGame.npcs.Exists(item =>
                    !string.IsNullOrEmpty(item.action) || item.moving ||
                    item.facing != "right"))
                throw new System.Exception("连续拍点之间 NPC 触发了额外动作或改变了朝向。");

            cadenceGame.musicTime = CadenceBeatTable.LoopEndTime - .01f;
            cadenceGame.nextCadenceBeat = CadenceBeatTable.Points.Count;
            cadenceGame.cadenceCommands[CadenceBeatTable.LoopStartIndex] = "moveLeft";
            GameSimulation.Tick(cadenceGame, .02f, 3.1f);
            if (Mathf.Abs(cadenceGame.musicTime -
                    (CadenceBeatTable.LoopStartTime + .01f)) > .001f ||
                cadenceGame.nextCadenceBeat != CadenceBeatTable.LoopStartIndex + 1 ||
                cadenceGame.npcs.Exists(item =>
                    item.action != "moveLeft" || item.actionFacing != "left"))
                throw new System.Exception("音乐循环后 NPC 动作没有与循环起点同步。");

            Debug.Log("原型冒烟测试通过：粉色军官眩晕、绿色死亡尸体保留、音乐循环与 NPC 拍点同步均正常。");
        }
    }
}
