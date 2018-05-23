using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using NuClear.Broadway.Host.Commands;
using NuClear.Broadway.Interfaces.Grains;

using Orleans;

namespace NuClear.Broadway.Host.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/{api-version:apiVersion}/campaigns")]
    public class CampaignsController : Controller
    {
        private readonly IClusterClient _clusterClient;

        public CampaignsController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        [HttpGet("{id}")]
        public async Task<JsonResult> Get(long id)
        {
            var campaignGrain = _clusterClient.GetGrain<ICampaignGrain>(id);
            return Json(await campaignGrain.GetStateAsync());
        }

        [HttpGet("{id}/events")]
        public async Task<JsonResult> GetEvents(long id)
        {
            var campaignGrain = _clusterClient.GetGrain<ICampaignGrain>(id);
            return Json(await campaignGrain.GetConfirmedEvents());
        }

        [HttpPut("{id}/commands")]
        public async Task SetName(long id, [FromBody]ChangeCompaignNameCommand command)
        {
            var campaignGrain = _clusterClient.GetGrain<ICampaignGrain>(id);
            await campaignGrain.ChangeNameAsync(command.Name, command.IssuedBy);
        }
    }
}