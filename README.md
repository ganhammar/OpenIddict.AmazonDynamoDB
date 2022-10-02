# OpenIddict.AmazonDynamoDB

![Build Status](https://github.com/ganhammar/OpenIddict.AmazonDynamoDB/actions/workflows/ci.yml/badge.svg) [![codecov](https://codecov.io/gh/ganhammar/OpenIddict.AmazonDynamoDB/branch/main/graph/badge.svg?token=S4M1VCX8J6)](https://codecov.io/gh/ganhammar/OpenIddict.AmazonDynamoDB) [![NuGet](https://img.shields.io/nuget/v/OpenIddict.AmazonDynamoDB)](https://www.nuget.org/packages/OpenIddict.AmazonDynamoDB)

## Tests

In order to run the tests, you need to have DynamoDB running locally on `localhost:8000`. This can easily be done using [Docker](https://www.docker.com/) and the following command:

```
docker run -p 8000:8000 amazon/dynamodb-local
```