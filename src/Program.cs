using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Runtime.InteropServices;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT", EnvironmentVariableTarget.User);
var apiKey = Environment.GetEnvironmentVariable("OPENAI_APIKEY", EnvironmentVariableTarget.User);
var deploymentName = "gpt-4o-mini";

var chatService = new AzureOpenAIChatCompletionService(deploymentName, endpoint, apiKey);

var ans = await chatService.GetChatMessageContentAsync("Que es Global Azure?");
Console.WriteLine(ans);