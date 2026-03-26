using ClawMailCalCli.Models;
using ClawMailCalCli.Services;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="TenantDefaults"/>.
/// </summary>
[Trait("Category", "Unit")]
public class TenantDefaultsTests
{
	[Fact]
	public void GetDefaultTenantId_WhenPersonalAccount_ReturnsConsumers()
	{
		// Act
		var tenantId = TenantDefaults.GetDefaultTenantId(AccountType.Personal);

		// Assert
		tenantId.Should().Be("consumers");
	}

	[Fact]
	public void GetDefaultTenantId_WhenWorkAccount_ReturnsOrganizations()
	{
		// Act
		var tenantId = TenantDefaults.GetDefaultTenantId(AccountType.Work);

		// Assert
		tenantId.Should().Be("organizations");
	}

	[Fact]
	public void GetDefaultTenantId_WhenUnknownAccountType_ThrowsInvalidOperationException()
	{
		// Act
		var act = () => TenantDefaults.GetDefaultTenantId((AccountType)999);

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*999*");
	}
}
