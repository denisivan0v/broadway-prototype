using Microsoft.AspNetCore.Mvc;
using NuClear.Broadway.Interfaces;
using Orleans;

namespace NuClear.Broadway.Host.Controllers
{
    [Route("api/campaigns")]
    public class CampaignsController : Controller
    {
        private readonly IClusterClient _clusterClient;

        public CampaignsController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }
        
        [HttpGet]
        public JsonResult Get(long id)
        {
            var campaignGrain = _clusterClient.GetGrain<ICampaignGrain>(1);
            return Json(campaignGrain.GetState());
        }
        
        [HttpPatch("{id}")]
        public void SetName(long id, [FromBody] string name)
        {
            var campaignGrain = _clusterClient.GetGrain<ICampaignGrain>(1);
            campaignGrain.ChangeName(name, 1);
        }
    }
}