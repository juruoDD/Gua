using System;
using System.Collections.Generic;
using System.Linq;
using FrogCamp.Networking;
using FrogCamp.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.Tasks
{
    /// <summary>
    /// 游戏内任务面板。玩法代码完成任务时调用 CompleteTask("任务 id")。
    /// </summary>
    public sealed class TaskPanelController : MonoBehaviour
    {
        public static TaskPanelController Instance { get; private set; }

        public event Action<TaskDefinition> TaskCompleted;
        public event Action<int> ProgressChanged;

        private const string CatalogResourcePath = "Tasks/task_pool";
        private const string ReedTaskId = "slack_in_reeds";
        private const float ReedTaskDuration = 5f;
        private const string BirdNestTaskId = "steal_nest_key";
        private const string CabinetTaskId = "open_officer_cabinet";
        private const string AttackOfficerTaskId = "attack_officer";
        private const string EatInsectsTaskId = "eat_small_insects";
        private const string IdleOfficerHomeTaskId = "idle_officer_home";
        private const string IdleBirdNestTaskId = "idle_bird_nest";
        private const string CroakFiveTimesTaskId = "croak_five_times";
        private const string LickCompanionTaskId =
            "lick_disruptor_companion";
        private const string RollCallLateTaskId = "roll_call_late";
        private const string LickTenUniqueFrogsTaskId =
            "lick_ten_unique_frogs";
        private const string SaluteFiveTimesTaskId =
            "salute_five_times";
        private const float IdleTaskDuration = 5f;
        private const float RollCallAssemblyRadius = 135f;
        private const float TaskAreaNearbyPadding = 24f;
        private static readonly Rect ReedTaskWorldArea =
            new Rect(110f, 20f, 220f, 165f);
        private static readonly Rect BirdNestTaskWorldArea =
            new Rect(660f, 335f, 185f, 145f);
        private static readonly Rect CabinetTaskWorldArea =
            new Rect(700f, 55f, 145f, 150f);
        private static readonly Rect InsectTaskWorldArea =
            new Rect(65f, 330f, 235f, 150f);
        private TaskPool taskPool;
        private Dictionary<string, TaskDefinition> taskDefinitions;
        private readonly List<TaskDefinition> sharedActiveTasks =
            new List<TaskDefinition>();
        private readonly HashSet<string> knownCompletedTaskIds =
            new HashSet<string>();
        private int sharedProgressPercent;
        private bool sharedTasksFinished;
        private int lastTaskStateVersion = -1;
        [SerializeField] private string progressPrefix = "任务进度：";
        [SerializeField] private Text progressText;
        [SerializeField] private Image progressFill;
        [SerializeField] private RectTransform taskList;
        [SerializeField] private GameObject reedTaskArea;
        [SerializeField] private GameObject birdNestTaskArea;
        [SerializeField] private GameObject cabinetTaskArea;
        private float reedIdleTime;
        private float birdNestSlackIdleTime;
        private float officerHomeIdleTime;
        private int consecutiveCroakCount;
        private int saluteCount;
        private string lastSpecialMusicPhase;
        private bool uniqueLickTaskWasActive;
        private int lastLocalActionId = -1;
        private readonly Dictionary<string, int> lastTongueActionByPlayer =
            new Dictionary<string, int>();
        private readonly Dictionary<string, HashSet<string>>
            uniqueLickedTargetsByPlayer =
                new Dictionary<string, HashSet<string>>();
        private readonly List<GameObject> taskRows = new List<GameObject>();
        private string lastObservedGamePhase;

        public int ProgressPercent => sharedProgressPercent;
        public IReadOnlyList<TaskDefinition> ActiveTasks =>
            sharedActiveTasks;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            TaskCatalog catalog = LoadCatalog();
            taskDefinitions = catalog.tasks
                .Where(task => task != null &&
                               !string.IsNullOrEmpty(task.id))
                .GroupBy(task => task.id)
                .ToDictionary(group => group.Key,
                    group => group.First());
            if (!HasBakedLayout()) BuildRuntimeFallbackLayout();
            LanRoomService service = LanRoomService.Instance;
            if (service.IsHost)
            {
                taskPool = new TaskPool(catalog);
                PublishHostTaskState();
            }
            SyncSharedTaskState(true);
        }

        private void Update()
        {
            GameStateData game =
                LanRoomService.Instance.CurrentRoom?.game;
            string phase = game == null ? null : game.phase;
            if (phase != lastObservedGamePhase)
            {
                if (phase == GameSimulation.PhaseFormalIntro)
                    ResetForFormalGame();
                lastObservedGamePhase = phase;
            }
            SyncSharedTaskState(false);
            if (!GameSimulation.IsInteractivePhase(game)) return;
            UpdateReedTask();
            UpdateBirdNestTask();
            UpdateCabinetTask();
            UpdateRollCallLateTask();
            UpdateUniqueLickTask();
            UpdateActionTasks();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 完成当前面板中的任务。成功时进度增加 10%，并自动补抽下一个可用任务。
        /// </summary>
        public bool CompleteTask(string taskId)
        {
            GameStateData game =
                LanRoomService.Instance.CurrentRoom?.game;
            if (string.IsNullOrEmpty(taskId) ||
                !GameSimulation.IsInteractivePhase(game) ||
                !sharedActiveTasks.Any(task => task.id == taskId))
                return false;
            LanRoomService.Instance.RequestCompleteTask(taskId);
            return true;
        }

        private void ResetForFormalGame()
        {
            knownCompletedTaskIds.Clear();
            lastTaskStateVersion = -1;
            sharedActiveTasks.Clear();
            sharedProgressPercent = 0;
            sharedTasksFinished = false;
            if (LanRoomService.Instance.IsHost)
            {
                taskPool = new TaskPool(LoadCatalog());
                PublishHostTaskState();
            }
            else
            {
                RefreshPanel();
            }
        }

        public bool CompleteTaskAsHost(string taskId)
        {
            LanRoomService service = LanRoomService.Instance;
            if (!service.IsHost || taskPool == null ||
                !taskPool.Complete(taskId))
                return false;
            PublishHostTaskState();
            return true;
        }

        [ContextMenu("测试：完成面板第一个任务")]
        private void CompleteFirstTaskForTesting()
        {
            if (sharedActiveTasks.Count > 0)
                CompleteTask(sharedActiveTasks[0].id);
        }

        private static TaskCatalog LoadCatalog()
        {
            TextAsset asset = Resources.Load<TextAsset>(CatalogResourcePath);
            if (asset == null)
            {
                Debug.LogError("找不到 Assets/Resources/Tasks/task_pool.json。");
                return new TaskCatalog();
            }

            TaskCatalog catalog = JsonUtility.FromJson<TaskCatalog>(asset.text);
            return catalog ?? new TaskCatalog();
        }

        private void PublishHostTaskState()
        {
            GameStateData game =
                LanRoomService.Instance.CurrentRoom?.game;
            if (game == null || taskPool == null) return;
            if (game.tasks == null)
                game.tasks = new TaskStateData();

            game.tasks.activeTaskIds =
                taskPool.ActiveTasks.Select(task => task.id).ToList();
            game.tasks.completedTaskIds =
                taskPool.CompletedTaskIds.ToList();
            game.tasks.progressPercent = taskPool.ProgressPercent;
            game.tasks.finished = taskPool.IsFinished;
            game.tasks.version++;
            SyncSharedTaskState(true);
        }

        private void SyncSharedTaskState(bool force)
        {
            TaskStateData state =
                LanRoomService.Instance.CurrentRoom?.game?.tasks;
            if (state == null)
            {
                if (force)
                {
                    sharedActiveTasks.Clear();
                    sharedProgressPercent = 0;
                    sharedTasksFinished = false;
                    RefreshPanel();
                }
                return;
            }
            if (!force && state.version == lastTaskStateVersion)
                return;

            bool initialSync = lastTaskStateVersion < 0;
            IEnumerable<string> completedIds =
                state.completedTaskIds ?? new List<string>();
            if (!initialSync)
            {
                foreach (string id in completedIds)
                {
                    if (knownCompletedTaskIds.Contains(id)) continue;
                    int completedIndex =
                        sharedActiveTasks.FindIndex(task => task.id == id);
                    Transform completedRow = taskList != null &&
                                             completedIndex >= 0 &&
                                             completedIndex < taskList.childCount
                        ? taskList.GetChild(completedIndex)
                        : null;
                    TaskCompletionFeedback.Play(completedRow);
                }
            }
            int previousProgress = sharedProgressPercent;
            lastTaskStateVersion = state.version;
            sharedActiveTasks.Clear();
            if (state.activeTaskIds != null && taskDefinitions != null)
            {
                foreach (string id in state.activeTaskIds)
                {
                    TaskDefinition task;
                    if (taskDefinitions.TryGetValue(id, out task))
                        sharedActiveTasks.Add(task);
                }
            }
            sharedProgressPercent = state.progressPercent;
            sharedTasksFinished = state.finished;
            RefreshPanel();

            if (!initialSync && taskDefinitions != null)
            {
                foreach (string id in completedIds)
                {
                    TaskDefinition task;
                    if (!knownCompletedTaskIds.Contains(id) &&
                        taskDefinitions.TryGetValue(id, out task))
                        TaskCompleted?.Invoke(task);
                }
                if (previousProgress != sharedProgressPercent)
                    ProgressChanged?.Invoke(sharedProgressPercent);
            }
            knownCompletedTaskIds.Clear();
            foreach (string id in completedIds)
                knownCompletedTaskIds.Add(id);
        }

        public void BuildLayoutForEditor(RectTransform map)
        {
#if UNITY_EDITOR
            Transform oldPanel = map.Find("TaskPanel");
            Transform oldArea = map.Find("ReedTaskArea");
            Transform oldBirdArea = map.Find("BirdNestTaskArea");
            Transform oldCabinetArea = map.Find("CabinetTaskArea");
            if (oldPanel != null) DestroyImmediate(oldPanel.gameObject);
            if (oldArea != null) DestroyImmediate(oldArea.gameObject);
            if (oldBirdArea != null) DestroyImmediate(oldBirdArea.gameObject);
            if (oldCabinetArea != null) DestroyImmediate(oldCabinetArea.gameObject);
#endif
            BuildPanel(map);
            TaskDefinition[] previewTasks =
            {
                new TaskDefinition
                {
                    id = BirdNestTaskId,
                    title = "鸟窝偷钥匙",
                    description = "鸟窝旁吐舌",
                    guaranteed = true
                },
                new TaskDefinition
                {
                    id = CabinetTaskId,
                    title = "打开私房柜",
                    description = "私房柜旁吐舌",
                    guaranteed = true
                },
                new TaskDefinition
                {
                    id = AttackOfficerTaskId,
                    title = "袭击军官蛙",
                    description = "对军官蛙吐舌",
                    guaranteed = true
                },
                new TaskDefinition
                {
                    id = ReedTaskId,
                    title = "芦苇丛偷懒",
                    description = "芦苇丛静止 5 秒",
                    guaranteed = true
                },
                new TaskDefinition
                {
                    id = EatInsectsTaskId,
                    title = "偷吃小飞虫",
                    description = "小飞虫旁吐舌"
                }
            };
            const float rowHeight = 0.188f;
            for (int index = 0; index < previewTasks.Length; index++)
                AddTaskRow(previewTasks[index], index, rowHeight);
        }

        private bool HasBakedLayout()
        {
            return progressText != null && progressFill != null &&
                   taskList != null && reedTaskArea != null &&
                   birdNestTaskArea != null &&
                   cabinetTaskArea != null;
        }

        private void BuildRuntimeFallbackLayout()
        {
            GameObject map = GameObject.Find("CampMap");
            Transform parent = map != null ? map.transform :
                FindObjectOfType<Canvas>()?.transform;
            if (parent == null)
            {
                Canvas canvas = CampUiFactory.CreateCanvas(transform);
                parent = canvas.transform;
            }
            BuildPanel(parent);
        }

        private void BuildPanel(Transform parent)
        {
            RectTransform panel = CampUiFactory.Panel(parent, "TaskPanel",
                new Vector2(0.018f, 0.49f), new Vector2(0.255f, 0.875f),
                Vector2.zero, Vector2.zero, new Color(0.035f, 0.075f, 0.09f, 0.88f));

            Image panelImage = panel.GetComponent<Image>();
            panelImage.raycastTarget = false;

            CampUiFactory.Text(panel, "Eyebrow", "CAMP DUTIES  /  营地任务", 14,
                new Color(0.57f, 0.78f, 0.69f, 1f),
                new Vector2(0.07f, 0.88f), new Vector2(0.93f, 0.97f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);

            progressText = CampUiFactory.Text(panel, "ProgressText", "任务进度：0%", 25,
                CampUiFactory.White, new Vector2(0.07f, 0.76f),
                new Vector2(0.93f, 0.88f), Vector2.zero, Vector2.zero,
                TextAnchor.MiddleLeft, true);

            RectTransform progressTrack = CampUiFactory.Panel(panel, "ProgressTrack",
                new Vector2(0.07f, 0.70f), new Vector2(0.93f, 0.735f),
                Vector2.zero, Vector2.zero, new Color(0.08f, 0.13f, 0.14f, 0.95f));
            progressTrack.GetComponent<Image>().raycastTarget = false;

            RectTransform fill = CampUiFactory.Panel(progressTrack, "Fill",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.83f, 0.93f, 0.76f, 1f));
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            progressFill = fill.GetComponent<Image>();
            progressFill.raycastTarget = false;

            CampUiFactory.Panel(panel, "Divider", new Vector2(0.07f, 0.655f),
                new Vector2(0.93f, 0.66f), Vector2.zero, Vector2.zero,
                new Color(0.49f, 0.65f, 0.60f, 0.45f)).GetComponent<Image>().raycastTarget = false;

            taskList = CampUiFactory.Panel(panel, "TaskList",
                new Vector2(0.055f, 0.06f), new Vector2(0.945f, 0.63f),
                Vector2.zero, Vector2.zero, Color.clear);
            taskList.GetComponent<Image>().raycastTarget = false;
            BuildReedTaskArea(parent);
            BuildBirdNestTaskArea(parent);
            BuildCabinetTaskArea(parent);
        }

        private void RefreshPanel()
        {
            progressText.text = progressPrefix +
                                sharedProgressPercent + "%";
            RectTransform fillRect = progressFill.rectTransform;
            fillRect.anchorMax =
                new Vector2(sharedProgressPercent / 100f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            if (HasBakedTaskRows())
            {
                RefreshBakedTaskRows();
                return;
            }

            ClearTaskRows();

            if (sharedTasksFinished)
            {
                AddEmptyState("捣蛋呱获胜", "全部任务已完成");
                return;
            }

            if (sharedActiveTasks.Count == 0)
            {
                AddEmptyState("暂无可执行任务", "完成前置任务后将自动解锁");
                return;
            }

            int count = sharedActiveTasks.Count;
            float rowHeight = Mathf.Min(0.235f, 0.94f / Mathf.Max(1, count));
            for (int index = 0; index < count; index++)
                AddTaskRow(sharedActiveTasks[index], index, rowHeight);
        }

        private bool HasBakedTaskRows()
        {
            if (taskList == null || taskList.childCount == 0) return false;
            Transform first = taskList.GetChild(0);
            return first.Find("Title") != null &&
                   first.Find("Description") != null;
        }

        private void RefreshBakedTaskRows()
        {
            if (sharedTasksFinished)
            {
                ApplyBakedEmptyState(
                    "捣蛋呱获胜", "全部任务已完成");
                return;
            }

            if (sharedActiveTasks.Count == 0)
            {
                ApplyBakedEmptyState(
                    "暂无可执行任务", "完成前置任务后将自动解锁");
                return;
            }

            for (int index = 0; index < taskList.childCount; index++)
            {
                Transform row = taskList.GetChild(index);
                bool active = index < sharedActiveTasks.Count;
                row.gameObject.SetActive(active);
                if (!active) continue;

                TaskDefinition task = sharedActiveTasks[index];
                SetChildText(row, "Title", task.title);
                SetChildText(row, "Description",
                    string.IsNullOrEmpty(task.description)
                        ? "等待任务条件"
                        : task.description);
                Transform guaranteed = row.Find("Guaranteed");
                if (guaranteed != null)
                {
                    guaranteed.gameObject.SetActive(task.guaranteed);
                    Text label = guaranteed.GetComponent<Text>();
                    if (label != null) label.text = "必";
                }
            }
        }

        private void ApplyBakedEmptyState(
            string title, string description)
        {
            for (int index = 0; index < taskList.childCount; index++)
            {
                Transform row = taskList.GetChild(index);
                row.gameObject.SetActive(index == 0);
                if (index != 0) continue;
                SetChildText(row, "Title", title);
                SetChildText(row, "Description", description);
                Transform guaranteed = row.Find("Guaranteed");
                if (guaranteed != null)
                    guaranteed.gameObject.SetActive(false);
            }
        }

        private static void SetChildText(
            Transform parent, string childName, string value)
        {
            Transform child = parent.Find(childName);
            Text text = child != null ? child.GetComponent<Text>() : null;
            if (text != null) text.text = value;
        }

        private void AddTaskRow(TaskDefinition task, int index, float rowHeight)
        {
            float top = 1f - index * rowHeight;
            float bottom = top - rowHeight + 0.018f;
            RectTransform row = CampUiFactory.Panel(taskList,
                "TaskSlot" + (index + 1),
                new Vector2(0f, bottom), new Vector2(1f, top),
                Vector2.zero, Vector2.zero,
                index == 0
                    ? new Color(0.30f, 0.43f, 0.40f, 0.34f)
                    : new Color(0f, 0f, 0f, 0f));
            row.GetComponent<Image>().raycastTarget = false;
            taskRows.Add(row.gameObject);

            CampUiFactory.Text(row, "Icon", "▤", 30,
                index == 0 ? CampUiFactory.White : new Color(0.73f, 0.82f, 0.80f, 1f),
                new Vector2(0.025f, 0.08f), new Vector2(0.20f, 0.92f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
            CampUiFactory.Text(row, "Title", task.title, 23, CampUiFactory.White,
                new Vector2(0.20f, 0.43f), new Vector2(0.86f, 0.94f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            CampUiFactory.Text(row, "Description",
                string.IsNullOrEmpty(task.description) ? "等待任务条件" : task.description,
                14, new Color(0.62f, 0.75f, 0.71f, 1f),
                new Vector2(0.20f, 0.06f), new Vector2(0.91f, 0.46f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);

            if (task.guaranteed)
            {
                CampUiFactory.Text(row, "Guaranteed", "必", 13,
                    new Color(0.93f, 0.86f, 0.45f, 1f),
                    new Vector2(0.88f, 0.04f), new Vector2(0.97f, 0.38f),
                    Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
            }
        }

        private void ClearTaskRows()
        {
            if (taskList == null) return;
            taskRows.Clear();
            for (int index = taskList.childCount - 1; index >= 0; index--)
            {
                GameObject child = taskList.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child);
#if UNITY_EDITOR
                else DestroyImmediate(child);
#endif
            }
        }

        private void AddEmptyState(string title, string subtitle)
        {
            RectTransform row = CampUiFactory.Panel(taskList, "EmptyState",
                new Vector2(0f, 0.2f), new Vector2(1f, 0.82f),
                Vector2.zero, Vector2.zero, Color.clear);
            taskRows.Add(row.gameObject);
            CampUiFactory.Text(row, "Title", title, 23, CampUiFactory.White,
                new Vector2(0f, 0.48f), new Vector2(1f, 0.85f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
            CampUiFactory.Text(row, "Subtitle", subtitle, 15,
                new Color(0.57f, 0.72f, 0.66f, 1f),
                new Vector2(0f, 0.17f), new Vector2(1f, 0.50f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        }

        private void BuildReedTaskArea(Transform map)
        {
            BuildTaskArea(map, "ReedTaskArea", ReedTaskWorldArea,
                out reedTaskArea);
        }

        private void BuildBirdNestTaskArea(Transform map)
        {
            BuildTaskArea(map, "BirdNestTaskArea", BirdNestTaskWorldArea,
                out birdNestTaskArea);
        }

        private void BuildCabinetTaskArea(Transform map)
        {
            BuildTaskArea(map, "CabinetTaskArea", CabinetTaskWorldArea,
                out cabinetTaskArea);
        }

        private static void BuildTaskArea(Transform map, string objectName,
            Rect worldArea, out GameObject areaObject)
        {
            float minX = worldArea.xMin / GameSimulation.WorldWidth;
            float maxX = worldArea.xMax / GameSimulation.WorldWidth;
            float minY = 1f - worldArea.yMax / GameSimulation.WorldHeight;
            float maxY = 1f - worldArea.yMin / GameSimulation.WorldHeight;
            GameObject areaObjectInstance =
                new GameObject(objectName, typeof(RectTransform));
            RectTransform area =
                areaObjectInstance.GetComponent<RectTransform>();
            area.SetParent(map, false);
            CampUiFactory.SetRect(area,
                new Vector2(minX, minY), new Vector2(maxX, maxY),
                Vector2.zero, Vector2.zero);
            areaObject = area.gameObject;
        }

        private void UpdateReedTask()
        {
            if (reedTaskArea == null) return;
            bool taskActive =
                sharedActiveTasks.Any(task => task.id == ReedTaskId);
            reedTaskArea.SetActive(taskActive);
            if (!taskActive)
            {
                reedIdleTime = 0f;
                return;
            }

            LanRoomService service = LanRoomService.Instance;
            GameActorData actor = service.CurrentRoom?.game?.players
                .FirstOrDefault(player => player.id == service.LocalPlayerId);
            bool isSoldier = actor != null && actor.role != "officer";
            bool inside = isSoldier &&
                          IsInsideTaskArea(actor, reedTaskArea, ReedTaskWorldArea);
            bool idling = inside && !actor.moving &&
                           Mathf.Abs(actor.inputX) < 0.01f &&
                           Mathf.Abs(actor.inputY) < 0.01f &&
                           string.IsNullOrEmpty(actor.action);

            if (idling) reedIdleTime += Time.unscaledDeltaTime;
            else reedIdleTime = 0f;

            if (reedIdleTime >= ReedTaskDuration)
            {
                reedIdleTime = 0f;
                CompleteTask(ReedTaskId);
            }
        }

        private void UpdateBirdNestTask()
        {
            if (birdNestTaskArea == null) return;
            bool keyTaskActive =
                sharedActiveTasks.Any(task => task.id == BirdNestTaskId);
            bool slackTaskActive =
                sharedActiveTasks.Any(
                    task => task.id == IdleBirdNestTaskId);
            birdNestTaskArea.SetActive(keyTaskActive || slackTaskActive);
            if (!keyTaskActive && !slackTaskActive)
            {
                birdNestSlackIdleTime = 0f;
                return;
            }

            GameActorData actor = GetLocalActor();
            bool isSoldier = actor != null && actor.role != "officer";
            bool inside = isSoldier &&
                          IsInsideTaskArea(actor, birdNestTaskArea,
                              BirdNestTaskWorldArea);
            bool idling = IsIdling(actor, inside);
            birdNestSlackIdleTime = slackTaskActive && idling
                ? birdNestSlackIdleTime + Time.unscaledDeltaTime : 0f;
            if (slackTaskActive &&
                birdNestSlackIdleTime >= IdleTaskDuration)
            {
                birdNestSlackIdleTime = 0f;
                CompleteTask(IdleBirdNestTaskId);
            }
        }

        private void UpdateCabinetTask()
        {
            if (cabinetTaskArea == null) return;
            bool cabinetTaskActive =
                sharedActiveTasks.Any(task => task.id == CabinetTaskId);
            bool slackTaskActive =
                sharedActiveTasks.Any(
                    task => task.id == IdleOfficerHomeTaskId);
            cabinetTaskArea.SetActive(cabinetTaskActive || slackTaskActive);
            if (!cabinetTaskActive && !slackTaskActive)
            {
                officerHomeIdleTime = 0f;
                return;
            }

            GameActorData actor = GetLocalActor();
            bool isSoldier = actor != null && actor.role != "officer";
            bool inside = isSoldier &&
                          IsInsideTaskArea(actor, cabinetTaskArea,
                              CabinetTaskWorldArea);
            bool idling = IsIdling(actor, inside);
            officerHomeIdleTime = slackTaskActive && idling
                ? officerHomeIdleTime + Time.unscaledDeltaTime : 0f;

            if (slackTaskActive &&
                officerHomeIdleTime >= IdleTaskDuration)
            {
                officerHomeIdleTime = 0f;
                CompleteTask(IdleOfficerHomeTaskId);
            }
        }

        private void UpdateRollCallLateTask()
        {
            RoomStateData room = LanRoomService.Instance.CurrentRoom;
            GameStateData game = room?.game;
            string phase = game?.specialMusicPhase;
            bool rollCallJustEnded =
                lastSpecialMusicPhase == GameSimulation.DancePhaseBell &&
                phase == GameSimulation.DancePhaseMusic;
            lastSpecialMusicPhase = phase;
            if (!rollCallJustEnded ||
                !sharedActiveTasks.Any(
                    task => task.id == RollCallLateTaskId))
                return;

            GameActorData actor = GetLocalActor();
            if (actor == null || actor.role == "officer" ||
                actor.eliminated || !actor.online)
                return;
            Vector2 offset = new Vector2(
                actor.x - GameSimulation.AssemblyCenterX,
                actor.y - GameSimulation.AssemblyCenterY);
            if (offset.magnitude > RollCallAssemblyRadius)
                CompleteTask(RollCallLateTaskId);
        }

        private void UpdateUniqueLickTask()
        {
            bool taskActive =
                sharedActiveTasks.Any(
                    task => task.id == LickTenUniqueFrogsTaskId);
            RoomStateData room = LanRoomService.Instance.CurrentRoom;
            GameStateData game = room?.game;
            if (!taskActive || game == null)
            {
                uniqueLickTaskWasActive = false;
                lastTongueActionByPlayer.Clear();
                uniqueLickedTargetsByPlayer.Clear();
                return;
            }

            List<GameActorData> soldiers = game.players.Where(
                player => player.role == "disguiser" &&
                          player.online && !player.eliminated).ToList();
            if (!uniqueLickTaskWasActive)
            {
                uniqueLickTaskWasActive = true;
                foreach (GameActorData soldier in soldiers)
                    lastTongueActionByPlayer[soldier.id] = soldier.actionId;
                return;
            }

            List<GameActorData> allTargets =
                game.players.Concat(game.npcs)
                    .Where(target => target.online && !target.eliminated)
                    .ToList();
            foreach (GameActorData soldier in soldiers)
            {
                int lastActionId;
                if (lastTongueActionByPlayer.TryGetValue(
                        soldier.id, out lastActionId) &&
                    lastActionId == soldier.actionId)
                    continue;
                lastTongueActionByPlayer[soldier.id] = soldier.actionId;
                if (soldier.action != "tongue") continue;

                GameActorData target = allTargets
                    .Where(candidate => candidate.id != soldier.id &&
                                        IsTongueAimedAt(soldier, candidate))
                    .OrderBy(candidate =>
                        (candidate.x - soldier.x) *
                        (candidate.x - soldier.x) +
                        (candidate.y - soldier.y) *
                        (candidate.y - soldier.y))
                    .FirstOrDefault();
                if (target == null) continue;

                HashSet<string> lickedTargets;
                if (!uniqueLickedTargetsByPlayer.TryGetValue(
                        soldier.id, out lickedTargets))
                {
                    lickedTargets = new HashSet<string>();
                    uniqueLickedTargetsByPlayer[soldier.id] = lickedTargets;
                }
                lickedTargets.Add(target.id);
                if (lickedTargets.Count < 10) continue;

                uniqueLickTaskWasActive = false;
                lastTongueActionByPlayer.Clear();
                uniqueLickedTargetsByPlayer.Clear();
                CompleteTask(LickTenUniqueFrogsTaskId);
                return;
            }
        }

        private void UpdateActionTasks()
        {
            GameActorData actor = GetLocalActor();
            if (actor == null) return;
            if (actor.actionId == lastLocalActionId) return;
            lastLocalActionId = actor.actionId;
            if (actor.role == "officer") return;

            bool croakTaskActive = sharedActiveTasks.Any(
                task => task.id == CroakFiveTimesTaskId);
            if (!croakTaskActive)
                consecutiveCroakCount = 0;
            else if (actor.action == "croak")
            {
                consecutiveCroakCount++;
                if (consecutiveCroakCount >= 5)
                {
                    consecutiveCroakCount = 0;
                    CompleteTask(CroakFiveTimesTaskId);
                }
            }
            else if (!string.IsNullOrEmpty(actor.action))
            {
                consecutiveCroakCount = 0;
            }

            bool saluteTaskActive = sharedActiveTasks.Any(
                task => task.id == SaluteFiveTimesTaskId);
            if (!saluteTaskActive)
                saluteCount = 0;
            else if (actor.action == "salute")
            {
                saluteCount++;
                if (saluteCount >= 5)
                {
                    saluteCount = 0;
                    CompleteTask(SaluteFiveTimesTaskId);
                }
            }
            else if (!string.IsNullOrEmpty(actor.action))
            {
                saluteCount = 0;
            }

            if (actor.action != "tongue") return;

            if (sharedActiveTasks.Any(
                    task => task.id == BirdNestTaskId) &&
                IsInsideTaskArea(actor, birdNestTaskArea,
                    BirdNestTaskWorldArea) &&
                CompleteTask(BirdNestTaskId))
                return;

            if (sharedActiveTasks.Any(
                    task => task.id == CabinetTaskId) &&
                IsInsideTaskArea(actor, cabinetTaskArea,
                    CabinetTaskWorldArea) &&
                CompleteTask(CabinetTaskId))
                return;

            if (sharedActiveTasks.Any(
                    task => task.id == EatInsectsTaskId) &&
                IsInsideTaskArea(actor, null, InsectTaskWorldArea) &&
                CompleteTask(EatInsectsTaskId))
                return;

            RoomStateData room = LanRoomService.Instance.CurrentRoom;
            if (room?.game == null) return;
            IEnumerable<GameActorData> targets =
                room.game.players.Concat(room.game.npcs);

            if (sharedActiveTasks.Any(
                    task => task.id == AttackOfficerTaskId))
            {
                IEnumerable<GameActorData> officers = targets.Where(
                    target => target.role == "officer" &&
                              target.online && !target.eliminated);
                if (officers.Any(
                        target => IsTongueAimedAt(actor, target)) &&
                    CompleteTask(AttackOfficerTaskId))
                    return;
            }

            if (sharedActiveTasks.Any(
                    task => task.id == LickCompanionTaskId))
            {
                IEnumerable<GameActorData> companions = targets.Where(
                    target => target.id != actor.id &&
                              target.role == "disguiser" &&
                              target.online && !target.eliminated);
                if (companions.Any(target =>
                        IsTongueAimedAt(actor, target) &&
                        IsBehindTarget(actor, target)))
                    CompleteTask(LickCompanionTaskId);
            }
        }

        private static bool IsTongueAimedAt(
            GameActorData attacker, GameActorData target)
        {
            string facing = string.IsNullOrEmpty(attacker.actionFacing)
                ? attacker.facing : attacker.actionFacing;
            Vector2 direction = GameSimulation.FacingVector(facing);
            Vector2 offset = new Vector2(
                target.x - attacker.x, target.y - attacker.y);
            float projection = Vector2.Dot(offset, direction);
            if (projection < 5f ||
                projection > GameSimulation.TongueRange + GameSimulation.ColliderRadius)
                return false;
            Vector2 sideways = offset - direction * projection;
            return sideways.magnitude <= GameSimulation.ColliderRadius + 5f;
        }

        private static bool IsBehindTarget(
            GameActorData attacker, GameActorData target)
        {
            Vector2 fromTargetToAttacker = new Vector2(
                attacker.x - target.x, attacker.y - target.y).normalized;
            Vector2 targetForward =
                GameSimulation.FacingVector(target.facing);
            return Vector2.Dot(fromTargetToAttacker, targetForward) < -0.45f;
        }

        private static bool IsIdling(
            GameActorData actor, bool inside)
        {
            return actor != null && inside && !actor.moving &&
                   Mathf.Abs(actor.inputX) < 0.01f &&
                   Mathf.Abs(actor.inputY) < 0.01f &&
                   string.IsNullOrEmpty(actor.action);
        }

        private static GameActorData GetLocalActor()
        {
            LanRoomService service = LanRoomService.Instance;
            return service.CurrentRoom?.game?.players
                .FirstOrDefault(player => player.id == service.LocalPlayerId);
        }

        private static bool IsInsideTaskArea(
            GameActorData actor, GameObject areaObject, Rect fallbackWorldArea)
        {
            if (actor == null) return false;
            RectTransform area = areaObject == null
                ? null : areaObject.GetComponent<RectTransform>();
            RectTransform map = area == null ? null : area.parent as RectTransform;
            if (area == null || map == null ||
                map.rect.width < 1f || map.rect.height < 1f)
                return fallbackWorldArea.Contains(new Vector2(actor.x, actor.y));

            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                map, area);
            Vector2 actorInMap = new Vector2(
                (actor.x / GameSimulation.WorldWidth - map.pivot.x) * map.rect.width,
                (1f - actor.y / GameSimulation.WorldHeight - map.pivot.y) *
                map.rect.height);
            float horizontalPadding = TaskAreaNearbyPadding /
                                      GameSimulation.WorldWidth * map.rect.width;
            float verticalPadding = TaskAreaNearbyPadding /
                                    GameSimulation.WorldHeight * map.rect.height;
            return actorInMap.x >= bounds.min.x - horizontalPadding &&
                   actorInMap.x <= bounds.max.x + horizontalPadding &&
                   actorInMap.y >= bounds.min.y - verticalPadding &&
                   actorInMap.y <= bounds.max.y + verticalPadding;
        }
    }

    internal static class TaskPanelBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "游戏界面" ||
                UnityEngine.Object.FindObjectOfType<TaskPanelController>() != null)
                return;
            new GameObject("TaskSystem").AddComponent<TaskPanelController>();
        }
    }
}
