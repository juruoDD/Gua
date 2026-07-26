# 任务池配置

编辑同目录的 `task_pool.json` 就能继续添加任务，无需修改任务系统代码。

每个任务字段：

- `id`：唯一英文标识，玩法代码用它完成任务。
- `title`：任务面板显示名称。
- `description`：任务说明，已预留给后续详情 UI。
- `guaranteed`：`true` 表示一定包含在本局任务中，但不保证第一轮出现。
- `prerequisites`：前置任务 id 数组；全部完成后，这个任务才进入可抽取范围。

`panelSize` 控制面板同时显示几项任务，`runTaskCount` 控制本局抽取的任务总数。
每完成一项固定增加 10%。连续任务完成前置后，后续任务会优先补入面板。

玩法代码完成任务：

```csharp
using FrogCamp.Tasks;

TaskPanelController.Instance.CompleteTask("eat_small_insects");
```

也可以订阅 `TaskCompleted` 和 `ProgressChanged` 事件，连接奖励、音效或结算逻辑。
