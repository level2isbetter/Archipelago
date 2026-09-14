# Look at init or Items.py for more information on imports
from typing import Dict, TYPE_CHECKING
import logging

from .Types import LocData

if TYPE_CHECKING:
    from . import APSkeletonWorld

# This is technique in programming to make things more readable for booleans
# A boolean is true or false
def did_include_extra_locations(world: "BoboWorld") -> bool:
    return bool(world.options.ExtraLocations)

# This is used by ap and in Items.py
# Theres a multitude of reasons to need to grab how many locations there are
def get_total_locations(world: "BoboWorld") -> int:
    # This is the total that we'll keep updating as we count how many locations there are
    total = 0
    for name in location_table:
        # If we did not turn on extra locations (see how readable it is with that thing from the top)
        # AND the name of it is found in our extra locations table, then that means we dont want to count it
        # So continue moves onto the next name in the table
        if not did_include_extra_locations(world) and name in extra_locations:
            continue

        # If the location is valid though, count it
        if is_valid_location(world, name):
            total += 1

    return total

def get_location_names() -> Dict[str, int]:
    names = {name: data.ap_code for name, data in location_table.items()}

    return names

# check valid location for extra locations
def is_valid_location(world: "APSkeletonWorld", name) -> bool:
    if not did_include_extra_locations(world) and name in extra_locations:
        return False
    
    return True

# Heres where you do the next fun part of listing out all those locations
# Its a lot
# My advice, zone out for half an hour listening to music and hope you wake up to a completed list
bobo_locations = {
    # E Rank Competitions
    "Baby's First Steps (Race, E)": LocData(20050100, "Competitions", "BabysFirstSteps_Race_E", "E"),
    "Let's Try Climbing (Race, E)": LocData(20050101, "Competitions", "LetsTryClimbing_Race_E", "E"),
    "I Can Climb That (Race, E)":   LocData(20050102, "Competitions", "ICanClimbThat_Race_E", "E"),
    "Up To The Moon (Race, E)":     LocData(20050103, "Competitions", "UpToTheMoon_Race_E", "E"),
    "The New Me (Race, E)":         LocData(20050104, "Competitions", "TheNewMe_Race_E", "E"),
    "Comet O'Clock (Race, E)":      LocData(20050105, "Competitions", "CometOClock_Race_E", "E"),
    "Swim Lessons (Race, E)":       LocData(20050106, "Competitions", "SwimLessons_Race_E", "E"),
    "Still Crawling (Race, E)":     LocData(20050107, "Competitions", "StillCrawling_Race_E", "E"),
    "Bigger Water Train (Race, E)": LocData(20050108, "Competitions", "BiggerWaterTrain_Race_E", "E"),
    "Little Water Train (Race, E)": LocData(20050109, "Competitions", "LittleWaterTrain_Race_E", "E"),
    "Qwench (Race, E)":             LocData(20050110, "Competitions", "Qwench_Race_E", "E"),
    "In The Drink (Brawl, E)":      LocData(20050111, "Competitions", "InTheDrink_Brawl_E", "E"),
    "Punch The Baby (Brawl, E)":    LocData(20050112, "Competitions", "PunchTheBaby_Brawl_E", "E"),

    # D Rank Competitions
    "Beam With A View (Race, D)":   LocData(20050113, "Competitions", "BeamWithAView_Race_D", "D"),
    "Walk Along High (Race, D)":    LocData(20050114, "Competitions", "WalkAlongHigh_Race_D", "D"),
    "Channel It (Race, D)":         LocData(20050115, "Competitions", "ChannelIt_Race_D", "D"),
    "Test The Jump (Race, D)":      LocData(20050116, "Competitions", "TestTheJump_Race_D", "D"),
    "Punch It (Race, D)":           LocData(20050117, "Competitions", "PunchIt_Race_D", "D"),
    "Splish Block (Race, D)":       LocData(20050118, "Competitions", "SplishBlock_Race_D", "D"),
    "Little Longer Now (Race, D)":  LocData(20050119, "Competitions", "LittleLongerNow_Race_D", "D"),
    "Under The Scramble (Brawl, D)": LocData(20050120, "Competitions", "UndertheScramble_Brawl_D", "D"),
    "Double Kee Laps (Race, D)":    LocData(20050121, "Competitions", "Double Kee Laps_Race_D", "D"),
    "Vertical Paddle (Race, D)":    LocData(20050122, "Competitions", "VerticalDoggyPaddle_Race_D", "D"),
    "Wavey Baby (Race, D)":         LocData(20050123, "Competitions", "WaveyBaby_Race_D", "D"),
    "Woovy (Race, D)":              LocData(20050124, "Competitions", "Woovy_Race_D", "D"),
    "Big Jam (Race, D)":            LocData(20050125, "Competitions", "BigJam_Race_D", "D"),
    "Beefy (Brawl, D)":             LocData(20050126, "Competitions", "Beefy_Brawl_D", "D"),
    "Channel It (Race, D)":         LocData(20050127, "Competitions", "ChannelIt_Race_D", "D"),
    "Pow! (Brawl, D)":              LocData(20050128, "Competitions", "Pow_Brawl_D", "D"),
    "Muckula (Race, D)":            LocData(20050129, "Competitions", "Muckula_Race_D", "D"),
    "Mini Match (Brawl, D)":        LocData(20050130, "Competitions", "MiniMatch_Brawl_D", "D"),
    "You Jump, I Climb (Race, D)":  LocData(20050131, "Competitions", "YouJumpIClimb_Race_D", "D"),
    "Crawfish Cookie (Brawl, D)":   LocData(20050132, "Competitions", "CrawfishCookie_Brawl_D", "D"),
    "Boy Alphard (Race, D)":        LocData(20050133, "Competitions", "BoyAlphard_Race_D", "D"),
    "Pumpkick (Brawl, D)":          LocData(20050134, "Competitions", "Pumpkick_Brawl_D", "D"),
    "Drippy (Brawl, D)":            LocData(20050135, "Competitions", "Drippy_Brawl_D", "D"),
    "Take Me Out (Brawl, D)":       LocData(20050136, "Competitions", "TakeMeOut_Brawl_D", "D"),
}

extra_locations = {}

event_locations = {
    "Beat Big Jam": LocData(None, "Competitions"),
}

location_table = {
    **bobo_locations,
    **extra_locations,
    **event_locations,
}