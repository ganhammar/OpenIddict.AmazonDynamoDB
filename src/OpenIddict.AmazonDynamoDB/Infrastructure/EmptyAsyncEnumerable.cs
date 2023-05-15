namespace OpenIddict.AmazonDynamoDB;

public class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
{
  public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
  {
    return new EmptyAsyncEnumerator<T>();
  }
}
