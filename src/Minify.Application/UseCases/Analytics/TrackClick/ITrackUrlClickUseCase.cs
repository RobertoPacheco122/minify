namespace Minify.Application.UseCases.Analytics.TrackClick;

public interface ITrackUrlClickUseCase
{
    Task Execute(TrackUrlClickInput input, CancellationToken cancellationToken = default);
}
