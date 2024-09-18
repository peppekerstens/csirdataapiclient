public class LoginTests
{
    public static async Task Main(string[] args)
    {
        // Instantiate the IrDataClient with username and password
        string username = "tt9325951@gmail.com";
        string password = "F3rR@rI2@24!!";

        IrDataClient irClient = new IrDataClient(username, password);

        // Call LoginAsync and check if login is successful
        string loginResult = await irClient.LoginAsync();

        if (loginResult == "Logged in")
        {
            Console.WriteLine("Login successful!");
        }
        else
        {
            Console.WriteLine("Login failed.");
        }
    }
}