using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ResearchRecoveryTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private Type Client => Type.GetType("Backend.TelemetryClient, Assembly-CSharp", true);
    private Type Settings => Type.GetType("Backend.BackendSettings, Assembly-CSharp", true);
    private MonoBehaviour _client;
    private ScriptableObject _settings;
    private HttpListener _server;
    private readonly ConcurrentQueue<string> _requests = new ConcurrentQueue<string>();
    private readonly ConcurrentQueue<string> _payloads = new ConcurrentQueue<string>();
    private int _port;
    private string _url;
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);

    [SetUp]
    public void SetUp()
    {
        _requests.Clear();
        _payloads.Clear();
        foreach (string key in new[] { "Backend.PendingTelemetry", "Backend.PendingSessionEnd.v1", "Backend.PendingProgressSnapshot.v2", "Backend.CurrentCodeHash.v1", "Backend.CurrentCodeVerified.v1", "Backend.LastVerifiedCodeHash.v1" }) PlayerPrefs.DeleteKey(key);
        var portProbe = new TcpListener(IPAddress.Loopback, 0);
        portProbe.Start();
        _port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
        portProbe.Stop();
        _url = "http://127.0.0.1:" + _port;
        _settings = ScriptableObject.CreateInstance(Settings);
        Set(_settings, "enableBackend", true);
        Set(_settings, "supabaseUrl", _url);
        Set(_settings, "publishableKey", "local-test-key");
        Set(_settings, "maxBatchSize", 100);
        PlayerPrefs.SetString("Backend.AccessToken", "local-test-token");
        PlayerPrefs.SetString("Backend.RefreshToken", "local-test-refresh");
        PlayerPrefs.SetString("Backend.UserId", "11111111-1111-4111-8111-111111111111");
        PlayerPrefs.SetString("Backend.AccessTokenExpiresAtUnix", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString());
        _client = (MonoBehaviour)new GameObject("Research recovery test").AddComponent(Client);
    }

    private void StartServer(int authStatus = 200)
    {
        _server = new HttpListener();
        _server.Prefixes.Add(_url + "/");
        _server.Start();
        var server = _server;
        Task.Run(async () =>
        {
            while (server.IsListening)
            {
                HttpListenerContext context;
                try { context = await server.GetContextAsync(); }
                catch { break; }
                string path = context.Request.Url.AbsolutePath;
                _requests.Enqueue(path);
                _payloads.Enqueue(new StreamReader(context.Request.InputStream).ReadToEnd());
                bool auth = path.StartsWith("/auth/");
                context.Response.StatusCode = auth ? authStatus : 200;
                string body = path.EndsWith("research-participation")
                    ? "{\"ok\":true,\"participantId\":\"22222222-2222-4222-8222-222222222222\",\"studyId\":\"33333333-3333-4333-8333-333333333333\",\"condition\":\"A\",\"protocolVersion\":\"test\"}"
                    : "{\"ok\":true}";
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                context.Response.Close();
            }
        });
    }

    [UnityTest]
    public IEnumerator OfflineExit_ShouldRetainIdentityAndUploadOriginalSessionAfterReconnect()
    {
        var context = Activator.CreateInstance(Type.GetType("Backend.ResearchContext, Assembly-CSharp", true));
        Set(context, "participantId", "22222222-2222-4222-8222-222222222222");
        Set(context, "studyId", "33333333-3333-4333-8333-333333333333");
        Set(context, "condition", "A");
        Set(context, "protocolVersion", "test");
        Type.GetType("Backend.BackendSessionStore, Assembly-CSharp", true).GetMethod("SaveResearchContext").Invoke(null, new[] { context });
        Call(_client, "InitializeAuthorized", _settings, context);
        bool ended = false;
        Call(_client, "EndResearchSession", "offline_test", (Action)(() => ended = true));
        float deadline = Time.realtimeSinceStartup + 25;
        while (!ended && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(ended);
        yield return null;
        Assert.AreEqual("local-test-refresh", PlayerPrefs.GetString("Backend.RefreshToken"));
        Assert.IsTrue(PlayerPrefs.HasKey("Backend.PendingSessionEnd.v1"));
        Assert.IsTrue(PlayerPrefs.HasKey("Backend.PendingTelemetry"));
        string pendingEnd = PlayerPrefs.GetString("Backend.PendingSessionEnd.v1");
        StartServer();
        _client = (MonoBehaviour)new GameObject("Research reconnect test").AddComponent(Client);
        bool active = false;
        yield return (IEnumerator)Call(_client, "ActivateForResearch", _settings, "  qa-recover-01  ", (Action<bool, string>)((ok, error) => active = ok));
        Assert.IsTrue(active);
        Assert.IsFalse(PlayerPrefs.HasKey("Backend.PendingSessionEnd.v1"));
        Assert.IsTrue(Array.Exists(_payloads.ToArray(), body => body == pendingEnd), "The original session end payload must be acknowledged before starting a new session.");
        Assert.IsFalse(Array.Exists(_requests.ToArray(), path => path.Contains("signup")), "Recovery must reuse the bound identity.");
        Assert.IsTrue(Array.Exists(_payloads.ToArray(), body => body.Contains("QA-RECOVER-01")));
    }

    [UnityTest]
    public IEnumerator RefreshFailure_ShouldKeepBoundIdentityAndNeverCreateReplacementAnonymousUser()
    {
        StartServer(503);
        PlayerPrefs.SetString("Backend.AccessTokenExpiresAtUnix", "0");
        bool active = true;
        yield return (IEnumerator)Call(_client, "ActivateForResearch", _settings, "QA-REFRESH-01", (Action<bool, string>)((ok, error) => active = ok));
        Assert.IsFalse(active);
        Assert.AreEqual("local-test-refresh", PlayerPrefs.GetString("Backend.RefreshToken"));
        CollectionAssert.AreEqual(new[] { "/auth/v1/token" }, _requests.ToArray());
        bool ended = false;
        Call(_client, "EndResearchSession", "not_active", (Action)(() => ended = true));
        Assert.IsTrue(ended);
        Assert.AreEqual("local-test-refresh", PlayerPrefs.GetString("Backend.RefreshToken"));
    }

    [TearDown]
    public void TearDown()
    {
        if (_client != null) UnityEngine.Object.DestroyImmediate(_client.gameObject);
        if (_settings != null) UnityEngine.Object.DestroyImmediate(_settings);
        _server?.Close();
        foreach (string key in new[] { "Backend.PendingTelemetry", "Backend.PendingSessionEnd.v1", "Backend.PendingProgressSnapshot.v2", "Backend.CurrentCodeHash.v1", "Backend.CurrentCodeVerified.v1", "Backend.LastVerifiedCodeHash.v1", "Backend.AccessToken", "Backend.RefreshToken", "Backend.UserId", "Backend.AccessTokenExpiresAtUnix", "Backend.ResearchParticipantId", "Backend.ResearchStudyId", "Backend.ResearchCondition", "Backend.ResearchProtocolVersion" }) PlayerPrefs.DeleteKey(key);
    }
}
