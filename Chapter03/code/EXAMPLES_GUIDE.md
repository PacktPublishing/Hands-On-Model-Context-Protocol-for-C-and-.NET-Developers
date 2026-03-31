# Chapter 3 - Running Examples Guide

## ✅ All Examples Are Now Working .cs Files

Chapter 3 now has **4 different ways** to run the MCP server:

## 🚀 Running Options

### 1. Main Stdio Mode (Default)
```powershell
dotnet run
```
**Uses**: `Program.cs` → `RunStdioServerAsync()`  
**Tool Class**: `FlightTools`

### 2. Main HTTP Mode
```powershell
dotnet run -- --mode http
```
**Uses**: `Program.cs` → `RunHttpServerAsync()`  
**Tool Class**: `FlightTools`  
**Endpoint**: `http://localhost:5001/mcp`

### 3. Example Stdio Mode (Section 3.1)
```powershell
dotnet run -- --example stdio
```
**Uses**: `ch03_1_flights_server_stdio.cs` → `StdioServerExample.RunAsync()`  
**Tool Class**: `FlightToolsStdio`  
**Purpose**: Shows minimal stdio configuration from the chapter

### 4. Example HTTP Mode (Section 3.3)
```powershell
dotnet run -- --example http
```
**Uses**: `ch03_2_flights_server_http.cs` → `HttpServerExample.RunAsync()`  
**Tool Class**: `FlightToolsHttp`  
**Endpoint**: `http://localhost:5001/mcp`  
**Purpose**: Shows HTTP configuration for Docker scenarios

## 📁 File Structure

```
Chapter03/code/
├── Program.cs                              ✅ Main entry point (dual-mode)
├── FlightTools.cs                          ✅ Shared tool (transport-agnostic)
├── Shared.cs                               ✅ Domain models + mock service
│
├── ch03_1_flights_server_stdio.cs          ✅ Example: Stdio (Section 3.1)
├── ch03_2_flights_server_http.cs           ✅ Example: HTTP (Section 3.3)
│
├── ch03_1_flights_server_stdio.cs.example  📝 Original reference
└── ch03_2_flights_server_http.cs.example   📝 Original reference
```

## 🎯 Key Differences

### Main vs Example

| Aspect | Main (Program.cs) | Examples (ch03_*.cs) |
|--------|-------------------|----------------------|
| **Purpose** | Production-ready dual-mode | Educational chapter examples |
| **Tool Class** | Shared `FlightTools` | Separate `FlightTools{Stdio/Http}` |
| **Configuration** | Command-line arg switching | Self-contained example code |
| **Usage** | General development | Learning specific patterns |

### Tool Classes

All three tool classes (`FlightTools`, `FlightToolsStdio`, `FlightToolsHttp`) have **identical** implementations. They demonstrate that:
- ✅ Tools are transport-agnostic
- ✅ Same logic works with stdio and HTTP
- ✅ Only server configuration differs

## 🔍 Testing Each Mode

### Test Main Stdio
```powershell
# Terminal 1
cd HandsOnMCPCSharp\Chapter03\code
dotnet run

# Terminal 2  
npx @modelcontextprotocol/inspector dotnet run --project HandsOnMCPCSharp/Chapter03/code
```

### Test Main HTTP
```powershell
# Terminal 1
dotnet run -- --mode http

# Terminal 2
npx @modelcontextprotocol/inspector
# Then connect to: http://localhost:5001/mcp
```

### Test Example Stdio
```powershell
# Terminal 1
dotnet run -- --example stdio

# Terminal 2
npx @modelcontextprotocol/inspector dotnet run --project HandsOnMCPCSharp/Chapter03/code -- --example stdio
```

### Test Example HTTP
```powershell
# Terminal 1
dotnet run -- --example http

# Terminal 2
npx @modelcontextprotocol/inspector
# Then connect to: http://localhost:5001/mcp
```

## 🎓 Educational Value

The examples (`ch03_*.cs`) show:

1. **Section 3.1 (Stdio)**: 
   - Minimal stdio configuration
   - Why logs go to stderr
   - `Host.CreateApplicationBuilder` pattern

2. **Section 3.3 (HTTP)**:
   - HTTP transport setup
   - `WebApplication.CreateBuilder` pattern
   - `MapMcp("/mcp")` endpoint registration
   - Docker-ready configuration

## 💡 When to Use Each

**Use Main Modes** (`--mode`):
- ✅ Regular development work
- ✅ Building actual applications
- ✅ When you need flexibility

**Use Example Modes** (`--example`):
- ✅ Following chapter tutorials
- ✅ Learning specific patterns
- ✅ Understanding section-specific concepts
- ✅ Comparing with book examples

## ✅ Verification

All modes compile and run successfully:

```powershell
# Build
dotnet build   # ✅ Success

# Test all modes
dotnet run                      # ✅ Stdio (main)
dotnet run -- --mode http       # ✅ HTTP (main)
dotnet run -- --example stdio   # ✅ Stdio (example)
dotnet run -- --example http    # ✅ HTTP (example)
```

## 📝 Summary

**Before**: Examples were `.example` files (not compiled)  
**After**: All examples are working `.cs` files that compile alongside main code

**Result**: 
- ✅ 4 different runnable configurations
- ✅ All demonstrate the same capabilities
- ✅ Clear separation between main and educational examples
- ✅ Transport-agnostic tool design proven

---

**Status**: ✅ All Chapter 3 examples working  
**Build**: ✅ Successful  
**Run**: ✅ All 4 modes tested
