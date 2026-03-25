using CrewBot.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace CrewBot.Tests;

public class JsonFileStoreTests : IDisposable
{
    private readonly string _root;
    private readonly JsonFileStore _store = new JsonFileStore();

    public JsonFileStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "CrewBotTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void LoadOrDefault_WhenFileMissing_CreatesFileAndReturnsDefault()
    {
        string path = Path.Combine(_root, "missing", "config.json");
        var fallback = new BotConfig { Prefix = "+" };

        BotConfig loaded = _store.LoadOrDefault(path, fallback);

        Assert.True(File.Exists(path));
        Assert.Equal("+", loaded.Prefix);
    }

    [Fact]
    public void LoadOrDefault_WhenInvalidJson_ReturnsDefaultAndPreservesOriginalFile()
    {
        string path = Path.Combine(_root, "invalid.json");
        File.WriteAllText(path, "not-json");

        var fallback = new ConcurrentDictionary<string, string>();
        fallback.TryAdd("hello", "world");

        ConcurrentDictionary<string, string> loaded = _store.LoadOrDefault(path, fallback);

        Assert.True(loaded.ContainsKey("hello"));
        Assert.Equal("not-json", File.ReadAllText(path));
        Assert.True(File.Exists($"{path}.bak"));
        Assert.Equal("not-json", File.ReadAllText($"{path}.bak"));
    }

    [Fact]
    public void Save_ThenLoad_PreservesValues()
    {
        string path = Path.Combine(_root, "list.json");
        var expected = new List<ulong> { 10, 20, 30 };

        _store.Save(path, expected);
        List<ulong> loaded = _store.LoadOrDefault(path, new List<ulong>());

        Assert.Equal(expected, loaded);
    }

    [Fact]
    public void Save_DoesNotLeaveTempFileBehind()
    {
        string path = Path.Combine(_root, "atomic.json");

        _store.Save(path, new BotConfig { Prefix = "!" });

        Assert.True(File.Exists(path));
        Assert.False(File.Exists($"{path}.tmp"));
    }

    [Fact]
    public void Save_WhenReplacingExistingFile_CreatesBackupOfPreviousContents()
    {
        string path = Path.Combine(_root, "replace.json");
        File.WriteAllText(path, "{\"Prefix\":\"+\"}");

        _store.Save(path, new BotConfig { Prefix = "!" });

        Assert.True(File.Exists($"{path}.bak"));
        Assert.Contains("\"Prefix\":\"+\"", File.ReadAllText($"{path}.bak"));
        Assert.Contains("\"Prefix\":\"!\"", File.ReadAllText(path));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }
        catch
        {
            // Ignore temp cleanup failures in tests.
        }
    }
}
