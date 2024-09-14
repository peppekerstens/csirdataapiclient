Create a C# function in Azure using Visual Studio Code

https://learn.microsoft.com/en-us/azure/azure-functions/create-first-function-vs-code-csharp

Install the Azure Functions Core Tools
https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=windows%2Cisolated-process%2Cnode-v4%2Cpython-v2%2Chttp-trigger%2Ccontainer-apps&pivots=programming-language-csharp#install-the-azure-functions-core-tools

func init MyProjFolder --worker-runtime dotnet-isolated

Tijdens laden van het project, foutmeldingen
https://stackoverflow.com/questions/68283730/error-nu1100-unable-to-resolve

Tijdens het starten van dubug (F5) een foutmelding
https://stackoverflow.com/questions/77319480/no-c-sharp-project-is-currently-loaded-in-visual-studio-code-when-debugging

antwoord 2:
To set up build and debug assets in your .NET project:

Open the Command Palette: Press Ctrl + Shift + P.
Select the Command: Type and select >.NET: Generate Assets for Build and Debug.
Enable Debugging: On the left sidebar, click on the Debug button located above the Extensions icon.
You should now be all set to start debugging!
