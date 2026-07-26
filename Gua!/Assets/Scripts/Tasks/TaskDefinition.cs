using System;
using System.Collections.Generic;

namespace FrogCamp.Tasks
{
    [Serializable]
    public sealed class TaskDefinition
    {
        public string id;
        public string title;
        public string description;
        public bool guaranteed;
        public List<string> prerequisites = new List<string>();
    }

    [Serializable]
    public sealed class TaskCatalog
    {
        public int panelSize = 4;
        public int runTaskCount = 10;
        public List<TaskDefinition> tasks = new List<TaskDefinition>();
    }
}
