# SwarmTests

This project contains both **unit tests** and **integration tests** for the SwarmUI.ApiClient library.

## Test Types

### Unit Tests
Unit tests verify the internal logic of the client library using test doubles (mocks/fakes). They:
- ✅ Run **fast** (milliseconds)
- ✅ **No external dependencies** required
- ✅ Test logic in isolation
- ✅ Always available in CI/CD

**Location:** Root of test project (e.g., `SwarmClientTests.cs`, `Endpoints/GenerationEndpointTests.cs`)

### Integration Tests
Integration tests verify the client library against a **real SwarmUI instance**. They:
- ⚠️ Require SwarmUI running at `http://192.168.0.163:7801`
- ⚠️ Require valid authorization token configured in [SwarmIntegrationTestBase.cs](IntegrationTests/SwarmIntegrationTestBase.cs)
- ⚠️ Run **slower** (seconds/minutes - actual image generation)
- ✅ Validate **real API responses**
- ✅ Test **end-to-end functionality**

**Location:** `IntegrationTests/` folder

**Configuration:** Update `BaseUrl` and `Authorization` constants in [SwarmIntegrationTestBase.cs:18-19](IntegrationTests/SwarmIntegrationTestBase.cs#L18-L19) to point to your SwarmUI server.

## Running Tests

### Run All Tests (Unit + Integration)
```powershell
dotnet test
```

### Run Only Unit Tests
```powershell
dotnet test --filter "Category!=Integration"
```

### Run Only Integration Tests
```powershell
dotnet test --filter "Category=Integration"
```

### Run Tests in Visual Studio Test Explorer
- **All Tests**: Click "Run All Tests"
- **Unit Tests Only**: Right-click solution → Filter by "Category != Integration"
- **Integration Tests Only**: Right-click solution → Filter by "Category = Integration"

## Prerequisites for Integration Tests

1. **Ensure SwarmUI Server is Running**
   - SwarmUI must be running at: `http://192.168.0.163:7801`
   - Server must be accessible from your test machine
   - Valid authorization token must be configured

2. **Configure Test Server Settings**
   - Open: [SwarmIntegrationTestBase.cs](IntegrationTests/SwarmIntegrationTestBase.cs)
   - Update `BaseUrl` constant (line 18) if your server is at a different address
   - Update `Authorization` constant (line 19) with your API token

3. **Verify SwarmUI is Accessible**
   - Open browser: http://192.168.0.163:7801
   - Ensure you can access the SwarmUI interface
   - You should see the SwarmUI web interface

4. **Run Integration Tests**
   ```powershell
   cd Tests
   dotnet test --filter "Category=Integration"
   ```

## Integration Test Coverage

### GenerationIntegrationTests
- ✅ WebSocket streaming with real image generation
- ✅ Single and batch image generation
- ✅ LoRA model application
- ✅ Negative prompts
- ✅ Cancellation support
- ✅ Error handling

### ModelsIntegrationTests
- ✅ List available models
- ✅ Model metadata validation
- ✅ Response deserialization

### BackendsIntegrationTests
- ✅ List backends
- ✅ Get backend types
- ✅ Backend metadata validation

### UserIntegrationTests
- ✅ Get user settings
- ✅ Create new sessions
- ✅ Settings metadata validation

## What Gets Tested?

### Unit Tests Verify:
- Request validation logic
- Data transformations (e.g., `BatchSize` → `batchsize`)
- Message parsing
- Error handling logic
- Null checks and edge cases

### Integration Tests Verify:
- Real API connectivity
- Request/response models match actual API
- WebSocket streaming works end-to-end
- Image generation produces valid base64 data
- Session management
- Cancellation propagates correctly

## Troubleshooting

### Integration Tests Fail with "Failed to connect to SwarmUI"
- ✅ Ensure SwarmUI is running at http://localhost:7801
- ✅ Check SwarmUI logs for errors
- ✅ Verify no firewall blocking localhost:7801

### Integration Tests Timeout
- ✅ Image generation tests can take 10-60+ seconds depending on model/steps
- ✅ Check SwarmUI has models downloaded
- ✅ Ensure GPU/backend is functioning

### "Model not found" Errors
- ✅ Update test model names to match your SwarmUI installation
- ✅ Check available models: http://localhost:7801
- ✅ Download required models in SwarmUI settings

## CI/CD Recommendations

For CI/CD pipelines:
- **Always run unit tests** (fast, no dependencies)
- **Optionally run integration tests** if you can spin up SwarmUI in CI
- Consider tagging integration tests with `[Trait("Category", "IntegrationSlow")]` for long-running tests

## Adding New Tests

### Adding a Unit Test
1. Create test class in appropriate folder
2. Use fake/mock implementations of interfaces
3. Test logic, not infrastructure

### Adding an Integration Test
1. Create test class in `IntegrationTests/` folder
2. Inherit from `SwarmIntegrationTestBase`
3. Add `[Trait("Category", "Integration")]` to class
4. Use the real `Client` instance provided by base class
5. Test against real API responses

Example:
```csharp
[Trait("Category", "Integration")]
public class MyIntegrationTests : SwarmIntegrationTestBase
{
    [Fact]
    public async Task MyTest()
    {
        var result = await Client.MyEndpoint.MyMethodAsync(...);
        Assert.NotNull(result);
    }
}
```
