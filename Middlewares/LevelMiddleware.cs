using Tracker.Services;

namespace Tracker.Middlewares
{
    public class LevelMiddleware
    {
        private readonly RequestDelegate _next;

        public LevelMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, LevelService levelService)
        {
            if (context.User.Identity!.IsAuthenticated)
            {
                int? level = await levelService.GetLevelAsync();
                context.Items["UserLevel"] = level;
            }

            await _next.Invoke(context);
        }
    }
}
