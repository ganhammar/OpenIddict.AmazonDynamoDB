namespace OpenIddict.AmazonDynamoDB;

public class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
{
  public T Current => throw new InvalidOperationException();

  public ValueTask<bool> MoveNextAsync()
  {
    return new ValueTask<bool>(false);
  }

  public ValueTask DisposeAsync()
  {
    return default;
  }
}
