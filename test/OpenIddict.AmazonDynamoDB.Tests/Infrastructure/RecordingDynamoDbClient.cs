using System.Collections.Concurrent;
using Amazon.DynamoDBv2;
using Amazon.Runtime;

namespace OpenIddict.AmazonDynamoDB.Tests;

// A client for DynamoDB Local that records the requests that are sent and lets tests change
// responses to simulate DynamoDB behaviour that DynamoDB Local doesn't have
public class RecordingDynamoDbClient : AmazonDynamoDBClient
{
  public ConcurrentQueue<AmazonWebServiceRequest> Requests { get; } = new();

  public Action<AmazonWebServiceResponse>? OnResponse { get; set; }

  public RecordingDynamoDbClient()
    : base(new BasicAWSCredentials("test", "test"), new AmazonDynamoDBConfig
    {
      ServiceURL = "http://localhost:8000",
    })
  {
    BeforeRequestEvent += (_, args) =>
    {
      if (args is WebServiceRequestEventArgs requestArgs)
      {
        Requests.Enqueue(requestArgs.Request);
      }
    };
    AfterResponseEvent += (_, args) =>
    {
      if (args is WebServiceResponseEventArgs responseArgs)
      {
        OnResponse?.Invoke(responseArgs.Response);
      }
    };
  }
}
