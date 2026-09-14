# So the goal here is to have a catalog of all the items in your game
# To correctly generate a games items they need to be bundled in a list
# A list in programming terms is anything in square brackets [] to put it simply

# When a list is described its described as a list of x where x is the type of variable within it
# IE: ["apple", "pear", "grape"] is a list of strings (anything inside "" OR '' are considered strings)

# Logging = output. How you'll figure out whats going wrong
import logging

# Built in AP imports
from BaseClasses import Item, ItemClassification

# These come from the other files in this example. If you want to see the source ctrl + click the name
# You can also do that ctrl + click for any functions to see what they do
from .Types import ItemData, ChapterType, APSkeletonItem, chapter_type_to_name
from .Locations import get_total_locations
from typing import List, Dict, TYPE_CHECKING
from .CompetitionUnlocks import get_competition_unlock_order

# This is just making sure nothing gets confused dw about what its doing exactly
if TYPE_CHECKING:
    from . import APSkeletonWorld

# If you're curious about the -> List[Item] that is a syntax to make sure you return the correct variable type
# In this instance we're saying we only want to return a list of items
# You'll see a bunch of other examples of this in other functions
# It's main purpose is to protect yourself from yourself
def create_itempool(world: "APSkeletonWorld") -> List[Item]:
    itempool: List[Item] = []

    # Add all unique items (excluding Victory, which is placed on Beat Big Jam)
    for name, data in bobo_items.items():
        if name not in ("Victory", "Bobo Ticket", "Progressive Competitions"):
            itempool.append(create_item(world, name))
    
    # bobo ticket logic stuff
    ticket_count = world.options.BoboTicketsRequired.value
    itempool += create_multiple_items(world, "Bobo Ticket", ticket_count, ItemClassification.progression)

    # progressive competitions logic
    thresholds = get_competition_unlock_order(world)
    max_batch = max(thresholds.values()) if thresholds else 0
    itempool += create_multiple_items(world, "Progressive Competitions", max_batch, ItemClassification.progression)

    # Place victory at the final location
    victory = create_item(world, "Victory")
    world.multiworld.get_location("Beat Big Jam", world.player).place_locked_item(victory)

    # Fill remainder of locations with junk
    needed_junk = get_total_locations(world) - len(itempool) - 1
    if needed_junk > 0:
        itempool += create_junk_items(world, needed_junk)

    return itempool

# This is a generic function to create a singular item
def create_item(world: "APSkeletonWorld", name: str) -> Item:
    data = item_table[name]
    return APSkeletonItem(name, data.classification, data.ap_code, world.player)

# Another generic function. For creating a bunch of items at once!
def create_multiple_items(world: "APSkeletonWorld", name: str, count: int,
                          item_type: ItemClassification = ItemClassification.progression) -> List[Item]:
    data = item_table[name]
    itemlist: List[Item] = []

    for i in range(count):
        itemlist += [APSkeletonItem(name, item_type, data.ap_code, world.player)]

    return itemlist

# Finally, where junk items are created
def create_junk_items(world: "APSkeletonWorld", count: int) -> List[Item]:
    junk_pool: List[Item] = []
    junk_names = list(junk_weights.keys())
    weights = list(junk_weights.values())

    for _ in range(count):
        chosen = world.random.choices(junk_names, weights=weights, k=1)[0]
        junk_pool.append(world.create_item(chosen))

    return junk_pool

# Items from Bobo Bay
bobo_items = {
    # Snacks & Consumables
    "Leftover Pizza":        ItemData(20050001, ItemClassification.filler),
    "Palmwelon":             ItemData(20050002, ItemClassification.filler),
    "Gumball":               ItemData(20050003, ItemClassification.filler),
    "Goldenana":             ItemData(20050004, ItemClassification.filler),
    "Bunny Cracker":         ItemData(20050005, ItemClassification.useful),
    "Skyberry":              ItemData(20050006, ItemClassification.filler),
    "Crackthrust Hype":      ItemData(20050007, ItemClassification.useful),

    # Accessories & Wearables
    "Business Glasses":      ItemData(20050008, ItemClassification.useful),
    "Hot Top Medal":         ItemData(20050009, ItemClassification.useful),
    "Sassy Sunglasses":      ItemData(20050010, ItemClassification.useful),
    "Sneakers":              ItemData(20050011, ItemClassification.useful),
    "Gear Star Medal":       ItemData(20050012, ItemClassification.useful),
    "Cool Helmet":           ItemData(20050013, ItemClassification.useful),
    "Dash Classic Medal":    ItemData(20050014, ItemClassification.useful),
    "Aviators":              ItemData(20050015, ItemClassification.useful),
    "Knit Hat":              ItemData(20050016, ItemClassification.useful),
    "Top Hat":               ItemData(20050017, ItemClassification.useful),
    
    # Toys
    "Bunny Stuffed Animal":  ItemData(20050018, ItemClassification.useful),

    # Progression
    "D-Rank License":        ItemData(20050050, ItemClassification.progression),
    "Progressive Competitions": ItemData(20050051, ItemClassification.progression),
    "Bobo Ticket":           ItemData(20050000, ItemClassification.progression),


    # Goal
    "Victory":               ItemData(20050099, ItemClassification.progression),
}

# Items used to fill empty slots
junk_items = {
    "150 money": ItemData(20050020, ItemClassification.filler, 0),
    "Gumball (Junk)":        ItemData(20050021, ItemClassification.filler, 0),
    "Skyberry (Junk)":       ItemData(20050022, ItemClassification.filler, 0),
}

junk_weights = {
    "150 money": 50,
    "Gumball (Junk)":        10,
    "Skyberry (Junk)":       40,
}

item_table = {
    **bobo_items,
    **junk_items,
}