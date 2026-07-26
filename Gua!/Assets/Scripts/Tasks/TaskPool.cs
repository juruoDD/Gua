using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FrogCamp.Tasks
{
    /// <summary>
    /// 负责本局任务抽取、前置条件和进度，不依赖具体玩法或 UI。
    /// </summary>
    public sealed class TaskPool
    {
        public const int ProgressPerTask = 10;

        private readonly List<TaskDefinition> definitions;
        private readonly Dictionary<string, TaskDefinition> byId;
        private readonly HashSet<string> runTaskIds = new HashSet<string>();
        private readonly List<TaskDefinition> active = new List<TaskDefinition>();
        private readonly HashSet<string> completed = new HashSet<string>();
        private readonly Queue<string> queuedSuccessors = new Queue<string>();
        private readonly System.Random random;
        private readonly int panelSize;

        public IReadOnlyList<TaskDefinition> ActiveTasks => active;
        public IReadOnlyCollection<string> CompletedTaskIds => completed;
        public int ProgressPercent =>
            Mathf.Min(100, completed.Count * ProgressPerTask);
        public bool IsFinished => ProgressPercent >= 100;

        public TaskPool(TaskCatalog catalog, int? seed = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            definitions = catalog.tasks
                .Where(task => task != null &&
                               !string.IsNullOrWhiteSpace(task.id))
                .ToList();
            byId = definitions
                .GroupBy(task => task.id)
                .ToDictionary(group => group.Key, group => group.First());
            panelSize = Mathf.Max(1, catalog.panelSize);
            random = seed.HasValue
                ? new System.Random(seed.Value)
                : new System.Random();
            WarnAboutInvalidDefinitions();
            BuildRunDeck(Mathf.Max(1, catalog.runTaskCount));
            Refill();
        }

        public bool Complete(string taskId)
        {
            if (IsFinished || string.IsNullOrEmpty(taskId)) return false;
            TaskDefinition task =
                active.FirstOrDefault(item => item.id == taskId);
            if (task == null) return false;

            active.Remove(task);
            completed.Add(task.id);
            QueueNewlyUnlockedSuccessors(task.id);
            if (!IsFinished) Refill();
            else active.Clear();
            return true;
        }

        public bool IsCompleted(string taskId)
        {
            return completed.Contains(taskId);
        }

        private void BuildRunDeck(int requestedCount)
        {
            HashSet<string> required = new HashSet<string>();
            foreach (TaskDefinition task in
                     definitions.Where(item => item.guaranteed))
                AddPrerequisiteClosure(task, required);

            int targetCount = Mathf.Min(definitions.Count,
                Mathf.Max(requestedCount, required.Count));
            foreach (string id in required)
                runTaskIds.Add(id);

            List<TaskDefinition> optional = definitions
                .Where(task => !runTaskIds.Contains(task.id))
                .OrderBy(_ => random.Next())
                .ToList();
            foreach (TaskDefinition task in optional)
            {
                if (runTaskIds.Count >= targetCount) break;
                runTaskIds.Add(task.id);
            }
        }

        private void Refill()
        {
            while (active.Count < panelSize)
            {
                TaskDefinition successor = TakeQueuedSuccessor();
                if (successor != null)
                {
                    active.Add(successor);
                    continue;
                }

                List<TaskDefinition> eligible = definitions
                    .Where(task => runTaskIds.Contains(task.id) &&
                                   IsEligible(task))
                    .ToList();
                if (eligible.Count == 0) return;
                active.Add(eligible[random.Next(eligible.Count)]);
            }
        }

        private TaskDefinition TakeQueuedSuccessor()
        {
            while (queuedSuccessors.Count > 0)
            {
                string id = queuedSuccessors.Dequeue();
                TaskDefinition task;
                if (byId.TryGetValue(id, out task) &&
                    runTaskIds.Contains(id) && IsEligible(task))
                    return task;
            }
            return null;
        }

        private void QueueNewlyUnlockedSuccessors(string completedTaskId)
        {
            foreach (TaskDefinition task in definitions)
            {
                if (!runTaskIds.Contains(task.id) ||
                    task.prerequisites == null ||
                    !task.prerequisites.Contains(completedTaskId) ||
                    !IsEligible(task) ||
                    queuedSuccessors.Contains(task.id))
                    continue;
                queuedSuccessors.Enqueue(task.id);
            }
        }

        private bool IsEligible(TaskDefinition task)
        {
            if (completed.Contains(task.id) ||
                active.Any(item => item.id == task.id))
                return false;
            return task.prerequisites == null ||
                   task.prerequisites.All(completed.Contains);
        }

        private void WarnAboutInvalidDefinitions()
        {
            if (byId.Count != definitions.Count)
                Debug.LogWarning(
                    "任务池中存在重复 id；重复项不会同时进入任务面板。");
            foreach (TaskDefinition task in definitions)
            {
                if (task.prerequisites == null) continue;
                foreach (string prerequisite in task.prerequisites)
                {
                    if (!byId.ContainsKey(prerequisite))
                        Debug.LogWarning("任务 " + task.id +
                                         " 的前置任务不存在：" +
                                         prerequisite);
                }
            }
        }

        private void AddPrerequisiteClosure(
            TaskDefinition task, HashSet<string> result)
        {
            if (!result.Add(task.id) || task.prerequisites == null) return;
            foreach (string prerequisiteId in task.prerequisites)
            {
                TaskDefinition prerequisite;
                if (byId.TryGetValue(prerequisiteId, out prerequisite))
                    AddPrerequisiteClosure(prerequisite, result);
            }
        }
    }
}
