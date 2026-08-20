using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace smart_home.UnitTests.TestSupport;

internal static class ControllerTestHelper
{
    public static T WithUser<T>(this T controller, params Claim[] claims) where T : ControllerBase
    {
        var identity = new ClaimsIdentity(claims, "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
        return controller;
    }
}
