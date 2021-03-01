using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace FormRecognizerFace.WebApp.Models
{
    public class HomeViewModel
    {
        public int CompanyId { get; set; }
        public List<SelectListItem> Companies { get; set; }
    }
}
