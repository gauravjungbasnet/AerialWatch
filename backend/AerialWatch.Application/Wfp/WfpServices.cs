using Microsoft.Extensions.DependencyInjection;
using AerialWatch.Application.Wfp.Estimates;
using AerialWatch.Application.Wfp.ModelInfo;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Application.Wfp.Planner;
using AerialWatch.Application.Wfp.Results;
using AerialWatch.Application.Wfp.Review;
using AerialWatch.Application.Wfp.SurveyImages;
using AerialWatch.Application.Wfp.Surveys;
using AerialWatch.Application.Wfp.Training;
using AerialWatch.Application.Wfp.TwinCheck;
using AerialWatch.Contracts.Wfp.Estimates.Services;
using AerialWatch.Contracts.Wfp.ModelInfo.Services;
using AerialWatch.Contracts.Wfp.ModelParameters.Services;
using AerialWatch.Contracts.Wfp.Planner.Services;
using AerialWatch.Contracts.Wfp.Results.Services;
using AerialWatch.Contracts.Wfp.Review.Services;
using AerialWatch.Contracts.Wfp.SurveyImages.Services;
using AerialWatch.Contracts.Wfp.Surveys.Services;
using AerialWatch.Contracts.Wfp.Training.Services;
using AerialWatch.Contracts.Wfp.TwinCheck.Services;

namespace AerialWatch.Application.Wfp;

/// <summary>Watch Feral Pigs: every service interface (Contracts) mapped to its implementation.</summary>
public static class WfpServices
{
    public static IServiceCollection AddWfpServices(this IServiceCollection services)
    {
        // Scoped = one instance per HTTP request, like the DbContext the repositories use.
        services.AddScoped<IActiveModelParameters, ActiveModelParameters>();
        services.AddScoped<IModelParametersService, ModelParametersService>();
        services.AddScoped<IPlannerService, PlannerService>();
        services.AddScoped<IEstimateService, EstimateService>();
        services.AddScoped<IModelInfoService, ModelInfoService>();
        services.AddScoped<ISurveyService, SurveyService>();
        services.AddScoped<ISurveyImageService, SurveyImageService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ITrainingDataService, TrainingDataService>(); // AI training data (D-038)
        services.AddScoped<ISurveyResultsService, SurveyResultsService>(); // Results + map (Slice 5)
        services.AddScoped<ITwinCheckService, TwinCheckService>(); // thermal/colour twin check (D-040)
        return services;
    }
}
