# Example Graph Service Client Calls

## Hotmail or Outlook

```
static async Task<string> FetchAccessTokenFromHotmail()
{
	var deviceCodeCredentialOptions = new DeviceCodeCredentialOptions()
	{
		AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
		ClientId = "...",
		TenantId = "common",
		TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
		DeviceCodeCallback = (code, cancellation) =>
		{
			Console.WriteLine("Log into outlook.com");
			Console.WriteLine(code.Message);
			return Task.FromResult(0);
		},
	};

	var deviceCodeCredential = await FetchOrCacheAuthenticationRecord("hotmail_token_cache", deviceCodeCredentialOptions);
	var accessToken = await deviceCodeCredential.GetTokenAsync(new TokenRequestContext(["https://graph.microsoft.com/Mail.Read"]));
	return accessToken.Token;
}
```

```
static async Task<GraphServiceClient> BuildHotmailGraphServiceClient()
{
	var deviceCodeCredentialOptions = new DeviceCodeCredentialOptions()
	{
		AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
		ClientId = "...",
		TenantId = "common",
		TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
		DeviceCodeCallback = (code, cancellation) =>
		{
			Console.WriteLine("Log into outlook.com");
			Console.WriteLine(code.Message);
			return Task.FromResult(0);
		},
	};

	var deviceCodeCredential = await FetchOrCacheAuthenticationRecord("hotmail_token_cache", deviceCodeCredentialOptions);
	return new GraphServiceClient(deviceCodeCredential);
}
```

## Exchange Online

```
static async Task<GraphServiceClient> BuildOfficeGraphServiceClient()
{
	var deviceCodeCredentialOptions = new DeviceCodeCredentialOptions()
	{
		AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
		ClientId = "...",
		TenantId = "...",
		TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
		DeviceCodeCallback = (code, cancellation) =>
		{
			Console.WriteLine("Log into jwendl.net");
			Console.WriteLine(code.Message);
			return Task.FromResult(0);
		},
	};

	var deviceCodeCredential = await FetchOrCacheAuthenticationRecord("office_token_cache", deviceCodeCredentialOptions);
	return new GraphServiceClient(deviceCodeCredential);
}
```

```
static async Task<string> FetchAccessTokenFromOffice()
{
	var deviceCodeCredentialOptions = new DeviceCodeCredentialOptions()
	{
		AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
		ClientId = "...",
		TenantId = "...",
		TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
		DeviceCodeCallback = (code, cancellation) =>
		{
			Console.WriteLine("Log into jwendl.net");
			Console.WriteLine(code.Message);
			return Task.FromResult(0);
		},
	};

	var deviceCodeCredential = await FetchOrCacheAuthenticationRecord("office_token_cache", deviceCodeCredentialOptions);
	var accessToken = await deviceCodeCredential.GetTokenAsync(new TokenRequestContext(["https://graph.microsoft.com/Mail.Read"]));
	return accessToken.Token;
}
```


## General

```
static async Task<DeviceCodeCredential> FetchOrCacheAuthenticationRecord(string fileName, DeviceCodeCredentialOptions deviceCodeCredentialOptions)
{
	if (File.Exists(fileName))
	{
		var cachedTokenBytes = await File.ReadAllBytesAsync(fileName);
		using var cachedFileMemoryStream = new MemoryStream(cachedTokenBytes);
		var authenticationRecord = await AuthenticationRecord.DeserializeAsync(cachedFileMemoryStream);
		if (authenticationRecord != null)
		{
			deviceCodeCredentialOptions.AuthenticationRecord = authenticationRecord;
			return new DeviceCodeCredential(deviceCodeCredentialOptions);
		}
	}

	var scopes = new List<string>()
	{
		"https://graph.microsoft.com/Mail.ReadWrite",
	};

	var tokenRequestContext = new TokenRequestContext([.. scopes]);
	var deviceCodeCredential = new DeviceCodeCredential(deviceCodeCredentialOptions);
	var authenticationRecordResult = await deviceCodeCredential.AuthenticateAsync(tokenRequestContext);

	using var authenticationResultMemoryStream = new MemoryStream();
	await authenticationRecordResult.SerializeAsync(authenticationResultMemoryStream);
	await File.WriteAllBytesAsync(fileName, authenticationResultMemoryStream.ToArray());

	return deviceCodeCredential;
}
```
