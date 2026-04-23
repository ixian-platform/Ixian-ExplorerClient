# Ixian-ExplorerClient

Ixian-ExplorerClient is a console-based software designed to expose a RESTful API. This API includes comprehensive functionality from the Ixian-Core local API with request forwarding to the Ixian Explorer API. This projects aims to provide a low overhead way of interacting with the Ixian blockchain without requiring a full node.

## Features

- **Local Ixian API**: Expose local functionalities of the Ixian-Core.
- **Proxy for Ixian Explorer API**: Forward requests to the official Ixian Explorer API.
- **Lightweight and Console-Based**: Designed for minimal system overhead and easy deployment.
- **RESTful API**: Provides a simple, HTTP-based interface for integration with other tools or services.

## Requirements

- Ixian Explorer API key
- .NET SDK (version 10.0 or higher)

## Usage

1. Clone the repos:
   ```sh
   git clone https://github.com/ProjectIxian/Ixian-Core.git
   git clone https://github.com/ProjectIxian/Ixian-ExplorerClient.git
   cd Ixian-ExplorerClient
   ```

2. Restore dependencies and build the project:
   ```sh
   dotnet restore
   dotnet build --configuration Release
   ```

3. Run the software:
   ```sh
   cd IxianExplorerClient/bin/Release/net10.0/
   ./IxianExplorerClient --apiKey YOURAPIKEY
   ```

### Accessing the API

Once the Ixian-ExplorerClient is running, the API will be available at `http://localhost:8001`. You can use tools like `curl`, Postman, or your preferred HTTP client to interact with it.

### API Documentation

- **Ixian Local API**: Refer to the [Ixian Local API Documentation](https://docs.ixian.io/api_docs.html) **Ixian Core Generic API Commands** section for details on available endpoints and their usage.
- **Ixian Explorer API**: The API adheres to the OpenAPI specification. You can find the OpenAPI YAML file in this repository at [`docs/explorer-api.yaml`](./docs/explorer-api.yaml).

Ixian-ExplorerClient automatically attaches the API KEY to all requests forwarded to the Explorer API.

## Usage

Below are some example use cases for the Ixian-ExplorerClient API. For more commands and optional parameters check the API Documentation section above.

### Get all addresses and balances for the currently loaded wallet

```sh
curl http://localhost:8001/mywallet
```

### Fetching address details such as amount, lastblock and txcount for a specific address

```sh
curl http://localhost:8001/addresses/16LUmwUnU9M4Wn92nrvCStj83LDCRwvAaSio6Xtb3yvqqqCCz
```

### Fetch recent transactions for a specific address

```sh
curl http://localhost:8001/addresses/16LUmwUnU9M4Wn92nrvCStj83LDCRwvAaSio6Xtb3yvqqqCCz/recent
```

## Configuration

Configuration is done via the command line by providing the following parameters:

- `--apiKey`: Required. Specifies the Explorer API key.
- `--apiUrl`: Optional. Specifies a custom Explorer API URL. Default is `https://explorer.ixian.io/api/v1`
- `-w`: Optional. Specifies a different wallet name when the software starts. Default is `ixian.wal`

The API Key and URL can also be provided via the .cfg file.
Alternatively, the API key can be hardcoded by modifying the `Meta/Config.cs` file and supplying the `explorerAPIKey` with the proper value.

## Get in touch / Contributing

If you feel like you can contribute to the project, or have questions or comments, you can get in touch with the team through Discord: https://discord.gg/pdJNVhv

## Pull requests

If you would like to send an improvement or bugfix to this repository, but without permanently joining the team, follow these approximate steps:

1. Fork this repository
2. Create a branch (preferably with a name that describes the change)
3. Create commits (the commit messages should contain some information on what and why was changed)
4. Create a pull request to this repository for review and inclusion.
