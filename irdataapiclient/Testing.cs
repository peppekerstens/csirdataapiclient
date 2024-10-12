public class LoginTests
{
    public static async Task Main(string[] args)
    {
        // Instantiate the IrDataClient with username and password
        Console.WriteLine("Provide iRacing username");
        string username = Console.ReadLine();
        Console.WriteLine("Provide iRacing password");
        string password = Console.ReadLine();   
        
        IrDataClient irClient = new IrDataClient(username, password);

        await LoginTest(irClient);
        await GetCarsTest(irClient);
    }

    public static async Task LoginTest(IrDataClient irClient)
    {
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

    public static async Task GetCarsTest(IrDataClient irClient)
    {
        Console.WriteLine("Requesting cars...");
        List<Dictionary<string, object>> test = await irClient.GetCarsAsync();
        Console.WriteLine("Received cars.");

        foreach (Dictionary<string, object> testEntry in test)
        {
            foreach (string key in testEntry.Keys)
            {
                Console.WriteLine($"{key} {testEntry[key]}");
            }
        }
    }
}