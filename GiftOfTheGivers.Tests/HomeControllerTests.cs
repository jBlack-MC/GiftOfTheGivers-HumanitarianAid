using System.Reflection;
using GiftOfTheGivers.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GiftOfTheGivers.Tests;

public class HomeControllerTests
{
	[Fact]
	public void Confirmation_AllowsAnonymousUsersAndRendersTempDataReceipt()
	{
		var httpContext = new DefaultHttpContext();
		var controller = new HomeController(null!, null!, null!, null!, NullLogger<HomeController>.Instance)
		{
			ControllerContext = new ControllerContext { HttpContext = httpContext },
			TempData = new TempDataDictionary(httpContext, new ReceiptTempDataProvider())
		};

		var result = Assert.IsType<ViewResult>(controller.Confirmation());
		var action = typeof(HomeController).GetMethod(nameof(HomeController.Confirmation));

		Assert.Equal("DonationSuccess", result.ViewName);
		Assert.NotNull(action?.GetCustomAttribute<AllowAnonymousAttribute>());
		Assert.Equal(25m, (decimal)controller.ViewBag.Amount);
	}

	private sealed class ReceiptTempDataProvider : ITempDataProvider
	{
		public IDictionary<string, object> LoadTempData(HttpContext context) =>
			new Dictionary<string, object> { ["Amount"] = 25m };

		public void SaveTempData(HttpContext context, IDictionary<string, object> values)
		{
		}
	}
}
