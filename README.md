# SKFProductAssistant

# Add EnvironmentVariable
Windows (PowerShell - Run as Administrator):

powershell
[Environment]::SetEnvironmentVariable("AzureOpenAI__Endpoint", "https://skf-openai-dev-eval.openai.azure.com/", "User")
[Environment]::SetEnvironmentVariable("AzureOpenAI__ApiKey", "your api key", "User")

# Run ..\SKFProductAssistant\src\SKFProductAssistant.Functions\SKFProductAssistant.Functions.csproj
 
# Execute Test ..\SKFProductAssistant\tests\SKFProductAssistant.Functions.IntegrationTests\SKFProductAssistant.Functions.IntegrationTests.csproj