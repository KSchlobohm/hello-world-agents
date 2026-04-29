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
- `workflow.AsAgentAsync()` — does not exist. Use `InProcessExecution.Default.RunAsync(workflow, message, sessionId)` and extract the result from `run.OutgoingEvents.OfType<AgentResponseEvent>()`.
- `AgentWorkflowBuilder.BuildSequential(writer, editor)` — no params overload. Pass a collection: `BuildSequential([writer, editor])`.

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
