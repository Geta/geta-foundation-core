using Geta.EPi.Commerce.UI.Facets.Attributes;
using Geta.EPi.Commerce.UI.Facets.Models;
using System.Globalization;
using ICategorizable = EPiServer.Commerce.Catalog.ContentTypes.ICategorizable;

namespace Foundation.Features.CatalogContent
{
    [UseAsFacet(DisplayName = "Searchable catalog content")]
    public interface ISearchableProduct : IContentFacet, IContent, ICategorizable, IAssetContainer
    {
        string DisplayName { get; set; }

        string Code { get; set; }

        CultureInfo Language { get; }

        int CatalogId { get; }

        DateTime Created { get; }

        //Add more properties to filter on here
        [UseAsFacetItem]
        string Size { get; set; }

        [UseAsFacetItem]
        string Color { get; set; }

        [UseAsFacetItem]
        string Brand { get; set; }

        [UseAsFacetItem]
        decimal FakePrice { get; }
    }
}
