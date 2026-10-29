namespace AerialWatch.Application.Common.Training;

/// <summary>One labelled object on a photo, in original image pixels (top-left origin). ClassId indexes the dataset's class names.</summary>
public sealed record TrainingLabel(int ClassId, double X, double Y, double Width, double Height);
