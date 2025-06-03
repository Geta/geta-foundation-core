using Geta.EPi.Commerce.UI.Facets.Models;
using Geta.EPi.Commerce.UI.Facets.Models.FormModels;

namespace Foundation.Features.Search
{
    public class FilterOptionModel : IFilterOptionFormModel
    {
        public int FacetSize { get; set; }
        public bool FacetAllTerms { get; set; }
        public IList<FacetOptionGroup> Facets { get; set; } = new List<FacetOptionGroup>();
    }
}