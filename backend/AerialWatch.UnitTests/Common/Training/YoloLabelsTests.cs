using Xunit;
using AerialWatch.Application.Common.Training;
using AerialWatch.Domain.Common.Training;

namespace AerialWatch.UnitTests.Common.Training;

/// <summary>YOLO label maths and files (D-038). Photo 640 × 512.</summary>
public class YoloLabelsTests
{
    [Fact]
    public void Line_is_class_then_centre_and_size_relative_to_the_photo()
    {
        var line = YoloLabels.Line(new TrainingLabel(0, X: 64, Y: 128, Width: 32, Height: 64), 640, 512);
        Assert.Equal("0 0.125000 0.312500 0.050000 0.125000", line);
    }

    [Fact]
    public void Box_partly_outside_the_photo_is_clamped()
    {
        var line = YoloLabels.Line(new TrainingLabel(0, X: -10, Y: 480, Width: 30, Height: 60), 640, 512);
        Assert.Equal("0 0.015625 0.968750 0.031250 0.062500", line);
    }

    [Theory]
    [InlineData(700, 10, 20, 20)]   // fully outside on the right
    [InlineData(10, 10, 0.5, 20)]   // under 1 px wide
    public void Box_with_less_than_one_pixel_left_is_dropped(double x, double y, double w, double h) =>
        Assert.Null(YoloLabels.Line(new TrainingLabel(0, x, y, w, h), 640, 512));

    [Fact]
    public void Photo_without_objects_gets_an_empty_label_file() =>
        Assert.Equal(string.Empty, YoloLabels.File(Array.Empty<TrainingLabel>(), 640, 512));

    [Fact]
    public void Label_file_has_one_line_per_box_and_ends_with_a_newline()
    {
        var text = YoloLabels.File(new[] { new TrainingLabel(0, 64, 128, 32, 64), new TrainingLabel(0, 0, 0, 64, 64) }, 640, 512);
        Assert.Equal(2, text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.EndsWith("\n", text);
    }

    [Fact]
    public void Data_yaml_lists_folders_and_classes_without_a_path_line()
    {
        var yaml = YoloLabels.DataYaml(new[] { "pig" });
        Assert.Contains("train: images/train\nval: images/val\ntest: images/test\n", yaml);
        Assert.Contains("names:\n  0: pig\n", yaml);
        Assert.DoesNotContain("path:", yaml);
    }

    [Theory]
    [InlineData(TrainingSplit.Train, "train")]
    [InlineData(TrainingSplit.Validation, "val")]
    [InlineData(TrainingSplit.Test, "test")]
    public void Split_folders_use_yolo_names(TrainingSplit split, string folder) => Assert.Equal(folder, YoloLabels.FolderOf(split));

    [Theory]
    [InlineData("NACAS 140221_0600!", "NACAS_140221_0600")]
    [InlineData("  ", "session")]
    public void Session_names_become_safe_file_names(string name, string slug) => Assert.Equal(slug, TrainingDatasetExporter.Slug(name));
}
