using Microsoft.ML;
using Microsoft.ML.Data;

var outputPath = args.Length > 0
    ? args[0]
    : Path.Combine("src", "HubPay.WebApi", "Models", "antifraud.onnx");

var ml = new MLContext(seed: 42);
var samples = Enumerable.Range(0, 5000).Select(i => new FraudRow
{
    F1 = Random.Shared.NextSingle(),
    F2 = Random.Shared.NextSingle(),
    F3 = Random.Shared.NextSingle() * 0.3f,
    F4 = Random.Shared.NextSingle() * 0.2f,
    Label = Math.Clamp(Random.Shared.NextSingle() * 0.5f + Random.Shared.NextSingle() * 0.5f, 0f, 1f)
}).ToList();

var data = ml.Data.LoadFromEnumerable(samples);
var pipeline = ml.Transforms.Concatenate("Features", nameof(FraudRow.F1), nameof(FraudRow.F2), nameof(FraudRow.F3), nameof(FraudRow.F4))
    .Append(ml.Regression.Trainers.Sdca(labelColumnName: nameof(FraudRow.Label), featureColumnName: "Features"));

var model = pipeline.Fit(data);

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
using var stream = File.Create(outputPath);
ml.Model.ConvertToOnnx(model, data, stream);

Console.WriteLine($"ONNX exportado: {Path.GetFullPath(outputPath)}");

internal sealed class FraudRow
{
    public float F1 { get; set; }
    public float F2 { get; set; }
    public float F3 { get; set; }
    public float F4 { get; set; }
    public float Label { get; set; }
}
