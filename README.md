# XML to JSON Function App

Azure Functions isolated worker (.NET 9) that accepts an XML payload and returns a JSON array. Intended to be called from an Azure Logic App over HTTP.

## Assumptions

- The Logic App POSTs XML to this function (`Content-Type: application/xml`).
- A well-formed document uses a single root. Repeating children of that root (same element name) become JSON array items.
- A single entity (mixed child names) is returned as a one-element array.
- If the body is an XML **fragment** (multiple top-level elements, no single root), each top-level element becomes an array item.
- XML namespaces are ignored; JSON property names use element/attribute local names.
- Repeating nested siblings of the same name become nested JSON arrays.
- Leaf elements with attributes are objects with the attribute names plus `#text` for the element value.

## Endpoint

| | |
|---|---|
| Function | `ProcessXml` |
| Method | `POST` |
| Route | `/api/process-xml` |
| Auth | Function key (`?code=` or `x-functions-key`) |
| Request | XML body |
| Response | `200` JSON array, or `400` `{ "error": "..." }` |

## Example

Request body (`samples/orders.xml`):

```xml
<Orders>
  <Order id="1001">
    <Customer>Alice Smith</Customer>
    <Total currency="USD">125.50</Total>
    <Lines>
      <Line><Sku>WID-001</Sku><Quantity>2</Quantity></Line>
      <Line><Sku>GAD-014</Sku><Quantity>1</Quantity></Line>
    </Lines>
  </Order>
  <Order id="1002">
    <Customer>Bob Jones</Customer>
    <Total currency="EUR">89.00</Total>
    <Lines>
      <Line><Sku>WID-002</Sku><Quantity>4</Quantity></Line>
    </Lines>
  </Order>
</Orders>
```

Response:

```json
[
  {
    "id": "1001",
    "Customer": "Alice Smith",
    "Total": { "currency": "USD", "#text": "125.50" },
    "Lines": {
      "Line": [
        { "Sku": "WID-001", "Quantity": "2" },
        { "Sku": "GAD-014", "Quantity": "1" }
      ]
    }
  },
  {
    "id": "1002",
    "Customer": "Bob Jones",
    "Total": { "currency": "EUR", "#text": "89.00" },
    "Lines": {
      "Line": { "Sku": "WID-002", "Quantity": "4" }
    }
  }
]
```

## Logic App HTTP action

1. Add an **HTTP** action (or **Azure Functions** built-in connector).
2. Method: `POST`
3. URI: `https://func-xmlint-dev-processor-efczb3aeb3avajcs.southafricanorth-01.azurewebsites.net/api/process-xml?code=<function-key>`
4. Headers:
   - `Content-Type`: `application/xml`
5. Body: the XML (Compose, XML variable, or a sample such as `samples/orders.xml`).
6. Parse the function response as JSON if later steps need the array.

The function key is in Azure Portal → Function App → **Functions** → `ProcessXml` → **Function keys**, or:

```bash
az functionapp keys list --name <function-app-name> --resource-group <resource-group>
```

## Run locally

Prerequisites: [.NET 9 SDK](https://dotnet.microsoft.com/download), [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local), and Azurite (or another storage emulator).

```bash
cd FunctionApp
cp local.settings.json.example local.settings.json
func start
```

Then:

```bash
curl -X POST http://localhost:7071/api/process-xml \
  -H "Content-Type: application/xml" \
  --data-binary @../samples/orders.xml
```

Visual Studio / Cursor can also launch the `FunctionApp` profile (port 7101 in `Properties/launchSettings.json`).

## Tests

```bash
dotnet test FunctionApp.Tests/FunctionApp.Tests.csproj
```

## Deploy

Pushes to `main` (and manual **workflow_dispatch**) deploy through [`.github/workflows/main_func-xmlint-dev-processor.yml`](.github/workflows/main_func-xmlint-dev-processor.yml) to Function App **func-xmlint-dev-processor**.

| | |
|---|---|
| Function App | `func-xmlint-dev-processor` |
| Production URL | `https://func-xmlint-dev-processor-efczb3aeb3avajcs.southafricanorth-01.azurewebsites.net/api/process-xml` |

The publish profile is stored as a GitHub secret (`AZUREAPPSERVICE_PUBLISHPROFILE_...`). Azure Portal → Function App → **Deployment Center** can regenerate it if deploy auth fails.

Required Azure resources:

- Resource group
- Storage account (`AzureWebJobsStorage`)
- Function App **func-xmlint-dev-processor** (.NET 9 isolated, Functions v4)
- Application Insights (optional; wired if `APPLICATIONINSIGHTS_CONNECTION_STRING` is set)
