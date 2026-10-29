namespace AerialWatch.Contracts.Wfp.Surveys.Models;

/// <summary>Totals for the dashboard cards, counted in SQL (not from a page of the list).</summary>
public sealed record SurveyStatsDto(int Surveys, int AwaitingReview, int Photos);
