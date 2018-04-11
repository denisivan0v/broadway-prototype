using Microsoft.AspNetCore.Mvc;
using NuClear.Broadway.Interfaces;
using Orleans;

namespace NuClear.Broadway.Host.Controllers
{
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
        public JsonResult Get(long id)
        {
            var campaignGrain = _clusterClient.GetGrain<ICampaignGrain>(id);
            return Json(campaignGrain.GetState());
        }
        
        [HttpPatch("{id}")]
        public void SetName(long id, [FromBody] string name)
        {
            var campaignGrain = _clusterClient.GetGrain<ICampaignGrain>(id);
            campaignGrain.ChangeName(name, 1);
        }
    }
}