using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class NachaController : Controller
    {
        private readonly INachaService _nachaService;

        public NachaController(INachaService nachaService)
        {
            _nachaService = nachaService;
        }
        /// <summary>
        /// Endpoint de la API ACH Interbank.
        /// </summary>

        [HttpPost("header")]
        public async Task<IActionResult> SaveHeader([FromBody] NachaHeader header)
        {
            await _nachaService.SaveHeaderAsync(header);
            return Ok();
        }
    }
}
