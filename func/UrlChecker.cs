using System;
using System.Collections;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

// using OpenQA.Selenium;
// using OpenQA.Selenium.Chrome;
// using OpenQA.Selenium.Support.UI;
// using SeleniumExtras.WaitHelpers;


// Azure function, timer triggered, that checks whether a given web page has changed
// since the last time it was checked. If yes, sends an email to that effect.
// Uses configured "target" string near the text that is expected to change on the page
// to tell whether the page has changed. Uses AWS SNS to send the email, as Azure doesn't
// appear to have service for sending emails, and I thought it would be interesting to see
// what it takes to get Azure and AWS to work together.
// Automatically publishes local settings to the azure function, for most configuration,
// but also stores some secrets in an Azure Key Vault - currently the AWS access keys.
// Stores "previous" values for each website in an Azure Storage file share.
// Only works with static web pages.

namespace GAWUrlChecker
{
    public static class UrlChecker
    {
        // Timer runs at 5am and 11am every day, eastern standard
        // (Cron expression uses UTC.)
        [FunctionName("UrlChecker")]
        public static async Task Run([TimerTrigger("0 0 10,16 * * *")]TimerInfo myTimer, 
                                ILogger log)
        {
            try
            {
                LoggerFacade.UseILogger(log);
                LoggerFacade.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                // LogEnvStrings();

                string previousContentsFileName = ConfigValues.GetValue("lastChangedFileName");
                var myChecker = new UrlCheckManager();
                bool result = await myChecker.HaveAnyPagesChanged(previousContentsFileName);
                // TestDynamicScraping();
                LoggerFacade.LogInformation("Finished Run.");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in UrlChecker.Run.");
            }
        }

        /* 
        // Proof of concept, using WebDriver and headless chrome to do scraping of dynamic web page.
        // Investigated because my second desired target site, while externally identical in layout
        // to the first site, incorporates a lot more dynamic data, including the "dateModified" field
        // that I was targeting.
        // Ended up not using because doesn't appear to be easy way to install chrome in a windows-based
        // azure function environment, and it turned out that the second target site updates once a week
        // at a defined time, so I don't really need to track it.
        // It does seem that could install chrome in a linux environment to get this to work, but not
        // worth the effort at this time. https://anthonychu.ca/post/azure-functions-headless-chromium-puppeteer-playwright/
        private static void TestDynamicScraping()
        {
                string url = "https://health-infobase.canada.ca/covid-19/vaccination-coverage/";
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("headless");
                using (IWebDriver driver = new ChromeDriver(options))
                {
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    driver.Navigate().GoToUrl(url);
                    var source = driver.PageSource;
                    // WebDriverWait wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    var pathElement = driver.FindElement(By.ClassName("dateModified"));
                    LoggerFacade.LogInformation($"className={pathElement}");
                    //var newelem = pathElement.GetAttribute("dateModified");
                    //LoggerFacade.LogInformation($"attribute newelem={newelem}");
                    //var newelem = pathElement.GetProperty("dateModified");
                    //LoggerFacade.LogInformation($"property newelem={newelem}");
                    var stuff = pathElement.Text;
                    LoggerFacade.LogInformation($"dateModified={stuff}");
                    //#var newElem = pathElement.FindElement(By.Name("dateModified"));
                    //LoggerFacade.LogInformation($"source={source}");
                    //var pathElement = driver.FindElement(By.Name("dateModified"));
                    //LoggerFacade.LogInformation($"dm.dm={pathElement}");
                    //var pathElement = driver.FindElement(By.Id("wb-dtmd"));
                    //LoggerFacade.LogInformation($"wb-dtmd={pathElement}");
                    //#pathElement = driver.FindElement(By.CssSelector("#dateModified #dateModified"));
                    //#LoggerFacade.LogInformation($"#dm #dm={pathElement}");
                }
        }
        */

        // Write all environment variables to the log.
        // Not a good idea if worried about others seeing these!
        public static void LogEnvStrings()
        {
            var envStrings = System.Environment.GetEnvironmentVariables();
            var sortedEnv = new SortedList(envStrings);
            LoggerFacade.LogInformation("\nEnvironment variables");
            foreach (string s in sortedEnv.Keys)
            {
                LoggerFacade.LogInformation( $"key: {s}, value:{envStrings[s]}");
            }
            LoggerFacade.LogInformation("--------\n");
        }
    }
}
