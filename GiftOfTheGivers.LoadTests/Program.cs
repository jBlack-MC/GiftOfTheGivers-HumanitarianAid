using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

// Load and stress test runner for the public pages of the web app.
//
//   dotnet run --project GiftOfTheGivers.LoadTests -c Release -- load   --base-url http://localhost:5080 --users 20 --duration 60
//   dotnet run --project GiftOfTheGivers.LoadTests -c Release -- stress --base-url http://localhost:5080 --stages 10,25,50,100,200,400 --stage-seconds 20
//
// Every virtual user loops through the donor journey (home → projects → donate form → health)
// with a think time between requests. Results go to --out (default ./load-results/<mode>-<timestamp>):
//   requests.csv  one row per request      timeline.csv  one row per second
//   summary.json  machine-readable totals  summary.md    the tables pasted into the report

var options = RunOptions.Parse(args);
Directory.CreateDirectory(options.OutputDirectory);

var endpoints = new[] { "/", "/Home/ReliefProjects", "/Home/Donate", "/health/live" };
var results = new ConcurrentQueue<RequestResult>();
var activeUsers = 0;

using var handler = new SocketsHttpHandler
{
    MaxConnectionsPerServer = 2_000,
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    AllowAutoRedirect = false
};
using var client = new HttpClient(handler)
{
    BaseAddress = new Uri(options.BaseUrl),
    Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
};

Console.WriteLine($"Mode {options.Mode}, target {options.BaseUrl}");
Console.WriteLine("Warming up...");
foreach (var endpoint in endpoints)
{
    try { using var _ = await client.GetAsync(endpoint); }
    catch (Exception exception) { Console.WriteLine($"  warm-up {endpoint}: {exception.Message}"); }
}

var clock = Stopwatch.StartNew();
var stages = options.Mode == "stress"
    ? options.Stages.Select(users => new Stage(users, TimeSpan.FromSeconds(options.StageSeconds))).ToList()
    : [new Stage(options.Users, TimeSpan.FromSeconds(options.DurationSeconds))];

var stageReports = new List<StageReport>();
var users = new List<(Task Task, CancellationTokenSource Stop)>();

foreach (var stage in stages)
{
    // Grow the user pool to the stage's size; users from earlier stages keep running.
    while (users.Count < stage.Users)
    {
        var stop = new CancellationTokenSource();
        var userId = users.Count;
        users.Add((Task.Run(() => VirtualUserAsync(userId, stop.Token)), stop));
    }

    var stageStart = clock.Elapsed;
    Console.WriteLine($"[{stageStart:mm\\:ss}] {stage.Users} concurrent users for {stage.Duration.TotalSeconds:0}s");
    await Task.Delay(stage.Duration);
    var stageEnd = clock.Elapsed;

    var report = StageReport.From(stage.Users, results.Where(r => r.Started >= stageStart && r.Started < stageEnd).ToList(),
        (stageEnd - stageStart).TotalSeconds);
    stageReports.Add(report);
    Console.WriteLine($"         {report.Requests} req, {report.Throughput:0.0} req/s, p95 {report.P95:0} ms, errors {report.ErrorRate:P1}");
}

foreach (var (_, stop) in users) { stop.Cancel(); }
await Task.WhenAll(users.Select(u => u.Task));
clock.Stop();

var all = results.OrderBy(r => r.Started).ToList();
WriteOutputs(all, stageReports);
Console.WriteLine($"Results written to {Path.GetFullPath(options.OutputDirectory)}");
return 0;

async Task VirtualUserAsync(int userId, CancellationToken stop)
{
    Interlocked.Increment(ref activeUsers);
    var random = new Random(userId);
    // Stagger start-up so users do not fire in lock-step.
    try { await Task.Delay(random.Next(0, 1000), stop); } catch (OperationCanceledException) { }

    while (!stop.IsCancellationRequested)
    {
        foreach (var endpoint in endpoints)
        {
            if (stop.IsCancellationRequested) { break; }

            var started = clock.Elapsed;
            var timer = Stopwatch.StartNew();
            int status;
            string? error = null;
            try
            {
                using var response = await client.GetAsync(endpoint, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
                status = (int)response.StatusCode;
                if (status >= 400) { error = $"HTTP {status}"; }
            }
            catch (TaskCanceledException) { status = 0; error = "Timeout"; }
            catch (HttpRequestException exception) { status = 0; error = exception.HttpRequestError.ToString(); }
            timer.Stop();

            results.Enqueue(new RequestResult(started, endpoint, status, timer.Elapsed.TotalMilliseconds, error, Volatile.Read(ref activeUsers)));

            if (options.ThinkTimeMs > 0)
            {
                try { await Task.Delay(options.ThinkTimeMs + random.Next(0, options.ThinkTimeMs / 2 + 1), stop); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    Interlocked.Decrement(ref activeUsers);
}

void WriteOutputs(List<RequestResult> all, List<StageReport> stageReports)
{
    var csv = new StringBuilder("elapsed_s,endpoint,status,latency_ms,active_users,error\n");
    foreach (var r in all)
    {
        csv.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{r.Started.TotalSeconds:0.000},{r.Endpoint},{r.Status},{r.LatencyMs:0.0},{r.ActiveUsers},{r.Error}"));
    }
    File.WriteAllText(Path.Combine(options.OutputDirectory, "requests.csv"), csv.ToString());

    var timeline = new StringBuilder("second,active_users,requests,errors,p50_ms,p95_ms,max_ms\n");
    foreach (var bucket in all.GroupBy(r => (int)r.Started.TotalSeconds).OrderBy(g => g.Key))
    {
        var latencies = bucket.Select(r => r.LatencyMs).OrderBy(x => x).ToList();
        timeline.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{bucket.Key},{bucket.Max(r => r.ActiveUsers)},{bucket.Count()},{bucket.Count(r => r.Error is not null)},{Percentile(latencies, 50):0.0},{Percentile(latencies, 95):0.0},{latencies[^1]:0.0}"));
    }
    File.WriteAllText(Path.Combine(options.OutputDirectory, "timeline.csv"), timeline.ToString());

    var totalSeconds = all.Count == 0 ? 1 : Math.Max(1, all[^1].Started.TotalSeconds - all[0].Started.TotalSeconds);
    var overall = StageReport.From(stageReports.Count == 0 ? 0 : stageReports.Max(s => s.Users), all, totalSeconds);
    var perEndpoint = endpoints.Select(e => (Endpoint: e, Report: StageReport.From(0, all.Where(r => r.Endpoint == e).ToList(), totalSeconds))).ToList();
    var errorKinds = all.Where(r => r.Error is not null).GroupBy(r => r.Error!).ToDictionary(g => g.Key, g => g.Count());

    var summary = new
    {
        options.Mode,
        options.BaseUrl,
        options.ThinkTimeMs,
        RunAt = DateTime.UtcNow,
        Overall = overall,
        Stages = stageReports,
        Endpoints = perEndpoint.ToDictionary(p => p.Endpoint, p => p.Report),
        Errors = errorKinds
    };
    File.WriteAllText(Path.Combine(options.OutputDirectory, "summary.json"),
        JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));

    var md = new StringBuilder();
    md.AppendLine($"Target `{options.BaseUrl}`, mode **{options.Mode}**, think time {options.ThinkTimeMs} ms, run {DateTime.Now:yyyy-MM-dd HH:mm}.");
    md.AppendLine();
    md.AppendLine("| Users | Requests | Throughput (req/s) | Avg (ms) | p50 (ms) | p90 (ms) | p95 (ms) | p99 (ms) | Max (ms) | Errors |");
    md.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
    foreach (var s in stageReports) { md.AppendLine(Row(s.Users.ToString(), s)); }
    md.AppendLine();
    md.AppendLine("| Endpoint | Requests | Throughput (req/s) | Avg (ms) | p50 (ms) | p90 (ms) | p95 (ms) | p99 (ms) | Max (ms) | Errors |");
    md.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
    foreach (var (endpoint, report) in perEndpoint) { md.AppendLine(Row($"`{endpoint}`", report)); }
    if (errorKinds.Count > 0)
    {
        md.AppendLine();
        md.AppendLine("Errors: " + string.Join(", ", errorKinds.Select(e => $"{e.Key} × {e.Value}")));
    }
    File.WriteAllText(Path.Combine(options.OutputDirectory, "summary.md"), md.ToString());
    Console.WriteLine();
    Console.WriteLine(md.ToString());

    static string Row(string label, StageReport s) => string.Create(CultureInfo.InvariantCulture,
        $"| {label} | {s.Requests} | {s.Throughput:0.0} | {s.Average:0} | {s.P50:0} | {s.P90:0} | {s.P95:0} | {s.P99:0} | {s.Max:0} | {s.Errors} ({s.ErrorRate:P1}) |");
}

static double Percentile(IReadOnlyList<double> sorted, double percentile)
{
    if (sorted.Count == 0) { return 0; }
    var rank = (int)Math.Ceiling(percentile / 100d * sorted.Count) - 1;
    return sorted[Math.Clamp(rank, 0, sorted.Count - 1)];
}

internal sealed record RequestResult(TimeSpan Started, string Endpoint, int Status, double LatencyMs, string? Error, int ActiveUsers);

internal sealed record Stage(int Users, TimeSpan Duration);

internal sealed record StageReport(int Users, int Requests, int Errors, double ErrorRate, double Throughput,
    double Average, double P50, double P90, double P95, double P99, double Max)
{
    public static StageReport From(int users, List<RequestResult> results, double seconds)
    {
        var latencies = results.Select(r => r.LatencyMs).OrderBy(x => x).ToList();
        var errors = results.Count(r => r.Error is not null);
        return new StageReport(users, results.Count, errors,
            results.Count == 0 ? 0 : (double)errors / results.Count,
            results.Count / Math.Max(seconds, 1),
            latencies.Count == 0 ? 0 : latencies.Average(),
            P(50), P(90), P(95), P(99), latencies.Count == 0 ? 0 : latencies[^1]);

        double P(double p)
        {
            if (latencies.Count == 0) { return 0; }
            var rank = (int)Math.Ceiling(p / 100d * latencies.Count) - 1;
            return latencies[Math.Clamp(rank, 0, latencies.Count - 1)];
        }
    }
}

internal sealed class RunOptions
{
    public string Mode { get; private set; } = "load";
    public string BaseUrl { get; private set; } = Environment.GetEnvironmentVariable("LOADTEST_BASE_URL") ?? "http://localhost:5080";
    public int Users { get; private set; } = 20;
    public int DurationSeconds { get; private set; } = 60;
    public int[] Stages { get; private set; } = [10, 25, 50, 100, 200, 400];
    public int StageSeconds { get; private set; } = 20;
    public int ThinkTimeMs { get; private set; } = -1;
    public int TimeoutSeconds { get; private set; } = 30;
    public string OutputDirectory { get; private set; } = string.Empty;

    public static RunOptions Parse(string[] args)
    {
        var options = new RunOptions();
        var i = 0;
        if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            options.Mode = args[0].ToLowerInvariant();
            i = 1;
        }

        for (; i < args.Length - 1; i += 2)
        {
            var value = args[i + 1];
            switch (args[i])
            {
                case "--base-url": options.BaseUrl = value.TrimEnd('/'); break;
                case "--users": options.Users = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "--duration": options.DurationSeconds = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "--stages": options.Stages = value.Split(',').Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray(); break;
                case "--stage-seconds": options.StageSeconds = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "--think-ms": options.ThinkTimeMs = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "--timeout": options.TimeoutSeconds = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "--out": options.OutputDirectory = value; break;
                default: throw new ArgumentException($"Unknown option {args[i]}");
            }
        }

        if (options.Mode is not ("load" or "stress"))
        {
            throw new ArgumentException("Mode must be 'load' or 'stress'.");
        }

        // Load tests model real users (1 s think time); stress tests remove it to push the server.
        if (options.ThinkTimeMs < 0) { options.ThinkTimeMs = options.Mode == "load" ? 1000 : 0; }
        if (string.IsNullOrEmpty(options.OutputDirectory))
        {
            options.OutputDirectory = Path.Combine("load-results", $"{options.Mode}-{DateTime.Now:yyyyMMdd-HHmmss}");
        }

        return options;
    }
}
