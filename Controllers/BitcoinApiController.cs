using BitcoinTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace BitcoinTracker.Controllers
{
    [Route("api")]
    [ApiController]
    public class BitcoinApiController : ControllerBase
    {
        private readonly IBitcoinService _bitcoinService;

        public BitcoinApiController(IBitcoinService bitcoinService)
        {
            _bitcoinService = bitcoinService ?? throw new ArgumentNullException(nameof(bitcoinService));
        }

        [HttpGet("live")]
        public async Task<IActionResult> GetLiveRate()
        {
            var (eur, czk) = await _bitcoinService.GetCurrentRatesAsync();
            if (eur.HasValue)
            {
                return Ok(new { price_eur = eur, price_czk = czk });
            }
            return StatusCode(503, new { error = "Could not fetch data" });
        }



        [HttpPost("live")]
        public async Task<IActionResult> SaveLiveRate()
        {
            var (eur, czk) = await _bitcoinService.GetCurrentRatesAsync();
            if (eur.HasValue && czk.HasValue)
            {
                var saved = await _bitcoinService.SaveRateAsync(eur.Value, czk.Value);
                return CreatedAtAction(nameof(GetSavedRates), new { id = saved.Id }, saved);
            }
            return StatusCode(503, new { error = "Could not fetch data" });
        }

        [HttpGet("rates")]
        public async Task<IActionResult> GetSavedRates()
        {
            var rates = await _bitcoinService.GetSavedRatesAsync();
            return Ok(rates);
        }


        [HttpPatch("rates/{id}")]
        public async Task<IActionResult> UpdateNote(int id, [FromBody] UpdateNoteRequest noteObj)
        {
            try
            {
                await _bitcoinService.UpdateNoteAsync(id, noteObj.Note);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("rates/{id}")]
        public async Task<IActionResult> DeleteRate(int id)
        {
            await _bitcoinService.DeleteRatesAsync(new List<int> { id });
            return NoContent();
        }


        [HttpDelete("rates/batch")]
        public async Task<IActionResult> DeleteRatesBatch([FromBody] DeleteRatesRequest request)
        {
            if (request?.Ids == null || request.Ids.Count == 0)
            {
                return BadRequest(new { error = "No IDs provided" });
            }

            await _bitcoinService.DeleteRatesAsync(request.Ids);
            return NoContent();
        }
    }

    public record UpdateNoteRequest(string Note);

    public record DeleteRatesRequest(List<int> Ids);
}
