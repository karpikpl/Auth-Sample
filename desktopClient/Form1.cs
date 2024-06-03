using Microsoft.Extensions.Configuration;

namespace desktopClient;

public partial class Form1 : Form
{
    private readonly IConfiguration _configuration;

    public Form1(IConfiguration configuration)
    {
        InitializeComponent();
        _configuration = configuration;
    }

    private async void LoginButton_Click(object sender, EventArgs e)
    {
         var hWnd = this.Handle;
        var authHelper = new consoleClient.AuthHelper(_configuration);
        var token = await authHelper.GetTokenUsingWAMAsync(useTokenCache: true, handle: hWnd);

        // Call your method to get data from the API
        // Replace GetDataFromApi with your actual method
        //var data = await GetDataFromApi(token);

        // Do something with the data...
    }
}
