using Microsoft.Extensions.DependencyInjection;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Application.Wfp.Review;
using AerialWatch.Application.Wfp.SurveyImages;
using AerialWatch.Application.Wfp.Surveys;
using AerialWatch.Application.Wfp.Training;
using AerialWatch.Application.Wfp.TwinCheck;
using AerialWatch.Infrastructure.Persistence.Wfp.Repositories;

namespace AerialWatch.Infrastructure.Persistence.Wfp;

/// <summary>Watch Feral Pigs: every repository interface (Application) mapped to its SQL Server implementation.</summary>
public static class WfpRepositories
{
    public static IServiceCollection AddWfpRepositories(this IServiceCollection services)
    {
        services.AddScoped<ISurveyRepository, SurveyRepository>();
        services.AddScoped<ISurveyImageRepository, SurveyImageRepository>();
        services.AddScoped<IDetectionRepository, DetectionRepository>();
        services.AddScoped<ICoefficientTypeRepository, CoefficientTypeRepository>();
        services.AddScoped<IModelCoefficientSetRepository, ModelCoefficientSetRepository>();
        services.AddScoped<ITrainingDataRepository, TrainingDataRepository>(); // AI training data (D-038)
        services.AddScoped<ITwinMarkRepository, TwinMarkRepository>(); // thermal/colour twin check (D-040)
        return services;
    }
}
