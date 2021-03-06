# URL Checker
This is a project that I put together to monitor Canada's vaccine delivery web page (https://www.canada.ca/en/public-health/services/diseases/2019-novel-coronavirus-infection/prevention-risks/covid-19-vaccine-treatment/vaccine-rollout.html) for changes so that I could track whether suppliers were delivering on time, and whether delivery dates were being met or constantly adjusted. (Summary - Pfizer/BioNTech are amazing, Moderna is really struggling.) I've tried to generalize it so that it can monitor any static web page for changes, and also to monitor multiple pages. When a change is detected, an email notification is sent.

This C#/.NET Core code, intended to run on a timer as a Microsoft Azure Function, allows you configure a list of urls to check and sends an email (via AWS SNS) when one or more have changed. It uses an Azure Key Vault to store the SNS credentials and an Azure Files share to store the previous values for the tracked web page elements.

It monitors each page by watching a specific, configured element. I.e. for my original target, there is a "dateModified" element near the bottom of the page that is updated whenever the page is changed. The program retrieves the full text of the page, finds the last instance of the hardcoded text "dateModified", then looks for the changed value starting two characters after the end of the hardcoded text, for the next ten characters. It compares this value to the saved previous value, to see if it changed.

The url to retrieve, the target text that is searched for, the offset from the end of that text to the start of the changing value and the length of the changing value are all configurable, for as many web pages as desired. See example below for details.

This program only works with static web pages - the text that you want to monitor has to be populated when the page is first retrieved. I've included proof of concept code for parsing dynamic pages but it uses Selenium and headless Chrome, which I didn't want to try installing under Azure Functions.

This was a project intended to give me a bit of experience with C# and Microsoft Azure, while also accessing AWS - since Azure doesn't have any built-in email functionality. Other than storing the AWS credentials securely, accessing the AWS service from Azure was surprisingly easy (and the first bills don't indicate any "hey - what are you using those other guys for?" type charges.) When running twice a day, monitoring two urls, this function appears to be sneaking under the billable thresholds for both providers.

## Getting Started
As a cloud-based project, using both Azure and AWS, getting this running requires a lot of setup and configuration. I assume that you already have Azure and AWS accounts, with a default storage account for Azure, and the CLIs for both installed. Having the Azure Functions tools installed is also recommended. There are batch files for creating some of the required resources while others are created programmatically, but some still need to be created manually. There is also quite a bit of configuration required - sample files are provided.
1. Decide on some names and locations. You need to choose names for your function, the function app, the resource group for the function and the key vault to use with the function. You also need to choose which Azure location you want everything to run in. (You can put different resources in different locations, but would have to manually edit the batch files - they assume that everything is set up in one.) The function name must be globally unique. Mine is UrlCheckerGAW, so please select something else! The rest of the names just need to be unique within your Azure subscription. Once decided, copy batch.sample.cfg to batch.cfg and place put your names in batch.cfg. Please do not put any extra spaces in this file, or use names with special DOS characters, such as &, %, ), <, > or |.
2. Decide on the storage account to use. I used the default for my subscription, as there was no sensitive information in it, but you may want to create a new one depending on what is in yours. Since I was using the default, I had to use the its full id - if you create one and assign it to the resource group for the function app you can just use its name. Place either the name or the id, as necessary, in batch.cfg as the "storageAccount". To get the id of a storage account, go to the portal, select the storage account, go to settings\endpoints and copy the "Storage account resource ID".
3. If you haven't already logged in to Azure, use 00-login.bat to do so. 
4. Run 01-keyvault.bat to create a resource group for the function, the key vault and the function app.
5. Copy the sample local.settings.sample.json in the func directory to local.settings.json and update the vaultName in func\local.settings.json to your chosen name for the key vault.
6. Configure the access key for your storage account. Go to the portal for your chosen storage account and select "Access keys" under the "Security + networking" section, copy one of the connection strings (they seem to start with "DefaultEndpointsProtocol") and copy that into the "AzureWebJobsStorage" entry of local.settings.json. Then, set the path to the file that will be used to store the previous values - the shareName, dirName and lastChangedFileName. Note that the share, directory and file will all be automatically created if they don't already exist, when the function first runs.
7. While in local.settings.json, you may as well also set your AWS region in the awsRegionName setting and the name of the SNS topic to publish to, in snsTopic. (The topic will be automatically created if necessary.)
8. Configure the web site(s) that you want to monitor, again in local.settings.json. For the first site, set the webSiteUrl0, targetText0, targetTextOffset0 and targetTextLength0 entries. The targetText is the fixed (i.e. unchanging) text used to locate the text that changes, targetTextOffset is the number of characters from the end of the target text to the beginning of the text that changes and the targetTextLength is the number of characters to monitor from that position. 

	For example, the following snippet is from the page that I am tracking:

    	\<dd>\<time property="dateModified">2021-04-21\</time>\</dd>

	The settings that I use to track the date are:

    	"targetText0": "dateModified",
    	"targetTextOffset0": "2",
    	"targetTextLength0": "10",

	The corresponding settings for the second url end with 1 rather than 0, and you can add as many more sets as you like, with increasing indexes. (The parser stops reading when it hits the first webSiteUrl? entry that doesn't exist.)

	getHtml.ps1 is a PowerShell script that can be used to retrieve the static results from a web page and save them in a text file, to look for targets near text that changes. Set the URL in the file and change the name of the file to save the results in (the default is html.txt) then run the script. By default Windows will no longer let you run a PowerShell script from a normal command line, but you can enable it - see (https://superuser.com/questions/106360/how-to-enable-execution-of-powershell-scripts).

9. Publish the function to Azure by running 02-publish.bat.
10. You then need to create an identity for the group/function and give that identity access to the key vault. 03-keyvaultid.bat will create an identify for the function, but, in April, 2021 when I tried it, the az keyvault role command to give the identify access to the key vault wasn't functioning (it gave "unable to establish connection" errors), so I had to do it through the Azure console. Use the steps in https://docs.microsoft.com/en-us/azure/key-vault/general/assign-access-policy-portal. Once in the "Add Access Policy" page, select the first seven "secret permissions", then select "Select Principal". Enter the first part of your function app name to filter the options and select your function app. Leave the "Authorized application" setting as "None selected" and hit the "Add" button. Then hit the "Save" button!
11. Create a new AWS user for this program and only give it the required access - AmazonSNSFullAccess. I don't recommend using an existing user. Save the associated AccessKeyId and SecretAccessKey.
12. Store the AWS secrets in the key vault - the aws AccessKeyId and aws SecretAccessKey for the user that you just created. You can either do this manually through the portal or using the provided 04-setsecrets.bat batch file. Since you should never store secrets in GitHub, and so also shouldn't include them in the project (unless you have a way of guaranteeing that you will never accidentally add them to GitHub), you should move 04-setsecrets.bat out of your project structure and modify that copy. First, set the path to readcfg.bat and batch.cfg so that they can be found. Then change the awsAccessKeyId and awsSecretAccessKeyId to the values for your new aws user. Finally run 04-setsecrets.bat (the version that contains your actual secrets.) If setting the secrets through the portal, see the batch file for the secret names - note that "secret1" is used for testing.
13. The last piece of configuration is to adjust the schedule that the function runs on. This is set by a cron expression, in UrlChecker.Run, in UrlChecker.cs. Note that the time is UTC and the function is currently configured to run at 10am and 4pm UTC. You can change it to anything that your cron skills can conjure - bearing in mind that if the function runs too frequently it could escape the free tier.
14. The project should be ready to run. If you have all the expected tools locally installed, you should be able to run the function locally. Start the project in your development environment and, if it doesn't automatically run (it usually will run automatically the first time it starts up on a given day, as it seems to recognize that it hasn't run yet today, but it should have - depending on when it is scheduled to run), use runLocal.bat to trigger it. It doesn't return any values, but you should be able to see the logging output in your development environment.
15. Even though the AWS SNS topic that you configured will automatically be created if it doesn't already exist, no-one will know whether anything is published to it unless they subscribe to it. This can be done in various ways, but I've included snsSubscribe.sample.bat that you can modify to include your aws account number, topic name, aws region and the email address that you want notifications sent to. Rename it to snsSubscribe.bat, put in your desired values ("aws sns list-topics" will show all your aws topics, with the arn in the format needed here) and then run it. Again, I recommend against including the modified file in GitHub, due to the private information that it contains. Note that the topic has to be created before you can subscribe to it, which is why this step comes after running the function locally.
16. Once you can get it to run locally, and have published the function again if you had to make any changes (using 02-publish.bat), you can try running it on Azure. To trigger it you could modify the times that it is scheduled to run, or you can manually trigger it using runAzure.bat. Copy the provided runAzure.Sample.bat to runAzure.bat and paste your function's _master app key (which can be found in the portal for the Function App under Functions\App keys\_master) into the "<your _master App key goes here>" section, being sure to also overwrite the <>. Then, you should be able to trigger the function by running runAzure.bat. If you have run the function locally, the previous values file likely contains the current values, so you are unlikely to receive a notification. You could manually edit that file in the Azure portal to ensure that there is a discrepancy, or you could view the logs for the function to confirm that it ran successfully. Again, I recommend against adding the modified runAzure.bat file to GitHub, as it now contains secret information that should not be published.

## Development Environment
I used VS Code under Windows 10 as my development environment, with the following extensions:

	.Net Core Test Explorer
	AWS Toolkit
	Azure Account
	Azure Functions
	Azure Resources
	C#
	NuGet Package Manager


Prerequisites

	.NET Core 5.0
	C# 8
	Azure cli
	Azure Functions Core Tools (https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=windows%2Ccsharp%2Cbash)
	AWS cli
	Azure subscription
	AWS account

Authors

Greg Walker - Initial work - (https://github.com/gregw18)


License

MIT