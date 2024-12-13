namespace TeleprompterConsole;

public class Program
{
    
    public static async Task Main(string[] args)
    {
        // Initialize the Sentry SDK.  (It is not necessary to dispose it.)
        SentrySdk.Init(options =>
        {
            // You can set here in code, or you can set it in the SENTRY_DSN environment variable.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            options.Dsn = "https://f1737c236ec81613cb16e853ec82f4f5@o4508461602439168.ingest.de.sentry.io/4508461610893392";

            // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
            // This might be helpful, or might interfere with the normal operation of your application.
            // We enable it here for demonstration purposes.
            // You should not do this in your applications unless you are troubleshooting issues with Sentry.
            options.Debug = true;

            // This option is recommended, which enables Sentry's "Release Health" feature.
            options.AutoSessionTracking = true;

            // This option is recommended for client applications only. It ensures all threads use the same global scope.
            // If you are writing a background service of any kind, you should remove this.
            options.IsGlobalModeEnabled = true;

            // This option tells Sentry to capture 100% of traces. You still need to start transactions and spans.
            options.TracesSampleRate = 1.0;

            
        });


        // This starts a new transaction and attaches it to the scope.
        var transaction = SentrySdk.StartTransaction("Program Main", "function");
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        await RunTeleprompter();

        // Always try to finish the transaction successfully.
        // Unhandled exceptions will fail the transaction automatically.
        // Optionally, you can try/catch the exception, and call transaction.Finish(exception) on failure.
        transaction.Finish();
    }

    private static async Task RunTeleprompter()
    {
        SentrySdk.CaptureMessage("Something went wrong");

        var config = new TelePrompterConfig();
        var displayTask = ShowTeleprompter(config);

        var speedTask = GetInput(config);
        await Task.WhenAny(displayTask, speedTask);

    }

    private static async Task ShowTeleprompter(TelePrompterConfig config)
    {
        var words = ReadFrom("sampleQuotes.txt");
        foreach (var word in words)
        {
            Console.Write(word);
            if (!string.IsNullOrWhiteSpace(word))
            {
                await Task.Delay(config.DelayInMilliseconds);
            }
        }
        config.SetDone();
    }

    private static async Task GetInput(TelePrompterConfig config)
    {
        Action work = () =>
        {
            do
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == '>')
                    config.UpdateDelay(-10);
                else if (key.KeyChar == '<')
                    config.UpdateDelay(10);
                else if (key.KeyChar == 'X' || key.KeyChar == 'x')
                    config.SetDone();
            } while (!config.Done);
        };
        await Task.Run(work);
    }
    static IEnumerable<string> ReadFrom(string file)
    {
        string? line;
        using (var reader = File.OpenText(file))
        {
            while ((line = reader.ReadLine()) != null)
            {
                var words = line.Split(' ');
                var lineLength = 0;
                foreach (var word in words)
                {
                    yield return word + " ";
                    lineLength += word.Length + 1;
                    if (lineLength > 70)
                    {
                        yield return Environment.NewLine;
                        lineLength = 0;
                    }
                }
                yield return Environment.NewLine;
            }
        }
    }
}
