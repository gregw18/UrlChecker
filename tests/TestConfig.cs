using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Collections.Generic;
//using System.Environment;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using GAWUrlChecker;

namespace tests
{
    // Test that am accessing config values correctly.
    // Get expected value for good env, empty for bad env.
    // Get expected value for good secret, empty for bad secret.
    // Need separate class to populate the env, based on local.settings.json,
    // so that can access config values in other tests.
    public class ConfigValueTests : IClassFixture<ConfigFixture>
    {

        ConfigFixture fixture;

        public ConfigValueTests(ConfigFixture fixture)
        {
            Console.WriteLine("Starting COnfigValueTests ctor");
            this.fixture = fixture;
            Console.WriteLine("Finished COnfigValueTests ctor");
        }

        [Fact]
        public void ReadGoodEnvGetExpected()
        {
            string key = "vaultName";
            Console.WriteLine("About to call getvalue");
            string value = ConfigValues.GetValue(key);
            Console.WriteLine($"Called GetValue, value={value}");
            Assert.Equal("urlcheckerkvus", value);
        }

    }
}


class LocalSettings
{
    public bool IsEncrypted {get; set; }
    public Dictionary<string, string> Values {get; set; }
}

public class ConfigFixture
{
    public ConfigFixture()
    {
        Console.WriteLine("Starting ConfigFixture.");
        ReadSettingsIntoEnv();
        TimerTriggerCSharp1.LogEnvStrings();
        ConfigValues.Initialize(NullLogger.Instance);
    }

    // Read settings from local.settings.json into environment variables, to simulate
    // normal azure functions environment.
    private void ReadSettingsIntoEnv()
    {
        //private Dictionary<string, string> Values;

        Console.WriteLine("Starting ReadSettingsIntoEnv().");
        string settingsFile = @"..\..\..\..\func\local.settings.json";
        var text = File.ReadAllText(settingsFile);
        //EnvironmentVariableTarget settings = JsonConvert.DeserializeObject<LocalSettings>(
        //    File.ReadAllText(settingsFile));
        Console.WriteLine($"text={text}");

        //var values = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
        var values = JsonSerializer.Deserialize<LocalSettings>(text);

        //Console.WriteLine($"values.Count={values.Count}.");
        foreach (var setting in values.Values)
        {
            Console.WriteLine($"key={setting.Key}, value={setting.Value}");
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
        Console.WriteLine("Finished ReadSettingsIntoEnv().\n");
    }
}
