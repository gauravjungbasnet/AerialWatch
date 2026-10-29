using AerialWatch.Domain.Common;

namespace AerialWatch.Domain.Wfp.ResearchModel;

public sealed record TimeWindowMatch(TimeWindow? Window, bool InsideStudyHours, Assumption? Note);
