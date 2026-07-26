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
        public const float ColliderRadius = 14f;
        public const float JumpDistance = 48f;
        public const float TongueRange = 44f;
        public const int NpcCount = 20;

        private const float EdgeMargin = 90f;
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

        public static GameStateData Create(RoomStateData room, float now)
        {
            GameStateData game = new GameStateData();
            foreach (RoomPlayerData player in room.players)
            {
                GameActorData actor = NewActor(player.id, player.name, player.role, false, now);
                PlaceActor(actor, game.players);
                game.players.Add(actor);
            }
            for (int index = 0; index < NpcCount; index++)
            {
                GameActorData npc = NewActor("npc-" + (index + 1), "", "disguiser", true, now);
                PlaceActor(npc, game.players.Concat(game.npcs).ToList());
                game.npcs.Add(npc);
            }
            return game;
        }

        public static void SetInput(GameStateData game, string id, float x, float y)
        {
            GameActorData actor = FindPlayer(game, id);
            if (!CanControl(actor)) return;
            Vector2 input = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            actor.inputX = input.x;
            actor.inputY = input.y;
            if (input.sqrMagnitude > 0.01f) actor.facing = FacingFrom(input);
        }

        public static void StartAction(GameStateData game, string id, string action, float now)
        {
            GameActorData actor = FindPlayer(game, id);
            if (!CanControl(actor) || !string.IsNullOrEmpty(actor.action)) return;
            bool allowed = actor.role == "officer"
                ? action == "jump" || action == "croak" || action == "tongue" || action == "whistle"
                : action == "jump" || action == "armLeft" || action == "armRight" ||
                  action == "legLeft" || action == "legRight" ||
                  action == "croak" || action == "tongue" || action == "salute";
            if (allowed) BeginAction(actor, action, now);
        }

        public static void Tick(GameStateData game, float deltaTime, float now)
        {
            if (game == null) return;
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
                if (actor.action == "jump")
                    Move(actor, actor.jumpX * deltaTime, actor.jumpY * deltaTime, actors);
                else if (string.IsNullOrEmpty(actor.action))
                {
                    float speed = actor.role == "officer" ? OfficerMoveSpeed : MoveSpeed;
                    actor.moving = Move(actor, actor.inputX * speed * deltaTime,
                        actor.inputY * speed * deltaTime, actors);
                }
                else actor.moving = false;
                ResolveOfficerTongue(game, actor, now);
            }
            foreach (GameActorData npc in game.npcs.ToArray())
            {
                FinishAction(npc, now);
                if (npc.action == "jump")
                {
                    npc.moving = Move(npc, npc.jumpX * deltaTime, npc.jumpY * deltaTime, actors);
                    continue;
                }
                if (!string.IsNullOrEmpty(npc.action))
                {
                    npc.moving = false;
                    continue;
                }
                if (now >= npc.nextDecisionAt) ChooseNpcBehaviour(npc, now);
                npc.moving = Move(npc, npc.inputX * MoveSpeed * deltaTime,
                    npc.inputY * MoveSpeed * deltaTime, actors);
                if (!npc.moving && (npc.inputX != 0f || npc.inputY != 0f))
                    npc.nextDecisionAt = now;
            }
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
                case "jump": return 0.72f;
                case "armLeft":
                case "armRight": return 0.56f;
                case "legLeft":
                case "legRight": return 0.62f;
                case "croak": return 0.82f;
                case "tongue": return 0.92f;
                case "whistle": return 1f;
                case "salute": return 1f;
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
            float stopLimit = edge ? 0.08f : 0.34f;
            float walkLimit = edge ? 0.84f : 0.76f;
            if (roll < stopLimit)
            {
                npc.inputX = npc.inputY = 0f;
                npc.nextDecisionAt = now + (edge ? Random.Range(0.25f, 0.7f) : Random.Range(0.7f, 2.2f));
                return;
            }
            if (roll < walkLimit)
            {
                Vector2 direction = RandomDirection(npc, edge);
                npc.inputX = direction.x;
                npc.inputY = direction.y;
                npc.facing = FacingFrom(direction);
                npc.nextDecisionAt = now + Random.Range(0.85f, 2.4f);
                return;
            }
            BeginAction(npc, NpcActions[Random.Range(0, NpcActions.Length)], now);
            npc.nextDecisionAt = npc.actionUntil + Random.Range(0.25f, 0.9f);
        }

        private static void BeginAction(GameActorData actor, string action, float now)
        {
            actor.inputX = actor.inputY = 0f;
            actor.moving = false;
            actor.action = action;
            actor.actionId++;
            actor.actionStartedAt = now;
            actor.actionUntil = now + ActionDuration(action);
            actor.actionResolved = false;
            if (action == "jump")
            {
                Vector2 direction = FacingVector(actor.facing);
                float speed = JumpDistance / ActionDuration(action);
                actor.jumpX = direction.x * speed;
                actor.jumpY = direction.y * speed;
            }
        }

        private static void FinishAction(GameActorData actor, float now)
        {
            if (string.IsNullOrEmpty(actor.action) || now < actor.actionUntil) return;
            actor.action = null;
            actor.jumpX = actor.jumpY = 0f;
            actor.actionUntil = 0f;
            actor.actionResolved = false;
        }

        private static void ResolveOfficerTongue(GameStateData game, GameActorData officer, float now)
        {
            if (officer.role != "officer" || officer.action != "tongue" ||
                officer.actionResolved) return;
            float progress = Mathf.Clamp01((now - officer.actionStartedAt) / ActionDuration("tongue"));
            float reach = 10f + Mathf.Sin(progress * Mathf.PI) * (TongueRange - 10f);
            Vector2 direction = FacingVector(officer.facing);
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
                game.npcs.Remove(nearest);
                officer.stunnedUntil = now + 5f;
                officer.inputX = officer.inputY = 0f;
            }
            else
            {
                nearest.eliminated = true;
                nearest.inputX = nearest.inputY = 0f;
                nearest.action = null;
                game.announcement = nearest.name + " 被消灭了";
                game.announcementId++;
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

        private static bool CanOccupy(GameActorData actor, float x, float y,
            IEnumerable<GameActorData> actors)
        {
            float minimum = ColliderRadius * 2f;
            foreach (GameActorData other in actors)
            {
                if (other.id == actor.id || other.eliminated || !other.online) continue;
                float dx = other.x - x;
                float dy = other.y - y;
                if (dx * dx + dy * dy < minimum * minimum) return false;
            }
            return true;
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
