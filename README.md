# XML to JSON Function App

.NET 9 Azure Function that accepts XML and returns a JSON array. A Logic App POSTs the XML to this function.

**Endpoint:** `POST /api/process-xml` (function key required)

Repeating child elements become array items. Sample payload: `samples/orders.xml`.

```xml
<Orders>
  <Order id="1001"><Customer>Motheo Malope</Customer></Order>
  <Order id="1002"><Customer>Tshepang Sefako</Customer></Order>
</Orders>
```

```json
[
  { "id": "1001", "Customer": "Motheo Malope" },
  { "id": "1002", "Customer": "Tshepang Sefako" }
]
```

## Azure

- Function App: `func-xmlint-dev-processor`
- URL: `https://func-xmlint-dev-processor-efczb3aeb3avajcs.southafricanorth-01.azurewebsites.net/api/process-xml?code=<function-key>`
- Logic App HTTP action: `POST`, header `Content-Type: application/xml`, body = XML
- Deploy: push to `main` (GitHub Action)

## Local

```bash
cd FunctionApp
cp local.settings.json.example local.settings.json
func start

curl -X POST http://localhost:7071/api/process-xml \
  -H "Content-Type: application/xml" \
  --data-binary @../samples/orders.xml
```

```bash
dotnet test FunctionApp.Tests/FunctionApp.Tests.csproj
```

Postman collection: `postman/XmlToJson.postman_collection.json`.
