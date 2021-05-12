using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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
            this.fixture = fixture;
        }

        [Fact]
        public void ReadGoodEnv_GetExpected()
        {
            string key = "vaultName";
            string value = ConfigValues.GetValue(key);
            Assert.Equal("urlcheckerkvus", value);
        }

        [Fact]
        public void ReadBadEnv_GetEmpty()
        {
            string key = "invalidKey";
            string value = ConfigValues.GetValue(key);
            Assert.Equal("", value);
        }

        [Fact]
        public void ReadGoodSecret_GetExpected()
        {
            string key = "secret1";
            string value = ConfigValues.GetValue(key);
            Assert.Equal("ACTUALSECRETVALUE", value);
        }

        [Fact]
        public void ReadBadSecret_GetEmpty()
        {
            string key = "NotASecret";
            string value = ConfigValues.GetValue(key);
            Assert.Equal("", value);
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
        LoggerFacade.UseConsole();

        LoggerFacade.LogInformation("Starting ConfigFixture.");
        ReadSettingsIntoEnv();
        UrlChecker.LogEnvStrings();
        ConfigValues.Initialize();
    }

    // Read settings from local.settings.json into environment variables, to simulate
    // normal azure functions environment.
    private void ReadSettingsIntoEnv()
    {
        string settingsFile = @"..\..\..\..\func\local.settings.json";
        var text = File.ReadAllText(settingsFile);
        LoggerFacade.LogInformation($"text={text}");

        var values = JsonSerializer.Deserialize<LocalSettings>(text);

        foreach (var setting in values.Values)
        {
            // LoggerFacade.LogInformation($"key={setting.Key}, value={setting.Value}");
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
        // LoggerFacade.LogInformation("Finished ReadSettingsIntoEnv().\n");
    }
}
