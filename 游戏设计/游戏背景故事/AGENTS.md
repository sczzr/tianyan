# 《苍玄界》AGENTS 协议总览

本协议是 Codex 在《苍玄界》知识库中工作的顶层宪章，规定了语言、路由与审计要求。详细的子代理说明请参见 [[.cangxuan/AGENTS.md]]。

## 使用方式
- 在 Codex CLI 中，进入知识库根目录后运行命令 codex ，即可载入代理环境。
- 在 Gemini CLI 中，于项目根目录执行 gemini agents load （或依版本调整为最新加载指令），确保读取本协议。
- 在 Claude Code 中，将仓库加入工作空间后打开 AGENTS.md 与 .cangxuan/AGENTS.md ，系统会自动注入路由说明。
- 在 Qwen CLI 中，于仓库根目录运行 qwen agents sync （或对应的加载指令）以刷新代理配置。
- 与用户沟通时保持中文输出，并引用路径或代办请使用双中括号格式。
- 需要调用代理时，直接提及角色与任务，如：使用 角色总建筑师 创建卷三敌对势力成员。
- 如遇复杂或多阶段指令，先调用 任务编排师 orchestrator 审核信息缺口并生成执行计划。

## 核心约束
- 语言约束：所有回复必须为中文，遵守修饰符前后留空规则。
- 路由约束：创作类任务必须由专业子代理执行，主代理仅负责调度与审计。
- 世界观约束：禁止生成与《苍玄界》知识库冲突的内容，必要时调用查询代理核对。
- 审计约束：每轮交付前需完成 TODO、格式与路径检查，并记录后续行动建议。

## 任务路由矩阵

| 场景提示 | 调用代理 |
| --- | --- |
| 大纲/一级网络/全书起承转合 | [[.cangxuan/agents/outline-architect.md]] |
| 卷纲/单元故事/节奏点/章纲 | [[.cangxuan/agents/outline-refiner.md]] |
| 章节创作/正文生成/QA | [[.cangxuan/agents/prose-supervisor.md]] |
| 世界观查询/设定检索/一致性核对 | [[.cangxuan/agents/query-agent.md]] |
| 读者视角体验与反馈评估 | [[.cangxuan/agents/beta-reader.md]] |
| 多领域/需求模糊/需要计划拆解 | [[.cangxuan/agents/orchestrator.md]] |
| 人物卡创建（工具） | [[tools/creative-tools/agents/character-agent.md]] |

## 执行流程
1. 分析指令，识别关键词与潜在冲突点。
2. 按路由矩阵选择主代理，必要时调用 orchestrator 先行规划。
3. 调用 cangxuan-query-agent 对名称或设定进行存在性检查。
4. 激活目标代理并移交上下文，明确目标产出与验收标准。
5. 收集各步骤产物路径、关键结论与待办事项。
6. 汇总 TODO 并确认是否需要追加执行或提醒用户。
7. 输出最终交付，标注文件路径、验证状态与后续建议。

## 规划工作流参考
- orchestrator 应以 [[.cangxuan/workflows/orchestrator-multi-phase.workflow.md]] 作为多阶段蓝图，覆盖计划、执行与复盘。
- 在规划阶段优先调用 [[.cangxuan/tasks/create-creative-brief.md]] 汇总需求，并通过 [[.cangxuan/checklists/orchestrator-plan-checklist.md]] 做信息完备性审查。
- 创作流水线以 [[.cangxuan/workflows/novel-pipeline.workflow.md]] 为唯一主链模板。

## 质量与模板资源
- 执行过程中请结合 [[.cangxuan/checklists]] 内的清单，如 orchestrator-plan-checklist、world-asset-checklist、prose-chapter-checklist。
- 文档撰写建议使用 [[.cangxuan/templates/template-project-brief.md]] 及 template-* 系列模板，保持字段规范。
- 若在执行中发现缺口，可触发 [[.cangxuan/tasks/create-creative-brief.md]]、[[.cangxuan/tasks/execute-checklist.md]] 等任务以补充信息与复核结果。

## 子系统索引
- 代理索引与团队信息：[[.cangxuan/AGENTS.md]]
- 创作团队编排：[[.cangxuan/agent-teams/creative-teams.yaml]]
- 标准工作流：[[.cangxuan/workflows/novel-pipeline.workflow.md]]
- 检查清单仓库：[[.cangxuan/checklists]]
- 标准任务库：[[.cangxuan/tasks]]
- 模板集合：[[.cangxuan/templates]]

## 最终检查清单
- [ ] 语言检查：所有输出为中文并符合格式化规则。
- [ ] 协议遵循：已按照任务路由与执行流程操作。
- [ ] 知识库一致性：完成查询代理或索引核对。
- [ ] 档案路径：新增或修改文件路径已在回复中准确标注。
- [ ] TODO 状态：列出的代办事项已有明确处理安排。
