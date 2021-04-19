using Microsoft.Extensions.Logging.Abstractions;

using System;
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

        ConfigValueTests(ConfigFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void ReadGoodEnvGetExpected()
        {
            string key = "vaultName";
            string value = ConfigValues.GetValue(key);
            Assert.Equal("urlcheckerkvus", value);
        }

    }
}

public class ConfigFixture
{
    public ConfigFixture()
    {
        ConfigValues.Initialize(NullLogger.Instance);
    }
}