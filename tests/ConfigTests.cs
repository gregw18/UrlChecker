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
    [CollectionDefinition("Serial Collection", DisableParallelization = true)]
    [Collection("Serial Collection")]
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

        // First target should exist by default.
        [Fact]
        public void FirstTarget_Exists()
        {
            TargetTextData target1 = ConfigValues.GetTarget(0);
            Assert.True(target1.targetUrl.Length > 0);
            Assert.Equal(1, ConfigValues.GetNumberOfTargets());
        }

        // If add second target, values should match
        [Fact]
        public void SecondTarget_Exists()
        {
            TargetTextData target2 = new TargetTextData("http://www.url2.gov", "targetText2", 10, 100);
            fixture.AddTarget(target2);
            Assert.Equal(2, ConfigValues.GetNumberOfTargets());
            Assert.True(target2.AreValuesSame(ConfigValues.GetTarget(1)));
            fixture.RemoveLastTarget();
        }

        // If add third target, values should match.
        [Fact]
        public void ThirdTarget_Exists()
        {
            TargetTextData target2 = new TargetTextData("http://www.url2.ca", "targetText2", 10, 100);
            fixture.AddTarget(target2);
            TargetTextData target3 = new TargetTextData("http://www.url3.com", "target2", 20, 30);
            fixture.AddTarget(target3);
            Assert.Equal(3, ConfigValues.GetNumberOfTargets());
            Assert.True(target2.AreValuesSame(ConfigValues.GetTarget(1)));
            Assert.True(target3.AreValuesSame(ConfigValues.GetTarget(2)));
            fixture.RemoveLastTarget();
            fixture.RemoveLastTarget();
        }

        // If add second target, but ask for third, should get null back.
        [Fact]
        public void AddTwoTargetsRequestThird_Fails()
        {
            TargetTextData target2 = new TargetTextData("http://www.url2.ca", "targetText2", 10, 100);
            fixture.AddTarget(target2);
            Assert.Equal(2, ConfigValues.GetNumberOfTargets());
            Assert.True(ConfigValues.GetTarget(2) is null);
            fixture.RemoveLastTarget();
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
    private List<string> dontSave = new List<string>()
    {
        "webSiteUrl",
        "targetText",
        "targetTextOffset",
        "targetTextLength"
    };

    public ConfigFixture()
    {
        LoggerFacade.UseConsole();

        LoggerFacade.LogInformation("Starting ConfigFixture.");
        ReadSettingsIntoEnv();

        // Put in one known target.
        TargetTextData target1 = new TargetTextData("https://www.canada.ca/en/public-health/services/diseases/2019-novel-coronavirus-infection/prevention-risks/covid-19-vaccine-treatment/vaccine-rollout.html", 
                                                    "dateModified", 2, 10);
        AddTarget(target1);

        UrlChecker.LogEnvStrings();
        ConfigValues.Initialize();
        EnsureHaveOneTarget();
    }

    // Read settings from local.settings.json into environment variables, to simulate
    // normal azure functions environment.
    // However, don't write any target data, as tests need to specify it.
    private void ReadSettingsIntoEnv()
    {
        string settingsFile = @"..\..\..\..\func\local.settings.json";
        var text = File.ReadAllText(settingsFile);
        LoggerFacade.LogInformation($"text={text}");

        var values = JsonSerializer.Deserialize<LocalSettings>(text);

        foreach (var setting in values.Values)
        {
            // LoggerFacade.LogInformation($"key={setting.Key}, value={setting.Value}");
            if (SaveSetting(setting.Key))
            {
                Environment.SetEnvironmentVariable(setting.Key, setting.Value);
            }
        }
        // LoggerFacade.LogInformation("Finished ReadSettingsIntoEnv().\n");
    }

    // If setting name matches anything in dontSave list, don't want to add it
    // to the environment.
    private bool SaveSetting(string settingName)
    {
        bool saveIt = true;
        foreach (string name in dontSave)
        {
            if (settingName.StartsWith(name))
            { 
                saveIt = false;
                break;
            }
        }

        return saveIt;
    }

    // Add another target to the environment, for testing multiple sites.
    public void AddTarget(TargetTextData myTarget)
    {
        int index = ConfigValues.GetNumberOfTargets();
        string urlKey = "webSiteUrl" + index.ToString().Trim();
        string labelKey = "targetText" + index.ToString().Trim();
        string offsetKey = "targetTextOffset" + index.ToString().Trim();
        string lengthKey = "targetTextLength" + index.ToString().Trim();
        Environment.SetEnvironmentVariable(urlKey, myTarget.targetUrl);
        Environment.SetEnvironmentVariable(labelKey, myTarget.targetLabel);
        Environment.SetEnvironmentVariable(offsetKey, myTarget.targetOffset.ToString());
        Environment.SetEnvironmentVariable(lengthKey, myTarget.targetLength.ToString());
        ConfigValues.Reinitialize();
    }

    // Remove the last target in the current list.
    public void RemoveLastTarget()
    {
        int lastIndex = ConfigValues.GetNumberOfTargets() - 1;
        string urlKey = "webSiteUrl" + lastIndex.ToString().Trim();
        string labelKey = "targetText" + lastIndex.ToString().Trim();
        string offsetKey = "targetTextOffset" + lastIndex.ToString().Trim();
        string lengthKey = "targetTextLength" + lastIndex.ToString().Trim();
        Environment.SetEnvironmentVariable(urlKey, null);
        Environment.SetEnvironmentVariable(labelKey, null);
        Environment.SetEnvironmentVariable(offsetKey, null);
        Environment.SetEnvironmentVariable(lengthKey, null);
        ConfigValues.Reinitialize();
    }

    // Remove all targets except first.
    private void EnsureHaveOneTarget()
    {
        int numTargets = ConfigValues.GetNumberOfTargets();
        if (numTargets > 1)
        {
            for (int i = numTargets; i > 1; i--)
            {
                RemoveLastTarget();
            }
            ConfigValues.Reinitialize();
        }
    }
}
