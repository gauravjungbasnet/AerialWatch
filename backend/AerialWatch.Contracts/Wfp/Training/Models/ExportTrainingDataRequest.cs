using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.Training.Models;

/// <summary>Export one camera's tagged surveys as a YOLO dataset; thermal and RGB are never mixed (D-038 rule R5).</summary>
public sealed record ExportTrainingDataRequest(CameraType Camera);
