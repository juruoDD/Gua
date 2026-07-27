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
            if (SessionState.GetBool("FrogCamp.PrototypeSmokeV19", false)) return;
            SessionState.SetBool("FrogCamp.PrototypeSmokeV19", true);
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Tools/Frog Camp/Run Prototype Smoke Test")]
        public static void Run()
        {
            RoomStateData room = new RoomStateData();
            room.players.Add(new RoomPlayerData { id = "officer", name = "军官", role = "officer" });
            room.players.Add(new RoomPlayerData { id = "spy", name = "测试玩家", role = "disguiser" });
            GameStateData game = GameSimulation.Create(room, 1f);
            EnterFormalPhase(game);
            if (game.players.Count != 2 || game.npcs.Count != 20)
                throw new System.Exception("玩家或 AI 初始化数量不正确。");

            GameActorData officer = game.players[0];
            GameActorData npc = game.npcs[0];
            for (int index = 1; index < game.npcs.Count; index++)
            {
                game.npcs[index].x = 850f;
                game.npcs[index].y = 450f;
            }
            game.players[1].x = 900f;
            game.players[1].y = 480f;
            officer.x = 300f; officer.y = 300f; officer.facing = "right";
            npc.x = 330f; npc.y = 300f;
            GameSimulation.StartAction(game, officer.id, "tongue", 2f);
            GameSimulation.Tick(game, .05f, 2.46f);
            GameSimulation.Tick(game, .05f, 2.51f);
            if (game.npcs.Count != 20 || !npc.eliminated ||
                npc.action != "death" || !officer.stunned ||
                officer.soundEvent != "tongueWrong" || officer.soundEventId != 2)
                throw new System.Exception(
                    "军官吐舌命中 AI 后保留尸体或眩晕逻辑失败。" +
                    " phase=" + game.phase +
                    " eliminated=" + npc.eliminated +
                    " action=" + npc.action +
                    " stunned=" + officer.stunned +
                    " sound=" + officer.soundEvent +
                    " soundId=" + officer.soundEventId);
            GameSimulation.Tick(game, 4f, 7f);
            if (game.npcs.Count != 20 || !npc.eliminated || npc.action != "death")
                throw new System.Exception("绿色 AI 尸体没有保持死亡最终状态。");

            GameStateData soundGame = GameSimulation.Create(room, 8f);
            EnterFormalPhase(soundGame);
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
            EnterFormalPhase(directionGame);
            GameActorData directionActor = directionGame.players[1];
            directionActor.facing = "right";
            GameSimulation.StartAction(directionGame, directionActor.id, "armRight", 4.1f);
            GameSimulation.SetInput(directionGame, directionActor.id, -1f, 0f);
            if (directionActor.facing != "right" || directionActor.actionFacing != "right")
                throw new System.Exception("动作播放期间角色朝向没有正确锁定。");

            GameStateData cadenceGame = GameSimulation.Create(room, 3f);
            EnterFormalPhase(cadenceGame);
            cadenceGame.cadenceCommands[0] = "armLeft";
            foreach (GameActorData cadenceNpc in cadenceGame.npcs)
                cadenceNpc.facing = "right";
            GameSimulation.Tick(cadenceGame,
                CadenceBeatTable.Points[0].time + .001f, 3.05f);
            GameSimulation.Tick(cadenceGame, .2f, 3.25f);
            if (cadenceGame.nextCadenceBeat != 1 ||
                cadenceGame.npcs.Exists(item =>
                    item.action != "armLeft" || item.actionFacing != "right"))
                throw new System.Exception("跑操第一拍未让全部 NPC 同步执行预生成命令。");

            float firstActionEnd = 3.25f +
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
            GameSimulation.Tick(cadenceGame, .02f, firstActionEnd + .1f);
            if (Mathf.Abs(cadenceGame.musicTime -
                    (CadenceBeatTable.LoopStartTime + .01f)) > .001f ||
                cadenceGame.nextCadenceBeat != CadenceBeatTable.LoopStartIndex + 1)
                throw new System.Exception("音乐没有正确循环到循环拍点。");
            GameSimulation.Tick(cadenceGame, .2f, firstActionEnd + .3f);
            if (
                cadenceGame.npcs.Exists(item =>
                    item.action != "moveLeft" || item.actionFacing != "left"))
                throw new System.Exception("音乐循环后 NPC 动作没有与循环起点同步。");

            GameStateData taskWinGame = GameSimulation.Create(room, 20f);
            EnterFormalPhase(taskWinGame);
            GameSimulation.SetTaskProgress(taskWinGame, "officer", 100);
            if (taskWinGame.ended)
                throw new System.Exception("军官任务进度错误触发了游戏结束。");
            GameSimulation.SetTaskProgress(taskWinGame, "spy", 100);
            if (!taskWinGame.ended || taskWinGame.winnerRole != "disguiser")
                throw new System.Exception("伪装蛙任务进度达到 100% 后没有获胜。");

            GameStateData officerWinGame = GameSimulation.Create(room, 30f);
            EnterFormalPhase(officerWinGame);
            officerWinGame.players[1].eliminated = true;
            GameSimulation.Tick(officerWinGame, .05f, 30.05f);
            if (!officerWinGame.ended ||
                officerWinGame.winnerRole != "officer")
                throw new System.Exception("全部伪装蛙被消灭后军官没有获胜。");

            GameStateData phaseGame = GameSimulation.Create(room, 40f);
            GameSimulation.BeginTutorialRules(phaseGame);
            GameSimulation.Tick(phaseGame,
                GameSimulation.RulesDuration + .01f, 46f);
            if (phaseGame.phase != GameSimulation.PhaseTrialIntro)
                throw new System.Exception("规则说明结束后没有进入试玩提示。");
            GameSimulation.Tick(phaseGame,
                GameSimulation.TrialIntroDuration + .01f, 48f);
            if (phaseGame.phase != GameSimulation.PhaseTrialCountdown)
                throw new System.Exception("试玩提示结束后没有进入试玩倒计时。");
            GameSimulation.Tick(phaseGame,
                GameSimulation.ReadyCountdownDuration + .01f, 52f);
            if (phaseGame.phase != GameSimulation.PhaseTrial)
                throw new System.Exception("试玩倒计时结束后没有进入试玩阶段。");
            GameSimulation.Tick(phaseGame,
                GameSimulation.TrialDuration + .01f, 83f);
            if (phaseGame.phase != GameSimulation.PhaseTrialEnd)
                throw new System.Exception("30 秒试玩结束提示没有触发。");
            GameSimulation.Tick(phaseGame,
                GameSimulation.TrialEndDuration + .01f, 86f);
            GameSimulation.Tick(phaseGame,
                GameSimulation.FormalIntroDuration + .01f, 88f);
            GameSimulation.Tick(phaseGame,
                GameSimulation.ReadyCountdownDuration + .01f, 92f);
            if (phaseGame.phase != GameSimulation.PhaseFormal)
                throw new System.Exception("试玩结束后没有进入正式游戏。");

            Debug.Log("原型冒烟测试通过：结算胜负、角色动画、音乐循环与 NPC 拍点同步均正常。");
        }

        private static void EnterFormalPhase(GameStateData game)
        {
            game.phase = GameSimulation.PhaseFormal;
            game.phaseRemaining = 0f;
            game.countdownRemaining = 0f;
        }
    }
}
