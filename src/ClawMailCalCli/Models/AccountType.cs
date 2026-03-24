namespace ClawMailCalCli.Models;

/// <summary>
/// Represents the type of a Microsoft account.
/// </summary>
public enum AccountType
{
	/// <summary>Personal account (Hotmail, Outlook.com). Uses TenantId "common".</summary>
	Personal,

	/// <summary>Work or school account (Exchange Online). Uses an organisation-specific TenantId.</summary>
	Work,
}
