using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Reads connection string "chat" from user secrets / appsettings:
// "Endpoint=https://models.github.ai/inference;Key=YOUR-GITHUB-TOKEN"
// Setup: dotnet user-secrets set ConnectionStrings:chat "Endpoint=https://models.github.ai/inference;Key=YOUR-GITHUB-TOKEN"
builder.AddOpenAIClient("chat")
    .AddChatClient(Environment.GetEnvironmentVariable("MODEL_NAME") ?? "gpt-4o-mini");

builder.AddAIAgent("Writer", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    return new ChatClientAgent(
        chatClient,
        name: key,
        instructions:
            """
            You are a creative writing assistant who crafts vivid,
            well-structured stories with compelling characters based on user prompts,
            and formats them after writing.
            """,
        tools: [
            AIFunctionFactory.Create(GetAuthor),
            AIFunctionFactory.Create(FormatStory)
        ]
    );
});

builder.AddAIAgent(
    name: "Editor",
    instructions:
        """
        You are an editor who improves a writer's draft by providing 4-8 concise recommendations and
        a fully revised Markdown document, focusing on clarity, coherence, accuracy, and alignment.
        """);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseStaticFiles();
}

app.UseHttpsRedirection();

app.MapGet("/agent/chat", async (
    [FromKeyedServices("Writer")] AIAgent writer,
    [FromKeyedServices("Editor")] AIAgent editor,
    string prompt,
    CancellationToken cancellationToken) =>
{
    Workflow workflow =
        AgentWorkflowBuilder
            .BuildSequential([writer, editor]);

    await using Run run = await InProcessExecution.Default.RunAsync(
        workflow, prompt, "session-1", cancellationToken);

    // RunAsync streams AgentResponseUpdateEvent chunks — collect and reconstruct
    var updates = run.OutgoingEvents
        .OfType<AgentResponseUpdateEvent>()
        .ToList();

    string lastExecutorId = updates.Last().ExecutorId;

    string result = string.Concat(
        updates
            .Where(e => e.ExecutorId == lastExecutorId)
            .Select(e => e.Update.Text));

    return Results.Ok(result);
});

app.Run();

[Description("Gets the author of the story.")]
string GetAuthor() => "Jack Torrance";

[Description("Formats the story for display.")]
string FormatStory(string title, string author, string story) =>
    $"Title: {title}\nAuthor: {author}\n\n{story}";
