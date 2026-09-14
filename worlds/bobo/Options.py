from typing import List, Dict, Any
from dataclasses import dataclass
from worlds.AutoWorld import PerGameCommonOptions
from Options import Choice, OptionGroup, Toggle, Range

# If youve ever gone to an options page and seen how sometimes options are grouped
# This is that
def create_option_groups() -> List[OptionGroup]:
    option_group_list: List[OptionGroup] = []
    for name, options in ap_skeleton_option_groups.items():
        option_group_list.append(OptionGroup(name=name, options=options))

    return option_group_list
class ExtraLocations(Toggle):
    """
    This will enable the extra locations option. Toggle is just true or false.
    """
    display_name = "Add Extra Locations"

class BoboTicketsRequired(Range):
    """
    How many Bobo Tickets are required to unlock the goal competition?
    """
    display_name = "Bobo Tickets Required"
    range_start = 1
    range_end = 10
    default = 3

class SnackMultiplier(Range):
    """
    Multiplier for snack stats
    1 = Normal (1x), 2 = 2x, etc.
    """
    display_name = "Snack Multiplier"
    range_start = 1
    range_end = 100
    default = 1

class UnlimitedSnacks(Toggle):
    """
    If enabled, the daily feeding limit will be removed,
    allowing you to feed your bobo an unlimited amount of snacks per day.
    """
    display_name = "Unlimited Snacks"

class CompetitionsPerUnlock(Range):
    """
    How many competitions unlock (per rank) each time a Progressive Competitions item is received.
    """
    display_name = "Competitions Per Unlock"
    range_start = 1
    range_end = 15
    default = 4

@dataclass
class APSkeletonOptions(PerGameCommonOptions):
    ExtraLocations:              ExtraLocations
    BoboTicketsRequired:         BoboTicketsRequired
    SnackMultiplier:            SnackMultiplier
    UnlimitedSnacks:            UnlimitedSnacks
    CompetitionsPerUnlock:        CompetitionsPerUnlock

# This is where you organize your options
# Its entirely up to you how you want to organize it
ap_skeleton_option_groups: Dict[str, List[Any]] = {
    "General Options": [BoboTicketsRequired, SnackMultiplier, UnlimitedSnacks, ExtraLocations, CompetitionsPerUnlock],
}