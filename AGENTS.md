# AGENTS.md

## Context for AI Agents

This repository is a personal learning project following along with the blog post:
[Introducing Microsoft Agent Framework Preview](https://devblogs.microsoft.com/dotnet/introducing-microsoft-agent-framework-preview/)

## ⚠️ Code in This Repo May Be Broken

The blog post contains sample code that does **not compile against the published NuGet packages** (v1.3.0 at time of writing). Specific known issues:

- `ChatClientAgentOptions.Instructions` — does not exist. Use the `ChatClientAgent(IChatClient, instructions: "...", name: "...")` constructor overload instead.
- `AgentRunResponse` — does not exist. Use `AgentResponse` instead.
- `workflow.AsAgentAsync()` — does not exist. Use `InProcessExecution.Default.RunAsync(workflow, message, sessionId)` and extract the result from `run.OutgoingEvents.OfType<AgentResponseEvent>()`.
- `AgentWorkflowBuilder.BuildSequential(writer, editor)` — no params overload. Pass a collection: `BuildSequential([writer, editor])`.

## ✅ Working Reference Implementation

The blog author's own working source code lives here:
**https://github.com/luisquintanilla/hello-world-agents**

When in doubt about the correct API usage, consult that repository first. It uses `Version="*-*"` (latest prerelease) for the Agent Framework packages, which may have a different API surface than the latest stable release on NuGet.
