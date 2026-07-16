using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SqlSugar;

namespace Fantasy.ControlCenter.Controllers;

internal sealed class ConfigurationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var statusCode = context.Exception switch
        {
            ArgumentException or InvalidOperationException => StatusCodes.Status400BadRequest,
            SqlSugarException or Microsoft.Data.Sqlite.SqliteException => StatusCodes.Status409Conflict,
            _ => 0
        };

        if (statusCode == 0)
        {
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Detail = context.Exception.Message
        })
        {
            StatusCode = statusCode
        };
        context.ExceptionHandled = true;
    }
}
