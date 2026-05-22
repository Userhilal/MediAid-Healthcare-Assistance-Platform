using MediAid.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace MediAid.Filters;

public class LayoutDataFilter : IAsyncActionFilter
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<LayoutDataFilter> _logger;

    public LayoutDataFilter(
        INotificationService notificationService,
        ILogger<LayoutDataFilter> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrWhiteSpace(userId) && context.Controller is Microsoft.AspNetCore.Mvc.Controller controller)
            {
                try
                {
                    var unreadNotifications = await _notificationService.GetNotificationsByUserIdAsync(userId, unreadOnly: true);

                    controller.ViewBag.UnreadNotifications = unreadNotifications.Count;
                    controller.ViewBag.CurrentUserRole = user.FindFirst(ClaimTypes.Role)?.Value;
                    controller.ViewBag.CurrentUserId = userId;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to load layout notification data.");
                    controller.ViewBag.UnreadNotifications = 0;
                }
            }
        }

        await next();
    }
}
