namespace PhotoScore.DotNet;

public sealed class ScoringService
{
    private readonly string _modelPath;
    private readonly string _requestedDevice;

    public ScoringService(string modelPath, string requestedDevice = "auto")
    {
        _modelPath = modelPath;
        _requestedDevice = requestedDevice;
    }

    public ScoreResult ScoreImage(string imagePath)
    {
        using var scorer = new AestheticScorer(_modelPath, _requestedDevice);
        return scorer.Score(imagePath);
    }

    public ScoreBatchResult ScorePaths(string inputPath, bool recursive = true)
    {
        var imagePaths = ImagePathEnumerator.Enumerate(inputPath, recursive);
        using var scorer = new AestheticScorer(_modelPath, _requestedDevice);

        var results = new List<ScoreResult>();
        var errors = new List<ScoreError>();
        for (var index = 0; index < imagePaths.Count; index++)
        {
            var imagePath = imagePaths[index];
            Console.Error.WriteLine($"[{index + 1}/{imagePaths.Count}] {imagePath}");
            try
            {
                results.Add(scorer.Score(imagePath));
            }
            catch (Exception ex)
            {
                errors.Add(new ScoreError(imagePath, ex.Message));
            }
        }

        var fullInputPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(inputPath));
        return new ScoreBatchResult(
            fullInputPath,
            scorer.ModelPath,
            results.Count,
            errors.Count,
            results,
            errors);
    }
}
