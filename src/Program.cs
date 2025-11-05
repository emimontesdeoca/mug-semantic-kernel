using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT", EnvironmentVariableTarget.User);
var apiKey = Environment.GetEnvironmentVariable("OPENAI_APIKEY", EnvironmentVariableTarget.User);
var deploymentName = "gpt-5-mini";

var chatService = new AzureOpenAIChatCompletionService(deploymentName, endpoint!, apiKey!);

var kernelBuilder = Kernel.CreateBuilder();

kernelBuilder.Plugins.AddFromType<ShoppingCartPlugin>();
kernelBuilder.Plugins.AddFromType<PaymentPlugin>();
kernelBuilder.Plugins.AddFromType<PizzaMenuPlugin>();

var kernel = kernelBuilder.Build();

var promptSettings = new AzureOpenAIPromptExecutionSettings()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

var systemMessage = "You are a member of a pizza shop called PizzerIA, people are going to come to you asking for information about pizzas, orders and pay. You must only answer about this and do not answer about any other topic! Asnwer people like they are kids from age 5";
var chatHistory = new ChatHistory(systemMessage);

while (true)
{
    Console.Write("Q: ");
    var prompt = Console.ReadLine();
    chatHistory.AddUserMessage(prompt!);

    var answer = await chatService.GetChatMessageContentAsync(chatHistory, promptSettings, kernel);
    Console.WriteLine($"A: {answer.ToString()}");
}

public class PizzaMenuPlugin
{
    [KernelFunction("get_available_pizzas")]
    [Description("Retrieves the list of available pizzas with their details")]
    public async Task<List<Pizza>> GetAvailablePizzasAsync()
    {
        return new List<Pizza>
        {
            new Pizza { Name = "Pepperoni", Ingredients = "Pepperoni, cheese, tomato sauce", Price = 12 },
            new Pizza { Name = "Margherita", Ingredients = "Tomato, mozzarella, basil", Price = 10 },
            new Pizza { Name = "Hawaiian", Ingredients = "Ham, pineapple, cheese", Price = 11 },
            new Pizza { Name = "Vegetarian", Ingredients = "Mushrooms, peppers, onions, olives", Price = 13 }
        };
    }
}

public class Pizza
{
    public string Name { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class ShoppingCartPlugin
{
    [KernelFunction("add_to_cart")]
    [Description("Adds a pizza to the shopping cart")]
    public async Task<string> AddToCartAsync(
        [Description("The name of the pizza to add, use the one we have in our list if the user typed it wrong")] string pizzaName)
    {
        CartService.AddItem(pizzaName);
        return $"{pizzaName} added to cart.";
    }

    [KernelFunction("remove_from_cart")]
    [Description("Removes a pizza from the shopping cart")]
    public async Task<string> RemoveFromCartAsync(
        [Description("The name of the pizza to remove")] string pizzaName)
    {
        bool removed = CartService.CartItems.Remove(pizzaName);
        return removed ? $"{pizzaName} removed from cart." : "Item not found in cart.";
    }

    [KernelFunction("view_cart")]
    [Description("Displays the current contents of the shopping cart")]
    public async Task<List<string>> ViewCartAsync()
    {
        return CartService.CartItems;
    }

    [KernelFunction("clear_cart")]
    [Description("Clears all items from the shopping cart")]
    public async Task<string> ClearCartAsync()
    {
        CartService.ClearCart();
        return "Cart cleared.";
    }
}

public class PaymentPlugin
{
    [KernelFunction("process_payment")]
    [Description("Processes the payment for the current order. Calculates total automatically.")]
    public async Task<string> ProcessPaymentAsync(
        [Description("Payment method (e.g., credit, paypal)")] string paymentMethod)
    {
        var validMethods = new[] { "credit", "paypal", "debit" };
        if (!validMethods.Contains(paymentMethod.ToLower()))
            return $"Error: Payment method '{paymentMethod}' is not supported.";

        decimal total = CalculateTotal();
        if (total == 0) return "Error: Cart is empty.";

        CartService.ClearCart();
        return $"Payment of {total:C} via {paymentMethod} processed successfully!";
    }

    private decimal CalculateTotal()
    {
        var prices = new Dictionary<string, decimal>
        {
            { "Pepperoni", 12m }, { "Margherita", 10m }, { "Hawaiian", 11m }, { "Vegetarian", 13m }
        };

        var total = 0m;

        foreach (var item in CartService.CartItems)
        {
            total += prices.GetValueOrDefault(item, 0);
        }

        return total;
    }
}

public class CartService
{
    public static List<string> CartItems { get; set; } = new();
    public static void AddItem(string item) => CartItems.Add(item);
    public static void RemoveItem(string item) => CartItems.Remove(item);
    public static void ClearCart() => CartItems.Clear();
}