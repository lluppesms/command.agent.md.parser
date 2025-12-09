// Demo of loading agent profiles from a Markdown File

AgentPromptManager.Initialize();

Console.WriteLine($"\nGetting List of Agents...");
var agentNames = AgentPromptManager.GetAgentList();
Console.WriteLine($"  Agents Found: {string.Join(", ", agentNames)}");

var agentName = agentNames[1];
Console.WriteLine($"\nGetting List of Prompts for Agent: {agentName}");
var promptsForAgent = AgentPromptManager.GetPromptsForAgent(agentName);
Console.WriteLine($"  Prompts Found: {string.Join(", ", promptsForAgent)}");

var promptName = promptsForAgent[0];
var prompt = AgentPromptManager.GetPrompt(agentName, promptName);
Console.WriteLine($"\nGetting Prompt Content For {agentName}: {promptName}...");
Console.WriteLine($"Prompt Contents:\n{prompt}\n");
