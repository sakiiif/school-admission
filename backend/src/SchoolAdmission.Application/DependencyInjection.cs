using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Application.Services;

namespace SchoolAdmission.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services, AdmissionOptions options)
    {
        if (options.HoldDurationSeconds <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(options), "HOLD_DURATION_SECONDS must be greater than zero.");

        services.AddSingleton(options);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IHoldService, HoldService>();

        return services;
    }
}