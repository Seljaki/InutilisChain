using Microsoft.Extensions.DependencyInjection;

namespace InutilisChain;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

public class RESTServer
{
    private BlockchainServer blockChainServer;
    private const int PORT = 3336;

    public RESTServer(BlockchainServer blockChainServer)
    {
        this.blockChainServer = blockChainServer;
    }
    
    public void Start()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(blockChainServer);
        builder.Services.AddControllers();
        var app = builder.Build();

        app.UseRouting();
        app.MapControllers();

        app.Run($"http://localhost:{PORT}");
    }
}

[ApiController]
[Route("blockchain")]
public class BlockchainController : ControllerBase
{
    private readonly BlockchainServer _blockChain;

    public BlockchainController(BlockchainServer blockChain)
    {
        _blockChain = blockChain;
    }

    [HttpGet("blocks")]
    public IActionResult GetBlocks()
    {
        return Ok(_blockChain.blockChain.getBlockChain());
    }
    
    [HttpGet("lastBlock")]
    public IActionResult GetLastBlock()
    {
        return Ok(_blockChain.blockChain.getLastBlock());
    }

    [HttpPost("addData")]
    public IActionResult AddBlock([FromBody] Data blockData)
    {
        Console.WriteLine(blockData);
        _blockChain.onNewData(blockData);
        return Ok(new { Message = "Block added successfully." });
    }
}