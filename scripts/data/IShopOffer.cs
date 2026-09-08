// Common shape for anything the shop can offer — lets ShopManager roll
// upgrades and charms from one unified pool without type-specific logic.
public interface IShopOffer
{
	string Id { get; }
	string Name { get; }
	string Description { get; }
	int BaseCost { get; }
}
