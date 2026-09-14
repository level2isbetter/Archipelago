from typing import Dict, List, TYPE_CHECKING
from .Locations import location_table

if TYPE_CHECKING:
    from . import APSkeletonWorld

GOAL_ASSET_NAMES = {"BigJam_Race_D"}
RANK_ORDER = ["E", "D"]  # add more ranks here later, in the order they should unlock

def build_competition_unlock_order(world: "APSkeletonWorld") -> Dict[str, int]:
    batch_size = world.options.CompetitionsPerUnlock.value

    thresholds: Dict[str, int] = {}
    batch_offset = 0
    for rank in RANK_ORDER:
        names = [
            data.asset_name for data in location_table.values()
            if data.rank == rank and data.asset_name and data.asset_name not in GOAL_ASSET_NAMES
        ]
        for i, name in enumerate(names):
            thresholds[name] = batch_offset + (i // batch_size)

        if names:
            batch_offset += ((len(names) - 1) // batch_size) + 1

    return thresholds


def get_competition_unlock_order(world: "APSkeletonWorld") -> Dict[str, int]:
    if not hasattr(world, "_competition_unlock_cache"):
        world._competition_unlock_cache = build_competition_unlock_order(world)
    return world._competition_unlock_cache