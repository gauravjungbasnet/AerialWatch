using AerialWatch.Contracts.Wfp.Estimates.Models;
using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Contracts.Wfp.Results.Models;

/// <summary>
/// Survey results (Slice 5): confirmed count, corrected estimate with range, AI vs ranger counts and photo locations.
/// Provisional until the review is complete. <see cref="Estimate"/> is null when the research model can't be applied
/// (for example a night flight); <see cref="EstimateUnavailableReason"/> then says why.
/// </summary>
public sealed record SurveyResultsDto(
    Guid SurveyId,
    SurveyStatus Status,
    bool Provisional,
    string? ProvisionalReason,
    ResultCountsDto Counts,
    EstimateResponse? Estimate,
    string? EstimateUnavailableReason,
    IReadOnlyList<PhotoPointDto> Photos,
    IReadOnlyList<Assumption> Notes);
