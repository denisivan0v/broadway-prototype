﻿using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NuClear.Broadway.DataProjection;

namespace NuClear.Broadway.Host.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/{api-version:apiVersion}")]
    public class DiscoveryController : Controller
    {
        private readonly DataProjectionContext _dbContext;

        public DiscoveryController(DataProjectionContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("cards")]
        public async Task<IActionResult> ListCards()
        {
            var cards = await _dbContext.Cards.AsNoTracking().Take(100).ToListAsync();
            return Json(cards);
        }

        [HttpGet("firms")]
        public async Task<IActionResult> ListFirms()
        {
            var cards = await _dbContext.Firms.AsNoTracking().Take(100).ToListAsync();
            return Json(cards);
        }

        [HttpGet("rubrics")]
        public async Task<IActionResult> ListRubrics()
        {
            var cards = await _dbContext.Rubrics.Where(x => !x.IsDeleted).Include(x => x.Localizations).AsNoTracking().Take(100).ToListAsync();
            return Json(cards);
        }
    }
}