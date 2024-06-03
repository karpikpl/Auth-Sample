using Azure.Messaging.WebPubSub.Clients;
using Microsoft.Extensions.Configuration;
using System;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Console = Colorful.Console;

namespace consoleClient;

public static class App
{
    public static async Task RunApp(string targetingFramework, string[] args)
    {
        var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

        var authHelper = new AuthHelper(configuration);
        var confidentialauthHelper = new ConfidentialAuthHelper(configuration);

        // keeping the client simple
        var serverAddress = args.Length > 0 ? args[0] : configuration["DownstreamApi:BaseUrl"];

        var token = "";
        HttpClient httpClient = new HttpClient() { BaseAddress = new Uri(serverAddress!) };

        MenuOptions choice;
        while ((choice = Menu(serverAddress!, targetingFramework)) != 0)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            Func<Task> task = () => Task.CompletedTask;
            switch (choice)
            {
                case MenuOptions.GetRoles:
                    task = async () => await GetRoles(httpClient);
                    break;
                case MenuOptions.CallGraph:
                    task = async () => await CallGraph(httpClient);
                    break;
                case MenuOptions.Authenticate_FromCache:
                    task = async () =>
                    {
                        token = await authHelper.GetTokenSilentlyFromCacheAsync();
                    };
                    break;
                case MenuOptions.Authenticate_Device:
                    task = async () =>
                    {
                        token = await authHelper.GetTokenUsingDeviceCodeAsync();
                    };
                    break;
                case MenuOptions.Authenticate_Windows:
                    task = async () =>
                    {
                        token = await authHelper.GetTokenUsingWindowsAuthAsync();
                    };
                    break;
                case MenuOptions.Authenticate_Interactive:
                    task = async () =>
                    {
                        token = await authHelper.GetTokenUsingInteractiveAsync();
                    };
                    break;
                case MenuOptions.Authenticate_ClientSecret:
                    task = async () =>
                    {
                        token = await confidentialauthHelper.GetAccessTokenAsync();
                    };
                    break;
                case MenuOptions.Authenticate_UsingWAM:
                    task = async () =>
                    {
                        token = await authHelper.GetTokenUsingWAMAsync();
                    };
                    break;
                default:
                    break;
            }

            try
            {
                await task();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message, Color.Red);
            }

            Console.WriteLine("Press any key to continue", Color.Blue);
            Console.ReadKey();
        }

        try
        {
            httpClient.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception Shutting down channel: " + ex.Message, Color.Red);
        }
    }

    private static async Task CallGraph(HttpClient httpClient)
    {
        Console.Write("Graph Call ", Color.Gray);

        try
        {
            var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response from MS Graph /me endpoint: " + content, Color.Green);
            }
            else
            {
                Console.WriteLine("Error calling MS Graph /me endpoint: " + response.StatusCode, Color.Red);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message, Color.Red);
        }
    }

    private static async Task GetRoles(HttpClient httpClient)
    {
        try
        {
            var response = await httpClient.GetAsync("/roles");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response Roles endpoint: " + content, Color.Green);
            }
            else
            {
                Console.WriteLine("Error calling Roles endpoint: " + response.StatusCode, Color.Red);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message, Color.Red);
        }
    }


    static MenuOptions Menu(string serverAddress, string targetingFramework = "???")
    {
        int choice = -1;
        int max = Enum.GetValues(typeof(MenuOptions)).Cast<int>().Max();
        while (choice < 0 || choice > max)
        {
            Console.Clear();

            Console.WriteAsciiStyled("API Client", new Colorful.StyleSheet(Color.OrangeRed));

            Info.DisplayInfo(serverAddress, targetingFramework);

            Console.WriteLine("{0:d}. Get Roles", MenuOptions.GetRoles);
            Console.WriteLine("{0:d}. Call Graph", MenuOptions.CallGraph);
            Console.WriteLine("{0:d}. Authenticate using local cache", MenuOptions.Authenticate_FromCache);
            Console.WriteLine("{0:d}. Authenticate silently using windows credentials", MenuOptions.Authenticate_Windows);
            Console.WriteLine("{0:d}. Authenticate using interactive flow (UI)", MenuOptions.Authenticate_Interactive);
            Console.WriteLine("{0:d}. Authenticate using device flow", MenuOptions.Authenticate_Device);
            Console.WriteLine("{0:d}. ConfidentialClient Authenticate using app credentials - client secret", MenuOptions.Authenticate_ClientSecret);
            Console.WriteLine("{0:d}. PublicClient Authenticate using WAM", MenuOptions.Authenticate_UsingWAM);
            Console.WriteLine("{0:d}. Exit", MenuOptions.Exit);
            Console.WriteLine("Enter your choice");

            var input = Console.ReadLine();
            if (!int.TryParse(input, out choice))
            {
                Console.WriteLine("Invalid input");
                choice = -1;
            }
        }
        return (MenuOptions)choice;
    }

}
