from BaseClasses import Tutorial
from worlds.AutoWorld import World, WebWorld
from .Items import item_table, create_itempool, create_item
from .Locations import location_table, get_location_names
from .Regions import create_regions
from .Rules import set_rules
from .Options import APSkeletonOptions, create_option_groups
from .CompetitionUnlocks import get_competition_unlock_order


class BoboWeb(WebWorld):
    theme = "partyTime"
    option_groups = create_option_groups()


class BoboBayWorld(World):
    """
    Bobo Bay is a cute Bobo-raising simulator!
    Raise, train, and compete with your Bobos across various tournaments.
    """

    game = "Bobo Bay"
    options_dataclass = APSkeletonOptions
    options: APSkeletonOptions

    item_name_to_id = {name: data.ap_code for name, data in item_table.items() if data.ap_code is not None}
    location_name_to_id = get_location_names()

    web = BoboWeb()

    def create_regions(self) -> None:
        create_regions(self)

    def set_rules(self) -> None:
        set_rules(self)

    def create_items(self) -> None:
        self.multiworld.itempool += create_itempool(self)

    def create_item(self, name: str):
        return create_item(self, name)

    def fill_slot_data(self) -> dict:
        return {
            "bobo_tickets_req": self.options.BoboTicketsRequired.value,
            "competition_unlock_thresholds": get_competition_unlock_order(self),"snack_multiplier": getattr(self.options, "SnackMultiplier", 1).value
            if hasattr(self.options, "SnackMultiplier") else 1,
            "unlimited_snacks": bool(getattr(self.options, "UnlimitedSnacks", False)),
        }


# Alias for any skeleton type hints expecting APSkeletonWorld
APSkeletonWorld = BoboBayWorld