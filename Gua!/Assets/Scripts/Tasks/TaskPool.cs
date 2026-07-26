using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FrogCamp.Tasks
{
    /// <summary>
    /// 负责抽取、前置条件和进度，不依赖具体任务玩法或 UI。
    /// </summary>
    public sealed class TaskPool
    {
        public const int ProgressPerTask = 10;

        private readonly List<TaskDefinition> definitions;
        private readonly Dictionary<string, TaskDefinition> byId;
        private readonly List<TaskDefinition> active = new List<TaskDefinition>();
        private readonly HashSet<string> completed = new HashSet<string>();
        private readonly System.Random random;
        private readonly int panelSize;

        public IReadOnlyList<TaskDefinition> ActiveTasks => active;
        public IReadOnlyCollection<string> CompletedTaskIds => completed;
        public int ProgressPercent => Mathf.Min(100, completed.Count * ProgressPerTask);
        public bool IsFinished => ProgressPercent >= 100;

        public TaskPool(TaskCatalog catalog, int? seed = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            definitions = catalog.tasks
                .Where(task => task != null && !string.IsNullOrWhiteSpace(task.id))
                .ToList();
            byId = definitions
                .GroupBy(task => task.id)
                .ToDictionary(group => group.Key, group => group.First());
            panelSize = Mathf.Max(1, catalog.panelSize);
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            WarnAboutInvalidDefinitions();
            Refill();
        }

        public bool Complete(string taskId)
        {
            if (IsFinished || string.IsNullOrEmpty(taskId)) return false;
            TaskDefinition task = active.FirstOrDefault(item => item.id == taskId);
            if (task == null) return false;

            active.Remove(task);
            completed.Add(task.id);
            if (!IsFinished) Refill();
            else active.Clear();
            return true;
        }

        public bool IsCompleted(string taskId)
        {
            return completed.Contains(taskId);
        }

        private void Refill()
        {
            while (active.Count < panelSize)
            {
                List<TaskDefinition> eligible = definitions
                    .Where(IsEligible)
                    .ToList();
                if (eligible.Count == 0) return;

                // 必出任务只要前置条件满足，就始终先于普通随机任务进入面板。
                List<TaskDefinition> guaranteed = eligible
                    .Where(task => task.guaranteed)
                    .ToList();
                List<TaskDefinition> guaranteedPrerequisites = eligible
                    .Where(IsNeededByIncompleteGuaranteedTask)
                    .ToList();
                List<TaskDefinition> candidates = guaranteed.Count > 0
                    ? guaranteed
                    : guaranteedPrerequisites.Count > 0
                        ? guaranteedPrerequisites
                        : eligible;
                active.Add(candidates[random.Next(candidates.Count)]);
            }
        }

        private bool IsEligible(TaskDefinition task)
        {
            if (completed.Contains(task.id) || active.Any(item => item.id == task.id))
                return false;
            if (task.prerequisites == null || task.prerequisites.Count == 0)
                return true;
            return task.prerequisites.All(completed.Contains);
        }

        private bool IsNeededByIncompleteGuaranteedTask(TaskDefinition candidate)
        {
            return definitions.Any(task =>
                task.guaranteed &&
                !completed.Contains(task.id) &&
                DependsOn(task, candidate.id, new HashSet<string>()));
        }

        private bool DependsOn(TaskDefinition task, string prerequisiteId,
            HashSet<string> visited)
        {
            if (task.prerequisites == null || !visited.Add(task.id)) return false;
            if (task.prerequisites.Contains(prerequisiteId)) return true;
            foreach (string parentId in task.prerequisites)
            {
                TaskDefinition parent;
                if (byId.TryGetValue(parentId, out parent) &&
                    DependsOn(parent, prerequisiteId, visited))
                    return true;
            }
            return false;
        }

        private void WarnAboutInvalidDefinitions()
        {
            if (byId.Count != definitions.Count)
                Debug.LogWarning("任务池中存在重复 id；重复项将不会同时进入任务面板。");

            foreach (TaskDefinition task in definitions)
            {
                if (task.prerequisites == null) continue;
                foreach (string prerequisite in task.prerequisites)
                {
                    if (!byId.ContainsKey(prerequisite))
                        Debug.LogWarning("任务 " + task.id + " 的前置任务不存在：" + prerequisite);
                }
            }

            HashSet<string> guaranteedClosure = new HashSet<string>();
            foreach (TaskDefinition task in definitions.Where(item => item.guaranteed))
                AddPrerequisiteClosure(task, guaranteedClosure);
            if (guaranteedClosure.Count > 100 / ProgressPerTask)
                Debug.LogWarning("必出任务及其前置任务超过 10 个，无法在 100% 前全部完成。");
        }

        private void AddPrerequisiteClosure(TaskDefinition task, HashSet<string> result)
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
