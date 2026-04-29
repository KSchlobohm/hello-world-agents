# AGENTS.md

## Context for AI Agents

This repository is a personal learning project following along with the blog post:
[Introducing Microsoft Agent Framework Preview](https://devblogs.microsoft.com/dotnet/introducing-microsoft-agent-framework-preview/)

## Official Documentation

For the latest Agent Framework documentation and guidance, see:

- [Agent Framework Overview](https://learn.microsoft.com/en-us/agent-framework/overview/?pivots=programming-language-csharp)
- [Microsoft Extensions AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)

## ⚠️ Code in This Repo May Be Broken

The blog post contains sample code that does **not compile against the published NuGet packages** (v1.3.0 at time of writing). Specific known issues:

- `ChatClientAgentOptions.Instructions` — does not exist. Use the `ChatClientAgent(IChatClient, instructions: "...", name: "...")` constructor overload instead.
- `AgentRunResponse` — does not exist. Use `AgentResponse` instead.
- `workflow.AsAgentAsync()` — does not exist. Use `InProcessExecution.Default.RunAsync(workflow, message, sessionId, cancellationToken)` and extract the result from streaming update events (see below).
- `AgentWorkflowBuilder.BuildSequential(writer, editor)` — no params overload. Pass a collection: `BuildSequential([writer, editor])`.

## ✅ Extracting the Response from a Workflow Run

`InProcessExecution.Default.RunAsync()` emits **streaming chunks** (`AgentResponseUpdateEvent`), NOT a single final `AgentResponseEvent`. To get the complete text output:

```csharp
await using Run run = await InProcessExecution.Default.RunAsync(
    workflow, message, "session-1", cancellationToken);

var updates = run.OutgoingEvents
    .OfType<AgentResponseUpdateEvent>()
    .ToList();

// The last executor's updates contain the final output
string lastExecutorId = updates.Last().ExecutorId;

string result = string.Concat(
    updates
        .Where(e => e.ExecutorId == lastExecutorId)
        .Select(e => e.Update.Text));
```

> **Do NOT** use `.OfType<AgentResponseEvent>()` — that type is never emitted by `RunAsync` in v1.3.0.

## ✅ Working Reference Implementation

The blog author's own working source code lives here:
**https://github.com/luisquintanilla/hello-world-agents**

When in doubt about the correct API usage, consult that repository first. It uses `Version="*-*"` (latest prerelease) for the Agent Framework packages, which may have a different API surface than the latest stable release on NuGet.

## 🚨 Console App Runtime Warning

If the console app runs for more than 1 minute without completing, it has likely encountered a runtime error or is hung. Terminate the process and debug for potential issues such as:

- Missing or invalid API configuration (model, endpoint, authentication)
- Hanging event loop or agent workflow
- Network connectivity issues
- Resource exhaustion or deadlock

## 📝 Key Learning

The blog post's API examples don't match the published v1.3.0 NuGet packages. The biggest gotcha: `RunAsync()` doesn't produce a single final `AgentResponseEvent` — it streams the response as many small `AgentResponseUpdateEvent` chunks (similar to how LLM APIs stream tokens). To reconstruct the full output, you must collect all update events, group by `ExecutorId` to identify which agent produced them, and concatenate their `.Update.Text` values. The last executor in a sequential workflow is the one whose output you want.
