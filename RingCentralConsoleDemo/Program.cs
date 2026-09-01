using System;
using System.Threading.Tasks;
using RingCentral;
using dotenv.net;

namespace RingCentralConsoleDemo;

class Program
{
    // This is not exactly a two way call its just an outgoing from RC to the callee - works with right variables if there is a problem its with the dotenv in root
    static RestClient restClient;
    static async Task Main(string[] args)
    {
        DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { "../../../.env" })); // small change from boiler plate, dotenv makes it easier just .gitignore
        restClient = new RestClient(
            Environment.GetEnvironmentVariable("RC_APP_CLIENT_ID"),
            Environment.GetEnvironmentVariable("RC_APP_CLIENT_SECRET"),
            Environment.GetEnvironmentVariable("RC_SERVER_URL"));
        await restClient.Authorize(Environment.GetEnvironmentVariable("RC_USER_JWT"));
        await call_ringout();
    }
    
    static private async Task call_ringout()
    {
        var parameters = new MakeRingOutRequest();
        parameters.from = new MakeRingOutCallerInfoRequestFrom {
            phoneNumber = Environment.GetEnvironmentVariable("RINGOUT_CALLER")
        };
        parameters.to = new MakeRingOutCallerInfoRequestTo {
            phoneNumber = Environment.GetEnvironmentVariable("RINGOUT_RECIPIENT")
        };
        parameters.playPrompt = false;

        var resp = await restClient.Restapi().Account().Extension().RingOut().Post(parameters);
        Console.WriteLine("Call Placed. Call status" + resp.status.callStatus);
    }
}