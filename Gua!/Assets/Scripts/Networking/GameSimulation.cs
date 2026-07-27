using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FrogCamp.Networking
{
    public static class GameSimulation
    {
        public const float WorldWidth = 960f;
        public const float WorldHeight = 540f;
        public const float MinX = 28f;
        public const float MaxX = 932f;
        public const float MinY = 28f;
        public const float MaxY = 512f;
        public const float MoveSpeed = 42f;
        public const float OfficerMoveSpeed = MoveSpeed;
        public const float AssemblyMoveSpeed = 100f;
        public const float AssemblyCompactionSpeed = 38f;
        public const float AssemblyCompactionMinimumDistance = 31f;
        public const float ColliderRadius = 13f;
        public const float DeadColliderRadius = 9f;
        public const float JumpDistance = 48f;
        public const float TongueRange = 44f;
        public const float AssemblyCenterX = 471f;
        public const float AssemblyCenterY = 274f;
        public const float CentralAreaRadiusX = 40f;
        public const float CentralAreaRadiusY = 40f;
        public const int NpcCount = 20;
        public const float AnimationSpeedMultiplier = 1.25f;
        public const float LimbAnimationExtraSpeed = 1.35f;
        public const string PhaseRules = "rules";
        public const string PhaseTrialIntro = "trialIntro";
        public const string PhaseTrialCountdown = "trialCountdown";
        public const string PhaseTrial = "trial";
        public const string PhaseTrialEnd = "trialEnd";
        public const string PhaseFormalIntro = "formalIntro";
        public const string PhaseFormalCountdown = "formalCountdown";
        public const string PhaseFormal = "formal";
        public const float RulesDuration = 25f;
        public const float TrialIntroDuration = 1.8f;
        public const float TrialDuration = 30f;
        public const float TrialEndDuration = 2.6f;
        public const float FormalIntroDuration = 1.8f;
        public const float ReadyCountdownDuration = 3f;
        public const float TrialRespawnDelay = 2f;
        public const string DancePhaseWhistle = "whistle";
        public const string DancePhaseBell = "bell";
        public const string DancePhaseMusic = "dance";
        public const string DancePhasePause = "pause";
        public const float WhistleSoundDuration = 1f;
        public const float WhistleCooldown = 30f;
        public const float BellSoundDuration = 4.2f;
        public const float PostDancePauseDuration = 2f;
        public const int DanceBeatCount = 24;
        public const int DanceIntroBeatCount = 8;
        public const float DanceBeatInterval = 7.836719f / 16f;
        public const float DanceMusicDuration = DanceBeatInterval * DanceBeatCount;
        public const float DanceActionStartTime =
            DanceBeatInterval * DanceIntroBeatCount;
        public const int DanceActionCount =
            (DanceBeatCount - DanceIntroBeatCount) / 2;
        public const float DanceActionInterval = DanceBeatInterval * 2f;

        private const float EdgeMargin = 90f;
        private const float AssemblyArrivalTolerance = 14f;
        private const float AssemblyReassignDelay = 1.2f;
        private const float CadenceSequenceMaxGap = 1.5f;
        private const float CadenceDecisionGuard = 0.05f;
        private const float NpcSequenceTimingErrorRatio = 0.2f;
        private const float WrongNpcStunDuration = 5f;
        private const float LongWrongNpcStunDuration = 8f;
        private const int WrongNpcKillsBeforeLongStun = 3;
        private static readonly string[] Facings =
        {
            "up", "upRight", "right", "downRight",
            "down", "downLeft", "left", "upLeft"
        };
        private static readonly string[] NpcActions =
        {
            "jump", "armLeft", "armRight", "legLeft",
            "legRight", "croak", "tongue", "salute"
        };
        private static readonly string[] CadenceCommands =
        {
            "armLeft", "armRight", "legLeft", "legRight",
            "moveUp", "moveDown", "moveLeft", "moveRight"
        };
        private static readonly string[] CadenceOpeningCommands =
        {
            "moveUp", "moveDown", "moveLeft", "moveRight"
        };
        private static readonly string[] DanceCommands =
        {
            "salute", "croak"
        };
        private static readonly float[] AssemblyAvoidanceAngles =
        {
            0f, 10f, -10f, 20f, -20f, 30f, -30f,
            40f, -40f, 50f, -50f, 60f, -60f
        };
        private static readonly float[] DispersalAvoidanceAngles =
        {
            0f, 24f, -24f, 48f, -48f, 78f, -78f,
            108f, -108f, 142f, -142f, 180f
        };
        private static readonly int[] AssemblyRingCounts = { 6, 10, 16, 20 };
        private static readonly Vector2[] AssemblyRingRadii =
        {
            new Vector2(40f, 25f),
            new Vector2(80f, 55f),
            new Vector2(120f, 85f),
            new Vector2(160f, 115f)
        };
        private const float AssemblyRotationDegrees = 30f;

        public static GameStateData Create(RoomStateData room, float now)
        {
            GameStateData game = new GameStateData
            {
                phase = PhaseRules,
                phaseRemaining = RulesDuration,
                phaseVersion = 1
            };
            bool needsTestOfficer =
                !room.players.Any(player => player.role == "officer");
            foreach (RoomPlayerData player in room.players)
            {
                GameActorData actor = NewActor(player.id, player.name, player.role, false, now);
                PlaceActor(actor, game.players);
                game.players.Add(actor);
            }
            for (int index = 0; index < NpcCount; index++)
            {
                bool testOfficer = needsTestOfficer && index == 0;
                GameActorData npc = NewActor("npc-" + (index + 1),
                    testOfficer ? "军官蛙" : "",
                    testOfficer ? "officer" : "disguiser", true, now);
                PlaceActor(npc, game.players.Concat(game.npcs).ToList());
                game.npcs.Add(npc);
            }
            IReadOnlyList<CadenceBeatPoint> cadenceBeats = CadenceBeatTable.Points;
            for (int index = 0; index < cadenceBeats.Count; index++)
            {
                string[] choices = IsCadenceSequenceStart(cadenceBeats, index)
                    ? CadenceOpeningCommands : CadenceCommands;
                game.cadenceCommands.Add(choices[Random.Range(0, choices.Length)]);
            }
            return game;
        }

        public static void SetInput(GameStateData game, string id, float x, float y)
        {
            if (!IsInteractivePhase(game)) return;
            GameActorData actor = FindPlayer(game, id);
            if (!CanControl(actor)) return;
            if (actor.role == "officer" && IsOfficerLockedForDance(game))
            {
                actor.inputX = actor.inputY = 0f;
                actor.moving = false;
                return;
            }
            if (!string.IsNullOrEmpty(actor.action))
            {
                actor.inputX = actor.inputY = 0f;
                return;
            }
            Vector2 input = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            actor.inputX = input.x;
            actor.inputY = input.y;
            if (input.sqrMagnitude > 0.01f) actor.facing = FacingFrom(input);
        }

        public static void StartAction(GameStateData game, string id, string action, float now)
        {
            if (!IsInteractivePhase(game)) return;
            GameActorData actor = FindPlayer(game, id);
            if (!CanControl(actor) || !string.IsNullOrEmpty(actor.action)) return;
            if (actor.role == "officer" && IsOfficerLockedForDance(game)) return;
            if (action == "whistle" && IsDanceSequenceActive(game)) return;
            if (action == "whistle" && actor.role == "officer" &&
                now < actor.nextWhistleAt)
            {
                game.announcement = "吹哨冷却中：" +
                    Mathf.CeilToInt(actor.nextWhistleAt - now) + " 秒";
                game.announcementId++;
                return;
            }
            if (action == "whistle" && actor.role == "officer" &&
                !IsOnCentralLily(actor))
            {
                game.announcement = "军官只能在集合旗帜下吹哨";
                game.announcementId++;
                return;
            }
            bool allowed = actor.role == "officer"
                ? action == "jump" || action == "croak" || action == "tongue" || action == "whistle"
                : action == "jump" || action == "armLeft" || action == "armRight" ||
                  action == "legLeft" || action == "legRight" ||
                  action == "croak" || action == "tongue" || action == "salute";
            if (!allowed) return;
            BeginAction(actor, action, now);
            if (action == "whistle")
                StartDanceSequence(game, actor);
        }

        public static void Tick(GameStateData game, float deltaTime, float now)
        {
            if (game == null || game.ended) return;
            if (game.phase == PhaseRules && !game.tutorialStarted)
            {
                FreezeActors(game);
                return;
            }
            if (!IsInteractivePhase(game))
            {
                AdvancePhase(game, deltaTime, now);
                FreezeActors(game);
                return;
            }
            if (game.phase == PhaseTrial)
            {
                game.phaseRemaining =
                    Mathf.Max(0f, game.phaseRemaining - deltaTime);
                if (game.phaseRemaining <= 0f)
                {
                    BeginPhase(game, PhaseTrialEnd, TrialEndDuration);
                    FreezeActors(game);
                    return;
                }
                RespawnTrialActors(game, now);
            }
            bool danceSequenceActive = IsDanceSequenceActive(game);
            if (danceSequenceActive)
                AdvanceDanceSequence(game, deltaTime, now);
            else
            {
                game.musicTime += deltaTime;
                WrapCadenceMusic(game);
                TriggerCadenceActions(game, now);
            }
            bool dancePerformanceActive = IsOfficerLockedForDance(game);
            bool assemblyActive =
                game.specialMusicPhase == DancePhaseWhistle ||
                game.specialMusicPhase == DancePhaseBell;
            List<GameActorData> actors = game.players.Concat(game.npcs).ToList();
            foreach (GameActorData actor in game.players)
            {
                actor.stunned = now < actor.stunnedUntil;
                if (actor.eliminated || !actor.online) continue;
                FinishAction(actor, now);
                if (actor.stunned)
                {
                    actor.inputX = actor.inputY = 0f;
                    actor.moving = false;
                    continue;
                }
                if (actor.role == "officer" && dancePerformanceActive)
                {
                    actor.inputX = actor.inputY = 0f;
                    actor.moving = false;
                    continue;
                }
                if (actor.action == "jump")
                    MoveDuringJump(actor, deltaTime, actors);
                else if (string.IsNullOrEmpty(actor.action))
                {
                    float speed = actor.role == "officer" ? OfficerMoveSpeed : MoveSpeed;
                    actor.moving = Move(actor, actor.inputX * speed * deltaTime,
                        actor.inputY * speed * deltaTime, actors);
                }
                else actor.moving = false;
                ResolveOfficerTongue(game, actor, now);
            }
            if (game.phase == PhaseFormal) EvaluateWinner(game);
            if (game.ended) return;
            GameActorData[] npcs = game.npcs.ToArray();
            for (int npcIndex = 0; npcIndex < npcs.Length; npcIndex++)
            {
                GameActorData npc = npcs[npcIndex];
                if (npc.eliminated || !npc.online) continue;
                FinishAction(npc, now);
                StartPendingNpcSequenceAction(npc, now);
                if (npc.action == "jump")
                {
                    MoveDuringJump(npc, deltaTime, actors);
                    continue;
                }
                if (IsCadenceMoveAction(npc.action))
                {
                    Vector2 direction = CadenceMoveDirection(npc.action);
                    npc.moving = Move(npc, direction.x * MoveSpeed * deltaTime,
                        direction.y * MoveSpeed * deltaTime, actors);
                    continue;
                }
                if (!string.IsNullOrEmpty(npc.action))
                {
                    npc.moving = false;
                    continue;
                }
                if (danceSequenceActive)
                {
                    if (assemblyActive)
                        MoveNpcTowardAssembly(
                            game, npc, npcIndex, deltaTime, actors);
                    else if (game.specialMusicPhase == DancePhasePause)
                        MoveNpcDispersal(
                            npc, npcIndex, deltaTime, actors);
                    else
                    {
                        npc.inputX = npc.inputY = 0f;
                        npc.moving = false;
                    }
                    continue;
                }
                if (now >= npc.nextDecisionAt) ChooseNpcBehaviour(npc, now);
                npc.moving = Move(npc, npc.inputX * MoveSpeed * deltaTime,
                    npc.inputY * MoveSpeed * deltaTime, actors);
                if (!npc.moving && (npc.inputX != 0f || npc.inputY != 0f))
                    npc.nextDecisionAt = now;
            }
            if (game.phase == PhaseFormal) EvaluateWinner(game);
        }

        public static void BeginTutorialRules(GameStateData game)
        {
            if (game == null || game.tutorialStarted) return;
            game.tutorialStarted = true;
            game.phase = PhaseRules;
            game.phaseRemaining = RulesDuration;
            game.countdownRemaining = 0f;
            game.phaseVersion++;
        }

        public static bool IsInteractivePhase(GameStateData game)
        {
            return game != null && !game.ended &&
                   (game.phase == PhaseTrial ||
                    game.phase == PhaseFormal);
        }

        private static void AdvancePhase(
            GameStateData game, float deltaTime, float now)
        {
            if (game.phase == PhaseTrialCountdown ||
                game.phase == PhaseFormalCountdown)
            {
                game.countdownRemaining =
                    Mathf.Max(0f, game.countdownRemaining - deltaTime);
                if (game.countdownRemaining > 0f) return;
                BeginPhase(game,
                    game.phase == PhaseTrialCountdown
                        ? PhaseTrial : PhaseFormal,
                    game.phase == PhaseTrialCountdown
                        ? TrialDuration : 0f);
                return;
            }

            game.phaseRemaining =
                Mathf.Max(0f, game.phaseRemaining - deltaTime);
            if (game.phaseRemaining > 0f) return;
            switch (game.phase)
            {
                case PhaseRules:
                    BeginPhase(game, PhaseTrialIntro, TrialIntroDuration);
                    break;
                case PhaseTrialIntro:
                    BeginCountdown(game, PhaseTrialCountdown);
                    break;
                case PhaseTrialEnd:
                    ResetForFormalGame(game, now);
                    BeginPhase(game, PhaseFormalIntro, FormalIntroDuration);
                    break;
                case PhaseFormalIntro:
                    BeginCountdown(game, PhaseFormalCountdown);
                    break;
                default:
                    BeginPhase(game, PhaseRules, RulesDuration);
                    break;
            }
        }

        private static void BeginCountdown(
            GameStateData game, string phase)
        {
            BeginPhase(game, phase, ReadyCountdownDuration);
            game.countdownRemaining = ReadyCountdownDuration;
        }

        private static void BeginPhase(
            GameStateData game, string phase, float duration)
        {
            game.phase = phase;
            game.phaseRemaining = duration;
            game.countdownRemaining = 0f;
            game.phaseVersion++;
        }

        private static void FreezeActors(GameStateData game)
        {
            foreach (GameActorData actor in game.players.Concat(game.npcs))
            {
                actor.inputX = actor.inputY = 0f;
                actor.moving = false;
            }
        }

        private static void RespawnTrialActors(
            GameStateData game, float now)
        {
            List<GameActorData> actors =
                game.players.Concat(game.npcs).ToList();
            foreach (GameActorData actor in actors)
            {
                if (!actor.eliminated || actor.trialRespawnAt <= 0f ||
                    now < actor.trialRespawnAt)
                    continue;
                actor.eliminated = false;
                actor.action = null;
                actor.actionFacing = null;
                actor.actionResolved = false;
                actor.trialRespawnAt = 0f;
                actor.stunned = false;
                actor.stunnedUntil = 0f;
                PlaceActor(actor, actors);
            }
        }

        private static void ResetForFormalGame(
            GameStateData game, float now)
        {
            game.musicTime = 0f;
            game.nextCadenceBeat = 0;
            game.specialMusicPhase = null;
            game.specialMusicTime = 0f;
            game.nextDanceBeat = 0;
            game.danceCommands.Clear();
            game.announcement = null;
            game.announcementId++;
            int taskVersion = game.tasks == null ? 0 : game.tasks.version + 1;
            game.tasks = new TaskStateData { version = taskVersion };

            List<GameActorData> placed = new List<GameActorData>();
            foreach (GameActorData actor in game.players.Concat(game.npcs))
            {
                actor.eliminated = false;
                actor.moving = false;
                actor.stunned = false;
                actor.taskProgress = 0;
                actor.inputX = actor.inputY = 0f;
                actor.action = null;
                actor.actionFacing = null;
                actor.actionResolved = false;
                actor.pendingSequenceAction = null;
                actor.pendingSequenceActionAt = 0f;
                actor.pendingSequenceActionEmitSound = false;
                actor.jumpX = actor.jumpY = 0f;
                actor.stunnedUntil = 0f;
                actor.nextWhistleAt = 0f;
                actor.officerNpcMistakeCount = 0;
                actor.assemblySlot = -1;
                actor.assemblyBlockedTime = 0f;
                actor.trialRespawnAt = 0f;
                actor.nextDecisionAt =
                    now + Random.Range(0.25f, 1.5f);
                PlaceActor(actor, placed);
                placed.Add(actor);
            }
        }

        public static void SetTaskProgress(GameStateData game, string id, int progress)
        {
            GameActorData actor = FindPlayer(game, id);
            if (actor == null || actor.role != "disguiser" ||
                actor.eliminated || game.ended ||
                game.phase != PhaseFormal)
                return;
            actor.taskProgress = Mathf.Max(actor.taskProgress,
                Mathf.Clamp(progress, 0, 100));
            EvaluateWinner(game);
        }

        private static void EvaluateWinner(GameStateData game)
        {
            if (game == null || game.ended) return;
            if (game.tasks != null && game.tasks.finished)
            {
                EndGame(game, "disguiser");
                return;
            }
            List<GameActorData> disguisers = game.players
                .Where(actor => actor.role == "disguiser").ToList();
            if (disguisers.Any(actor => actor.taskProgress >= 100))
            {
                EndGame(game, "disguiser");
                return;
            }
            if (disguisers.Count > 0 &&
                disguisers.All(actor => actor.eliminated))
                EndGame(game, "officer");
        }

        private static void EndGame(GameStateData game, string winnerRole)
        {
            game.ended = true;
            game.winnerRole = winnerRole;
            foreach (GameActorData actor in game.players)
            {
                actor.inputX = actor.inputY = 0f;
                actor.moving = false;
            }
            game.announcement = winnerRole == "officer"
                ? "军官蛙获胜" : "捣蛋呱获胜";
            game.announcementId++;
        }

        private static void WrapCadenceMusic(GameStateData game)
        {
            float loopStart = CadenceBeatTable.LoopStartTime;
            float loopEnd = CadenceBeatTable.LoopEndTime;
            float loopLength = loopEnd - loopStart;
            if (loopLength <= 0f || game.musicTime < loopEnd) return;

            game.musicTime = loopStart +
                Mathf.Repeat(game.musicTime - loopEnd, loopLength);
            game.nextCadenceBeat = CadenceBeatTable.LoopStartIndex;
        }

        public static bool IsDanceSequenceActive(GameStateData game)
        {
            return game != null && !string.IsNullOrEmpty(game.specialMusicPhase);
        }

        public static bool IsOnCentralLily(GameActorData actor)
        {
            if (actor == null) return false;
            float normalizedX =
                (actor.x - AssemblyCenterX) / CentralAreaRadiusX;
            float normalizedY =
                (actor.y - AssemblyCenterY) / CentralAreaRadiusY;
            return normalizedX * normalizedX +
                   normalizedY * normalizedY <= 1f;
        }

        private static bool IsOfficerLockedForDance(GameStateData game)
        {
            return IsDanceSequenceActive(game) &&
                   game.specialMusicPhase != DancePhasePause;
        }

        private static void StartDanceSequence(
            GameStateData game, GameActorData officer)
        {
            game.specialMusicPhase = DancePhaseWhistle;
            game.specialMusicTime = 0f;
            game.nextDanceBeat = 0;
            officer.inputX = officer.inputY = 0f;
            officer.moving = false;
            foreach (GameActorData npc in game.npcs)
            {
                if (!npc.online || npc.eliminated) continue;
                npc.action = null;
                npc.actionFacing = null;
                npc.actionUntil = 0f;
                npc.actionResolved = false;
                npc.pendingSequenceAction = null;
                npc.pendingSequenceActionAt = 0f;
                npc.pendingSequenceActionEmitSound = false;
                npc.jumpX = npc.jumpY = 0f;
                npc.inputX = npc.inputY = 0f;
                npc.moving = false;
            }
            AssignAssemblySlots(game, officer);
            game.danceCommands.Clear();
            for (int action = 0; action < DanceActionCount; action++)
                game.danceCommands.Add(
                    DanceCommands[Random.Range(0, DanceCommands.Length)]);
        }

        private static void AssignAssemblySlots(
            GameStateData game, GameActorData officer)
        {
            foreach (GameActorData npc in game.npcs)
            {
                npc.assemblySlot = -1;
                npc.assemblyBlockedTime = 0f;
            }

            int ringStart = 0;
            for (int ring = 0; ring < AssemblyRingCounts.Length; ring++)
            {
                List<int> availableSlots = new List<int>();
                for (int slot = 0; slot < AssemblyRingCounts[ring]; slot++)
                {
                    int positionIndex = ringStart + slot;
                    Vector2 position =
                        GetAssemblyPosition(officer, positionIndex);
                    if (IsAssemblyPositionBlocked(game, position)) continue;
                    if (IsTooCloseToAssignedPosition(
                        game, officer, position, null)) continue;
                    availableSlots.Add(positionIndex);
                }

                while (availableSlots.Count > 0)
                {
                    GameActorData bestNpc = null;
                    int bestSlot = -1;
                    float bestDistanceSquared = float.MaxValue;
                    foreach (GameActorData npc in game.npcs)
                    {
                        if (!npc.online || npc.eliminated ||
                            npc.assemblySlot >= 0) continue;
                        Vector2 npcPosition = new Vector2(npc.x, npc.y);
                        foreach (int slot in availableSlots)
                        {
                            float distanceSquared =
                                (GetAssemblyPosition(officer, slot) -
                                 npcPosition).sqrMagnitude;
                            if (distanceSquared >= bestDistanceSquared) continue;
                            bestDistanceSquared = distanceSquared;
                            bestNpc = npc;
                            bestSlot = slot;
                        }
                    }
                    if (bestNpc == null) break;
                    bestNpc.assemblySlot = bestSlot;
                    availableSlots.Remove(bestSlot);
                }
                ringStart += AssemblyRingCounts[ring];
            }
        }

        private static int FindAvailableAssemblySlot(GameStateData game,
            GameActorData officer, GameActorData npc, int excludedSlot)
        {
            int maximumRing = excludedSlot < 0
                ? AssemblyRingCounts.Length - 1
                : GetAssemblyRing(excludedSlot);
            int ringStart = 0;
            for (int ring = 0; ring <= maximumRing; ring++)
            {
                int bestSlot = -1;
                float bestDistanceSquared = float.MaxValue;
                for (int slot = 0; slot < AssemblyRingCounts[ring]; slot++)
                {
                    int positionIndex = ringStart + slot;
                    if (positionIndex == excludedSlot ||
                        IsAssemblySlotUsed(game, positionIndex, npc))
                        continue;
                    Vector2 position =
                        GetAssemblyPosition(officer, positionIndex);
                    if (IsAssemblyPositionBlocked(game, position) ||
                        IsTooCloseToAssignedPosition(
                            game, officer, position, npc))
                        continue;
                    float distanceSquared =
                        (position - new Vector2(npc.x, npc.y)).sqrMagnitude;
                    if (distanceSquared >= bestDistanceSquared) continue;
                    bestDistanceSquared = distanceSquared;
                    bestSlot = positionIndex;
                }
                if (bestSlot >= 0) return bestSlot;
                ringStart += AssemblyRingCounts[ring];
            }
            return -1;
        }

        private static int GetAssemblyRing(int positionIndex)
        {
            int ringStart = 0;
            for (int ring = 0; ring < AssemblyRingCounts.Length; ring++)
            {
                ringStart += AssemblyRingCounts[ring];
                if (positionIndex < ringStart) return ring;
            }
            return AssemblyRingCounts.Length - 1;
        }

        private static Vector2 GetAssemblyPosition(
            GameActorData officer, int positionIndex)
        {
            int ringStart = 0;
            for (int ring = 0; ring < AssemblyRingCounts.Length; ring++)
            {
                int count = AssemblyRingCounts[ring];
                if (positionIndex < ringStart + count)
                {
                    int slot = positionIndex - ringStart;
                    float angle = Mathf.PI * 2f * slot / count;
                    Vector2 radii = AssemblyRingRadii[ring];
                    Vector2 local = new Vector2(
                        Mathf.Cos(angle) * radii.x,
                        Mathf.Sin(angle) * radii.y);
                    float rotation = AssemblyRotationDegrees * Mathf.Deg2Rad;
                    Vector2 rotated = new Vector2(
                        local.x * Mathf.Cos(rotation) -
                        local.y * Mathf.Sin(rotation),
                        local.x * Mathf.Sin(rotation) +
                        local.y * Mathf.Cos(rotation));
                    return new Vector2(
                        Mathf.Clamp(AssemblyCenterX + rotated.x,
                            MinX, MaxX),
                        Mathf.Clamp(AssemblyCenterY + rotated.y,
                            MinY, MaxY));
                }
                ringStart += count;
            }
            return new Vector2(AssemblyCenterX, AssemblyCenterY);
        }

        private static bool IsAssemblySlotUsed(
            GameStateData game, int slot, GameActorData except)
        {
            return game.npcs.Any(npc =>
                npc != except && npc.online && !npc.eliminated &&
                npc.assemblySlot == slot);
        }

        private static bool IsAssemblyPositionBlocked(
            GameStateData game, Vector2 position)
        {
            if (!PondObstacleMap.CanOccupy(position, ColliderRadius))
                return true;

            float minimumSquared =
                AssemblyCompactionMinimumDistance *
                AssemblyCompactionMinimumDistance;
            foreach (GameActorData player in game.players)
            {
                if (!player.online) continue;
                Vector2 offset =
                    new Vector2(player.x, player.y) - position;
                if (offset.sqrMagnitude < minimumSquared) return true;
            }
            foreach (GameActorData npc in game.npcs)
            {
                if (!npc.online || !npc.eliminated) continue;
                Vector2 offset = new Vector2(npc.x, npc.y) - position;
                if (offset.sqrMagnitude < minimumSquared) return true;
            }
            return false;
        }

        private static bool IsTooCloseToAssignedPosition(GameStateData game,
            GameActorData officer, Vector2 position, GameActorData except)
        {
            float minimumSquared =
                AssemblyCompactionMinimumDistance *
                AssemblyCompactionMinimumDistance;
            foreach (GameActorData npc in game.npcs)
            {
                if (npc == except || npc.assemblySlot < 0) continue;
                Vector2 assigned =
                    GetAssemblyPosition(officer, npc.assemblySlot);
                if ((assigned - position).sqrMagnitude < minimumSquared)
                    return true;
            }
            return false;
        }

        private static void AdvanceDanceSequence(
            GameStateData game, float deltaTime, float now)
        {
            game.specialMusicTime += deltaTime;
            if (game.specialMusicPhase == DancePhaseWhistle &&
                game.specialMusicTime >= WhistleSoundDuration)
            {
                game.specialMusicTime -= WhistleSoundDuration;
                game.specialMusicPhase = DancePhaseBell;
            }
            if (game.specialMusicPhase == DancePhaseBell &&
                game.specialMusicTime >= BellSoundDuration)
            {
                game.specialMusicTime -= BellSoundDuration;
                game.specialMusicPhase = DancePhaseMusic;
                game.nextDanceBeat = 0;
            }
            if (game.specialMusicPhase == DancePhaseMusic)
            {
                while (game.nextDanceBeat < DanceActionCount &&
                       DanceActionStartTime +
                       game.nextDanceBeat * DanceActionInterval <=
                       game.specialMusicTime)
                {
                    string action = game.nextDanceBeat < game.danceCommands.Count
                        ? game.danceCommands[game.nextDanceBeat]
                        : DanceCommands[game.nextDanceBeat % DanceCommands.Length];
                    bool croakSoundEmitted = false;
                    foreach (GameActorData npc in game.npcs)
                    {
                        if (npc.eliminated || !npc.online) continue;
                        bool emitSound = action == "croak" && !croakSoundEmitted;
                        ScheduleNpcSequenceAction(npc, action, now,
                            DanceActionInterval, emitSound);
                        if (emitSound) croakSoundEmitted = true;
                        npc.nextDecisionAt = now + DanceActionInterval;
                    }
                    game.nextDanceBeat++;
                }

                if (game.specialMusicTime < DanceMusicDuration) return;
                game.specialMusicPhase = DancePhasePause;
                game.specialMusicTime -= DanceMusicDuration;
                game.nextDanceBeat = 0;
                foreach (GameActorData npc in game.npcs)
                {
                    npc.assemblySlot = -1;
                    npc.assemblyBlockedTime = 0f;
                }
                PrepareNpcDispersal(game);
            }

            if (game.specialMusicPhase != DancePhasePause ||
                game.specialMusicTime < PostDancePauseDuration) return;
            game.specialMusicPhase = null;
            game.specialMusicTime = 0f;
            game.nextDanceBeat = 0;
            game.musicTime = 0f;
            game.nextCadenceBeat = 0;
            GameActorData officer = game.players.FirstOrDefault(
                actor => actor.role == "officer" &&
                         actor.online && !actor.eliminated);
            if (officer != null)
                officer.nextWhistleAt = now + WhistleCooldown;
        }

        public static void SetPlayerOffline(GameStateData game, string id)
        {
            GameActorData actor = FindPlayer(game, id);
            if (actor != null) actor.online = false;
        }

        public static float ActionDuration(string action)
        {
            switch (action)
            {
                case "jump": return 0.72f / AnimationSpeedMultiplier;
                case "armLeft":
                case "armRight": return 0.56f /
                    (AnimationSpeedMultiplier * LimbAnimationExtraSpeed);
                case "legLeft":
                case "legRight": return 0.62f /
                    (AnimationSpeedMultiplier * LimbAnimationExtraSpeed);
                case "croak": return 0.82f / AnimationSpeedMultiplier;
                case "tongue": return 0.92f / AnimationSpeedMultiplier;
                case "whistle": return 1f / AnimationSpeedMultiplier;
                case "salute": return 1f / AnimationSpeedMultiplier;
                case "death": return 0.9f / AnimationSpeedMultiplier;
                case "moveUp":
                case "moveDown":
                case "moveLeft":
                case "moveRight":
                    return 0.56f / AnimationSpeedMultiplier;
                default: return 0f;
            }
        }

        private static GameActorData NewActor(string id, string name, string role, bool npc, float now)
        {
            return new GameActorData
            {
                id = id, name = name, role = role, npc = npc,
                facing = Facings[Random.Range(0, Facings.Length)],
                nextDecisionAt = now + Random.Range(0.25f, 1.5f)
            };
        }

        private static void PlaceActor(GameActorData actor, List<GameActorData> blockers)
        {
            for (int attempt = 0; attempt < 80; attempt++)
            {
                actor.x = Mathf.Lerp(MinX, MaxX, (Random.value + Random.value) * 0.5f);
                actor.y = Mathf.Lerp(MinY, MaxY, (Random.value + Random.value) * 0.5f);
                if (CanOccupy(actor, actor.x, actor.y, blockers)) return;
            }
        }

        private static void ChooseNpcBehaviour(GameActorData npc, float now)
        {
            bool edge = IsNearEdge(npc);
            float roll = Random.value;
            float stopLimit = edge ? 0.06f : 0.22f;
            float walkLimit = edge ? 0.84f : 0.76f;
            if (roll < stopLimit)
            {
                npc.inputX = npc.inputY = 0f;
                npc.nextDecisionAt = now + (edge
                    ? Random.Range(0.2f, 0.55f)
                    : Random.Range(0.5f, 1.4f));
                return;
            }
            if (roll < walkLimit)
            {
                Vector2 direction = RandomDirection(npc, edge);
                npc.inputX = direction.x;
                npc.inputY = direction.y;
                npc.facing = FacingFrom(direction);
                npc.nextDecisionAt = now + Random.Range(0.95f, 2.8f);
                return;
            }
            BeginAction(npc, NpcActions[Random.Range(0, NpcActions.Length)], now);
            npc.nextDecisionAt = npc.actionUntil + Random.Range(0.25f, 0.9f);
        }

        private static void BeginAction(
            GameActorData actor, string action, float now, bool emitSound = true)
        {
            actor.inputX = actor.inputY = 0f;
            actor.moving = false;
            if (IsCadenceMoveAction(action))
                actor.facing = FacingFrom(CadenceMoveDirection(action));
            actor.action = action;
            actor.actionFacing = actor.facing;
            actor.actionId++;
            actor.actionStartedAt = now;
            actor.actionUntil = now + ActionDuration(action);
            actor.actionResolved = false;
            if (emitSound && action == "croak") EmitSound(actor, "frog");
            if (emitSound && action == "tongue" && actor.role == "officer")
                EmitSound(actor, "tongueCast");
            if (emitSound && action == "whistle") EmitSound(actor, "whistle");
            if (action == "jump")
            {
                Vector2 direction = FacingVector(actor.actionFacing);
                float speed = JumpDistance / ActionDuration(action);
                actor.jumpX = direction.x * speed;
                actor.jumpY = direction.y * speed;
            }
        }

        private static void TriggerCadenceActions(GameStateData game, float now)
        {
            IReadOnlyList<CadenceBeatPoint> beats = CadenceBeatTable.Points;
            while (game.nextCadenceBeat < beats.Count &&
                   beats[game.nextCadenceBeat].time <= game.musicTime)
            {
                int currentBeatIndex = game.nextCadenceBeat;
                string action = game.nextCadenceBeat < game.cadenceCommands.Count
                    ? game.cadenceCommands[game.nextCadenceBeat]
                    : CadenceCommands[game.nextCadenceBeat % CadenceCommands.Length];
                int nextBeatIndex = currentBeatIndex + 1;
                bool sequenceContinues = nextBeatIndex < beats.Count &&
                    beats[nextBeatIndex].time - beats[currentBeatIndex].time <=
                    CadenceSequenceMaxGap;
                float actionInterval = sequenceContinues
                    ? beats[nextBeatIndex].time -
                      beats[currentBeatIndex].time
                    : currentBeatIndex > 0
                        ? Mathf.Min(CadenceSequenceMaxGap,
                            beats[currentBeatIndex].time -
                            beats[currentBeatIndex - 1].time)
                        : 0f;
                foreach (GameActorData npc in game.npcs)
                {
                    if (npc.eliminated || !npc.online) continue;
                    ScheduleNpcSequenceAction(
                        npc, action, now, actionInterval);
                    npc.nextDecisionAt = sequenceContinues
                        ? now + Mathf.Max(0f,
                            beats[nextBeatIndex].time - game.musicTime) +
                            CadenceDecisionGuard
                        : npc.actionUntil + 0.15f;
                }
                game.nextCadenceBeat++;
            }
        }

        private static bool IsCadenceSequenceStart(
            IReadOnlyList<CadenceBeatPoint> beats, int index)
        {
            return index == 0 || index > 0 &&
                beats[index].time - beats[index - 1].time >
                CadenceSequenceMaxGap;
        }

        private static void FinishAction(GameActorData actor, float now)
        {
            if (string.IsNullOrEmpty(actor.action) || now < actor.actionUntil) return;
            actor.action = null;
            actor.actionFacing = null;
            actor.jumpX = actor.jumpY = 0f;
            actor.actionUntil = 0f;
            actor.actionResolved = false;
        }

        private static void ScheduleNpcSequenceAction(
            GameActorData npc, string action, float now,
            float actionInterval, bool emitSound = true)
        {
            float maxDelay = Mathf.Max(0f, actionInterval) *
                             NpcSequenceTimingErrorRatio;
            npc.pendingSequenceAction = action;
            npc.pendingSequenceActionAt =
                now + Random.Range(0f, maxDelay);
            npc.pendingSequenceActionEmitSound = emitSound;
        }

        private static void StartPendingNpcSequenceAction(
            GameActorData npc, float now)
        {
            if (string.IsNullOrEmpty(npc.pendingSequenceAction) ||
                now < npc.pendingSequenceActionAt)
                return;
            string action = npc.pendingSequenceAction;
            bool emitSound = npc.pendingSequenceActionEmitSound;
            npc.pendingSequenceAction = null;
            npc.pendingSequenceActionAt = 0f;
            npc.pendingSequenceActionEmitSound = false;
            BeginAction(npc, action, now, emitSound);
        }

        private static void ResolveOfficerTongue(GameStateData game, GameActorData officer, float now)
        {
            if (officer.role != "officer" || officer.action != "tongue" ||
                officer.actionResolved) return;
            float progress = Mathf.Clamp01((now - officer.actionStartedAt) / ActionDuration("tongue"));
            float reach = 10f + Mathf.Sin(progress * Mathf.PI) * (TongueRange - 10f);
            Vector2 direction = FacingVector(string.IsNullOrEmpty(officer.actionFacing)
                ? officer.facing : officer.actionFacing);
            GameActorData nearest = null;
            float nearestProjection = float.MaxValue;
            foreach (GameActorData target in game.npcs.Concat(game.players))
            {
                if (target.id == officer.id || target.eliminated || !target.online) continue;
                Vector2 offset = new Vector2(target.x - officer.x, target.y - officer.y);
                float projection = Vector2.Dot(offset, direction);
                if (projection < 6f || projection > reach + ColliderRadius) continue;
                Vector2 closest = new Vector2(officer.x, officer.y) +
                                  direction * Mathf.Clamp(projection, 0f, reach);
                if (Vector2.Distance(new Vector2(target.x, target.y), closest) >
                    ColliderRadius + 3f) continue;
                if (projection < nearestProjection)
                {
                    nearest = target;
                    nearestProjection = projection;
                }
            }
            if (nearest == null) return;
            officer.actionResolved = true;
            if (nearest.npc)
            {
                EmitSound(officer, "tongueWrong");
                BeginDeath(nearest, now);
                if (game.phase == PhaseTrial)
                    nearest.trialRespawnAt = now + TrialRespawnDelay;
                officer.officerNpcMistakeCount++;
                bool longStun = officer.officerNpcMistakeCount >
                                WrongNpcKillsBeforeLongStun;
                officer.stunnedUntil = now +
                    (longStun
                        ? LongWrongNpcStunDuration
                        : WrongNpcStunDuration);
                if (longStun)
                {
                    officer.officerNpcMistakeCount = 0;
                    game.announcement = "打错太多，眩晕延长！";
                    game.announcementId++;
                }
                officer.inputX = officer.inputY = 0f;
            }
            else
            {
                EmitSound(officer, "tongueCorrect");
                BeginDeath(nearest, now);
                if (game.phase == PhaseTrial)
                    nearest.trialRespawnAt = now + TrialRespawnDelay;
                game.announcement = nearest.name + " 被消灭了";
                game.announcementId++;
            }
        }

        private static void BeginDeath(GameActorData actor, float now)
        {
            actor.eliminated = true;
            actor.inputX = actor.inputY = 0f;
            actor.moving = false;
            actor.action = "death";
            actor.actionFacing = actor.facing;
            actor.actionId++;
            actor.actionStartedAt = now;
            actor.actionUntil = now + ActionDuration("death");
            actor.actionResolved = true;
            actor.jumpX = actor.jumpY = 0f;
        }

        private static void EmitSound(GameActorData actor, string soundEvent)
        {
            actor.soundEvent = soundEvent;
            actor.soundEventId++;
        }

        public static bool IsCadenceMoveAction(string action)
        {
            return action == "moveUp" || action == "moveDown" ||
                   action == "moveLeft" || action == "moveRight";
        }

        private static Vector2 CadenceMoveDirection(string action)
        {
            switch (action)
            {
                case "moveUp": return Vector2.down;
                case "moveDown": return Vector2.up;
                case "moveLeft": return Vector2.left;
                case "moveRight": return Vector2.right;
                default: return Vector2.zero;
            }
        }

        private static bool Move(GameActorData actor, float dx, float dy, List<GameActorData> actors)
        {
            if (Mathf.Abs(dx) < 0.001f && Mathf.Abs(dy) < 0.001f) return false;
            float nextX = Mathf.Clamp(actor.x + dx, MinX, MaxX);
            float nextY = Mathf.Clamp(actor.y + dy, MinY, MaxY);
            if (CanOccupy(actor, nextX, nextY, actors))
            {
                actor.x = nextX; actor.y = nextY; return true;
            }

            Vector2 movement = new Vector2(nextX - actor.x, nextY - actor.y);
            foreach (GameActorData other in actors)
            {
                if (other.id == actor.id || !other.online) continue;
                float minimum = MinimumDistance(
                    actor, other, ColliderRadius * 2f);
                float blockedX = nextX - other.x;
                float blockedY = nextY - other.y;
                if (blockedX * blockedX + blockedY * blockedY >=
                    minimum * minimum) continue;

                Vector2 normal = new Vector2(
                    actor.x - other.x, actor.y - other.y);
                if (normal.sqrMagnitude < 0.001f)
                    normal = new Vector2(-movement.y, movement.x);
                normal.Normalize();

                float inward = Vector2.Dot(movement, normal);
                Vector2 slide = inward < 0f
                    ? movement - normal * inward
                    : movement;
                if (slide.sqrMagnitude < 0.0001f) continue;

                float slideX = Mathf.Clamp(actor.x + slide.x, MinX, MaxX);
                float slideY = Mathf.Clamp(actor.y + slide.y, MinY, MaxY);
                if (!CanOccupy(actor, slideX, slideY, actors)) continue;
                actor.x = slideX;
                actor.y = slideY;
                return true;
            }

            if (CanOccupy(actor, nextX, actor.y, actors))
            {
                actor.x = nextX; return true;
            }
            if (CanOccupy(actor, actor.x, nextY, actors))
            {
                actor.y = nextY; return true;
            }
            return false;
        }

        private static void MoveNpcTowardAssembly(GameStateData game,
            GameActorData npc, int npcIndex, float deltaTime,
            List<GameActorData> actors)
        {
            GameActorData officer = game.players.FirstOrDefault(actor =>
                actor.role == "officer" && actor.online && !actor.eliminated);
            if (officer == null)
            {
                npc.inputX = npc.inputY = 0f;
                npc.moving = false;
                return;
            }

            if (npc.assemblySlot < 0)
                npc.assemblySlot = FindAvailableAssemblySlot(
                    game, officer, npc, -1);
            if (npc.assemblySlot < 0)
            {
                MoveNpcTowardAssemblyCenter(npc, npcIndex, deltaTime, actors);
                return;
            }
            Vector2 npcPosition = new Vector2(npc.x, npc.y);
            Vector2 bestPosition =
                GetAssemblyPosition(officer, npc.assemblySlot);
            Vector2 officerPosition =
                new Vector2(AssemblyCenterX, AssemblyCenterY);
            float currentOfficerDistance =
                Vector2.Distance(npcPosition, officerPosition);
            float assignedOfficerDistance =
                Vector2.Distance(bestPosition, officerPosition);
            if (currentOfficerDistance <=
                assignedOfficerDistance + AssemblyArrivalTolerance)
            {
                CompactNpcTowardOfficer(
                    officer, npc, npcIndex, deltaTime, actors);
                return;
            }
            Vector2 offset = bestPosition - npcPosition;
            float remainingDistance =
                offset.magnitude - AssemblyArrivalTolerance;
            if (remainingDistance <= 0f)
            {
                npc.inputX = npc.inputY = 0f;
                npc.moving = false;
                npc.assemblyBlockedTime = 0f;
                return;
            }

            Vector2 direction = offset.normalized;
            Vector2 previousDirection =
                new Vector2(npc.inputX, npc.inputY);
            if (previousDirection.sqrMagnitude > 0.1f)
                direction = Vector2.Lerp(
                    previousDirection.normalized, direction, 0.22f).normalized;
            float speed = Mathf.Lerp(32f, AssemblyMoveSpeed,
                Mathf.Clamp01(remainingDistance / 80f));
            float distance = Mathf.Min(
                speed * deltaTime, remainingDistance);
            npc.moving = MoveWithAssemblyAvoidance(
                npc, direction, distance, npcIndex, actors, false);
            if (npc.moving)
                npc.assemblyBlockedTime = 0f;
            else
            {
                npc.inputX = npc.inputY = 0f;
                npc.assemblyBlockedTime += deltaTime;
                if (npc.assemblyBlockedTime >= AssemblyReassignDelay)
                {
                    int replacement = FindAvailableAssemblySlot(
                        game, officer, npc, npc.assemblySlot);
                    if (replacement >= 0) npc.assemblySlot = replacement;
                    npc.assemblyBlockedTime = 0f;
                }
            }
        }

        private static void MoveNpcTowardAssemblyCenter(
            GameActorData npc, int npcIndex, float deltaTime,
            List<GameActorData> actors)
        {
            Vector2 direction = new Vector2(
                AssemblyCenterX - npc.x, AssemblyCenterY - npc.y);
            if (direction.sqrMagnitude < 0.001f)
            {
                npc.inputX = npc.inputY = 0f;
                npc.moving = false;
                return;
            }

            npc.moving = MoveWithAssemblyAvoidance(
                npc, direction.normalized,
                AssemblyMoveSpeed * deltaTime,
                npcIndex, actors, false,
                AssemblyCompactionMinimumDistance);
            if (!npc.moving)
                npc.inputX = npc.inputY = 0f;
        }

        private static void CompactNpcTowardOfficer(GameActorData officer,
            GameActorData npc, int npcIndex, float deltaTime,
            List<GameActorData> actors)
        {
            Vector2 direction = new Vector2(
                AssemblyCenterX - npc.x, AssemblyCenterY - npc.y);
            if (direction.sqrMagnitude < 0.001f)
            {
                npc.inputX = npc.inputY = 0f;
                npc.moving = false;
                return;
            }

            npc.moving = MoveWithAssemblyAvoidance(
                npc, direction.normalized,
                AssemblyCompactionSpeed * deltaTime,
                npcIndex, actors, false,
                AssemblyCompactionMinimumDistance);
            npc.assemblyBlockedTime = 0f;
            if (!npc.moving)
                npc.inputX = npc.inputY = 0f;
        }

        private static void PrepareNpcDispersal(GameStateData game)
        {
            foreach (GameActorData npc in game.npcs)
            {
                if (!npc.online || npc.eliminated) continue;
                Vector2 direction = new Vector2(
                    npc.x - AssemblyCenterX,
                    npc.y - AssemblyCenterY).normalized;
                if (direction.sqrMagnitude < 0.001f)
                    direction = Random.insideUnitCircle.normalized;
                float angle =
                    Random.Range(-80f, 80f) * Mathf.Deg2Rad;
                float sine = Mathf.Sin(angle);
                float cosine = Mathf.Cos(angle);
                direction = new Vector2(
                    direction.x * cosine - direction.y * sine,
                    direction.x * sine + direction.y * cosine);
                float speedScale = Random.Range(0.65f, 1f);
                npc.inputX = direction.x * speedScale;
                npc.inputY = direction.y * speedScale;
                npc.facing = FacingFrom(direction);
            }
        }

        private static void MoveNpcDispersal(GameActorData npc,
            int npcIndex, float deltaTime, List<GameActorData> actors)
        {
            Vector2 input = new Vector2(npc.inputX, npc.inputY);
            if (input.sqrMagnitude < 0.001f)
            {
                npc.moving = false;
                return;
            }
            float speedScale = Mathf.Clamp01(input.magnitude);
            npc.moving = MoveWithAssemblyAvoidance(
                npc, input.normalized, MoveSpeed * speedScale * deltaTime,
                npcIndex, actors, false);
            if (!npc.moving)
                npc.inputX = npc.inputY = 0f;
        }

        private static bool MoveWithAssemblyAvoidance(GameActorData npc,
            Vector2 desiredDirection, float distance, int npcIndex,
            List<GameActorData> actors, bool allowRetreat,
            float minimumSeparation = ColliderRadius * 2f)
        {
            float sidePreference = npcIndex % 2 == 0 ? 1f : -1f;
            float[] avoidanceAngles = allowRetreat
                ? DispersalAvoidanceAngles : AssemblyAvoidanceAngles;
            foreach (float sourceAngle in avoidanceAngles)
            {
                float radians = sourceAngle * sidePreference * Mathf.Deg2Rad;
                float sine = Mathf.Sin(radians);
                float cosine = Mathf.Cos(radians);
                Vector2 direction = new Vector2(
                    desiredDirection.x * cosine - desiredDirection.y * sine,
                    desiredDirection.x * sine + desiredDirection.y * cosine);
                float nextX = Mathf.Clamp(
                    npc.x + direction.x * distance, MinX, MaxX);
                float nextY = Mathf.Clamp(
                    npc.y + direction.y * distance, MinY, MaxY);
                if (Mathf.Abs(nextX - npc.x) < 0.001f &&
                    Mathf.Abs(nextY - npc.y) < 0.001f)
                    continue;
                if (!CanOccupy(
                    npc, nextX, nextY, actors, minimumSeparation)) continue;

                npc.x = nextX;
                npc.y = nextY;
                npc.inputX = direction.x;
                npc.inputY = direction.y;
                npc.facing = FacingFrom(direction);
                return true;
            }
            return false;
        }

        private static void MoveDuringJump(GameActorData actor, float deltaTime,
            List<GameActorData> actors)
        {
            actor.moving = false;
            Move(actor, actor.jumpX * deltaTime, actor.jumpY * deltaTime, actors);
        }

        private static bool CanOccupy(GameActorData actor, float x, float y,
            IEnumerable<GameActorData> actors)
        {
            return CanOccupy(
                actor, x, y, actors, ColliderRadius * 2f);
        }

        private static bool CanOccupy(GameActorData actor, float x, float y,
            IEnumerable<GameActorData> actors, float minimum)
        {
            if (!PondObstacleMap.CanOccupy(
                    new Vector2(x, y), CollisionRadius(actor)))
                return false;

            foreach (GameActorData other in actors)
            {
                if (other.id == actor.id || !other.online) continue;
                float dx = other.x - x;
                float dy = other.y - y;
                float pairMinimum = MinimumDistance(actor, other, minimum);
                if (dx * dx + dy * dy <
                    pairMinimum * pairMinimum) return false;
            }
            return true;
        }

        private static float MinimumDistance(
            GameActorData actor, GameActorData other, float fallback)
        {
            return actor.eliminated || other.eliminated
                ? CollisionRadius(actor) + CollisionRadius(other)
                : fallback;
        }

        private static float CollisionRadius(GameActorData actor)
        {
            return actor != null && actor.eliminated
                ? DeadColliderRadius : ColliderRadius;
        }

        private static bool CanControl(GameActorData actor)
        {
            return actor != null && actor.online && !actor.eliminated && !actor.stunned;
        }

        private static GameActorData FindPlayer(GameStateData game, string id)
        {
            return game == null ? null : game.players.FirstOrDefault(actor => actor.id == id);
        }

        private static bool IsNearEdge(GameActorData actor)
        {
            return actor.x - MinX < EdgeMargin || MaxX - actor.x < EdgeMargin ||
                   actor.y - MinY < EdgeMargin || MaxY - actor.y < EdgeMargin;
        }

        private static Vector2 RandomDirection(GameActorData actor, bool towardCenter)
        {
            Vector2[] directions =
            {
                Vector2.up, new Vector2(1, 1).normalized, Vector2.right,
                new Vector2(1, -1).normalized, Vector2.down,
                new Vector2(-1, -1).normalized, Vector2.left,
                new Vector2(-1, 1).normalized
            };
            if (!towardCenter) return directions[Random.Range(0, directions.Length)];
            Vector2 inward = new Vector2(480f - actor.x, 270f - actor.y).normalized;
            List<Vector2> choices = directions.Where(value => Vector2.Dot(value, inward) > 0.25f).ToList();
            return choices[Random.Range(0, choices.Count)];
        }

        private static string FacingFrom(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            return Facings[Mathf.RoundToInt(angle / 45f) % 8];
        }

        public static Vector2 FacingVector(string facing)
        {
            int index = System.Array.IndexOf(Facings, facing);
            float angle = Mathf.Deg2Rad * (index < 0 ? 0 : index * 45f);
            return new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
        }
    }
}
